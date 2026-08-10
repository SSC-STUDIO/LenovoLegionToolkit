export default {
  translation: {
    app: {
      name: '通用设备工具箱'
    },
    nav: {
      dashboard: '仪表盘',
      settings: '设置',
      automation: '自动化',
      keyboardBacklight: '键盘背光',
      macro: '宏',
      windowsOptimization: 'Windows 优化',
      pluginExtensions: '插件扩展',
      about: '关于'
    },
    home: {
      title: 'UDT Electron',
      subtitle: '通用设备工具箱 · Electron 客户端'
    },
    dashboard: {
      title: '仪表盘',
      cpu: 'CPU',
      gpu: 'GPU',
      memory: '内存',
      temperature: '温度',
      usage: '使用率',
      power: '功耗',
      fanSpeed: '风扇转速',
      vram: '显存',
      memoryUsed: '内存已用',
      memoryTotal: '内存总量',
      storageTemp: '存储温度',
      notAvailable: '--',
      group: {
        power: '电源',
        graphics: '显卡',
        display: '显示',
        other: '其他',
        custom: '自定义'
      },
      card: {
        error: '设置失败'
      }
    },
    feature: {
      powerMode: '电源模式',
      battery: '电池模式',
      batteryNightCharge: '电池夜间充电',
      alwaysOnUsb: '常开USB',
      instantBoot: '即时启动',
      flipToStart: '翻转到启动',
      fnLock: '功能键锁',
      gSync: 'GSync',
      hdr: 'HDR',
      hybridMode: '混合模式',
      igpuMode: '独显直连模式',
      itsMode: 'ITS模式',
      microphone: '麦克风',
      overDrive: '超频',
      panelLogo: '面板Logo',
      portsBacklight: '接口背光',
      refreshRate: '刷新率',
      resolution: '分辨率',
      dpiScale: 'DPI缩放',
      speaker: '扬声器',
      touchpadLock: '触摸板锁',
      whiteKeyboard: '白色键盘背光',
      winKey: 'Windows键锁',
      oneLevelWhiteKeyboard: '单级白色背光'
    },
    common: {
      loading: '加载中…',
      error: '出错了',
      retry: '重试'
    },
    pages: {
      placeholder: '该功能即将推出'
    },
    settings: {
      title: '设置',
      nav: {
        appearance: '外观',
        application: '应用行为',
        power: '电源',
        display: '显示',
        smartKeys: '智能键',
        update: '更新',
        integrations: '集成'
      },
      appearance: {
        language: '语言',
        temperatureUnit: '温度单位',
        theme: '主题',
        accentColor: '强调色',
        appScale: 'UI 缩放',
        themeOptions: {
          system: '跟随系统',
          light: '浅色',
          dark: '深色'
        }
      },
      application: {
        minimizeToTray: '最小化到托盘',
        minimizeToTrayDesc: '最小化时隐藏到系统托盘而不是任务栏',
        minimizeOnClose: '关闭时最小化到托盘',
        minimizeOnCloseDesc: '点击关闭按钮时最小化到托盘而不是退出程序',
        disableUnsupportedWarning: '禁用不受支持硬件警告',
        disableUnsupportedWarningDesc: '隐藏不受支持硬件的警告提示',
        enableHardwareSensors: '启用硬件传感器',
        enableHardwareSensorsDesc: '启用温度、功耗等硬件传感器数据采集',
        dontShowNotifications: '不显示通知',
        dontShowNotificationsDesc: '关闭应用内与系统通知',
        extensionsEnabled: '启用扩展',
        extensionsEnabledDesc: '启用插件与扩展加载',
        valueOn: '开',
        valueOff: '关'
      },
      saved: '设置已保存',
      saveFailed: '保存设置失败',
      power: {
        powerModeMapping: '电源模式映射',
        mappingModes: {
          disabled: '已禁用',
          windowsPowerMode: 'Windows 电源模式',
          windowsPowerPlan: 'Windows 电源计划'
        },
        powerModes: 'Windows 电源模式映射',
        powerModesHint: '将设备电源模式映射到对应的 Windows 电源模式，以下为当前映射（只读）。',
        powerModesEmpty: '暂无电源模式映射',
        powerModeStates: {
          quiet: '安静',
          balance: '平衡',
          performance: '性能',
          extreme: '极限',
          godMode: '自定义'
        },
        windowsPowerModes: {
          bestPowerEfficiency: '最佳能效',
          balanced: '平衡',
          bestPerformance: '最佳性能'
        },
        synchronizeBrightness: '同步亮度到所有电源计划',
        smartFnLock: 'Smart Fn Lock 修饰键',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        }
      },
      display: {
        navigationItems: '导航项可见性',
        navigationKeys: {
          keyboard: '键盘背光',
          battery: '电池',
          automation: '自动化',
          macro: '宏',
          windowsOptimization: 'Windows 优化',
          pluginExtensions: '插件扩展',
          about: '关于'
        },
        notificationPosition: '通知位置',
        notificationPositions: {
          bottomRight: '右下',
          bottomCenter: '底部居中',
          bottomLeft: '左下',
          centerLeft: '中部偏左',
          topLeft: '左上',
          topCenter: '顶部居中',
          topRight: '右上',
          centerRight: '中部偏右',
          center: '居中'
        },
        notificationDuration: '通知时长',
        notificationDurations: {
          short: '短',
          normal: '普通',
          long: '长'
        },
        excludedRefreshRates: '排除的刷新率',
        excludedRefreshRatesHint: '高级编辑将在后续版本中提供',
        excludedRefreshRatesEmpty: '暂无排除的刷新率'
      },
      smartKeys: {
        title: 'Smart Keys',
        description: '配置 Smart Keys 相关行为，包括 Smart Fn Lock 修饰键。',
        smartFnLock: '当前 Smart Fn Lock 修饰键',
        off: '关闭',
        hint: '可在“电源”设置中修改 Smart Fn Lock 修饰键。'
      },
      update: {
        frequency: '更新检查频率',
        frequencies: {
          perHour: '每小时',
          perThreeHours: '每 3 小时',
          perTwelveHours: '每 12 小时',
          perDay: '每天',
          perWeek: '每周',
          perMonth: '每月'
        },
        includePrerelease: '包含预发布版本',
        check: '检查更新',
        comingSoon: '检查更新将在后续版本中接入'
      },
      integrations: {
        hwinfo: 'HWiNFO',
        cli: 'CLI'
      }
    },
    automation: {
      title: '自动化',
      enable: '启用自动化',
      empty: '暂无管线',
      runNow: '立即运行',
      delete: '删除',
      deleteStep: '删除步骤',
      addPipeline: '添加管线',
      addStep: '添加步骤',
      stepType: '步骤类型',
      steps: '步骤',
      save: '保存',
      revert: '还原',
      pipelineName: '管线名称',
      pipelineNamePlaceholder: '输入管线名称',
      quickAction: '快速操作'
    },
    macro: {
      title: '宏',
      enable: '启用宏',
      numpad: '数字小键盘',
      sequence: '序列',
      repeat: '重复次数',
      events: '事件数',
      save: '保存',
      clear: '清空',
      play: '播放',
      empty: '此键尚无宏序列'
    },
    about: {
      title: '关于',
      appName: '应用名称',
      version: '版本',
      pid: '进程 ID',
      machine: '设备型号',
      bios: 'BIOS 版本',
      compatible: '兼容性检查',
      yes: '兼容',
      no: '不兼容',
      dataFolder: '数据目录',
      thirdParty: '第三方库',
      copyright: '版权所有'
    }
  }
}
