#!/usr/bin/env python3
"""Regenerate resources/device-packs.json from the built-in C# catalog.

Single source of truth is
UniversalDeviceToolkit.Lib/DeviceSupport/LenovoDeviceSupportProvider.cs
(see Docs/DEVICE_PROVIDERS.md). Run this after any catalog edit:

    python Scripts/gen-device-packs.py

The output mirrors System.Text.Json formatting used by the original
packdump tool: 2-space indent, CRLF, uppercase \\uXXXX escapes, no
trailing newline.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CATALOG_CS = ROOT / "UniversalDeviceToolkit.Lib" / "DeviceSupport" / "LenovoDeviceSupportProvider.cs"
OUTPUT_JSON = ROOT / "resources" / "device-packs.json"

GENERIC_BASIC_PACK_ID = "generic-pc-basic"


def strip_line_comments(text: str) -> str:
    """Remove // comments while preserving string literals."""
    out: list[str] = []
    i, n = 0, len(text)
    in_string = False
    while i < n:
        ch = text[i]
        if in_string:
            out.append(ch)
            if ch == "\\" and i + 1 < n:
                out.append(text[i + 1])
                i += 2
                continue
            if ch == '"':
                in_string = False
            i += 1
            continue
        if ch == '"':
            in_string = True
            out.append(ch)
            i += 1
            continue
        if ch == "/" and i + 1 < n and text[i + 1] == "/":
            while i < n and text[i] != "\n":
                i += 1
            continue
        out.append(ch)
        i += 1
    return "".join(out)


def parse_named_arrays(source: str) -> dict[str, list[str]]:
    """Parse `private static readonly string[] Name = [ ... ];` declarations."""
    arrays: dict[str, list[str]] = {}
    for match in re.finditer(
        r"private static readonly string\[\]\s+(\w+)\s*=\s*\[(.*?)\];",
        source,
        re.DOTALL,
    ):
        arrays[match.group(1)] = re.findall(r'"((?:[^"\\]|\\.)*)"', match.group(2))
    return arrays


def extract_device_packs_block(source: str) -> str:
    start = source.index("DevicePacks =")
    start = source.index("[", start) + 1
    depth = 1
    i = start
    while depth > 0:
        ch = source[i]
        if ch == '"':
            i += 1
            while source[i] != '"':
                i += 2 if source[i] == "\\" else 1
            i += 1
            continue
        if ch == "[":
            depth += 1
        elif ch == "]":
            depth -= 1
        i += 1
    return source[start : i - 1]


def split_top_level_calls(block: str) -> list[str]:
    """Split the DevicePacks initializer into individual pack expressions."""
    calls: list[str] = []
    depth = 0
    current: list[str] = []
    i, n = 0, len(block)
    while i < n:
        ch = block[i]
        if ch == '"':
            current.append(ch)
            i += 1
            while block[i] != '"':
                if block[i] == "\\":
                    current.append(block[i : i + 2])
                    i += 2
                else:
                    current.append(block[i])
                    i += 1
            current.append('"')
            i += 1
            continue
        if ch in "([{":
            depth += 1
        elif ch in ")]}":
            depth -= 1
        if ch == "," and depth == 0:
            calls.append("".join(current).strip())
            current = []
            i += 1
            continue
        current.append(ch)
        i += 1
    tail = "".join(current).strip()
    if tail:
        calls.append(tail)
    return calls


def parse_arguments(arg_text: str) -> list:
    """Parse a comma-separated argument list of string literals / arrays / identifiers."""
    args: list = []
    i, n = 0, len(arg_text)

    def skip_ws(j: int) -> int:
        while j < n and arg_text[j] in " \t\r\n":
            j += 1
        return j

    while i < n:
        i = skip_ws(i)
        if i >= n:
            break
        ch = arg_text[i]
        if ch == '"':
            j = i + 1
            buf: list[str] = []
            while arg_text[j] != '"':
                if arg_text[j] == "\\":
                    buf.append(arg_text[j + 1])
                    j += 2
                else:
                    buf.append(arg_text[j])
                    j += 1
            args.append("".join(buf))
            i = j + 1
        elif ch == "[":
            depth = 1
            j = i + 1
            while depth > 0:
                cj = arg_text[j]
                if cj == '"':
                    j += 1
                    while arg_text[j] != '"':
                        j += 2 if arg_text[j] == "\\" else 1
                elif cj == "[":
                    depth += 1
                elif cj == "]":
                    depth -= 1
                j += 1
            args.append(re.findall(r'"((?:[^"\\]|\\.)*)"', arg_text[i:j]))
            i = j
        else:
            j = i
            depth = 0
            while j < n and (depth > 0 or arg_text[j] != ","):
                if arg_text[j] in "([{":
                    depth += 1
                elif arg_text[j] in ")]}":
                    depth -= 1
                j += 1
            args.append(arg_text[i:j].strip())
            i = j
        i = skip_ws(i)
        if i < n and arg_text[i] == ",":
            i += 1
    return args


