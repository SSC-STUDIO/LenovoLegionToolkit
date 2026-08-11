"use strict";
const electron = require("electron");
const path = require("path");
const fs = require("fs");
const child_process = require("child_process");
const readline = require("readline");
class HostClient {
  child = null;
  rl = null;
  pending = /* @__PURE__ */ new Map();
  listeners = /* @__PURE__ */ new Map();
  nextId = 1;
  on(event, callback) {
    if (!this.listeners.has(event)) this.listeners.set(event, /* @__PURE__ */ new Set());
    this.listeners.get(event).add(callback);
    return () => this.listeners.get(event)?.delete(callback);
  }
  start(hostPath) {
    if (this.child) throw new Error("Host already started");
    this.child = child_process.spawn(hostPath, [], {
      stdio: ["pipe", "pipe", "pipe"],
      windowsHide: true
    });
    this.child.stderr.on("data", (d) => {
      console.error(`[host] ${d.toString().trim()}`);
    });
    this.child.on("error", (error) => {
      console.error(`[host] spawn error: ${error.message}`);
    });
    this.rl = readline.createInterface({ input: this.child.stdout });
    this.rl.on("line", (line) => this.handleLine(line));
    this.child.on("exit", (code, signal) => {
      console.error(`[host] exited code=${code} signal=${signal}`);
      const error = new Error(`Host exited (code=${code ?? "n/a"} signal=${signal ?? "n/a"})`);
      for (const [, request] of this.pending) request.reject(error);
      this.pending.clear();
      this.rl?.close();
      this.rl = null;
      this.child = null;
    });
  }
  get isRunning() {
    return this.child !== null && !this.child.killed;
  }
  invoke(method, params = {}) {
    if (!this.child || !this.child.stdin.writable) {
      return Promise.reject(new Error("Host is not running"));
    }
    const id = this.nextId++;
    const promise = new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
    });
    this.child.stdin.write(`${JSON.stringify({ id, method, params })}
`);
    return promise;
  }
  stop() {
    const child = this.child;
    if (!child || child.killed) return Promise.resolve();
    return new Promise((resolve) => {
      const timer = setTimeout(() => {
        child.kill();
        resolve();
      }, 5e3);
      child.once("exit", () => {
        clearTimeout(timer);
        resolve();
      });
      child.stdin.write(`${JSON.stringify({ id: this.nextId++, method: "app.quit", params: {} })}
`);
    });
  }
  handleLine(line) {
    let message;
    try {
      message = JSON.parse(line);
    } catch {
      return;
    }
    if (!message || typeof message !== "object") return;
    if (typeof message.event === "string") {
      const set = this.listeners.get(message.event);
      if (set) {
        for (const callback of set) {
          try {
            callback(message.data);
          } catch (error) {
            console.error(`[host] event handler failed: ${error}`);
          }
        }
      }
      return;
    }
    if (typeof message.id === "number") {
      const request = this.pending.get(message.id);
      if (!request) return;
      this.pending.delete(message.id);
      if (message.error) {
        request.reject(new Error(message.error.message ?? `Host error ${message.error.code}`));
      } else {
        request.resolve(message.result);
      }
    }
  }
}
const hostClient = new HostClient();
let getMainWindow = () => null;
function setMainWindowRef(getter) {
  getMainWindow = getter;
}
function initSingleInstance() {
  if (!electron.app.requestSingleInstanceLock()) {
    electron.app.quit();
    return false;
  }
  electron.app.on("second-instance", () => {
    const window = getMainWindow();
    if (!window || window.isDestroyed()) return;
    if (window.isMinimized()) window.restore();
    window.show();
    window.focus();
  });
  return true;
}
let tray = null;
const TRAY_ICON_DATA_URL = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADeSURBVDhPtZIrEsIwEIYrkUgkEomDJqI9AhKJRHKDNFvBETgCR0AiOQKyEtkZpruRZdJ2OnSTPhD8M5/a/feRbBD8U6F6r2RqYqGKNY/1KlLlTIA5C0AjgcoOGq+RKhbc08p2kkBPx/iFAHrZqbi36iwBM27wIYByZxIJdOGJQwhNt053784jbJRZVgWa3Z2EMULAXV1A45EHJ6ExqfdPTewEJyCgODRvkM95cAqdAxNAd54wDGat2cqe7U8/4TumEGg/qYjGE/e2qiehh2MaOmOfpMJt9b0aE9uxz/gBIPu9+GgGzAUAAAAASUVORK5CYII=";
function showWindow(window) {
  if (!window || window.isDestroyed()) return;
  if (window.isMinimized()) window.restore();
  window.show();
  window.focus();
}
function toggleWindow(window) {
  if (!window || window.isDestroyed()) return;
  if (window.isVisible() && !window.isMinimized()) {
    window.hide();
  } else {
    showWindow(window);
  }
}
function initTray(getWindow) {
  if (tray) return;
  const icon = electron.nativeImage.createFromDataURL(TRAY_ICON_DATA_URL);
  tray = new electron.Tray(icon);
  tray.setToolTip("Universal Device Toolkit");
  tray.setContextMenu(
    electron.Menu.buildFromTemplate([
      { label: "显示 / 隐藏", click: () => toggleWindow(getWindow()) },
      { type: "separator" },
      { label: "退出", click: () => electron.app.quit() }
    ])
  );
  tray.on("double-click", () => showWindow(getWindow()));
}
function destroyTray() {
  if (!tray) return;
  tray.destroy();
  tray = null;
}
const OSD_WIDTH = 320;
const OSD_HEIGHT = 96;
let osdWindow = null;
let unsubscribe = null;
function buildOsdUrl(state) {
  const time = (/* @__PURE__ */ new Date()).toLocaleTimeString();
  const html = [
    "<!DOCTYPE html>",
    "<html>",
    "<head>",
    '<meta charset="utf-8">',
    "<style>",
    'html,body{margin:0;padding:0;background:transparent;overflow:hidden;font-family:"Segoe UI",system-ui,sans-serif;}',
    "body{height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;}",
    ".title{font-size:20px;font-weight:600;color:#ffffff;}",
    ".meta{font-size:12px;color:rgba(255,255,255,0.75);margin-top:4px;}",
    "</style>",
    "</head>",
    `<body><div class="title">OSD</div><div class="meta">${state} · ${time}</div></body>`,
    "</html>"
  ].join("");
  return `data:text/html;charset=utf-8,${encodeURIComponent(html)}`;
}
function positionAtBottomRight(window) {
  const { workArea } = electron.screen.getPrimaryDisplay();
  const [width, height] = window.getSize();
  window.setPosition(
    workArea.x + workArea.width - width - 16,
    workArea.y + workArea.height - height - 16
  );
}
function showOsd(state) {
  const window = osdWindow;
  if (!window || window.isDestroyed()) return;
  void window.loadURL(buildOsdUrl(state)).then(() => {
    if (!window.isDestroyed()) {
      positionAtBottomRight(window);
      window.show();
    }
  });
}
function handleOsdChanged(data) {
  const state = data?.state;
  if (state === "Hidden") {
    osdWindow?.hide();
  } else if (state === "Toggle") {
    if (osdWindow?.isVisible()) {
      osdWindow.hide();
    } else {
      showOsd("Toggle");
    }
  } else if (state === "Show") {
    showOsd("Show");
  }
}
function initOsdWindow() {
  if (osdWindow && !osdWindow.isDestroyed()) return;
  osdWindow = new electron.BrowserWindow({
    width: OSD_WIDTH,
    height: OSD_HEIGHT,
    show: false,
    frame: false,
    transparent: true,
    backgroundColor: "#00000000",
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: false,
    hasShadow: false,
    webPreferences: {
      sandbox: true
    }
  });
  osdWindow.on("closed", () => {
    osdWindow = null;
  });
  if (!unsubscribe) {
    unsubscribe = hostClient.on("osd.changed", handleOsdChanged);
  }
}
function destroyOsdWindow() {
  unsubscribe?.();
  unsubscribe = null;
  if (osdWindow && !osdWindow.isDestroyed()) {
    osdWindow.destroy();
  }
  osdWindow = null;
}
if (!initSingleInstance()) {
  electron.app.exit(0);
}
let mainWindow = null;
let isQuitting = false;
function resolveHostPath() {
  const fromEnv = process.env["UDT_HOST_PATH"];
  if (fromEnv) return fromEnv;
  const projectRoot = path.join(__dirname, "..", "..");
  const candidates = [
    // packaged: Host copied into resources/host by electron-builder
    path.join(process.resourcesPath ?? "", "host", "UniversalDeviceToolkit.Host.exe"),
    // dev: sibling repo folder next to the Electron project
    path.join(
      projectRoot,
      "..",
      "UniversalDeviceToolkit.Host",
      "bin",
      "x64",
      "Debug",
      "net10.0-windows10.0.26100.0",
      "win-x64",
      "UniversalDeviceToolkit.Host.exe"
    ),
    // dev: Release build
    path.join(
      projectRoot,
      "..",
      "UniversalDeviceToolkit.Host",
      "bin",
      "x64",
      "Release",
      "net10.0-windows10.0.26100.0",
      "win-x64",
      "UniversalDeviceToolkit.Host.exe"
    ),
    // fallback: explicit build output inside this project
    path.join(projectRoot, "host", "UniversalDeviceToolkit.Host.exe")
  ];
  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) return candidate;
  }
  return candidates[0];
}
function forwardHostEvents(window) {
  for (const event of ["host.ready", "host.initialized", "host.log", "notifications.changed"]) {
    hostClient.on(event, (data) => {
      if (!window.isDestroyed()) {
        window.webContents.send("bridge:event", event, data);
      }
    });
  }
}
async function shouldMinimizeToTray(keys) {
  try {
    const result = await hostClient.invoke("settings.get", { scope: "application" });
    return keys.some((key) => result?.value?.[key] === true);
  } catch (error) {
    console.error("[main] failed to read settings:", error);
    return false;
  }
}
function createWindow() {
  mainWindow = new electron.BrowserWindow({
    width: 1200,
    height: 800,
    show: false,
    autoHideMenuBar: true,
    frame: false,
    webPreferences: {
      preload: path.join(__dirname, "../preload/index.js"),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  });
  mainWindow.on("ready-to-show", () => {
    mainWindow?.show();
  });
  mainWindow.on("close", (event) => {
    if (isQuitting) return;
    void shouldMinimizeToTray(["MinimizeOnClose", "MinimizeToTray"]).then((toTray) => {
      if (!toTray || !mainWindow || mainWindow.isDestroyed()) return;
      event.preventDefault();
      mainWindow.hide();
    });
  });
  mainWindow.on("minimize", () => {
    void shouldMinimizeToTray(["MinimizeToTray"]).then((toTray) => {
      if (!toTray || !mainWindow || mainWindow.isDestroyed()) return;
      mainWindow.hide();
    });
  });
  mainWindow.on("maximize", () => {
    const win = mainWindow;
    if (win && !win.isDestroyed()) {
      win.webContents.send("window:maximized-changed", true);
    }
  });
  mainWindow.on("unmaximize", () => {
    const win = mainWindow;
    if (win && !win.isDestroyed()) {
      win.webContents.send("window:maximized-changed", false);
    }
  });
  mainWindow.on("closed", () => {
    mainWindow = null;
    destroyTray();
    destroyOsdWindow();
  });
  if (!electron.app.isPackaged && process.env["ELECTRON_RENDERER_URL"]) {
    mainWindow.loadURL(process.env["ELECTRON_RENDERER_URL"]);
  } else {
    mainWindow.loadFile(path.join(__dirname, "../renderer/index.html"));
  }
}
function startHost() {
  const hostPath = resolveHostPath();
  console.log(`[main] starting host: ${hostPath}`);
  hostClient.start(hostPath);
}
electron.app.whenReady().then(() => {
  console.log("[main] app ready");
  electron.ipcMain.handle(
    "bridge:invoke",
    (_event, method, params) => hostClient.invoke(method, params)
  );
  electron.ipcMain.on("window:minimize", () => mainWindow?.minimize());
  electron.ipcMain.on("window:maximize-toggle", () => {
    if (!mainWindow) return;
    if (mainWindow.isMaximized()) {
      mainWindow.unmaximize();
    } else {
      mainWindow.maximize();
    }
  });
  electron.ipcMain.on("window:close", () => {
    if (isQuitting || !mainWindow) {
      mainWindow?.close();
      return;
    }
    void shouldMinimizeToTray(["MinimizeOnClose", "MinimizeToTray"]).then((toTray) => {
      if (!mainWindow || mainWindow.isDestroyed()) return;
      if (toTray) {
        mainWindow.hide();
      } else {
        mainWindow.close();
      }
    });
  });
  electron.ipcMain.handle("window:is-maximized", () => mainWindow?.isMaximized() ?? false);
  startHost();
  createWindow();
  setMainWindowRef(() => mainWindow);
  initTray(() => mainWindow);
  initOsdWindow();
  if (mainWindow) forwardHostEvents(mainWindow);
  electron.app.on("activate", () => {
    if (electron.BrowserWindow.getAllWindows().length === 0) {
      createWindow();
      initTray(() => mainWindow);
      initOsdWindow();
    }
  });
});
electron.app.on("window-all-closed", () => {
  console.log("[main] window-all-closed");
  if (process.platform !== "darwin") {
    electron.app.quit();
  }
});
electron.app.on("before-quit", (event) => {
  console.log("[main] before-quit, host running:", hostClient.isRunning);
  isQuitting = true;
  if (!hostClient.isRunning) return;
  event.preventDefault();
  void hostClient.stop().finally(() => {
    console.log("[main] host stopped, quitting");
    electron.app.quit();
  });
});
process.on("uncaughtException", (error) => {
  console.error("[main] uncaughtException:", error);
});
