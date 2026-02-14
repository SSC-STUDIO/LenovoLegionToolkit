using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件配置接口，用于插件持久化配置
/// </summary>
public interface IPluginConfiguration
{
    /// <summary>
    /// 获取配置值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值</returns>
    T GetValue<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// 设置配置值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值</param>
    void SetValue<T>(string key, T value);
    
    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>是否存在</returns>
    bool HasKey(string key);
    
    /// <summary>
    /// 删除配置键
    /// </summary>
    /// <param name="key">配置键</param>
    void RemoveKey(string key);
    
    /// <summary>
    /// 保存配置到存储
    /// </summary>
    Task SaveAsync();
    
    /// <summary>
    /// 重新加载配置
    /// </summary>
    Task ReloadAsync();
    
    /// <summary>
    /// 清除所有配置
    /// </summary>
    void Clear();
}
