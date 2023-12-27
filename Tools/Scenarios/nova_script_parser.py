# TODO: proper way to split chapters

import os
import re

import clr

nova_parser_dll_path = "../../Library/ScriptAssemblies/Nova.Parser.dll"
clr.AddReference(os.path.abspath(nova_parser_dll_path))


def is_chapter(head_eager_code):
    return any(
        x in head_eager_code for x in ["is_chapter", "is_start", "is_unlocked_start"]
    )


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


def format_name_dialogue(chara_name, disp_name, dialogue):
    if chara_name:
        if disp_name == chara_name:
            return f"{chara_name}：：{dialogue}"
        else:
            return f"{disp_name}//{chara_name}：：{dialogue}"
    else:
        return dialogue


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
            s_new = re.compile(r"<([^=>]*)(=[^>]*)?>(.*?)</\1>", re.DOTALL).sub(func, s)
            if s_new == s:
                break
            s = s_new

    if remove_todo:

        def func(m):
            if keep_todo and m.group(1)[:-1] in keep_todo:
                return m.group(0)
            else:
                return ""

        s = re.compile(r"\r?\n?（TODO：([^：]*：)?([^（）]*（[^）]*）)*[^）]*）", re.DOTALL).sub(
            func, s
        )

    s = re.compile(" +").sub(" ", s)
    s = s.strip()

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

            print(
                format_name_dialogue(
                    entry.characterName, entry.displayName, entry.dialogue
                )
            )
        print(format_code_block(node.tailEagerBlock))


if __name__ == "__main__":
    test_roundtrip()
