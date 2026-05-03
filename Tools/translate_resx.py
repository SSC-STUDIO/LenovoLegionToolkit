#!/usr/bin/env python3
"""Extract untranslated (English-identical) entries from .resx files and apply translations.

Usage:
  # Extract untranslated strings as JSON
  python3 Tools/translate_resx.py extract --lang no --module WPF > /tmp/no_wpf.json

  # Apply translations from a JSON file
  python3 Tools/translate_resx.py apply --lang no --module WPF /tmp/no_wpf_translated.json
"""
import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODULES = {
    "WPF": "LenovoLegionToolkit.WPF/Resources",
    "Lib": "LenovoLegionToolkit.Lib/Resources",
    "Automation": "LenovoLegionToolkit.Lib.Automation/Resources",
    "Macro": "LenovoLegionToolkit.Lib.Macro/Resources",
}

NS = "http://schemas.microsoft.com/winfx/2006/xaml"


def parse_resx(path):
    """Parse a .resx file and return {name: (value_element, value_text)} preserving order."""
    tree = ET.parse(path)
    root = tree.getroot()
    entries = {}
    for data in root.findall("data"):
        name = data.get("name")
        val_elem = data.find("value")
        if val_elem is not None:
            entries[name] = (val_elem, val_elem.text or "")
    return entries, tree


def extract(lang, module):
    """Return list of {name, english, translated} where translated == english (untranslated)."""
    res_dir = os.path.join(REPO, MODULES[module])
    en_path = os.path.join(res_dir, "Resource.en.resx")
    lang_path = os.path.join(res_dir, f"Resource.{lang}.resx")

    if not os.path.exists(en_path):
        en_path = os.path.join(res_dir, "Resource.resx")  # neutral = English
    if not os.path.exists(lang_path):
        print(f"ERROR: {lang_path} not found", file=sys.stderr)
        sys.exit(1)

    en_entries, _ = parse_resx(en_path)
    lang_entries, _ = parse_resx(lang_path)

    untranslated = []
    for name, (_, en_val) in en_entries.items():
        if name in lang_entries:
            _, lang_val = lang_entries[name]
            if lang_val == en_val:
                untranslated.append({"name": name, "english": en_val})
    return untranslated


def apply(lang, module, translations_file):
    """Apply translations from JSON file to the target .resx."""
    res_dir = os.path.join(REPO, MODULES[module])
    lang_path = os.path.join(res_dir, f"Resource.{lang}.resx")

    with open(translations_file, "r", encoding="utf-8") as f:
        translations = json.load(f)

    trans_map = {t["name"]: t["translated"] for t in translations if "translated" in t}

    entries, tree = parse_resx(lang_path)
    root = tree.getroot()
    changed = 0

    for data in root.findall("data"):
        name = data.get("name")
        if name in trans_map:
            val_elem = data.find("value")
            if val_elem is not None:
                old = val_elem.text or ""
                new = trans_map[name]
                if old != new:
                    val_elem.text = new
                    changed += 1

    # Preserve XML declaration and formatting
    tree.write(lang_path, xml_declaration=True, encoding="utf-8-sig")
    print(f"Applied {changed} translations to {lang_path}", file=sys.stderr)


def main():
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="cmd")

    ext = sub.add_parser("extract")
    ext.add_argument("--lang", required=True)
    ext.add_argument("--module", required=True, choices=MODULES.keys())

    app = sub.add_parser("apply")
    app.add_argument("--lang", required=True)
    app.add_argument("--module", required=True, choices=MODULES.keys())
    app.add_argument("file", help="JSON translations file")

    args = parser.parse_args()
    if args.cmd == "extract":
        result = extract(args.lang, args.module)
        json.dump(result, sys.stdout, ensure_ascii=False, indent=2)
    elif args.cmd == "apply":
        apply(args.lang, args.module, args.file)


if __name__ == "__main__":
    main()
