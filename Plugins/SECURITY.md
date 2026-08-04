# Security Policy

## Reporting a Vulnerability

We take the security of the Universal Device Toolkit plugin surface seriously. If you discover a security vulnerability, please report it responsibly.

### 📧 How to Report

**Do NOT open a public GitHub issue for security vulnerabilities.**

Instead, please report privately via one of these methods:

1. **GitHub Security Advisories** (preferred):
   - Go to the [Security Advisories page](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/security/advisories)
   - Click "New draft security advisory"
   - Fill in the vulnerability details

2. **Email**:
   - Send an email to: [security@ssc-studio.dev](mailto:security@ssc-studio.dev)
   - Use GPG encryption if possible (key available on request)

### What to Include

Please include as much of the following as possible:

- **Type of vulnerability** (e.g., privilege escalation, code injection, DLL hijacking)
- **Affected plugin(s)** and version(s)
- **Step-by-step reproduction instructions**
- **Proof-of-concept code** (if available)
- **Potential impact** assessment

### Response Timeline

- **Acknowledgment**: Within 48 hours
- **Initial assessment**: Within 1 week
- **Fix release**: Within 2 weeks for critical issues, 4 weeks for others
- **Public disclosure**: After the fix is released and users have had time to update

### Scope

This policy applies to:

- All official plugins in the `Plugins/Official/` directory
- The plugin SDK in the `Plugins/SDK/Runtime/` directory
- The PluginWorkbench and PluginTooling tools in `Plugins/Tooling/`

### Recognition

Security researchers who report vulnerabilities responsibly will be:
- Credited in the release notes (unless they prefer to remain anonymous)
- Added to the [Security Hall of Fame](#) (coming soon)

Thank you for helping keep Universal Device Toolkit secure!
