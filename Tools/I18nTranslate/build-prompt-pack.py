#!/usr/bin/env python3
"""Merge _agent_out drafts into glossary.json and prompts.json."""
from __future__ import annotations

import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
DRAFT = HERE / "_agent_out"
SKIP_KEEP = {
    "Hybrid-Auto",
    "Hybrid-iGPU",
    "Hosts",
    "Universal Device Toolkit",
}

FORMAT_CHARS = set(
    list(range(0x200B, 0x2010))
    + [0x2060, 0xFEFF, 0x2028, 0x2029, 0x00AD]
    + list(range(0x202A, 0x202F))
)
CYR_LOOK = {
    0x0410, 0x0430, 0x0415, 0x0435, 0x041E, 0x043E, 0x0420, 0x0440,
    0x0421, 0x0441, 0x0422, 0x0442, 0x041D, 0x043D, 0x041A, 0x043A,
    0x041C, 0x043C, 0x0412, 0x0432, 0x0417, 0x0437, 0x0425, 0x0445,
    0x0423, 0x0443, 0x0418, 0x044F, 0x0436, 0x0434, 0x0448, 0x0449,
}
GR_LOOK = {
    0x0391, 0x03B1, 0x0392, 0x03B2, 0x0395, 0x03B5, 0x039F, 0x03BF,
    0x03A1, 0x03C1, 0x03A4, 0x03C4, 0x039D, 0x03BD, 0x039C, 0x03BC,
    0x039A, 0x03BA, 0x03A3, 0x03C3, 0x0397, 0x03B7, 0x03A7, 0x03C7,
}

SLAVIC_STYLE = (
    "Translate software UI into the target Slavic locale. Use formal address "
    "(Russian/Belarusian vy, Ukrainian Vy, Bulgarian/Macedonian Vie, Czech/Slovak vy, "
    "Serbian/Croatian/Slovenian/Bosnian Vi). Button and menu labels are infinitive or a short noun; "
    "no trailing period; no surrounding quotes. "
    "Script: ru, uk, bg, mk, sr are Cyrillic. sr-Latn, pl, cs, sk, hr, sl, bs are Latin. "
    "Never mix scripts in one string except required Latin acronyms, brands, and placeholders. "
    "Keep CPU, GPU, dGPU, iGPU, USB, RGB, HDR, OSD, FPS, TDP, TGP, VRAM, RAM, SSD, HDD, BIOS, "
    "UEFI, WMI, ACPI, NVMe, PCIe, Fn, Windows, macOS, NVIDIA, AMD, Intel as ASCII Latin even inside "
    "a Cyrillic sentence. HybridModeState_Off stays dGPU. "
    "Never replace letters in those acronyms with lookalike Cyrillic. "
    "Do not write Russian yo-diaeresis unless the word is ambiguous without it. "
    "Modifier keys Alt, Ctrl, Shift stay Latin. WindowBackdropStyle_Windows stays Windows. "
    "Preserve placeholders {0}, {1}, {name}, %s, \\n, and HTML tags exactly, in source order. "
    "Keep glossary brands UniversalDeviceToolkit, UDT, ViveTool, Legion, Vantage, Spectrum unchanged. "
    "PowerModeState_GodMode is the localized Custom label, not a calque of God. "
    "Polish must keep diacritics. Czech and Slovak use formal vy; do not paste Czech wording into Slovak. "
    "Do not copy Russian into Ukrainian or Belarusian. Serbian sr is ekavian Cyrillic; sr-Latn is the same "
    "ekavian wording in Latin. Croatian and Bosnian are ijekavian Latin, never Cyrillic. "
    "Adjectives agree in gender and number. Reuse one rendering for the same English label. "
    "Errors are factual and keep the source period. No slang, no emoji, no leftover English except glossary terms."
)

