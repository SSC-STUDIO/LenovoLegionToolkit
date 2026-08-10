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
  }
};
electron.contextBridge.exposeInMainWorld("bridge", bridge);
