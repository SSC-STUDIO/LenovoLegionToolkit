## 欢迎来到 UDT 开发者指南！

### 其他语言版本：
* [English](CONTRIBUTING.md)

首先感谢你花时间为本项目做出贡献！随着 UDT 的热度越来越高，为了确保你的贡献能够被顺利采纳，你应该遵守一定的格式和规则。

<br/>

_由于 Issues 总量的增加，不符合标准的 Issue 会在无预先警告的情况下被关闭或删除。屡次违反者将被本项目封禁。_

<br/>

**开发环境准备**

1. 安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（Windows）
2. 克隆仓库：`git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit.git`
3. 还原（与 CI 一致）：`dotnet restore UniversalDeviceToolkit.sln --locked-mode`
4. 构建：`dotnet build -c Release -m:1 --no-restore`
5. 运行测试：`dotnet test -c Release`

NuGet 还原通过各项目已提交的 `packages.lock.json` 保证可复现（`Directory.Build.props` 中启用了 `RestorePackagesWithLockFile`）。CI 始终使用 `dotnet restore … --locked-mode`。本地对齐 CI 时请带上该参数；仅在有意更新包版本后刷新锁文件时省略，并将更新后的 `packages.lock.json` 一并提交。`Make.bat` 与多数本地脚本依赖构建/发布时的隐式还原，不会强制 `--locked-mode`，因此一般离线构建不会因锁文件严格校验而中断。

解决方案共有 16 个项目。请顺序构建（`-m:1`），以避免 VBCSCompiler 锁冲突。完整项目结构见 [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)。

<br/>

**1. 在报告 Issue 前请仔细阅读 README**

绝大多数常见问题的解决方法和重要信息都已在 [README](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/README_zh-hans.md) 内阐明。请务必在报告 Issue 或发起讨论前通读其中的内容。

**2. 检查已被报告的 Issues**

请检查项目仓库下的 [Issues](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/issues?q=is%3Aissue) 和 [Discussions](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/discussions?discussions_q=) 栏目。请不要报告重复的 Issue 或发起重复的讨论。即使你找到的 Issue 已经被关闭，你一样可以在那里留言。

**3. 使用英语**

这会让所有人之间的交流都更加便利。

译者提示：若你无法流利地使用英语表达，你可以在使用中文完成草稿后使用百度翻译或 [DeepL](https://www.deepl.com/zh/translator) 等翻译网站或软件将草稿翻译为英语后提交。

**4. 尊重项目目标**

这不是一个万能的应用。项目的愿景很明确：为拯救者笔记本提供一个 Legion Zone（海外版则为 Vantage）的替代品。请勿要求支持其他类型或型号的设备。

**5. 在新建 Issue 前审查你的问题**

请确保 Bug 确实是 UDT 的 Bug。这不是一个免费的系统故障排除论坛，如果你在使用被修改过的 Windows 版本或你的系统本身已经出现问题，请自行解决。

**6. 尽所能详细描述你的问题**

详细的描述是解决问题的关键所在。请在新建 Issue 时填写表单内的所有项目并提供日志文件。只有提供良好的描述我们才能更快地解决问题。

**7. 为你的 Issue 或讨论起一个好的标题**

这样可以极大方便浏览 Issue 和讨论列表。“使用 UDT 时出现问题”并不是一个好的标题。

**8. 围绕主题**

不要发表与主题无关或无意义的留言。

**9. 一个 Issue 一个问题**

请不要在一个 Issue 内同时报告多个问题或请求添加多个功能。请为每一个问题、主题或想法新建一个单独的 Issue 或讨论，这会让后期跟进更加容易。

**10. 翻译**

我们使用 [Crowdin](https://crowdin.com/project/llt) 作为软件翻译平台。如果你想为翻译做出贡献，请在那里申请访问项目的权限。

**11. Pull requests**

我们欢迎你提交 PR（当然了）。除非你提交了一个非常简单易懂的 PR，请先创建一个 Issue 并描述你正在解决的问题。为一个会被拒绝的点子花时间并没有什么意义，因为这不符合本项目的愿景。同时请遵循现有的代码风格和项目组织。

<br/>

再次感谢你花时间帮助项目变得更好！
