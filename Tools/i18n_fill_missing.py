#!/usr/bin/env python3
"""Fill missing .resx keys across locales from a translations JSON map.

Input JSON shape:
{
  "de": { "Key": "German text", ... },
  "fr": { ... }
}

For any English key present in Resource.resx but missing in Resource.<lang>.resx,
insert the translated value (or English fallback if translation not provided).
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

MODULES = {
    "WPF": "UniversalDeviceToolkit.WPF/Resources",
    "Lib": "UniversalDeviceToolkit.Lib/Resources",
    "Automation": "UniversalDeviceToolkit.Lib.Automation/Resources",
    "Macro": "UniversalDeviceToolkit.Lib.Macro/Resources",
}


def load_map(path: str) -> dict[str, str]:
    if not os.path.exists(path):
        return {}
    root = ET.parse(path).getroot()
    out: dict[str, str] = {}
    for data in root.findall("data"):
        name = data.get("name")
        val = data.find("value")
        if name is not None and val is not None:
            out[name] = val.text or ""
    return out


def escape_xml(text: str) -> str:
    return (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
    )


def append_entries(lang_path: str, entries: list[tuple[str, str]]) -> int:
    if not entries:
        return 0
    with open(lang_path, "r", encoding="utf-8") as f:
        content = f.read()
    # Insert before closing </root>
    idx = content.rfind("</root>")
    if idx < 0:
        raise RuntimeError(f"No </root> in {lang_path}")
    block = []
    for name, value in entries:
        block.append(f'  <data name="{name}" xml:space="preserve">\n')
        block.append(f"    <value>{escape_xml(value)}</value>\n")
        block.append("  </data>\n")
    new_content = content[:idx] + "".join(block) + content[idx:]
    with open(lang_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(new_content)
    return len(entries)


def list_lang_files(module_dir: str) -> list[str]:
    langs = []
    for name in os.listdir(module_dir):
        m = re.match(r"^Resource\.(.+)\.resx$", name, re.I)
        if not m:
            continue
        culture = m.group(1)
        if culture.lower() in {"en", "designer"}:
            continue
        langs.append(culture)
    return sorted(langs)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--module", choices=list(MODULES), default="WPF")
    ap.add_argument("--translations", required=True, help="JSON map culture -> {key: text}")
    ap.add_argument("--fallback-english", action="store_true", help="Use English when translation missing")
    ap.add_argument("--cultures", default="", help="Comma-separated cultures (default: all satellites)")
    args = ap.parse_args()

    module_dir = os.path.join(REPO, MODULES[args.module])
    en_path = os.path.join(module_dir, "Resource.resx")
    en = load_map(en_path)
    with open(args.translations, "r", encoding="utf-8") as f:
        translations = json.load(f)

    cultures = [c.strip() for c in args.cultures.split(",") if c.strip()] or list_lang_files(module_dir)
    total = 0
    for culture in cultures:
        lang_path = os.path.join(module_dir, f"Resource.{culture}.resx")
        if not os.path.exists(lang_path):
            print(f"SKIP missing file {lang_path}", file=sys.stderr)
            continue
        existing = load_map(lang_path)
        culture_map = translations.get(culture) or translations.get(culture.lower()) or {}
        to_add: list[tuple[str, str]] = []
        for key, en_val in en.items():
            if key in existing:
                continue
            if key in culture_map and culture_map[key]:
                to_add.append((key, culture_map[key]))
            elif args.fallback_english:
                to_add.append((key, en_val))
        n = append_entries(lang_path, to_add)
        total += n
        print(f"{args.module}/{culture}: added {n}")
    print(f"TOTAL added: {total}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
