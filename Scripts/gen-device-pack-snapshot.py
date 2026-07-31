#!/usr/bin/env python3
"""Regenerate Tools/Installer/DevicePackSnapshot.cs from the built-in C# catalog.

The installer snapshot is a trimmed mirror of the app catalog (see
Docs/DEVICE_PROVIDERS.md). DevicePackSnapshotGuardTests fails when the two
drift. Run this after any catalog edit:

    python Scripts/gen-device-pack-snapshot.py

Parsing is shared with gen-device-packs.py so both mirrors always agree.
"""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GEN_PACKS = ROOT / "Scripts" / "gen-device-packs.py"
CATALOG_CS = ROOT / "UniversalDeviceToolkit.Lib" / "DeviceSupport" / "LenovoDeviceSupportProvider.cs"
OUTPUT_CS = ROOT / "Tools" / "Installer" / "DevicePackSnapshot.cs"


def load_pack_parser():
    spec = importlib.util.spec_from_file_location("gen_device_packs", GEN_PACKS)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def cs_string(value: str) -> str:
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def cs_array(values: list[str]) -> str:
    if not values:
        return "[]"
    return "[" + ", ".join(cs_string(value) for value in values) + "]"


def main() -> int:
    parser = load_pack_parser()
    source = parser.strip_line_comments(CATALOG_CS.read_text(encoding="utf-8"))
    arrays = parser.parse_named_arrays(source)
    block = parser.extract_device_packs_block(source)
    packs = [parser.parse_pack(call, arrays) for call in parser.split_top_level_calls(block)]

    lines = [
        "namespace UniversalDeviceToolkit.Installer;",
        "",
        "/// <summary>",
        "/// Snapshot of the app's built-in device support catalog",
        "/// (UniversalDeviceToolkit.Lib/DeviceSupport/LenovoDeviceSupportProvider.cs),",
        "/// trimmed to the fields the installer matcher needs.",
        "/// DevicePackSnapshotGuardTests fails when the app catalog drifts —",
        "/// regenerate with `python Scripts/gen-device-pack-snapshot.py`.",
        "/// </summary>",
        "internal sealed record DevicePackInfo(",
        "    string Id,",
        "    string DisplayName,",
        "    string Vendor,",
        "    string[] VendorAliases,",
        "    string[] ModelKeywords,",
        "    string[] MachineTypes,",
        "    bool IsHardware);",
        "",
        "internal static class DevicePackSnapshot",
        "{",
        '    public const string GenericBasicPackId = "generic-pc-basic";',
        "",
        "    public static readonly DevicePackInfo[] Packs =",
        "    [",
    ]

    for pack in packs:
        is_hardware = any(
            feature.lower() == "lenovo-hardware-controls"
            for feature in pack["enabledFeatures"]
        )
        lines.append(
            "        new("
            + cs_string(pack["id"])
            + ", "
            + cs_string(pack["displayName"])
            + ", "
            + cs_string(pack["vendor"])
            + ", "
            + cs_array(pack["vendorAliases"])
            + ", "
            + cs_array(pack["modelKeywords"])
            + ", "
            + cs_array(pack["machineTypes"])
            + ", "
            + ("true" if is_hardware else "false")
            + "),"
        )

    lines += [
        "    ];",
        "}",
        "",
    ]

    OUTPUT_CS.write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(f"Wrote {len(packs)} device packs to {OUTPUT_CS}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
