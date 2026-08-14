# CheckSourceUnicode

源码 Unicode 卫生检测工具——扫描仓库中 AI 生成内容可能携带的隐式/混淆字符
（零宽字符、混淆空白、软连字符、变体选择符、全角 ASCII 相似字符、西里尔/希腊同形字符），
防止污染源码、破坏 diff 与编辑器体验。

## 用法

```powershell
node Tools/CheckSourceUnicode/check-unicode.mjs            # 扫描仓库根
node Tools/CheckSourceUnicode/check-unicode.mjs <路径>     # 扫描指定目录
```

退出码：`0` = 干净；`1` = 检出违规（输出 `文件:行:列  [字符名]  …上下文…`）。

## 检测内容

| 类别 | 示例 |
|---|---|
| 零宽/格式字符 | U+200B ZWSP、U+200C/200D、U+FEFF BOM、U+2060 WJ、U+200E/200F LRM/RLM、U+202A–202E |
| 混淆空白 | U+00A0 NBSP、U+2007、U+202F、U+2000–200A |
| 软连字符/变体选择符 | U+00AD、U+FE00–FE0F |
| 全角 ASCII 相似字符 | ＦＵＬＬＷＩＤＴＨ（U+FF01–FF5E，含全角字母数字标点） |
| 西里尔同形字符 | А а Е е О о Р р С с Т т Н н 等（冒充 A a E e O o P p C c T t H h） |
| 希腊同形字符 | Α α Β β Ε ε Ο ο Ρ ρ Τ τ 等 |

## 规则出处

- AGENTS.md「代码规范 → 字符编码与 AI 水印防污染」
- 提交前运行本工具；检出即修复（删除或替换为 ASCII/中文注释），修复后再提交。
