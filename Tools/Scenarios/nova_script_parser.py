# TODO: proper way to split chapters

import re

from luaparser import ast, astnodes


# now_entries may be None
# now_code and now_dialogue may be ''
def commit_eager_code(chapters, now_chapter_name, now_entries,
                      now_head_eager_code, now_eager_code, now_code,
                      now_dialogue, keep_line_num, line_num):
    if now_head_eager_code is None:
        match = re.compile(r'label[ \(]\'(.*?)\'').search(now_eager_code)
        if not match:
            raise ValueError(
                f'label() not found in head eager code:\n{now_eager_code}')
        now_chapter_name = match.group(1)
        now_entries = []
        now_head_eager_code = now_eager_code
    else:
        now_code, now_dialogue = commit_dialogue(now_entries, now_code,
                                                 now_dialogue, keep_line_num,
                                                 line_num)
        chapters.append((now_chapter_name, now_entries, now_head_eager_code,
                         now_eager_code))
        now_chapter_name = None
        now_entries = None
        now_head_eager_code = None
    return (now_chapter_name, now_entries, now_head_eager_code, '', now_code,
            now_dialogue)


# Assume now_entries is not None
# now_code and now_dialogue may be ''
def commit_dialogue(now_entries, now_code, now_dialogue, keep_line_num,
                    line_num):
    if now_dialogue:
        match = re.compile('(.*?)(：：|::)(.*?)',
                           re.DOTALL).fullmatch(now_dialogue)
        if match:
            chara_name = match.group(1)
            dialogue = match.group(3)
        else:
            chara_name = None
            dialogue = now_dialogue
    else:
        chara_name = None
        dialogue = None

    if now_code or now_dialogue:
        if keep_line_num:
            now_entries.append((now_code, chara_name, dialogue, line_num))
        else:
            now_entries.append((now_code, chara_name, dialogue))

    return '', ''


# Return a list of chapters
# chapter: (chapter_name, list of entries, head_eager_code, tail_eager_code)
# entry: (code, chara_name, dialogue)
# If keep_line_num is True, entry: (code, chara_name, dialogue, line_num)
def parse_chapters(lines, keep_line_num=False):
    STATE_TEXT = 0
    STATE_EAGER_CODE = 1
    STATE_LAZY_CODE = 2

    state = STATE_TEXT
    chapters = []

    # Between a tail eager code block and a head eager code block,
    # now_chapter_name, now_entries and now_head_eager_code are None
    now_chapter_name = None
    now_entries = None
    now_head_eager_code = None

    now_eager_code = ''
    now_code = ''
    now_dialogue = ''

    for line_num, line in enumerate(lines):
        line = line.rstrip()

        if state == STATE_TEXT:
            if line.startswith('@<|'):
                if line.endswith('|>'):
                    now_eager_code = line[3:-2].strip()
                    (now_chapter_name, now_entries, now_head_eager_code,
                     now_eager_code,
                     now_code, now_dialogue) = commit_eager_code(
                         chapters, now_chapter_name, now_entries,
                         now_head_eager_code, now_eager_code, now_code,
                         now_dialogue, keep_line_num, line_num)
                else:
                    now_eager_code = line[3:].lstrip()
                    state = STATE_EAGER_CODE
            elif line.startswith('<|'):
                if line.endswith('|>'):
                    now_code = line[3:-2].strip()
                else:
                    now_code = line[3:].lstrip()
                    state = STATE_LAZY_CODE
            elif line:
                if now_dialogue:
                    now_dialogue += '\n'
                now_dialogue += line
            else:    # Empty line
                if now_entries is not None:
                    now_code, now_dialogue = commit_dialogue(
                        now_entries, now_code, now_dialogue, keep_line_num,
                        line_num)

        elif state == STATE_EAGER_CODE:
            if line.endswith('|>'):
                line = line[:-2].rstrip()
                if line:
                    if now_eager_code:
                        now_eager_code += '\n'
                    now_eager_code += line
                (now_chapter_name, now_entries, now_head_eager_code,
                 now_eager_code, now_code, now_dialogue) = commit_eager_code(
                     chapters, now_chapter_name, now_entries,
                     now_head_eager_code, now_eager_code, now_code,
                     now_dialogue, keep_line_num, line_num)
                state = STATE_TEXT
            else:
                if now_eager_code:
                    now_eager_code += '\n'
                now_eager_code += line

        elif state == STATE_LAZY_CODE:
            if line.endswith('|>'):
                line = line[:-2].rstrip()
                if line:
                    if now_code:
                        now_code += '\n'
                    now_code += line
                state = STATE_TEXT
            else:
                if now_code:
                    now_code += '\n'
                now_code += line

        else:
            raise ValueError(f'Unknown state: {state}')

    return chapters


