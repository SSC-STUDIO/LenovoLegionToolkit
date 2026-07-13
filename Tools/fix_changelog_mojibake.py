#!/usr/bin/env python3
"""Recover CHANGELOG.md Chinese mojibake (UTF-8 misread as CP936/GBK).

History: commit 8e570a7b re-saved UTF-8 CJK as if it were CP936, producing
garbled Han + Microsoft PUA glyphs. Reverse with Windows CP936 encode → UTF-8
decode. Prefer Chinese from pre-corruption commit 809bb41c when English matches.
"""

from __future__ import annotations

import ctypes
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "CHANGELOG.md"
GOOD_COMMIT = "809bb41c"
_kernel32 = ctypes.windll.kernel32

HEADER = """# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。
"""

SECTION_MAP = {
    r"### Added\s*/\s*[^\n]+": "### Added / 新增",
    r"### Fixed\s*/\s*[^\n]+": "### Fixed / 修复",
    r"### Changed\s*/\s*[^\n]+": "### Changed / 变更",
    r"### Improved\s*/\s*[^\n]+": "### Improved / 改进",
    r"### Removed\s*/\s*[^\n]+": "### Removed / 移除",
    r"### Deprecated\s*/\s*[^\n]+": "### Deprecated / 弃用",
    r"### Security\s*/\s*[^\n]+": "### Security / 安全",
}


def encode_cp936(s: str) -> bytes:
    if not s:
        return b""
    n = _kernel32.WideCharToMultiByte(936, 0, s, len(s), None, 0, None, None)
    if n <= 0:
        raise UnicodeEncodeError("cp936", s, 0, len(s), "WideCharToMultiByte failed")
    buf = ctypes.create_string_buffer(n)
    r = _kernel32.WideCharToMultiByte(936, 0, s, len(s), buf, n, None, None)
    if r <= 0:
        raise UnicodeEncodeError("cp936", s, 0, len(s), "WideCharToMultiByte failed")
    return buf.raw


def try_fix(s: str) -> str | None:
    if not s or "\ufffd" in s:
        return None
    if all(ord(c) < 128 for c in s):
        return None
    try:
        fixed = encode_cp936(s).decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError, ValueError, OSError):
        return None
    if fixed == s:
        return None
    if any(0xE000 <= ord(c) <= 0xF8FF for c in fixed):
        return None
    good = 0
    for ch in fixed:
        o = ord(ch)
        if (
            o < 128
            or 0x4E00 <= o <= 0x9FFF
            or 0x3000 <= o <= 0x303F
            or 0xFF00 <= o <= 0xFFEF
            or ch in "—–…·°「」『』【】"
        ):
            good += 1
    if good / max(len(fixed), 1) < 0.85:
        return None
    return fixed


def recover_span(s: str) -> str:
    whole = try_fix(s)
    if whole is not None:
        return whole
    out: list[str] = []
    i = 0
    n = len(s)
    while i < n:
        recovered = None
        for length in range(min(160, n - i), 0, -1):
            chunk = s[i : i + length]
            if "\ufffd" in chunk:
                continue
            rec = try_fix(chunk)
            if rec is not None:
                recovered = (length, rec)
                break
        if recovered:
            length, rec = recovered
            out.append(rec)
            i += length
        else:
            out.append(s[i])
            i += 1
    return "".join(out)


def is_cjkish(ch: str) -> bool:
    o = ord(ch)
    return (
        0x2E80 <= o <= 0x9FFF
        or 0xF900 <= o <= 0xFAFF
        or 0xE000 <= o <= 0xF8FF
        or 0xFF00 <= o <= 0xFFEF
        or 0x3000 <= o <= 0x30FF
        or 0x2000 <= o <= 0x206F
        or 0x2500 <= o <= 0x25FF
        or ch in "·•°"
    )


