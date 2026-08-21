#!/usr/bin/env python3
"""translate_comments.py

Utility script that scans source files for Chinese comments and adds a placeholder English translation.
It supports common comment syntaxes and file types.

Usage:
    python translate_comments.py
"""
import re, os, sys

# File extensions to process
EXTENSIONS = {'.cs', '.ts', '.js', '.jsx', '.tsx', '.py', '.cpp', '.c', '.h', '.hpp', '.md', '.txt'}

# Regex to detect Chinese characters
CHINESE_RE = re.compile(r'[\u4e00-\u9fff]')

# Comment markers per language
COMMENT_MARKERS = {
    '.cs': ['//', '/*', '*/'],
    '.ts': ['//', '/*', '*/'],
    '.js': ['//', '/*', '*/'],
    '.jsx': ['//', '/*', '*/'],
    '.tsx': ['//', '/*', '*/'],
    '.py': ['#'],
    '.cpp': ['//', '/*', '*/'],
    '.c': ['//', '/*', '*/'],
    '.h': ['//', '/*', '*/'],
    '.hpp': ['//', '/*', '*/'],
    '.md': ['<!--', '-->'],
    '.txt': ['#']
}

def is_comment_line(line, ext):
    markers = COMMENT_MARKERS.get(ext, [])
    stripped = line.lstrip()
    for m in markers:
        if stripped.startswith(m):
            return True
    return False

def process_file(path):
    _, ext = os.path.splitext(path)
    if ext not in EXTENSIONS:
        return False
    try:
        with open(path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except Exception as e:
        print(f"[WARN] Unable to read {path}: {e}")
        return False
    modified = False
    new_lines = []
    for line in lines:
        if CHINESE_RE.search(line) and is_comment_line(line, ext):
            placeholder = ' EN: [Translation pending]'
            if line.rstrip().endswith('\n'):
                line = line.rstrip('\n') + placeholder + '\n'
            else:
                line = line + placeholder
            modified = True
        new_lines.append(line)
    if modified:
        try:
            with open(path, 'w', encoding='utf-8') as f:
                f.writelines(new_lines)
            print(f"[INFO] Updated {path}")
        except Exception as e:
            print(f"[ERROR] Unable to write {path}: {e}")
            return False
    return modified

def main():
    root = os.path.abspath(os.path.dirname(__file__))
    changed_files = []
    for dirpath, _, filenames in os.walk(root):
        for name in filenames:
            full_path = os.path.join(dirpath, name)
            if process_file(full_path):
                changed_files.append(os.path.relpath(full_path, root))
    print(f"Total files modified: {len(changed_files)}")
    if changed_files:
        print('\n'.join(changed_files))

if __name__ == '__main__':
    main()
