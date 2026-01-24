# 插件多语言资源键名规范

## 📋 键名命名约定

### 1. 基本格式
```
[PluginPrefix]_[Category]_[Item]_[OptionalSubItem]
```

### 2. 插件前缀 (PluginPrefix)
- **NetworkAcceleration**: `NetworkAcceleration_`
- **ViveTool**: `ViveTool_`

### 3. 分类 (Category)

#### 3.1 页面相关
- `PageTitle` - 页面标题
- `PageDescription` - 页面描述
- `Section[Name]` - 特定章节标题
- `Section[Name]Description` - 特定章节描述

#### 3.2 状态相关
- `ServiceStatus` - 服务状态
- `ServiceStatus[Running|Stopped]` - 具体状态
- `ServiceStatus[Running|Stopped]Description` - 状态描述
- `Status[Enabled|Disabled|Default|Unknown]` - 简单状态文本

#### 3.3 功能特性
- `[FeatureName]` - 功能名称
- `[FeatureName]Description` - 功能详细描述
- `[FeatureName]EnabledDescription` - 启用时的描述
- `[FeatureName]ShortDescription` - 简短描述

#### 3.4 操作相关
- `Refresh` - 刷新
- `Search` - 搜索
- `Search[Features|Placeholder]` - 搜索相关
- `Enable` - 启用
- `Disable` - 禁用
- `Import` - 导入
- `Import[FromFile|FromUrl]` - 导入方式
- `Download` - 下载
- `Browse` - 浏览
- `Cancel` - 取消
- `Reset` - 重置

#### 3.5 错误相关
- `Error_[ErrorType]` - 错误标题
- `Error_[ErrorType]Description` - 错误描述
- `[Operation]Failed` - 操作失败
- `[Operation]FailedDescription` - 操作失败描述

#### 3.6 统计数据
- `Downloaded` - 下载量
- `Uploaded` - 上传量
- `TotalTraffic` - 总流量
- `TrafficChart` - 流量图表
- `TrafficStatistics` - 流量统计
- `ResetStatistics` - 重置统计
- `ResetStatisticsButton` - 重置统计按钮

#### 3.7 设置项
- `Proxy[Settings|Address|Port]` - 代理相关
- `ConnectionTimeout` - 连接超时
- `[Setting]Placeholder` - 输入框占位符
- `[Setting]Description` - 设置说明

#### 3.8 平台相关
- `PlatformAcceleration` - 平台加速
- `Platform[PlatformName]` - 平台名称
- `[PlatformName]Acceleration` - 特定平台加速

#### 3.9 列表和表格
- `FeatureFlags` - 功能列表
- `FeatureId` - 功能ID
- `FeatureName` - 功能名称
- `Actions` - 操作列
- `NoFeaturesFound` - 无结果提示
- `Loading` - 加载中

#### 3.10 标点符号
- `Colon` - 冒号 (:)
- `Comma` - 逗号 (,)
- `Period` - 句号 (.)

### 4. 命名示例

#### NetworkAcceleration 插件示例
```
NetworkAcceleration_PageTitle                    ✅
NetworkAcceleration_ServiceStatus                ✅
NetworkAcceleration_GithubAcceleration           ✅
NetworkAcceleration_Platform_GitHub             ✅
NetworkAcceleration_Error_StartupFailed        ✅
NetworkAcceleration_ConnectionTimeout          ✅
```

#### ViveTool 插件示例
```
ViveTool_PageTitle                              ✅
ViveTool_FeatureEnabled                        ✅
ViveTool_SearchFeatures                        ✅
ViveTool_ImportFromFile                        ✅
ViveTool_Error_EnableFeatureFailed            ✅
```

### 5. 避免的命名模式

#### ❌ 不推荐的模式
- 过于简短: `Title`, `Desc`, `Stat`
- 缩写不规范: `NetAcc`, `VT`, `DL`
- 不一致: `GithubAcceleration`, `steam_acceleration`
- 无前缀: `ServiceStatus`, `ProxySettings`

#### ✅ 推荐的模式
- 完整描述: `NetworkAcceleration_ServiceStatus`
- 一致性: `NetworkAcceleration_GithubAcceleration`, `NetworkAcceleration_SteamAcceleration`
- 明确分类: `ViveTool_Error_EnableFeatureFailed`

### 6. 特殊情况处理

#### 6.1 平台名称
- 保持原始大小写: `GitHub`, `Steam`, `Discord`, `npm`, `PyPI`
- 使用统一前缀: `NetworkAcceleration_Platform_`

#### 6.2 错误消息
- 使用描述性名称: `CertificateSetupFailed`, `ProxyServiceStartupFailed`
- 包含错误类型: `Error_[SpecificError]`

#### 6.3 多语言友好性
- 避免在键名中包含特定语言的标点
- 使用 `_` 分隔，提高可读性
- 保持键名语言中立

### 7. 文档维护

#### 7.1 更新检查清单
- [ ] 新键名遵循命名约定
- [ ] 类似功能使用一致的命名模式
- [ ] 更新此文档以反映变更
- [ ] 检查现有资源文件的一致性

#### 7.2 代码审查要点
- 资源键名的可读性和一致性
- 确保所有硬编码字符串都已移除
- 验证所有语言的资源文件键名一致

### 8. 最佳实践

1. **一致性优先**: 同一插件内保持一致的命名风格
2. **可读性**: 键名应该能够清楚表达其用途
3. **可维护性**: 避免歧义，便于后续维护
4. **国际化友好**: 考虑不同语言的特殊需求
5. **版本控制**: 新增键名时不要删除旧的键名，保持向后兼容

---

## 📝 当前状态

### NetworkAcceleration 插件
- ✅ 键名规范化程度: 95%
- ✅ 使用统一前缀: `NetworkAcceleration_`
- ✅ 分类清晰: 服务、功能、设置、错误等
- ✅ 无硬编码字符串

### ViveTool 插件  
- ✅ 键名规范化程度: 98%
- ✅ 使用统一前缀: `ViveTool_`
- ✅ 分类清晰: 功能、操作、状态、错误等
- ✅ 命名一致性优秀

### 建议改进
- NetworkAcceleration插件可以考虑将平台名称统一使用 `Platform_` 前缀
- 考虑为长描述添加 `Long` 后缀，为短描述添加 `Short` 后缀（已在部分实现）
- 统一错误消息的命名模式

---

*最后更新时间: 2026-01-22*