SLAVIC_NOTES = {
    "ru": "Cyrillic, formal vy. Short buttons for Save/Cancel/Apply/Enable/Disable. Keep CPU/GPU/USB/RGB Latin (not expanded). No yo-diaeresis unless needed. Do not render Windows as a window/okno calque or Shift as a physical shift.",
    "uk": "Cyrillic with Ukrainian i/yi/ye/ghe. Formal Vy. Never Russian yery/e/hard-sign. Enable/Disable and On/Off stay distinct. Settings is native Ukrainian, not a Russian calque. CPU Fan keeps CPU Latin.",
    "pl": "Latin with obligatory Polish diacritics. Buttons are infinitive. No Pan/Pani. Keep CPU/GPU Latin. Ctrl/Shift/Alt stay Latin. Do not drop l-stroke.",
    "cs": "Formal vy. Native Czech diacritics. File/screen/settings/button use Czech words, never Slovak variants. CPU Fan keeps CPU Latin. dGPU stays dGPU.",
    "sk": "Formal vy. Distinct from Czech file/screen/settings/button words. Native Slovak diacritics. CPU Fan keeps CPU Latin. Never copy Czech wording.",
    "bg": "Cyrillic. Formal Vie. No infinitive in grammar; UI buttons still short. Keep CPU/GPU/USB/RGB Latin. Not Macedonian palatal letters.",
    "sr": "Ekavian Cyrillic. Formal Vi. Same wording as sr-Latn, different script. Keep CPU Latin. Never Croatian ijekavian, never Latin in this locale except acronyms.",
    "sr-Latn": "Same ekavian Serbian as sr, Latin only with caron diacritics. Formal Vi. CPU Fan keeps CPU Latin. Never insert Cyrillic. Never Croatian ijekavian.",
    "hr": "Ijekavian Croatian Latin. Formal Vi. Never Serbian ekavian, never Cyrillic. Keep CPU/GPU Latin.",
    "sl": "Slovene Latin. Formal Vi. Dual exists but UI labels stay singular/plural as in source. Keep CPU Latin.",
    "mk": "Macedonian Cyrillic. Formal Vie. Not Bulgarian. Keep CPU/GPU Latin.",
    "be": "Belarusian Cyrillic. Formal vy. Not Russian and not Ukrainian. Keep CPU/GPU Latin.",
    "bs": "Bosnian ijekavian Latin. Formal Vi. Never Cyrillic. Keep CPU/GPU Latin.",
}


def load(name: str) -> dict:
    return json.loads((DRAFT / name).read_text(encoding="utf-8"))


def sanitize_text(value: str) -> str:
    out: list[str] = []
    for ch in value:
        code = ord(ch)
        if 0xFF01 <= code <= 0xFF5E:
            out.append(chr(code - 0xFEE0))
            continue
        if code in (0x00A0, 0x2007, 0x202F) or 0x2000 <= code <= 0x200A:
            out.append(" ")
            continue
        if code in FORMAT_CHARS or code in CYR_LOOK or code in GR_LOOK:
            continue
        out.append(ch)
    text = "".join(out)
    text = re.sub(r"[ \t]{2,}", " ", text)
    text = re.sub(r" +([,.;:])", r"\1", text)
    text = re.sub(r"\( +", "(", text)
    text = re.sub(r" +\)", ")", text)
    return text


def sanitize(obj):
    if isinstance(obj, str):
        return sanitize_text(obj)
    if isinstance(obj, list):
        return [sanitize(item) for item in obj]
    if isinstance(obj, dict):
        return {str(key): sanitize(val) for key, val in obj.items()}
    return obj


def load(name: str) -> dict:
    return json.loads((DRAFT / name).read_text(encoding="utf-8"))


