namespace UniversalDeviceToolkit.CLI.Lib;

public static class Constants
{
    public const string DEFAULT_PIPE_NAME = "LenovoLegionToolkit-IPC-0";
    public const string PIPE_NAME_ENVIRONMENT_VARIABLE = "LLT_IPC_PIPE_NAME";
    public static string PIPE_NAME => System.Environment.GetEnvironmentVariable(PIPE_NAME_ENVIRONMENT_VARIABLE) ?? DEFAULT_PIPE_NAME;
}
