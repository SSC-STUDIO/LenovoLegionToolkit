export default {
  translation: {
    app: {
      name: 'Universal Device Toolkit'
    },
    nav: {
      dashboard: 'Dashboard',
      settings: 'Settings',
      automation: 'Automation',
      keyboardBacklight: 'Keyboard Backlight',
      macro: 'Macro',
      windowsOptimization: 'Windows Optimization',
      pluginExtensions: 'Plugins & Extensions',
      about: 'About'
    },
    home: {
      title: 'UDT Electron',
      subtitle: 'Universal Device Toolkit · Electron client'
    },
    dashboard: {
      title: 'Dashboard',
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Memory',
      temperature: 'Temperature',
      usage: 'Usage',
      power: 'Power',
      fanSpeed: 'Fan Speed',
      vram: 'VRAM',
      memoryUsed: 'Memory Used',
      memoryTotal: 'Memory Total',
      storageTemp: 'Storage Temp',
      notAvailable: '--',
      group: {
        power: 'Power',
        graphics: 'Graphics',
        display: 'Display',
        other: 'Other',
        custom: 'Custom'
      },
      card: {
        error: 'Failed to apply setting'
      }
    },
    feature: {
      powerMode: 'Power Mode',
      battery: 'Battery Mode',
      batteryNightCharge: 'Battery Night Charging',
      alwaysOnUsb: 'Always-On USB',
      instantBoot: 'Instant Boot',
      flipToStart: 'Flip to Start',
      fnLock: 'Fn Lock',
      gSync: 'GSync',
      hdr: 'HDR',
      hybridMode: 'Hybrid Mode',
      igpuMode: 'Discrete GPU Mode',
      itsMode: 'ITS Mode',
      microphone: 'Microphone',
      overDrive: 'Overclock',
      panelLogo: 'Panel Logo',
      portsBacklight: 'Ports Backlight',
      refreshRate: 'Refresh Rate',
      resolution: 'Resolution',
      dpiScale: 'DPI Scale',
      speaker: 'Speaker',
      touchpadLock: 'Touchpad Lock',
      whiteKeyboard: 'White Keyboard Backlight',
      winKey: 'Windows Key Lock',
      oneLevelWhiteKeyboard: 'One-Level White Backlight'
    },
    common: {
      loading: 'Loading…',
      error: 'Something went wrong',
      retry: 'Retry'
    },
    pages: {
      placeholder: 'Coming soon'
    },
    settings: {
      title: 'Settings',
      nav: {
        appearance: 'Appearance',
        application: 'Application',
        power: 'Power',
        display: 'Display',
        smartKeys: 'Smart Keys',
        update: 'Update',
        integrations: 'Integrations'
      },
      appearance: {
        language: 'Language',
        temperatureUnit: 'Temperature Unit',
        theme: 'Theme',
        accentColor: 'Accent Color',
        appScale: 'UI Scale',
        themeOptions: {
          system: 'System',
          light: 'Light',
          dark: 'Dark'
        }
      },
      application: {
        minimizeToTray: 'Minimize to Tray',
        minimizeToTrayDesc: 'Hide to the system tray instead of the taskbar when minimized',
        minimizeOnClose: 'Minimize on Close',
        minimizeOnCloseDesc: 'Minimize to the system tray instead of quitting when the window is closed',
        disableUnsupportedWarning: 'Disable Unsupported Hardware Warning',
        disableUnsupportedWarningDesc: 'Hide warnings about unsupported hardware',
        enableHardwareSensors: 'Enable Hardware Sensors',
        enableHardwareSensorsDesc: 'Collect hardware sensor data such as temperature and power',
        dontShowNotifications: "Don't Show Notifications",
        dontShowNotificationsDesc: 'Disable in-app and system notifications',
        extensionsEnabled: 'Enable Extensions',
        extensionsEnabledDesc: 'Enable loading of plugins and extensions',
        valueOn: 'On',
        valueOff: 'Off'
      },
      saved: 'Settings saved',
      saveFailed: 'Failed to save settings',
      power: {
        powerModeMapping: 'Power Mode Mapping',
        mappingModes: {
          disabled: 'Disabled',
          windowsPowerMode: 'Windows Power Mode',
          windowsPowerPlan: 'Windows Power Plan'
        },
        powerModes: 'Windows Power Mode Mapping',
        powerModesHint: 'Maps device power modes to Windows power modes. Current mapping is shown below (read-only).',
        powerModesEmpty: 'No power mode mapping',
        powerModeStates: {
          quiet: 'Quiet',
          balance: 'Balance',
          performance: 'Performance',
          extreme: 'Extreme',
          godMode: 'Custom'
        },
        windowsPowerModes: {
          bestPowerEfficiency: 'Best power efficiency',
          balanced: 'Balanced',
          bestPerformance: 'Best performance'
        },
        synchronizeBrightness: 'Synchronize brightness to all power plans',
        smartFnLock: 'Smart Fn Lock Modifier Keys',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        }
      },
      display: {
        navigationItems: 'Navigation Items Visibility',
        navigationKeys: {
          keyboard: 'Keyboard Backlight',
          battery: 'Battery',
          automation: 'Automation',
          macro: 'Macro',
          windowsOptimization: 'Windows Optimization',
          pluginExtensions: 'Plugins & Extensions',
          about: 'About'
        },
        notificationPosition: 'Notification Position',
        notificationPositions: {
          bottomRight: 'Bottom Right',
          bottomCenter: 'Bottom Center',
          bottomLeft: 'Bottom Left',
          centerLeft: 'Center Left',
          topLeft: 'Top Left',
          topCenter: 'Top Center',
          topRight: 'Top Right',
          centerRight: 'Center Right',
          center: 'Center'
        },
        notificationDuration: 'Notification Duration',
        notificationDurations: {
          short: 'Short',
          normal: 'Normal',
          long: 'Long'
        },
        excludedRefreshRates: 'Excluded Refresh Rates',
        excludedRefreshRatesHint: 'Advanced editing will be available in a future version',
        excludedRefreshRatesEmpty: 'No excluded refresh rates'
      },
      smartKeys: {
        title: 'Smart Keys',
        description: 'Configure Smart Keys behavior, including the Smart Fn Lock modifier keys.',
        smartFnLock: 'Current Smart Fn Lock Modifier Keys',
        off: 'Off',
        hint: 'Smart Fn Lock modifier keys can be changed in the Power settings.'
      },
      update: {
        frequency: 'Update Check Frequency',
        frequencies: {
          perHour: 'Every hour',
          perThreeHours: 'Every 3 hours',
          perTwelveHours: 'Every 12 hours',
          perDay: 'Every day',
          perWeek: 'Every week',
          perMonth: 'Every month'
        },
        includePrerelease: 'Include prerelease updates',
        check: 'Check for Updates',
        comingSoon: 'Update check will be available in a future version'
      },
      checkResult: {
        available: 'New version available: v{{version}}',
        latest: 'You are up to date'
      },
      integrations: {
        hwinfo: 'HWiNFO',
        cli: 'CLI'
      }
    },
    keyboard: {
      title: 'Keyboard Backlight',
      unsupported: 'Keyboard backlight is not supported on this device',
      rgb: {
        preset: 'Preset',
        settings: 'Backlight Settings',
        effect: 'Effect',
        speed: 'Speed',
        brightness: 'Brightness',
        zones: 'Zone Colors',
        presets: {
          off: 'Off',
          one: 'Preset 1',
          two: 'Preset 2',
          three: 'Preset 3',
          four: 'Preset 4'
        },
        effectOptions: {
          static: 'Static',
          breath: 'Breath',
          smooth: 'Smooth',
          waveRtl: 'Wave (RTL)',
          waveLtr: 'Wave (LTR)'
        },
        speedOptions: {
          slowest: 'Slowest',
          slow: 'Slow',
          fast: 'Fast',
          fastest: 'Fastest'
        },
        brightnessOptions: {
          low: 'Low',
          high: 'High'
        }
      },
      spectrum: {
        brightness: 'Brightness',
        profile: 'Profile',
        logo: 'Logo Light',
        effects: 'Effects',
        colors: 'Colors',
        addEffect: 'Add Effect',
        deleteEffect: 'Delete',
        noEffects: 'No effects',
        effectTypes: {
          always: 'Always',
          rainbowScrew: 'Rainbow Screw',
          rainbowWave: 'Rainbow Wave',
          colorChange: 'Color Change',
          colorWave: 'Color Wave',
          colorPulse: 'Color Pulse',
          smooth: 'Smooth',
          rain: 'Rain',
          ripple: 'Ripple',
          type: 'Type',
          audioBounce: 'Audio Bounce',
          audioRipple: 'Audio Ripple',
          auroraSync: 'Aurora Sync'
        }
      }
    },
    automation: {
      title: 'Automation',
      enable: 'Enable automation',
      empty: 'No pipelines yet',
      runNow: 'Run Now',
      delete: 'Delete',
      deleteStep: 'Delete step',
      addPipeline: 'Add Pipeline',
      addStep: 'Add Step',
      stepType: 'Step type',
      steps: 'Steps',
      save: 'Save',
      revert: 'Revert',
      pipelineName: 'Pipeline name',
      pipelineNamePlaceholder: 'Enter pipeline name',
      quickAction: 'Quick action'
    },
    macro: {
      title: 'Macro',
      enable: 'Enable macros',
      numpad: 'Numpad',
      sequence: 'Sequence',
      repeat: 'Repeat count',
      events: 'Events',
      save: 'Save',
      clear: 'Clear',
      play: 'Play',
      empty: 'No macro sequence for this key yet'
    },
    plugins: {
      title: 'Plugins & Extensions',
      search: 'Search plugins',
      filterAll: 'All',
      filterInstalled: 'Installed',
      filterNotInstalled: 'Not Installed',
      refresh: 'Refresh',
      total: '{{count}} total',
      summary: '{{count}} installed',
      updatable: '{{count}} update(s) available',
      install: 'Install',
      update: 'Update',
      updateAvailable: 'Update Available',
      uninstall: 'Uninstall',
      uninstallConfirm: 'Uninstall this plugin?',
      uninstallFailed: 'Failed to uninstall',
      installed: 'Installed',
      online: 'Online',
      installing: 'Installing…',
      offline: 'Online store is unavailable; showing locally installed plugins only',
      empty: 'No plugins found',
      dependencies: 'Dependencies',
      dependenciesBlocked: 'This plugin has unsatisfied dependencies and cannot be uninstalled',
      details: 'Details',
      usageGuide: 'Usage Guide',
      changelog: 'Changelog'
    },
    optimization: {
      title: 'Windows Optimization',
      tabs: {
        optimization: 'Optimization',
        cleanup: 'Cleanup',
        driverDownload: 'Driver Download',
        networkAcceleration: 'Network Acceleration'
      },
      recommended: 'Recommended',
      selected: 'Selected',
      selectedActions: 'Selected Actions',
      noSelection: 'No actions selected',
      selectRecommended: 'Select Recommended',
      applyRecommended: 'Apply All Recommended',
      apply: 'Apply',
      clear: 'Clear (Revert)',
      applied: 'Applied',
      applyFailed: 'Failed to apply (administrator rights may be required)',
      reverted: 'Reverted',
      revertFailed: 'Failed to revert (administrator rights may be required)',
      estimate: 'Estimate Size',
      estimateResult: 'Reclaimable space',
      runCleanup: 'Run Cleanup',
      cleanupHint: 'Cleanup runs your custom cleanup rules.',
      cleanupConfirm: 'Run cleanup now?',
      cleanupDone: 'Cleanup finished',
      cleanupFailed: 'Cleanup failed',
      network: {
        status: 'Status',
        running: 'Running',
        stopped: 'Stopped',
        backendReady: 'Backend ready',
        backendNotReady: 'Backend not ready',
        config: 'Basic Config',
        accelerationEnabled: 'Enable acceleration',
        mode: 'Mode',
        modes: {
          off: 'Off',
          systemProxy: 'System Proxy',
          hosts: 'Hosts',
          diagnosticsOnly: 'Diagnostics Only'
        },
        save: 'Save Config',
        saved: 'Config saved',
        saveFailed: 'Failed to save config',
        start: 'Start',
        stop: 'Stop',
        startFailed: 'Failed to start',
        stopFailed: 'Failed to stop'
      },
      driverDownload: {
        comingSoon: 'Driver download will be available in a future version'
      }
    },
    about: {
      title: 'About',
      appName: 'Application',
      version: 'Version',
      pid: 'Process ID',
      machine: 'Device model',
      bios: 'BIOS version',
      compatible: 'Compatibility',
      yes: 'Compatible',
      no: 'Not compatible',
      dataFolder: 'Data folder',
      thirdParty: 'Third-party libraries',
      copyright: 'Copyright'
    }
  }
}