def recover_text(s: str) -> str:
    result: list[str] = []
    i = 0
    n = len(s)
    while i < n:
        if is_cjkish(s[i]) or s[i] == "\ufffd":
            j = i + 1
            while j < n and (is_cjkish(s[j]) or s[j] == "\ufffd"):
                j += 1
            result.append(recover_span(s[i:j]))
            i = j
        else:
            result.append(s[i])
            i += 1
    return "".join(result)


def split_en_zh(line: str) -> tuple[str, str] | None:
    """Split bilingual line into (english_side, chinese_side) when possible.

    Only splits on ' / ' when one side is predominantly English and the other
    predominantly CJK. Avoids splitting paths like Docs/FlaUI_Testing.md.
    """
    if " / " not in line:
        return None
    # Find candidate splits from left to right; pick best EN/ZH pair
    parts = line.split(" / ")
    if len(parts) < 2:
        return None

    def score_en(s: str) -> int:
        return sum(1 for c in s if ord(c) < 128 and c.isalpha())

    def score_cjk(s: str) -> int:
        return sum(1 for c in s if 0x4E00 <= ord(c) <= 0x9FFF or 0xE000 <= ord(c) <= 0xF8FF)

    best = None
    best_score = -1
    for i in range(1, len(parts)):
        left = " / ".join(parts[:i])
        right = " / ".join(parts[i:])
        le, lc = score_en(left), score_cjk(left)
        re_, rc = score_en(right), score_cjk(right)
        # EN / ZH
        if le >= 12 and rc >= 2 and lc <= le // 3:
            sc = le + rc * 2
            if sc > best_score:
                best_score = sc
                best = (left, right, "en_zh")
        # ZH / EN
        if re_ >= 12 and lc >= 2 and rc <= re_ // 3:
            sc = re_ + lc * 2
            if sc > best_score:
                best_score = sc
                best = (right, left, "zh_en")
    if best is None:
        return None
    en, zh, _ = best
    return en, zh


def has_pua_or_fffd(s: str) -> bool:
    return bool(re.search(r"[\ue000-\uf8ff\ufffd]", s))


def residual_mojibake(s: str) -> bool:
    """True if s still looks like unrecovered CP936 mojibake (not valid Chinese)."""
    if has_pua_or_fffd(s):
        return True
    # Unrecovered full-string recovery possible
    if try_fix(s) is not None:
        return True
    # Common residual mojibake syllables after partial recovery
    markers = re.findall(
        r"琛ュ|鎵╁|鎺у埗|杩佺Щ|瀹规€|鍒扮ǔ|娓╁害|鍔熻€|鍦\?|布灞€|锟|鏁存€|鑳藉|承鎺|维鎶|不姹℃|准纭|检娴|妯℃|问棰|功鑰|塌鎺|安瑁|回閫|显绀|完鏁|管鐞|清鏅|提鍗\?",
        s,
    )
    if len(markers) >= 1:
        return True
    # High density of rare CJK often used only in mojibake
    rare = len(re.findall(r"[鍙鎺鏂淇鐩鎵€鏍煎鍩轰簬杩欓」琛ュ鎵╁]", s))
    cjk = sum(1 for c in s if 0x4E00 <= ord(c) <= 0x9FFF)
    if cjk >= 6 and rare >= 3 and rare / cjk >= 0.25:
        return True
    return False


def bullet_prefix(line: str) -> str:
    m = re.match(r"^(\s*-\s*)", line)
    return m.group(1) if m else ""


