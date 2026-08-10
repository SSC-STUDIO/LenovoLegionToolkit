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
let mainWindow = null;
function resolveHostPath() {
  const fromEnv = process.env["UDT_HOST_PATH"];
  if (fromEnv) return fromEnv;
  const projectRoot = path.join(__dirname, "..", "..");
  const candidates = [
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
    // fallback: explicit build output inside this project
    path.join(projectRoot, "host", "UniversalDeviceToolkit.Host.exe")
  ];
  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) return candidate;
  }
  return candidates[0];
}
function forwardHostEvents(window) {
  for (const event of ["host.ready", "host.initialized", "host.log"]) {
    hostClient.on(event, (data) => {
      if (!window.isDestroyed()) {
        window.webContents.send("bridge:event", event, data);
      }
    });
  }
}
function createWindow() {
  mainWindow = new electron.BrowserWindow({
    width: 1200,
    height: 800,
    show: false,
    autoHideMenuBar: true,
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
  mainWindow.on("closed", () => {
    mainWindow = null;
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
  startHost();
  createWindow();
  if (mainWindow) forwardHostEvents(mainWindow);
  electron.app.on("activate", () => {
    if (electron.BrowserWindow.getAllWindows().length === 0) createWindow();
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
