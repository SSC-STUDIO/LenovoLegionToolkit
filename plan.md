# 实施计划：修复 LenovoLegionToolkit 的远程 CI 检查 ci/cl-test

## 目标
- 解决当前阻塞发布的远程 CI 检查 `ci/cl-test` 失效问题
- 通过复现、分析并修复根本原因，确保 CI 通过后再进行发布准备

## 步骤
1. **读取当前状态**
   - 打开 `progress.md` 与 `findings.md`，获取 `ci/cl-test` 失效的最新记录
   - 记录失效的 GitHub Actions URL 与错误摘要

2. **定位仓库与触发命令**
   - 确认 LLT 代码仓库所在位置
   - 确认用于运行 CI 的脚本或配置文件路径（如 `.github/workflows/cl-test.yml`）

3. **本地复现失效**
   - 在 WSL 中执行与 GitHub Actions 相同的命令（如 `dotnet test`、`dotnet build` 或 `ci` 脚本），确认本地能重现相同错误

4. **根因分析**
   - 根据错误信息定位可能的根本原因（例如依赖版本冲突、环境变量缺失、构建脚本路径错误等）

5. **实施修复**
   - 编辑对应文件或脚本，修正根本原因
   - 添加或更新必要的依赖/环境变量/路径设置

6. **提交并推送**
   - 使用以下提交模板创建新提交：
     ```
     Fix ci/cl-test failure: <简要描述根因>
     ```
   - 推送到对应的 GitHub 分支（默认 `master`）

7. **重新触发 CI**
   - 使用 GitHub API 或手动方式重新启动 `ci/cl-test` 工作流
   - 等待运行完成并记录结果

8. **验证与记录**
   - 若 CI 通过，在 `progress.md` 中标记为 `✅ 通过`，并在 `findings.md` 总结修复过程
   - 若仍然失败，将状态及错误摘要追加至 `/home/chenrunsen/.claude/agent-ops/issues.md` 作为阻塞项
   - 将完整的复现步骤、修复补丁、验证截图（若有）记录在 `progress.md` 与 `findings.md`

9. **最终提交**
   - 如所有验证成功，执行最终 `git add -A && git commit -m "<简洁英文任务描述>"`

## 所需工具
- Git（已在环境中）
- Bash / WSL 命令行
- `gh` CLI（可选，用于触发工作流）
- 编辑器（已在环境中）

## 用户确认
请确认是否可以继续执行上述修复流程以及后续的 CI 重新触发和验证。如需调整优先级或范围，请在此回复中说明。