def main() -> None:
    # Start from last committed (known corrupted) blob
    text = subprocess.check_output(
        ["git", "show", "HEAD:CHANGELOG.md"], cwd=ROOT
    ).decode("utf-8")
    good = subprocess.check_output(
        ["git", "show", f"{GOOD_COMMIT}:CHANGELOG.md"], cwd=ROOT
    ).decode("utf-8")

    # 1) CP936 reverse recovery
    fixed = recover_text(text)

    # 2) Canonical header
    fixed = re.sub(r"^.*?(?=^## )", HEADER + "\n", fixed, count=1, flags=re.S | re.M)

    # 3) Section headers
    for pat, repl in SECTION_MAP.items():
        fixed = re.sub(pat, repl, fixed)

    # 4) Build good Chinese map from pre-corruption commit
    good_map: dict[str, str] = {}
    for line in good.splitlines():
        sp = split_en_zh(line)
        if not sp:
            continue
        en, zh = sp
        key = re.sub(r"^\s*-\s*", "", en).strip()[:120]
        if key:
            good_map[key] = zh

    # 5) Prefer good Chinese whenever English key matches
    replaced = 0
    lines_out: list[str] = []
    for line in fixed.splitlines():
        sp = split_en_zh(line)
        if not sp:
            lines_out.append(line)
            continue
        en, zh = sp
        key = re.sub(r"^\s*-\s*", "", en).strip()[:120]
        hit = good_map.get(key)
        if hit is None:
            k80 = key[:80]
            for gk, gzh in good_map.items():
                if gk[:80] == k80 or gk.startswith(k80) or k80.startswith(gk[:80]):
                    hit = gzh
                    break
        if hit is not None:
            # Reconstruct EN / ZH with original bullet on en side
            lines_out.append(f"{en} / {hit}")
            replaced += 1
        else:
            lines_out.append(line)

    fixed2 = "\n".join(lines_out)
    if text.endswith("\n"):
        fixed2 += "\n"

    # 6) Strip FFFD leftovers then recover again lightly
    fixed2 = re.sub(r"\ufffd\??", "", fixed2)
    fixed3 = recover_text(fixed2)

    # 7) Drop residual garbled Chinese halves; keep English (do not touch clean bilingual)
    final: list[str] = []
    dropped = 0
    for line in fixed3.splitlines():
        sp = split_en_zh(line)
        if sp:
            en, zh = sp
            if residual_mojibake(zh):
                # Keep English only
                if en.lstrip().startswith("-"):
                    final.append(en.rstrip())
                else:
                    final.append(bullet_prefix(line) + en.lstrip("- ").rstrip())
                dropped += 1
                continue
            final.append(line)
            continue

        # Chinese-first without clean ' / ' separator: e.g. "中文?/ English"
        m = re.match(
            r"^(\s*-\s*)(.+?)(?:\s*/\s*|\?/\s*)([A-Z][A-Za-z].*)$",
            line,
        )
        if m and residual_mojibake(m.group(2)):
            final.append(m.group(1) + m.group(3).strip())
            dropped += 1
            continue

        final.append(line)

    fixed4 = "\n".join(final)
    if text.endswith("\n"):
        fixed4 += "\n"

    for pat, repl in SECTION_MAP.items():
        fixed4 = re.sub(pat, repl, fixed4)

    # Normalize mid-line double spaces from removed FFFD, keep indentation
    norm_lines = []
    for ln in fixed4.splitlines():
        if ln.startswith("    "):
            norm_lines.append(ln)
        else:
            norm_lines.append(re.sub(r"[^\S\n]{2,}", " ", ln))
    fixed4 = "\n".join(norm_lines)
    if text.endswith("\n"):
        fixed4 += "\n"

    PATH.write_text(fixed4, encoding="utf-8", newline="\n")

    pua = len(re.findall(r"[\ue000-\uf8ff]", fixed4))
    fffd = fixed4.count("\ufffd")
    residual = sum(1 for ln in fixed4.splitlines() if residual_mojibake(ln))
    print(f"wrote {PATH}")
    print(f"replaced_from_good={replaced} dropped_broken_zh={dropped}")
    print(f"fffd={fffd} pua={pua} residual_garbled_lines={residual}")
    print("--- preview ---")
    for i, line in enumerate(fixed4.splitlines()[:55], 1):
        print(f"{i}: {line[:150]}")
    if residual:
        print("--- residual samples ---")
        n = 0
        for i, line in enumerate(fixed4.splitlines(), 1):
            if residual_mojibake(line):
                n += 1
                if n <= 12:
                    print(f"L{i}: {line[:150]}")


if __name__ == "__main__":
    main()
