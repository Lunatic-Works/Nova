#!/usr/bin/env python3

from nova_script_parser import parse_chapters

in_filename = "scenario.txt"
out_filename = "scenario_branches.txt"
label_only = True


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f, code_attribute="v")

    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        for chapter_name, entries, head_eager_code, tail_eager_code in chapters:
            print(chapter_name)

            if label_only:
                for line in head_eager_code.splitlines():
                    if line.lstrip().startswith("label"):
                        break
                f.write(line.strip() + "\n")
            else:
                f.write(head_eager_code.strip() + "\n")
            for code, _, _, _ in entries:
                if code:
                    f.write(code.strip() + "\n")
            f.write(tail_eager_code.strip() + "\n\n")


if __name__ == "__main__":
    from utils import run_merge

    run_merge()
    main()
