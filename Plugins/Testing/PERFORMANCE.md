# 性能基准测试

## 目的
本目录包含 Universal Device Toolkit Plugins 的性能基准测试，用于：
- 确保插件加载时间在可接受范围内
- 检测性能回归
- 为 README 提供性能数据

## 测试项目
- `Plugins.LoadPerformanceTests` — 插件加载性能测试

## 运行测试
```powershell
dotnet test Tests/LoadPerformanceTests/LoadPerformanceTests.csproj --configuration Release
```

## 性能指标
| 指标 | 目标 | 当前 |
|------|------|------|
| 插件加载时间 | < 100ms | 待测量 |
| 内存占用 | < 50MB | 待测量 |
| UI 响应时间 | < 16ms (60fps) | 待测量 |

## 集成
性能测试结果会自动更新到 README 的性能徽章。
