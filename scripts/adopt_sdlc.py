#!/usr/bin/env python3
"""Safely preview/copy a canonical Agentic SDLC checkout into GameFrameWork.

Run from GameFrameWork root with a separately checked-out source repository:
  python scripts/adopt_sdlc.py /path/to/agentic-sdlc-framework
  python scripts/adopt_sdlc.py /path/to/agentic-sdlc-framework --write

Does not overwrite AGENTS.md, CLAUDE.md or existing world/GameArts skills.
"""
import argparse
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[1]
BUNDLE = ROOT / ".agentic-framework"
SKILLS = ("sdlc-orch", "sdlc-orchestrator")
HOSTS = (".agents", ".claude")


def plan(source: Path):
    source = source.resolve()
    if not source.is_dir() or not (source / "core" / "ENGINEERING_CONSTITUTION.md").is_file():
        raise ValueError("Expected a complete canonical agentic-sdlc-framework checkout.")
    if source == ROOT or ROOT.is_relative_to(source) or source.is_relative_to(ROOT):
        raise ValueError("Use a separate checkout, outside GameFrameWork.")
    if BUNDLE.exists() or BUNDLE.is_symlink():
        raise ValueError("Existing .agentic-framework must be reviewed before replacing.")
    if not (source / "framework.json").is_file():
        raise ValueError("Canonical framework.json missing.")
    paths = []
    for f in source.rglob("*"):
        if not f.is_file() or f.is_symlink() or "__pycache__" in f.parts or f.suffix == ".pyc":
            continue
        if ".git" in f.relative_to(source).parts:
            continue
        dst = BUNDLE / f.relative_to(source)
        if dst.exists() or dst.is_symlink():
            raise ValueError(f"Collision: {dst}")
        paths.append((f, dst))
    for host in HOSTS:
        for skill in SKILLS:
            src = source / host / "skills" / skill / "SKILL.md"
            if not src.exists():
                continue
            dest = ROOT / host / "skills" / skill / "SKILL.md"
            if dest.exists() or dest.is_symlink():
                raise ValueError(f"Skill collision: {dest}")
            paths.append((src, dest))
    return paths


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("source", type=Path)
    p.add_argument("--write", action="store_true")
    args = p.parse_args()
    try:
        paths = plan(args.source)
        for src, dest in paths:
            print(dest.relative_to(ROOT))
        print(f"{len(paths)} files; " + ("installing" if args.write else "PREVIEW ONLY"))
        if args.write:
            for src, dest in paths:
                dest.parent.mkdir(parents=True, exist_ok=True)
                with dest.open("xb") as out:
                    if dest.relative_to(ROOT).parts[0] in HOSTS and dest.relative_to(ROOT).parts[1] == "skills":
                        body = src.read_text(encoding="utf-8").replace("Read `skills/", "Read `.agentic-framework/skills/").replace("The framework root is `.`.", "The framework root is `.agentic-framework/`.")
                        out.write(body.encode("utf-8"))
                    else:
                        with src.open("rb") as inp:
                            shutil.copyfileobj(inp, out)
            print("Installed. Review .agentic-framework and update project routing instructions.")
        return 0
    except (ValueError, OSError) as e:
        print(f"ERROR: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
