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
      checkResult: {
        available: '发现新版本 v{{version}}',
        latest: '已是最新版本'
      },
      integrations: {
        hwinfo: 'HWiNFO',
        cli: 'CLI'
      }
    },
    keyboard: {
      title: '键盘背光',
      unsupported: '此设备不支持键盘背光控制',
      rgb: {
        preset: '预设',
        settings: '背光设置',
        effect: '效果',
        speed: '速度',
        brightness: '亮度',
        zones: '分区颜色',
        presets: {
          off: '关闭',
          one: '预设 1',
          two: '预设 2',
          three: '预设 3',
          four: '预设 4'
        },
        effectOptions: {
          static: '静态',
          breath: '呼吸',
          smooth: '流光',
          waveRtl: '波浪（右→左）',
          waveLtr: '波浪（左→右）'
        },
        speedOptions: {
          slowest: '最慢',
          slow: '慢',
          fast: '快',
          fastest: '最快'
        },
        brightnessOptions: {
          low: '低',
          high: '高'
        }
      },
      spectrum: {
        brightness: '亮度',
        profile: '配置文件',
        logo: 'Logo 灯',
        effects: '效果列表',
        colors: '颜色数量',
        addEffect: '添加效果',
        deleteEffect: '删除效果',
        noEffects: '暂无效果',
        effectTypes: {
          always: '常亮',
          rainbowScrew: '彩虹旋转',
          rainbowWave: '彩虹波浪',
          colorChange: '变色',
          colorWave: '彩色波浪',
          colorPulse: '彩色脉冲',
          smooth: '流光',
          rain: '雨滴',
          ripple: '涟漪',
          type: '打字',
          audioBounce: '音频跳动',
          audioRipple: '音频涟漪',
          auroraSync: 'Aurora 同步'
        }
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
    plugins: {
      title: '插件扩展',
      search: '搜索插件',
      filterAll: '全部',
      filterInstalled: '已安装',
      filterNotInstalled: '未安装',
      refresh: '刷新',
      total: '共 {{count}} 个',
      summary: '已安装 {{count}} 个',
      updatable: '{{count}} 个可更新',
      install: '安装',
      update: '更新',
      updateAvailable: '可更新',
      uninstall: '卸载',
      uninstallConfirm: '确定要卸载此插件吗？',
      uninstallFailed: '卸载失败',
      installed: '已安装',
      online: '在线',
      installing: '安装中…',
      offline: '在线商店不可用，当前仅显示本地已安装插件',
      empty: '暂无插件',
      dependencies: '依赖',
      dependenciesBlocked: '该插件存在未满足的依赖，无法卸载',
      details: '详情',
      usageGuide: '使用指南',
      changelog: '更新日志'
    },
    optimization: {
      title: 'Windows 优化',
      tabs: {
        optimization: '系统优化',
        cleanup: '空间清理',
        driverDownload: '驱动下载',
        networkAcceleration: '网络加速'
      },
      recommended: '推荐',
      selected: '已选',
      selectedActions: '选中的动作',
      noSelection: '未选择任何动作',
      selectRecommended: '选择推荐',
      applyRecommended: '应用全部推荐',
      apply: '应用',
      clear: '清除（还原）',
      applied: '已应用',
      applyFailed: '应用失败（可能需要管理员权限）',
      reverted: '已还原',
      revertFailed: '还原失败（可能需要管理员权限）',
      estimate: '估算大小',
      estimateResult: '可释放空间',
      runCleanup: '运行清理',
      cleanupHint: '运行清理将按自定义清理规则执行。',
      cleanupConfirm: '确定要运行清理吗？',
      cleanupDone: '清理完成',
      cleanupFailed: '清理失败',
      network: {
        status: '运行状态',
        running: '运行中',
        stopped: '未运行',
        backendReady: '后端就绪',
        backendNotReady: '后端未就绪',
        config: '基本配置',
        accelerationEnabled: '启用加速',
        mode: '加速模式',
        modes: {
          off: '关闭',
          systemProxy: '系统代理',
          hosts: 'Hosts 加速',
          diagnosticsOnly: '诊断模式'
        },
        save: '保存配置',
        saved: '配置已保存',
        saveFailed: '保存配置失败',
        start: '启动',
        stop: '停止',
        startFailed: '启动失败',
        stopFailed: '停止失败'
      },
      driverDownload: {
        comingSoon: '驱动下载将在后续版本中提供'
      }
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