def get_node_name(node):
    if isinstance(node, astnodes.Name):
        return node.id
    elif isinstance(node, astnodes.String):
        return node.s
    elif isinstance(node, astnodes.Index):
        return f'{get_node_name(node.value)}.{get_node_name(node.idx)}'
    else:
        # raise ValueError(f'Unknown node: {type(node)}')
        return None


def isinstance_any(obj, classes):
    return any(isinstance(obj, c) for c in classes)


def walk_functions_block(nodes, env):
    nodes = list(reversed(nodes))
    invoke_stack = []
    while nodes:
        node = nodes.pop()
        if isinstance(node, astnodes.Call):
            yield get_node_name(node.func), node.args, env
            for _node in node.args:
                if isinstance(_node, astnodes.AnonymousFunction):
                    yield from walk_functions_block(_node.body.body, env)
        elif isinstance(node, astnodes.Invoke):
            while isinstance(node, astnodes.Invoke):
                invoke_stack.append(node)
                node = node.source

            source = get_node_name(node)
            yield source, [], env

            while invoke_stack:
                node = invoke_stack.pop()
                func_name = get_node_name(node.func)
                args = node.args
                if (func_name == 'action' and
                        not isinstance(args[0], astnodes.AnonymousFunction)):
                    yield get_node_name(args[0]), args[1:], env
                else:
                    yield func_name, args, env

                for _node in args:
                    if isinstance(_node, astnodes.AnonymousFunction):
                        yield from walk_functions_block(
                            _node.body.body, env + (source, ))
        elif isinstance(node, astnodes.Assign):
            nodes.extend(reversed(node.values))
        elif isinstance_any(node, [
                astnodes.Index, astnodes.While, astnodes.If, astnodes.Return,
                astnodes.Fornum, astnodes.Forin, astnodes.Function,
                astnodes.LocalFunction, astnodes.Nil, astnodes.TrueExpr,
                astnodes.FalseExpr, astnodes.Number, astnodes.String,
                astnodes.Table, astnodes.AnonymousFunction, astnodes.BinaryOp,
                astnodes.UnaryOp
        ]):
            pass
        else:
            raise ValueError(f'Unknown node: {type(node)}')


def walk_functions(code):
    tree = ast.parse(code)
    try:
        yield from walk_functions_block(tree.body.body, ())
    except Exception as e:
        print(e)
        print(code)


def parse_table(node):
    if isinstance(node, astnodes.Table):
        return tuple(parse_table(x.value) for x in node.fields)
    elif isinstance(node, astnodes.Number):
        return node.n
    elif isinstance(node, astnodes.Name):
        return node.id
    elif isinstance(node, astnodes.String):
        return node.s
    elif isinstance(node, astnodes.Nil):
        return None
    elif isinstance(node, astnodes.UMinusOp):
        return -parse_table(node.operand)
    elif isinstance(node, (astnodes.UnaryOp, astnodes.BinaryOp)):
        return 'expr'
    else:
        raise ValueError(f'Unknown node: {type(node)}')


def normalize_dialogue(s,
                       remove_rich=True,
                       keep_rich=None,
                       remove_todo=True,
                       keep_todo=None):
    if remove_rich:

        def func(m):
            if keep_rich and m.group(1) in keep_rich:
                return m.group(0)
            else:
                return m.group(3)

        while True:
            s_new = re.compile(r'<(.*?)(=.*?)?>(.*?)</\1>',
                               re.DOTALL).sub(func, s)
            if s_new == s:
                break
            s = s_new

    if remove_todo:

        def func(m):
            if keep_todo and m.group(2) in keep_todo:
                return m.group(0)
            else:
                return ''

        s = re.compile(r'\r?\n?（TODO：((.*?)：)?.*?）', re.DOTALL).sub(func, s)

    s = re.compile(' +').sub(' ', s)

    return s


def test():
    in_filename = 'scenario.txt'

    with open(in_filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f)

    for chapter_name, entries in chapters:
        print('chapter_name:', chapter_name)
        for code, chara_name, dialogue in entries:
            if code:
                print(f'begin code\n{code}\nend code')
            print(chara_name, dialogue)


def test_lua():
    code = """
f1()
a1:a2(t1):a3(t2, t3)
a4:f2(function()
        a5:f3(t4)
    end)
x = f6()
"""
    tree = ast.parse(code)
    print(ast.to_pretty_str(tree))
    for x in walk_functions(code):
        print(x)


if __name__ == '__main__':
    test_lua()