def parse_with_overrides(with_text: str) -> dict[str, list[str]]:
    overrides: dict[str, list[str]] = {}
    for match in re.finditer(r"(\w+)\s*=\s*\[(.*?)\]", with_text, re.DOTALL):
        overrides[match.group(1)] = re.findall(r'"((?:[^"\\]|\\.)*)"', match.group(2))
    return overrides


def parse_pack(call: str, arrays: dict[str, list[str]]) -> dict:
    match = re.match(r"(LenovoHardwarePack|LenovoBasicPack|BasicPack)\s*\(", call)
    if not match:
        raise ValueError(f"Unrecognized pack expression: {call[:80]}...")
    helper = match.group(1)

    # Separate the call arguments from an optional `with { ... }` suffix.
    open_paren = call.index("(", match.start(1))
    depth = 1
    i = open_paren + 1
    while depth > 0:
        ch = call[i]
        if ch == '"':
            i += 1
            while call[i] != '"':
                i += 2 if call[i] == "\\" else 1
        elif ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
        i += 1
    args = parse_arguments(call[open_paren + 1 : i - 1])
    overrides = {}
    with_match = re.search(r"with\s*\{(.*)\}\s*$", call[i:], re.DOTALL)
    if with_match:
        overrides = parse_with_overrides(with_match.group(1))

    if helper == "LenovoHardwarePack":
        pack_id, name, families, prefixes, machine_types, keywords = args
        vendor, aliases = "LENOVO", []
        enabled = arrays["LenovoHardwareEnabledFeatures"]
        hidden: list[str] = []
    elif helper == "LenovoBasicPack":
        pack_id, name, families, prefixes, machine_types, keywords = args
        vendor, aliases = "LENOVO", []
        enabled = arrays["UniversalBasicEnabledFeatures"]
        hidden = arrays["UniversalBasicHiddenFeatures"]
    else:
        pack_id, name, vendor, aliases, families, prefixes, machine_types, keywords = args
        enabled = arrays["UniversalBasicEnabledFeatures"]
        hidden = arrays["UniversalBasicHiddenFeatures"]

    if pack_id == "CatalogDeviceSupportProvider.GenericBasicPackId":
        pack_id = GENERIC_BASIC_PACK_ID

    return {
        "id": pack_id,
        "displayName": name,
        "vendor": vendor,
        "vendorAliases": aliases,
        "families": families,
        "modelPrefixes": prefixes,
        "modelKeywords": keywords,
        "machineTypes": machine_types,
        "enabledFeatures": overrides.get("EnabledFeatures", enabled),
        "hiddenFeatures": overrides.get("HiddenFeatures", hidden),
    }


def main() -> int:
    source = strip_line_comments(CATALOG_CS.read_text(encoding="utf-8"))
    arrays = parse_named_arrays(source)
    block = extract_device_packs_block(source)
    packs = [parse_pack(call, arrays) for call in split_top_level_calls(block)]

    text = json.dumps(packs, indent=2, ensure_ascii=True)
    # Match System.Text.Json default encoder: also escape ' & < > + as \uXXXX,
    # uppercase hex escapes, CRLF line endings, no final newline.
    text = re.sub(r"['&<>+]", lambda m: "\\u%04X" % ord(m.group(0)), text)
    text = re.sub(r"\\u([0-9a-f]{4})", lambda m: "\\u" + m.group(1).upper(), text)
    OUTPUT_JSON.write_bytes(text.replace("\n", "\r\n").encode("utf-8"))
    print(f"Wrote {len(packs)} device packs to {OUTPUT_JSON}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
