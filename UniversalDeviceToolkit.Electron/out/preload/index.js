"use strict";
const electron = require("electron");
const bridge = {
  invoke: (method, params) => electron.ipcRenderer.invoke("bridge:invoke", method, params),
  on: (event, callback) => {
    const listener = (_event, receivedEvent, data) => {
      if (receivedEvent === event) callback(data);
    };
    electron.ipcRenderer.on("bridge:event", listener);
    return () => electron.ipcRenderer.removeListener("bridge:event", listener);
  },
  minimize: () => electron.ipcRenderer.send("window:minimize"),
  maximizeToggle: () => electron.ipcRenderer.send("window:maximize-toggle"),
  closeWindow: () => electron.ipcRenderer.send("window:close"),
  isMaximized: () => electron.ipcRenderer.invoke("window:is-maximized"),
  onMaximizedChanged: (callback) => {
    const listener = (_event, maximized) => {
      callback(maximized);
    };
    electron.ipcRenderer.on("window:maximized-changed", listener);
    return () => electron.ipcRenderer.removeListener("window:maximized-changed", listener);
  }
};
electron.contextBridge.exposeInMainWorld("bridge", bridge);