def main() -> None:
    gloss_rows: dict[str, dict] = {}
    for name in (
        "01-brand-glossary.json",
        "02-hardware-glossary.json",
        "03-power-mode-glossary.json",
    ):
        data = load(name)
        for item in data.get("glossary", []):
            source = str(item.get("source", "")).strip()
            if not source or source in SKIP_KEEP:
                continue
            if len(source) <= 2 and source.isalpha():
                continue
            keep = bool(item.get("keep", False))
            target = str(item.get("target", source)).strip()
            if not keep:
                continue
            if target != source:
                continue
            if source not in gloss_rows or len(str(item.get("note", ""))) > len(
                str(gloss_rows[source].get("note", ""))
            ):
                gloss_rows[source] = {
                    "source": source,
                    "target": source,
                    "keep": True,
                    "note": str(item.get("note", "")).strip(),
                }

    glossary = sorted(gloss_rows.values(), key=lambda r: (r["source"].lower(), r["source"]))
    (HERE / "glossary.json").write_text(
        json.dumps(glossary, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )

    families: dict[str, dict] = {}
    locale_family: dict[str, str] = {}
    family_files = [
        ("14-family-other.json", "other"),
        ("13-family-turkic-uralic.json", "turkic-uralic-baltic"),
        ("12-family-sea.json", "sea"),
        ("11-family-indic.json", "indic"),
        ("10-family-slavic.json", "slavic"),
        ("09-family-germanic.json", "germanic"),
        ("08-family-romance.json", "romance"),
        ("06-family-cjk.json", "cjk"),
        ("07-family-rtl.json", "rtl"),
    ]
    for fname, fallback_name in family_files:
        data = load(fname)
        family = str(data.get("family") or fallback_name)
        families[family] = {
            "locales": list(data.get("locales") or []),
            "stylePrompt": str(data.get("stylePrompt") or "").strip(),
            "localeNotes": dict(data.get("localeNotes") or {}),
        }
        for loc in families[family]["locales"]:
            locale_family[str(loc)] = family
        for loc in families[family]["localeNotes"].keys():
            locale_family.setdefault(str(loc), family)

    locale_family.setdefault("zh-CN", "cjk")
    locale_family.setdefault("zh-TW", "cjk")
    locale_family.setdefault("nl-NL", "germanic")
    locale_family.setdefault("pt-PT", "romance")

    tg = load("16-engine-tg.json")
    g4 = load("17-engine-g4.json")
    placeholders = load("15-placeholders.json")
    quality = load("20-quality-gates.json")
    fewshot = load("18-fewshot.json")
    electron = load("19-electron-i18n.json")
    ui_verbs = load("04-ui-verbs.json")
    errors = load("05-error-tone.json")
    power = load("03-power-mode-glossary.json")
    hardware = load("02-hardware-glossary.json")

    placeholder_regex = ""
    for pat in placeholders.get("patterns") or []:
        if pat.get("name") == "pipeline-union-extended":
            placeholder_regex = str(pat.get("regex") or "")
            break
    if not placeholder_regex:
        raise SystemExit("missing pipeline-union-extended regex")

    prompts = {
        "version": 1,
        "placeholderRegex": placeholder_regex,
        "tgMaxFamilyChars": 400,
        "g4MaxFamilyChars": 1600,
        "engines": {
            "tg": {
                "batchUser": tg["batchUser"],
                "singleUser": tg["singleUser"],
                "retryUser": tg["retryUser"],
            },
            "g4": {
                "system": g4["system"],
                "batchUser": g4["batchUser"],
                "singleUser": g4["singleUser"],
                "retryUser": g4["retryUser"],
            },
        },
        "families": families,
        "localeFamily": locale_family,
        "fewShotZhHans": str(fewshot.get("promptBlock") or "").strip(),
        "fewShotExamples": fewshot.get("examples") or [],
        "commonRules": list(dict.fromkeys(
            list(hardware.get("promptRules") or [])
            + list(power.get("promptRules") or [])
            + list(ui_verbs.get("promptRules") or [])
            + list(errors.get("promptRules") or [])
            + list(placeholders.get("promptRules") or [])[:6]
            + list(quality.get("promptRules") or [])
        )),
        "retryInstruction": str(placeholders.get("retryInstruction") or "").strip(),
        "quality": {
            "promptRules": quality.get("promptRules") or [],
            "validators": quality.get("validators") or [],
        },
        "electron": {
            "batchUser": electron.get("batchUser"),
            "singleUser": electron.get("singleUser"),
            "retryUser": electron.get("retryUser"),
            "promptRules": electron.get("promptRules") or [],
            "localeCodeMap": electron.get("localeCodeMap") or {},
            "electronLocales": electron.get("electronLocales") or [],
            "placeholderRegex": electron.get("placeholderRegex") or "",
            "notes": electron.get("notes") or "",
        },
    }

    (HERE / "prompts.json").write_text(
        json.dumps(prompts, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(f"glossary entries: {len(glossary)}")
    print(f"families: {len(families)}")
    print(f"locale map: {len(locale_family)}")
    print(f"common rules: {len(prompts['commonRules'])}")
    print("wrote glossary.json and prompts.json")


if __name__ == "__main__":
    main()
