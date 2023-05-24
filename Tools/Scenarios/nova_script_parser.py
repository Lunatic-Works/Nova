# TODO: proper way to split chapters

import os
import re

import clr
from luaparser import ast, astnodes

nova_parser_dll_path = "../../Library/ScriptAssemblies/Nova.Parser.dll"
clr.AddReference(os.path.abspath(nova_parser_dll_path))


def is_start(head_eager_code):
    return any(x in head_eager_code for x in ["is_start", "is_unlocked_start"])


def parse_nodes(text):
    from Nova.Parser import NodeParser

    return NodeParser.ParseNodes(text)


# DEPRECATED
def parse_chapters(f):
    nodes = parse_nodes(f.read())
    return [
        (
            node.name,
            [
                (
                    "\n".join([block.content for block in entry.codeBlocks]),
                    entry.characterName,
                    entry.dialogue,
                    entry.line,
                )
                for entry in node.dialogueEntries
            ],
            node.headEagerBlock.content,
            node.tailEagerBlock.content,
        )
        for node in nodes
    ]


def format_attrs(attrs):
    if not attrs:
        return ""
    s = ", ".join(f"{k} = {v}" for k, v in sorted(attrs.items()))
    s = f"[{s}]"
    return s


def format_code_block(block):
    from Nova.Parser import BlockType

    at = "@" if block.type == BlockType.EagerExecution else ""
    attrs = format_attrs(block.attributes)
    s = f"{at}{attrs}<|{block.content}|>"
    return s


def get_node_name(node):
    if isinstance(node, astnodes.Name):
        return node.id
    elif isinstance(node, astnodes.String):
        return node.s
    elif isinstance(node, astnodes.Index):
        return f"{get_node_name(node.value)}.{get_node_name(node.idx)}"
    else:
        # raise ValueError(f'Unknown node: {type(node)}')
        return None


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
                if func_name == "action" and not isinstance(
                    args[0], astnodes.AnonymousFunction
                ):
                    yield get_node_name(args[0]), args[1:], env
                else:
                    yield func_name, args, env

                for _node in args:
                    if isinstance(_node, astnodes.AnonymousFunction):
                        yield from walk_functions_block(
                            _node.body.body, env + (source,)
                        )
        elif isinstance(node, astnodes.Assign):
            nodes.extend(reversed(node.values))
        elif isinstance(
            node,
            (
                astnodes.Index,
                astnodes.While,
                astnodes.If,
                astnodes.Return,
                astnodes.Fornum,
                astnodes.Forin,
                astnodes.Function,
                astnodes.LocalFunction,
                astnodes.Nil,
                astnodes.TrueExpr,
                astnodes.FalseExpr,
                astnodes.Number,
                astnodes.String,
                astnodes.Table,
                astnodes.AnonymousFunction,
                astnodes.BinaryOp,
                astnodes.UnaryOp,
            ),
        ):
            pass
        else:
            raise ValueError(f"Unknown node: {type(node)}")


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
        return "expr"
    else:
        raise ValueError(f"Unknown node: {type(node)}")


def normalize_dialogue(
    s, remove_rich=True, keep_rich=None, remove_todo=True, keep_todo=None
):
    if not s:
        return s

    if remove_rich:

        def func(m):
            if keep_rich and m.group(1) in keep_rich:
                return m.group(0)
            else:
                return m.group(3)

        while True:
            s_new = re.compile(r"<(.*?)(=.*?)?>(.*?)</\1>", re.DOTALL).sub(func, s)
            if s_new == s:
                break
            s = s_new

    if remove_todo:

        def func(m):
            if keep_todo and m.group(2) in keep_todo:
                return m.group(0)
            else:
                return ""

        s = re.compile(r"\r?\n?（TODO：((.*?)：)?.*?）", re.DOTALL).sub(func, s)

    s = re.compile(" +").sub(" ", s)

    return s


def test_roundtrip():
    in_filename = "scenario.txt"

    with open(in_filename, "r", encoding="utf-8") as f:
        nodes = parse_nodes(f.read())

    for node_count, node in enumerate(nodes):
        if node_count > 0:
            print()

        print(format_code_block(node.headEagerBlock))
        for entry_count, entry in enumerate(node.dialogueEntries):
            if entry_count > 0:
                print()

            for block in entry.codeBlocks:
                print(format_code_block(block))

            chara_name = entry.characterName
            disp_name = entry.displayName
            dialogue = entry.dialogue

            if chara_name:
                if disp_name == chara_name:
                    print(f"{chara_name}：：{dialogue}")
                else:
                    print(f"{disp_name}//{chara_name}：：{dialogue}")
            else:
                print(dialogue)
        print(format_code_block(node.tailEagerBlock))


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


if __name__ == "__main__":
    test_lua()
