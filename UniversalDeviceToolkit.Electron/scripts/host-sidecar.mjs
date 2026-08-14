import { existsSync } from 'node:fs'

/**
 * Framework-dependent .NET apphost requires Foo.runtimeconfig.json (and Foo.deps.json)
 * next to Foo.exe. Without them the runtime treats the exe as self-contained and
 * looks for hostpolicy.dll under Program Files\dotnet.
 */
export function hostSidecarPath(hostPath, extension, platform = process.platform) {
  if (platform === 'win32' && hostPath.toLowerCase().endsWith('.exe')) {
    return `${hostPath.slice(0, -4)}.${extension}`
  }
  return `${hostPath}.${extension}`
}

export function isRunnableHost(hostPath, exists = existsSync, platform = process.platform) {
  return (
    exists(hostPath) &&
    exists(hostSidecarPath(hostPath, 'runtimeconfig.json', platform)) &&
    exists(hostSidecarPath(hostPath, 'deps.json', platform))
  )
}

/** NetworkProxy worker lives beside Host.exe in Debug output and packaged extraResources/host. */
export function networkProxyPathBesideHost(hostPath, platform = process.platform) {
  const workerName =
    platform === 'win32' ? 'UniversalDeviceToolkit.NetworkProxy.exe' : 'UniversalDeviceToolkit.NetworkProxy'
  const slash = Math.max(hostPath.lastIndexOf('\\'), hostPath.lastIndexOf('/'))
  if (slash < 0) {
    return workerName
  }
  return `${hostPath.slice(0, slash + 1)}${workerName}`
}

export function isDevHostLayoutReady(hostPath, exists = existsSync, platform = process.platform) {
  return isRunnableHost(hostPath, exists, platform) && isRunnableHost(networkProxyPathBesideHost(hostPath, platform), exists, platform)
}
