from antlr4 import ParserRuleContext
from speedy_antlr_lua_parser import LuaParser, parse_chunk


class NIL:
    def __repr__(self):
        return "nil"


def is_nil(x):
    return isinstance(x, NIL)


# Value type is never tuple
class LazyList:
    def __init__(self, data):
        self.data = data

    def __getitem__(self, key):
        value = self.data[key]
        if isinstance(value, tuple):
            f, x = value
            value = f(x)
            self.data[key] = value
        return value


def get_prefix_exp_content(node):
    function_call = node.functioncall()
    if function_call or node.OP():
        name = None
    else:
        name = str(node.NAME(0))
    return name, function_call


def get_function_name(node):
    name, function_call = get_prefix_exp_content(node)
    if node.COL():
        after_name = str(node.NAME()[-1])
        if not name:
            return after_name
        else:
            return name, after_name
    else:
        if not name:
            if function_call:
                name = get_function_name(function_call)
            else:
                # name is exp
                name = "?"
        return name


def get_number_content(node):
    n = node.INT() or node.FLOAT()
    if n:
        return float(str(n))
    else:
        return "?"


def get_string_content(node):
    s = node.NORMALSTRING() or node.CHARSTRING()
    if s:
        return str(s)[1:-1]
    else:
        return "?"


# Duplicated keys will cause undefined behavior
# List is converted to 0-based
def get_table_content(node):
    fields = node.fieldlist()
    if not fields:
        return []

    out_list = []
    out_dict = {}

    for field in fields.field():
        name = field.NAME()
        if name:
            k = str(name)
            v = field.exp(0)
            v = get_exp_content(v)
            out_dict[k] = v
        elif field.OB():
            k, v = field.exp()
            k = get_exp_content(k)
            v = get_exp_content(v)
            out_dict[k] = v
        else:
            v = field.exp(0)
            v = get_exp_content(v)
            out_list.append(v)

    if not out_dict:
        return out_list
    else:
        for i, x in enumerate(out_list):
            out_dict[i + 1] = x
        return out_dict


def get_exp_content(node):
    # Handle prefixexp and string first because they are frequent
    content = node.prefixexp()
    if content:
        name, _ = get_prefix_exp_content(content)
        if not name:
            name = "?"
        return name

    content = node.string()
    if content:
        return get_string_content(content)

    content = node.NIL()
    if content:
        return NIL()

    # content = node.FALSE()
    # if content:
    #     return False
    #
    # content = node.TRUE()
    # if content:
    #     return True

    content = node.number()
    if content:
        return get_number_content(content)

    content = node.tableconstructor()
    if content:
        return get_table_content(content)

    children = node.children
    if (
        len(children) == 2
        and str(children[0]) == "-"
        and isinstance(children[1], LuaParser.ExpContext)
    ):
        content = children[1].number()
        if content:
            n = get_number_content(content)
            if n == "?":
                return n
            else:
                return -n

    return "?"


def get_function_args(node):
    exps = node.explist()
    if exps:
        return tuple(get_exp_content(x) for x in exps.exp())

    table = node.tableconstructor()
    if table:
        return (get_table_content(table),)

    string = node.string()
    if string:
        return (get_string_content(string),)

    return ()


def get_function_args_lazy(node):
    exps = node.explist()
    if exps:
        return LazyList([(get_exp_content, x) for x in exps.exp()])

    table = node.tableconstructor()
    if table:
        return LazyList([(get_table_content, table)])

    string = node.string()
    if string:
        return LazyList([(get_string_content, string)])

    return LazyList([])


def walk_functions_node(node, env, lazy_args):
    if isinstance(node, LuaParser.FunctioncallContext):
        for child in node.children[:-1]:
            if isinstance(child, ParserRuleContext):
                yield from walk_functions_node(child, env, lazy_args)

        name = get_function_name(node)
        if isinstance(name, tuple):
            before_name, name = name
            yield before_name, (), env
        else:
            before_name = None

        if lazy_args:
            args = get_function_args_lazy(node.args())
        else:
            args = get_function_args(node.args())
        yield name, args, env

        if before_name:
            env.append(before_name)

        yield from walk_functions_node(node.children[-1], env, lazy_args)

    elif isinstance(node, LuaParser.StatContext):
        last_env = list(env)

        for child in node.children:
            if isinstance(child, ParserRuleContext):
                yield from walk_functions_node(child, env, lazy_args)

        env.clear()
        env += last_env

    else:
        for child in node.children:
            if isinstance(child, ParserRuleContext):
                yield from walk_functions_node(child, env, lazy_args)


def walk_functions(code, *, lazy_args=False):
    try:
        chunk = parse_chunk(code)
        for stat in chunk.block().stat():
            yield from walk_functions_node(stat, [], lazy_args)
    except Exception as e:
        print(e)
        print(code)
        raise


def test():
    code = """
f1()
a2:f2(x2):f3(x3, y3, 's', '', 1, -1, true, nil, {}, {1, 2, 3}, { k = 'v', 4, 5 })
a4:f4(function()
        a5:f5(x5)
        b5:g5(function()
            c5(y5)
        end)
    end
    ):f6(function()
        a7(x7):f7(y7)
    end)
x8 = f8()
"""
    for x in walk_functions(code):
        print(x)


if __name__ == "__main__":
    test()
