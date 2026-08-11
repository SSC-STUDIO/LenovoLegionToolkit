import legacy from './ja'

export default {
  translation: {
    ...legacy.translation,
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'ログ',
      openLogs: 'ログフォルダーを開く',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'デバイス情報'
    },
    nav: {
      dashboard: 'ホーム',
      settings: '設定',
      automation: '自動化',
      keyboard: 'キーボード',
      keyboardBacklight: 'キーボードバックライト',
      macro: 'カスタムマクロ',
      windowsOptimization: 'システム最適化',
      pluginExtensions: 'プラグインと拡張機能',
      about: '情報'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: 'ようこそ！以下のセクションから始めてください',
      hostReady: 'バックエンド接続済み',
      hostState: 'バックエンド状態',
      hostVersion: 'バックエンドバージョン',
      initComplete: '初期化完了',
      safeStart: 'セーフスタート、スキップ済み',
      machine: 'デバイス',
      compatible: '互換性',
      status: '状態'
    },
    dashboard: {
      title: 'ホーム',
      customize: 'カスタマイズ',
      edit: {
        title: 'ダッシュボードを編集',
        description: 'ホームページに表示するセクションと機能を選択します。',
        showSensors: 'ハードウェアセンサー',
        groups: '機能グループ',
        save: '保存',
        cancel: 'キャンセル',
        saved: 'ダッシュボードのレイアウトを保存しました',
        error: 'ダッシュボードのレイアウトを保存できませんでした',
        disclaimer: 'デバイスの状態や設定によっては、一部の機能がダッシュボードに表示されないことがあります。',
        addGroup: '追加',
        renameGroup: 'グループ名を編集',
        deleteGroup: '削除',
        moveUp: '上に移動',
        moveDown: '下に移動',
        deleteItem: '削除',
        addItem: '追加',
        groupNamePlaceholder: '名前',
        items: {
          discreteGpu: 'ディスクリートGPUモード',
          overclockGpu: 'GPUのオーバークロック',
          turnOffMonitors: 'モニターをオフにする'
        }
      },
      addItem: {
        title: '追加',
        searchPlaceholder: '検索',
        empty: 'すべてのダッシュボード項目が追加済みです',
        addHint: '項目を追加'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'メモリ',
      temperature: '温度',
      usage: '使用率',
      power: '消費電力',
      fanSpeed: 'ファン',
      vram: 'VRAM',
      memoryUsed: 'メモリ使用量',
      memoryTotal: 'メモリ合計',
      storageTemp: 'ストレージ温度',
      notAvailable: '--',
      sensor: {
        cpu: 'プロセッサー',
        gpu: 'グラフィックスカード',
        memory: 'メモリ',
        temperature: '温度',
        usage: '使用率',
        power: '消費電力',
        fanSpeed: 'ファン',
        vram: 'VRAM',
        frequency: 'コアクロック',
        battery: 'バッテリー',
        charge: '充電',
        health: '健全性',
        rate: 'レート',
        fan: 'ファン',
        lowPowerAdapter: '低電力アダプターが接続されています',
        batteryLow: 'バッテリー残量が少ない',
        acCharging: '電源アダプター接続、充電中...',
        acNotCharging: '電源アダプター接続、充電していません...',
        remainingTime: '推定残り時間: {0}',
        memoryTemperature: 'メモリ温度',
        ssdTemperature: 'SSD温度',
        vramTemperature: 'VRAM温度',
        vramUsage: 'VRAM使用率',
        cycles: 'サイクル',
        capacity: '容量',
        fullCapacity: '満充電容量',
        designCapacity: '設計容量',
        date: '日付',
        voltage: 'コア電圧',
        voltageRange: '電圧範囲',
        powerRange: '電力範囲',
        details: '詳細',
        refreshInterval: '更新間隔',
        detail: {
          power: '消費電力',
          powerCores: 'コア',
          powerMemory: 'メモリ',
          powerPlatform: 'プラットフォーム',
          pCoreClock: 'Pコアクロック',
          eCoreClock: 'Eコアクロック',
          memoryUsage: 'メモリ使用量',
          sharedMemoryUsage: '共有メモリ使用量',
          vramUsage: 'VRAM使用量',
          hotSpot: 'GPUホットスポット',
          pcieThroughput: 'PCIeスループット',
          designCapacity: '設計容量',
          fullChargeCapacity: '満充電容量'
        }
      },
      group: {
        power: '電源',
        graphics: 'グラフィックス',
        display: 'ディスプレイ',
        other: 'その他',
        custom: 'カスタム'
      },
      card: {
        error: '設定の適用に失敗しました',
        config: '詳細設定',
        configComingSoon: '詳細設定は今後のバージョンで利用可能になります'
      }
    },
    balanceMode: {
      title: 'バランスモード設定',
      aiEngine: 'AIエンジンを有効化',
      aiEngineDesc: '特定のゲーム実行中にCPU/GPU性能を自動調整します。温度とファン音が上昇する場合があります。'
    },
    godMode: {
      title: 'カスタムモード設定',
      activePreset: 'アクティブプリセット',
      presetName: 'プリセット名',
      name: '名前',
      errorLoad: '設定を読み込めませんでした。',
      errorApply: '設定を適用できませんでした',
      applySuccess: 'カスタムモードの設定を適用しました。',
      defaultPresetName: 'プリセット',
      cpu: {
        title: 'CPU',
        longTermPL: '長期電力制限',
        'longTermPL.desc': 'CPUが継続的に使用できる消費電力。',
        shortTermPL: '短期電力制限',
        'shortTermPL.desc': 'CPUが短時間に到達できるピーク消費電力。',
        peakPL: 'ピーク電力制限',
        'peakPL.desc': 'CPUが瞬間的に到達できる最大消費電力。',
        crossLoading: '長期電力制限（クロスローディング）',
        'crossLoading.desc': 'CPUとGPUが共に最大負荷時のCPU最大消費電力。',
        pl1Tau: '短期電力制限の持続時間',
        'pl1Tau.desc': 'CPUが短期電力制限でブーストできる時間。Tau経過後は長期制限が使われます。',
        apuSppt: 'APU sPPT電力制限',
        'apuSppt.desc': 'CPUが少し遅れて到達できるピーク消費電力。',
        tempLimit: 'CPU温度制限',
        'tempLimit.desc': '周波数と電力が下がる前のCPUの最大温度。'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'ダイナミックブースト',
        'dynamicBoost.desc': 'CPUの消費電力に基づいてGPUに割り当てられる追加電力。',
        ctgp: '設定可能なTGP',
        'ctgp.desc': '基本消費電力に加えてGPUに割り当てられる追加電力。',
        tempLimit: 'GPU温度制限',
        'tempLimit.desc': '周波数と電力が下がる前のGPUの最大温度。',
        totalProcessingPowerTarget: 'AC接続時の総プロセッサー電力ターゲット',
        'totalProcessingPowerTarget.desc': 'CPUがGPUの動的電力調整をトリガーするポイント。',
        toCpuDynamicBoost: 'GPUからCPUへのダイナミックブースト',
        'toCpuDynamicBoost.desc': 'CPU使用率に基づきGPUからCPUへ割り当てられる追加電力。値が大きいほどCPU性能が向上します。'
      },
      fans: {
        title: 'ファン',
        curve: 'ファンカーブ',
        curveMessage: 'ファン速度はCPU、GPU、ヒートシンクのうち最も高い温度センサー値に従います。各ステップにホバーすると正確な値を表示します。',
        maxSpeed: '最大ファン速度',
        maxSpeedWarning: 'このオプションを長時間使用するとファンの寿命が短くなります。\n本当に注意してください！'
      },
      advanced: {
        title: '詳細設定',
        message: '以下はよく理解していない限り変更しないでください。',
        maxOffset: '最大オフセット',
        maxOffsetWarning: '値を大きくすると予期しない動作をする場合があります。不明な場合は0のままにしてください。',
        minOffset: '最小オフセット',
        minOffsetWarning: '値を小さくすると予期しない動作をする場合があります。不明な場合は0のままにしてください。',
        invalidOffset: '保存する前に整数を入力してください。'
      },
      vantageWarning: 'Lenovo Vantageまたはそのサービスが実行中の場合、カスタムモード設定は正しく適用されません。',
      legionZoneWarning: 'Legion Zoneまたはそのサービスが実行中の場合、カスタムモード設定は正しく適用されません。'
    },
    overclock: {
      title: 'GPUオーバークロック設定',
      preset: 'プリセット',
      coreOffset: 'コア周波数オフセット',
      memoryOffset: 'メモリ周波数オフセット',
      namePlaceholder: '名前...',
      newProfileName: 'プリセット',
      loadError: 'オーバークロック設定を読み込めませんでした。'
    },
    feature: {
      powerMode: '電力モード',
      'powerMode.desc': 'パフォーマンスモードを変更します。\nFn+Qショートカットでも変更できます。',
      'powerMode.hint': 'Fn+Qショートカットですばやく変更できます。',
      'powerMode.warning': '電源アダプターが接続されていない場合、パフォーマンスモードが正しく動作しないことがあります。',
      battery: 'バッテリー充電モード',
      'battery.desc': 'バッテリー充電モードを選択します。コンサベーションモードは充電を制限して寿命を延ばし、急速充電モードは高出力で充電します。',
      batteryNightCharge: '夜間バッテリー充電',
      'batteryNightCharge.desc': '有効にすると、夜間は80%まで充電し、朝までに100%まで充電します。',
      alwaysOnUsb: '常時USB給電',
      'alwaysOnUsb.desc': 'PCがオフ、スリープ、休止状態の間もUSBポートへの給電を維持します。通常はバッテリーアイコン付きのUSBポートのみ対象です。',
      instantBoot: 'インスタントブート',
      'instantBoot.desc': '電源が接続されるとすぐにPCの電源が入ります。',
      flipToStart: '開いて起動',
      'flipToStart.desc': '蓋を開くと自動的にノートPCの電源が入ります。',
      fnLock: 'Fnロック',
      'fnLock.desc': '有効にすると、Fnを押さずに機能キーを使用できます。元のF1〜F12キーを使用するにはFnと一緒に押します。',
      gSync: 'G-Sync',
      'gSync.desc': 'G-Sync可変リフレッシュレートを有効または無効にします',
      hdr: 'HDR',
      'hdr.desc': '内蔵ディスプレイでHDRを有効にします。',
      'hdr.warning': 'Windows設定によりHDRの使用がブロックされています。',
      hybridMode: 'ハイブリッドモード',
      'hybridMode.desc': 'ハイブリッドモードでは内蔵GPUとディスクリートGPUを切り替えられます。オフにするとディスクリートGPU直結モードになります。切り替えには再起動が必要です。',
      igpuMode: 'ディスクリートGPUモード',
      'igpuMode.desc': '省電力のため内蔵グラフィックス出力を強制します',
      refreshRate: 'リフレッシュレート',
      'refreshRate.desc': '内蔵ディスプレイのリフレッシュレートを切り替えます。',
      itsMode: 'ITSモード',
      'itsMode.desc': 'インテリジェント冷却ソリューション',
      microphone: 'マイク',
      'microphone.desc': 'オフにすると利用可能なすべてのマイクがミュートされます。',
      overDrive: 'オーバードライブ',
      'overDrive.desc': '有効にすると内蔵ディスプレイの応答速度が向上します。オーバーシュートによるゴーストが発生する場合があります。',
      panelLogo: 'Legionロゴライト',
      'panelLogo.desc': 'デバイス背面のLegionロゴライトをオン/オフします。',
      portsBacklight: 'ポートバックライト',
      'portsBacklight.desc': 'デバイス背面のポートライトをオン/オフします。',
      resolution: '解像度',
      'resolution.desc': '内蔵ディスプレイの解像度を切り替えます。',
      dpiScale: 'DPIスケール',
      'dpiScale.desc': '内蔵ディスプレイのスケーリングを切り替えます。',
      speaker: 'スピーカー',
      touchpadLock: 'タッチパッドロック',
      'touchpadLock.desc': 'タッチパッドを無効にします。マウス使用時に誤操作を防ぐため推奨されます。',
      whiteKeyboard: 'キーボードバックライト',
      'whiteKeyboard.desc': 'Fn + スペースキーでバックライトの切り替えと明るさ調整ができます。',
      winKey: 'Windowsキーを無効化',
      'winKey.desc': '内蔵キーボードのみ対象。有効にするとWinキーが反応しなくなります。',
      oneLevelWhiteKeyboard: 'キーボードバックライト',
      'oneLevelWhiteKeyboard.desc': 'Fn + スペースキーでバックライトを切り替えられます。',
      'hybridMode.states.hybrid': 'ハイブリッド',
      'hybridMode.states.hybridIGPUOnly': 'ハイブリッド-iGPU',
      'hybridMode.states.hybridAuto': 'ハイブリッド-自動',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'GPU動作モードについて',
      'hybridMode.info.hybrid.title': 'ハイブリッドモード',
      'hybridMode.info.hybrid.message': '内蔵GPUとディスクリートGPUの両方が有効で、システムが必要に応じて自動的に切り替えます。',
      'hybridMode.info.hybridIgpu.title': 'ハイブリッド-iGPUのみモード',
      'hybridMode.info.hybridIgpu.message': '内蔵GPUのみ使用。消費電力と騒音を最小限に抑えます。',
      'hybridMode.info.hybridIgpu.disclaimer': 'このモードはディスクリートGPUが動作していない場合にのみ有効です。',
      'hybridMode.info.hybridAuto.title': 'ハイブリッド自動モード',
      'hybridMode.info.hybridAuto.message': 'バッテリー駆動時は内蔵GPUのみ、ACアダプター接続時は両方を使用します。非標準アダプター接続時はハイブリッド-iGPUのみモードに切り替わります。',
      'hybridMode.info.dgpu.title': 'dGPUモード',
      'hybridMode.info.dgpu.message': 'ディスクリートGPUのみ使用。最高のグラフィックス性能を提供しますが、消費電力が増加します。',
      'hybridMode.info.dgpu.disclaimer': 'このモードへの切り替えには再起動が必要です。',
      'hybridMode.restartRequired.title': '再起動が必要です',
      'hybridMode.restartRequired.message': '{{mode}}への変更には再起動が必要です。今すぐ再起動しますか？',
      'hybridMode.restartRequired.now': '今すぐ再起動',
      'hybridMode.restartRequired.later': '後で再起動する',
      'hybridMode.restartFailed': '自動的に再起動できませんでした。変更を完了するには手動で再起動してください。',
      'hybridMode.changeFailed.title': 'GPU動作モードを変更できませんでした',
      'hybridMode.changeFailed.message': '数秒後に再度モード変更を試してください。dGPUがまったく反応しない場合はノートPCを再起動してください。',
      batteryModes: {
        conservation: 'コンサベーションモード',
        normal: '通常モード',
        rapidCharge: '急速充電モード'
      },
      powerModeOptions: {
        quiet: '静音',
        balance: 'バランス',
        performance: '性能',
        extreme: 'エクストリーム',
        godMode: 'カスタム'
      }
    },
    common: {
      loading: '読み込み中…',
      error: '問題が発生しました',
      retry: '再試行',
      close: '閉じる',
      cancel: 'キャンセル',
      moreActions: 'その他の操作',
      copied: 'クリップボードにコピーしました',
      add: '追加',
      save: '保存',
      saveAndClose: '保存して閉じる',
      apply: '適用',
      applyAndClose: '適用して閉じる',
      default: 'デフォルト',
      rename: '名前を変更',
      delete: '削除',
      ok: 'OK'
    },
    colorPicker: {
      hex: 'Hex',
      red: '赤',
      green: '緑',
      blue: '青',
      ok: 'OK'
    },
    fanCurve: {
      fanSpeed: 'ファン速度',
      fanSpeedMax: '100%',
      cpu: 'CPU',
      cpuSensor: 'CPUセンサー',
      gpu: 'GPU',
      gpu2: 'GPU #2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: '近日公開'
    },
    settings: {
      title: '設定',
      description: 'アプリケーションの外観、動作、機能オプションを設定します。',
      nav: {
        appearance: '外観',
        application: 'アプリケーション',
        power: '電源',
        display: 'ディスプレイ',
        smartKeys: 'スマートキー',
        update: '更新',
        integrations: '連携',
        osd: 'OSD'
      },
      appearance: {
        language: '言語',
        languageDesc: '言語を選択',
        temperature: '温度',
        temperatureDesc: '温度センサーで使用する単位を選択します。',
        theme: 'テーマ',
        accentColor: 'アクセントカラー',
        accentColorDesc: 'アプリケーションのアクセントカラーを変更します。',
        appScale: 'UIスケール',
        appScaleDesc: 'Windowsの表示スケールとは独立して、テキストとインターフェース全体を一括で拡大・縮小します。',
        themeOptions: {
          system: 'システム',
          light: 'ライト',
          dark: 'ダーク'
        }
      },
      application: {
        minimizeToTray: 'トレイに最小化',
        minimizeToTrayDesc: 'タスクバーではなく常にトレイに最小化します。',
        minimizeOnClose: '閉じる時に最小化',
        minimizeOnCloseDesc: '常にトレイに最小化します。有効にすると、トレイアイコンを右クリックして「閉じる」を選択しないと終了できません。',
        disableUnsupportedWarning: '互換性のないデバイスの警告を表示しない',
        disableUnsupportedWarningDesc: '起動時に表示される互換性のないデバイスの警告を非表示にします。',
        enableHardwareSensors: 'ハードウェアセンサー',
        enableHardwareSensorsDesc: '詳細な温度、周波数、電力制限を監視する高度なハードウェアポーリングを有効にします。',
        dontShowNotifications: '通知を表示しない',
        dontShowNotificationsDesc: 'アプリ内通知とシステム通知を無効にします',
        autorun: 'ログイン時に起動',
        autorunDesc: 'Windowsサインイン後にシステムトレイに最小化して起動します。',
        extensionsEnabled: '拡張機能を有効化',
        extensionsEnabledDesc: 'プラグインと拡張機能の読み込みを有効にします',
        sensorSections: 'センサーセクション',
        sensorSectionsDesc: '表示するセンサーセクションとその順序を選択します。',
        disableVantage: 'Lenovo Vantageを無効化',
        disableVantageDesc: 'Lenovo VantageとImControllerをアンインストールせずに無効化します。\n変更後は再起動をお勧めします。',
        disableLegionZone: 'Legion Zoneを無効化',
        disableLegionZoneDesc: 'Legion Zoneとそのサービスをアンインストールせずに無効化します。\n変更後は再起動をお勧めします。',
        disableLenovoHotkeys: 'Lenovo Hotkeysを無効化',
        disableLenovoHotkeysDesc: 'Lenovo Hotkeysとそのサービスをアンインストールせずに無効化します。\n無効にすると本アプリがFnショートカットを処理します。\n変更後は再起動をお勧めします。',
        valueOn: 'オン',
        valueOff: 'オフ'
      },
      saved: '設定を保存しました',
      saveFailed: '設定の保存に失敗しました',
      osd: {
        title: 'OSD',
        showOsd: 'OSDを表示',
        showOsdDesc: 'オンスクリーンディスプレイをすぐに表示します。',
        style: 'オーバーレイスタイル',
        styles: {
          panel: 'パネル',
          bar: 'バー'
        },
        refreshInterval: '更新間隔',
        snapThreshold: 'スナップしきい値',
        lockPosition: '位置を固定',
        resetPosition: '位置をリセット',
        previewHint: 'プレビュー',
        tabs: {
          general: '一般',
          appearance: '外観',
          thresholds: 'しきい値',
          sensors: 'センサー'
        },
        opacity: '不透明度',
        cornerRadius: '角の半径',
        cornerRadiusTop: '上',
        cornerRadiusBottom: '下',
        fontSize: 'フォントサイズ',
        background: '背景色',
        category: 'カテゴリ色',
        label: 'ラベル色',
        value: '値の色',
        warning: '警告色',
        critical: '重大色',
        separator: '区切り線の色',
        thresholds: {
          performance: '性能',
          fpsRedline: 'FPSレッドライン',
          lowFpsDelta: '低FPSデルタ',
          temperature: '温度',
          usage: '使用率',
          warning: '警告',
          critical: '重大'
        },
        items: {
          groups: {
            game: 'ゲーム',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1% Low',
            FrameTime: 'フレームタイム',
            CpuFrequency: 'コアクロック',
            CpuPCoreFrequency: 'Pコアクロック',
            CpuECoreFrequency: 'Eコアクロック',
            CpuUtilization: '使用率',
            CpuTemperature: '温度',
            CpuPower: '消費電力',
            CpuFan: 'ファン',
            GpuFrequency: 'コアクロック',
            GpuUtilization: '使用率',
            GpuTemperature: 'コア温度',
            GpuVramUtilization: 'VRAM使用率',
            GpuVramTemperature: 'VRAM温度',
            GpuPower: '消費電力',
            GpuFan: 'ファン',
            MemoryUtilization: '使用率',
            MemoryTemperature: '温度',
            Disk1Temperature: 'ディスク1温度',
            Disk2Temperature: 'ディスク2温度',
            PchTemperature: 'PCH温度',
            PchFan: 'ファン'
          }
        }
      },
      power: {
        powerModeMapping: '電源モードのマッピング',
        powerModeMappingDesc: '性能モード切り替え時にWindowsの電源プランまたは電源モードを同期して切り替えます。',
        mappingModes: {
          disabled: '無効',
          windowsPowerMode: 'Windows電源モード',
          windowsPowerPlan: 'Windows電源プラン'
        },
        windowsPowerModes: 'Windows電源モード',
        windowsPowerModesDesc: '電源モード変更時に適用するWindows電源モードを選択します。',
        windowsPowerPlans: 'Windows電源プラン',
        windowsPowerPlansDesc: '電源モード変更時に適用するWindows電源プランを選択します。',
        synchronizeBrightness: 'ディスプレイの明るさを固定',
        synchronizeBrightnessDesc: '有効にすると、電源プラン切り替え時も明るさが変わりません。',
        smartFnLock: 'スマートFnロックの修飾キー',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: '起動時に「バッテリー使用時間」をリセット',
        resetBatteryOnSinceDesc: 'システム再起動時にバッテリーセクションの「バッテリー使用時間」カウンターをリセットします。',
        godModeFnQ: 'Fn+Qでカスタムモードに切り替え',
        godModeFnQDesc: 'Fn+Qでカスタムモードに素早く切り替えられるようにします。'
      },
      display: {
        navigationItems: 'ナビゲーション項目の表示',
        navigationKeys: {
          keyboard: 'キーボードバックライト',
          battery: 'バッテリー',
          automation: '自動化',
          macro: 'マクロ',
          windowsOptimization: 'Windows最適化',
          pluginExtensions: 'プラグインと拡張機能',
          about: '情報'
        },
        notificationPosition: '通知の位置',
        notificationPositions: {
          bottomRight: '右下',
          bottomCenter: '下中央',
          bottomLeft: '左下',
          centerLeft: '中央左',
          topLeft: '左上',
          topCenter: '上中央',
          topRight: '右上',
          centerRight: '中央右',
          center: '中央'
        },
        notificationDuration: '通知の表示時間',
        notificationDurations: {
          short: '短い（3秒）',
          normal: '標準（5秒）',
          long: '長い（10秒）'
        },
        excludedRefreshRates: '除外するリフレッシュレート',
        excludedRefreshRatesDesc: 'リフレッシュレートを除外してFn+R切り替えを高速化します。',
        excludedRefreshRatesHint: '高度な編集は今後のバージョンで利用可能になります',
        excludedRefreshRatesEmpty: '除外されたリフレッシュレートはありません',
        excludedRefreshRatesManageHint: 'クリックして除外リフレッシュレートを管理',
        notifications: '通知',
        notificationsDesc: '表示する通知を選択します。',
        bootLogo: 'ブートロゴ',
        bootLogoDesc: '起動時に表示されるブートロゴをカスタマイズします。'
      },
      smartKeys: {
        smartFnLock: 'スマートFnロック',
        smartFnLockDesc: 'Alt、Ctrl、Shiftが押されている間、Fnが一時的にロック解除されます。',
        off: 'オフ',
        hint: 'スマートFnロックの修飾キーは電源設定で変更できます。',
        singlePressActionDesc: 'Fn+F9のシングルプレスにクイックアクションを割り当てます。',
        doublePressActionDesc: 'Fn+F9のダブルプレスにクイックアクションを割り当てます。'
      },
      update: {
        frequency: '更新を自動的に確認',
        frequencies: {
          perHour: '毎時間',
          perThreeHours: '3時間ごと',
          perTwelveHours: '12時間ごと',
          perDay: '毎日',
          perWeek: '毎週',
          perMonth: '毎月'
        },
        includePrerelease: 'プレリリース版を含める',
        includePrereleaseDesc: 'オフの場合は安定版のみ、オンの場合はプレリリース（ベータ）版も受信します。',
        repository: '更新リポジトリ',
        repositoryDesc: '更新を確認するGitHubリポジトリを設定します。空欄でデフォルトを使用します。',
        repositoryOwner: 'リポジトリ所有者',
        repositoryOwnerPlaceholder: '例：SSC-STUDIO',
        repositoryName: 'リポジトリ名',
        repositoryNamePlaceholder: '例：UniversalDeviceToolkit',
        check: '更新を確認',
        comingSoon: '更新の確認は今後のバージョンで利用可能になります'
      },
      checkResult: {
        available: '新しいバージョンが利用可能です: v{{version}}',
        latest: '最新版です'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'ファン速度やバッテリー温度などのデータをHWiNFO64と共有します。切り替え後にHWiNFO64の再起動が必要な場合があります。',
        cli: 'コマンドラインインターフェース',
        cliDesc: 'コマンドラインからの操作を可能にするCLIを有効にします。'
      }
    },
    keyboard: {
      title: 'キーボードバックライト',
      unsupported: 'このデバイスではキーボードバックライトはサポートされていません',
      rgb: {
        preset: 'プリセット',
        settings: 'バックライト設定',
        effect: 'エフェクト',
        speed: '速度',
        brightness: '明るさ',
        zones: 'ゾーンカラー',
        synchroniseZones: 'ゾーンを同期',
        presets: {
          off: 'オフ',
          one: 'プリセット1',
          two: 'プリセット2',
          three: 'プリセット3',
          four: 'プリセット4'
        },
        effectOptions: {
          static: '静的',
          breath: 'ブレス',
          smooth: 'スムーズ',
          waveRtl: 'ウェーブ（右→左）',
          waveLtr: 'ウェーブ（左→右）'
        },
        speedOptions: {
          slowest: '最も遅い',
          slow: '遅い',
          fast: '速い',
          fastest: '最も速い'
        },
        brightnessOptions: {
          low: '低',
          high: '高'
        }
      },
      spectrum: {
        brightness: '明るさ',
        profile: 'プロファイル',
        logo: 'ロゴライト',
        effects: 'エフェクト',
        colors: '色',
        addEffect: 'エフェクトを追加',
        deleteEffect: '削除',
        noEffects: 'エフェクトなし',
        selectAll: 'すべてのゾーンを選択',
        deselectAll: 'すべてのゾーンの選択を解除',
        switchLayout: 'キーボードレイアウトを切り替え',
        editEffect: '編集',
        allKeys: 'すべてのキー',
        zonesCount: '{{count}}ゾーン',
        noLayoutHint: 'キーボードレイアウトを読み込めませんでした。',
        selectEffectHint: '下のエフェクトを選択してキーをプレビュー・編集します。',
        effectEdit: {
          addTitle: 'エフェクトを追加',
          editTitle: 'エフェクトを編集',
          effect: 'エフェクト',
          speed: '速度',
          direction: '方向',
          clockwiseDirection: '方向',
          color: '色',
          colors: '色',
          addColor: '色を追加',
          keys: 'キー',
          alwaysWarning: 'このエフェクトはキーボード全体に適用され、他のすべてのエフェクトを置き換えます。'
        },
        effectTypes: {
          always: '常時',
          rainbowScrew: 'レインボースクリュー',
          rainbowWave: 'レインボーウェーブ',
          colorChange: 'カラーチェンジ',
          colorWave: 'カラーウェーブ',
          colorPulse: 'カラーパルス',
          smooth: 'スムーズ',
          rain: 'レイン',
          ripple: 'リップル',
          type: 'タイプ',
          audioBounce: 'オーディオバウンス',
          audioRipple: 'オーディオリップル',
          auroraSync: 'Aurora同期'
        }
      }
    },
    automation: {
      title: '自動化',
      enable: '自動化を有効化',
      enableDesc: '自動アクションを実行するにはUniversal Device Toolkitが起動している必要があります。',
      subtitle: '有効にすると、デバイスの状態が変化したときに一致するアクションを順番にチェックして実行します。',
      actionsTitle: 'アクション',
      actionsEmpty: '自動アクションはまだありません',
      quickActionsTitle: 'クイックアクション',
      quickActionsEmpty: 'クイックアクションはまだありません。「新規作成」をクリックして作成してください。',
      renamePipeline: 'パイプラインの名前を変更',
      renamePipelineTitle: 'パイプラインの名前を変更',
      renamePipelinePlaceholder: 'パイプライン名を入力',
      changeIcon: 'アイコンを変更',
      empty: '自動化スクリプトはまだありません。「新規作成」をクリックして作成してください。',
      runNow: '今すぐ実行',
      delete: '削除',
      deleteStep: 'ステップを削除',
      addPipeline: '新規作成',
      addStep: 'ステップを追加',
      configure: '設定',
      stepType: 'ステップタイプ',
      steps: 'ステップ',
      save: '保存',
      revert: '元に戻す',
      pipelineName: 'パイプライン名',
      pipelineNamePlaceholder: 'パイプライン名を入力',
      quickAction: 'クイックアクション',
      optionsLoading: 'オプションを読み込み中…',
      stepLabels: {
        rgbKeyboardBacklight: 'キーボードバックライト',
        run: '実行',
        showMainWindow: 'メインウィンドウを表示',
        speaker: 'スピーカー',
        spectrumKeyboardBacklightBrightness: 'キーボードバックライトの明るさ',
        spectrumKeyboardBacklightImportProfile: 'キーボードバックライトプロファイルをインポート',
        spectrumKeyboardBacklightProfile: 'キーボードバックライトプロファイル',
        touchpadLock: 'タッチパッドロック',
        turnOffMonitors: 'ディスプレイをオフ',
        turnOffWiFi: 'Wi-Fiをオフ',
        turnOnWiFi: 'Wi-Fiをオン',
        whiteKeyboardBacklight: 'キーボードバックライト',
        winKey: 'Windowsキーロック',
        scriptPath: '実行ファイルのパス',
        scriptArguments: '引数',
        runSilently: 'サイレント実行',
        runSilentlyDesc: 'コンソールウィンドウを作成せずにコンソールアプリを実行します。',
        runWaitUntilFinished: '終了まで待機',
        runWaitUntilFinishedDesc: 'プログラムまたはスクリプトの実行が終了するまで待機します',
        runHint: 'スクリプトまたはプログラムを実行します。\nスクリプトが正しく実行されることを確認してください。',
        importProfilePath: 'パス',
        browse: '参照',
        off: 'オフ',
        on: 'オン',
        mute: 'ミュート',
        unmute: 'ミュート解除',
        low: '低',
        high: '高',
        presetOne: 'プリセット1',
        presetTwo: 'プリセット2',
        presetThree: 'プリセット3',
        presetFour: 'プリセット4',
        values: {
          off: 'オフ',
          on: 'オン',
          mute: 'ミュート',
          unmute: 'ミュート解除',
          low: '低',
          high: '高',
          presetOne: 'プリセット1',
          presetTwo: 'プリセット2',
          presetThree: 'プリセット3',
          presetFour: 'プリセット4'
        }
      },
      state: {
        on: 'オン',
        off: 'オフ',
        hidden: '非表示',
        show: '表示',
        toggle: '状態を切り替え',
        quiet: '静音',
        balance: 'バランス',
        performance: '性能',
        extreme: 'エクストリーム',
        godMode: 'カスタム',
        hybrid: 'ハイブリッド',
        hybridIgpu: 'ハイブリッド-iGPU',
        hybridAuto: 'ハイブリッド-自動',
        dgpu: 'dGPU',
        acAdapter: 'ACアダプター',
        usbPd: 'USB給電',
        acAndUsbPd: 'ACアダプターとUSB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'GPU動作モード',
          desc: 'コンピューターの使用状況と電力状態に基づいてGPU動作モードを選択します。\nモードの切り替えには再起動が必要な場合があります。'
        },
        instantBoot: {
          title: 'インスタントブート',
          desc: '充電器が接続されたときにノートPCの電源を入れます。'
        },
        macro: {
          title: 'マクロ',
          desc: 'マクロを有効または無効にします。'
        },
        microphone: {
          title: 'マイク',
          desc: 'オフにするとすべてのマイクがミュートされます。'
        },
        notification: {
          title: '通知を表示',
          desc: '入力したテキストで通知を表示します。',
          placeholder: '通知テキスト'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'キーボードバックライト',
          desc: 'バックライトをオンまたはオフにします。'
        },
        osd: {
          title: 'OSD',
          desc: 'OSDを表示または非表示にします'
        },
        overclockDiscreteGPU: {
          title: 'GPUをオーバークロック',
          desc: 'ディスクリートGPUをオーバークロックして性能を向上させます。\n\n警告：ディスクリートGPUが利用できない場合、このアクションは正しく実行されません。'
        },
        overDrive: {
          title: 'オーバードライブ',
          desc: '内蔵ディスプレイの応答速度を向上させます。'
        },
        panelLogoBacklight: {
          title: 'パネルロゴバックライト',
          desc: 'ノートPCの蓋のバックライトをオンまたはオフにします。'
        },
        playSound: {
          title: 'サウンドを再生',
          desc: 'wavやmp3などの一般的な音楽形式に対応しています。',
          browse: '参照…',
          none: 'ファイルが選択されていません'
        },
        portsBacklight: {
          title: 'ポートバックライト',
          desc: 'ノートPC背面のポートのバックライトをオンまたはオフにします。'
        },
        powerMode: {
          title: '電力モード',
          desc: 'パフォーマンスモードを変更します。'
        },
        quickAction: {
          title: 'クイックアクション',
          desc: '保存済みのクイックアクションを実行します。',
          placeholder: 'クイックアクションを選択',
          empty: 'クイックアクションがまだありません。まずトリガーなしのパイプラインを作成してください。'
        },
        refreshRate: {
          title: 'リフレッシュレート',
          desc: '内蔵ディスプレイのリフレッシュレートを変更します。\n\n警告：内蔵ディスプレイがオフの場合、このアクションは正しく実行されません。',
          empty: '利用可能なリフレッシュレートがありません'
        },
        resolution: {
          title: '解像度',
          desc: '内蔵ディスプレイの解像度を変更します。\n\n警告：内蔵ディスプレイがオフの場合、このアクションは正しく実行されません。',
          empty: '利用可能な解像度がありません'
        },
        alwaysOnUsb: {
          title: '常時USB給電',
          desc: 'ノートPCがオフ、スリープ、休止状態のときにUSBデバイスを充電します。',
          options: {
            OnWhenSleeping: 'スリープ時にオン',
            OnAlways: '常にオン'
          }
        },
        battery: {
          title: 'バッテリーモード',
          desc: 'バッテリーの充電方法を選択します。',
          options: {
            Conservation: 'コンサベーション',
            Normal: '通常',
            RapidCharge: '急速充電'
          }
        },
        batteryNightCharge: {
          title: '夜間バッテリー充電',
          desc: '有効にすると、夜間の充電時に80%まで充電し、朝の使用時までに100%まで充電します。'
        },
        deactivateGPU: {
          title: 'GPUを無効化',
          desc: '不要にアクティブなディスクリートGPUを無効にします。\n\n警告：内蔵ディスプレイがオフまたはハイブリッドモードが無効の場合、このアクションは正しく実行されません。',
          options: {
            KillApps: 'アプリを終了',
            RestartGPU: 'GPUを再起動'
          }
        },
        delay: {
          title: '遅延',
          desc: '次のステップを実行する前に遅延を追加します。',
          second_one: '{{count}}秒',
          second_other: '{{count}}秒'
        },
        displayBrightness: {
          title: 'ディスプレイの明るさ',
          desc: '内蔵ディスプレイの明るさを変更します。\n\n警告：内蔵ディスプレイがオフの場合、このアクションは正しく実行されません。',
          percent: '{{value}}%'
        },
        dpiScale: {
          title: 'DPI',
          desc: '内蔵ディスプレイのスケーリングを変更します。\n\n警告：内蔵ディスプレイがオフの場合、このアクションは正しく実行されません。',
          percent: '{{value}}%'
        },
        flipToStart: {
          title: '開いて起動',
          desc: '蓋を開くとノートPCの電源が入ります。'
        },
        fnLock: {
          title: 'Fnロック',
          desc: 'Fnキーを押さずにF1-F12のセカンダリ機能を使用します。'
        },
        godModePreset: {
          title: 'カスタムモードプリセット',
          desc: 'カスタムモードのプリセットをアクティブにします。\nこの設定はカスタムモードが有効な場合のみ有効です。'
        },
        hdr: {
          title: 'HDR',
          desc: '内蔵ディスプレイでHDRを有効にします。\n\n警告：内蔵ディスプレイがオフの場合、このアクションは正しく実行されません。'
        },
        hideMainWindow: {
          title: 'メインウィンドウを非表示'
        },
        rgbKeyboardBacklight: {
          title: 'キーボードバックライト',
          desc: 'キーボードバックライトのプリセットを調整します。'
        },
        run: {
          title: '実行',
          desc: 'スクリプトまたはプログラムを実行します。\nスクリプトが正しく実行されることを確認してください。'
        },
        showMainWindow: {
          title: 'メインウィンドウを表示'
        },
        speaker: {
          title: 'スピーカー',
          desc: 'ミュートにすると、アクティブなすべてのオーディオ出力デバイスがミュートされます。'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'キーボードバックライトの明るさ',
          desc: 'キーボードバックライトの明るさを調整します。'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'キーボードバックライトプロファイルをインポート',
          desc: 'バックライト設定をインポートして現在のプロファイルに適用します。'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'キーボードバックライトプロファイル',
          desc: 'キーボードバックライトプロファイルを調整します。'
        },
        touchpadLock: {
          title: 'タッチパッドロック',
          desc: 'タッチパッドを無効にします。'
        },
        turnOffMonitors: {
          title: 'ディスプレイをオフ',
          desc: '利用可能なすべてのディスプレイをオフにします。'
        },
        turnOffWiFi: {
          title: 'Wi-Fiをオフ'
        },
        turnOnWiFi: {
          title: 'Wi-Fiをオン'
        },
        whiteKeyboardBacklight: {
          title: 'キーボードバックライト',
          desc: 'キーボードバックライトの明るさを調整します。'
        },
        winKey: {
          title: 'Windowsキーロック',
          desc: '内蔵キーボードのWindowsキーを無効にします。'
        }
      },
      moveUp: '上に移動',
      moveDown: '下に移動',
      noEditableParameters: 'このステップには編集可能なパラメーターがありません。',
      addAutomaticPipeline: '新しいアクション',
      addQuickAction: '新しいクイックアクション',
      quickActionName: 'クイックアクション名',
      triggerPicker: {
        title: '新しいアクション — トリガーを選択'
      },
      triggerConfig: {
        title: 'トリガーを設定',
        noEditableTriggers: 'このトリガーには設定可能なパラメーターがありません。'
      },
      triggerNames: {
        aCAdapterConnected: 'AC電源アダプターが接続されたとき',
        lowWattageACAdapterConnected: '低電力AC電源アダプターが接続されたとき',
        aCAdapterDisconnected: 'AC電源アダプターが切断されたとき',
        powerMode: '電力モードが変更されたとき',
        godModePresetChanged: 'カスタムモードのプリセットが変更されたとき',
        gamesAreRunning: 'ゲームが実行中のとき',
        gamesStop: 'ゲームが閉じたとき',
        processesAreRunning: 'アプリが起動したとき',
        processesStopRunning: 'アプリが閉じたとき',
        userInactivity: 'ユーザーが非アクティブになったとき',
        userInactivityZero: 'ユーザーがアクティブになったとき',
        sessionLock: 'セッションがロックされた',
        sessionUnlock: 'セッションがロック解除された',
        lidOpened: '蓋が開かれた',
        lidClosed: '蓋が閉じられた',
        displayOn: 'ディスプレイがオンになったとき',
        displayOff: 'ディスプレイがオフになったとき',
        hdrOn: 'HDRがオンになったとき',
        hdrOff: 'HDRがオフになったとき',
        deviceConnected: 'デバイスが接続されたとき',
        deviceDisconnected: 'デバイスが切断されたとき',
        externalDisplayConnected: '外部ディスプレイが接続されたとき',
        externalDisplayDisconnected: '外部ディスプレイが切断されたとき',
        wiFiConnected: 'Wi-Fiに接続されたとき',
        wiFiDisconnected: 'Wi-Fiが切断されたとき',
        time: '指定した時刻',
        periodic: '定期的なアクション',
        hardwareSensor: 'ハードウェアセンサー',
        batteryPercentage: 'バッテリー残量',
        onStartup: '起動時',
        onResume: '再開時'
      },
      triggerEditors: {
        noProcesses: 'プロセスが選択されていません。',
        noDevices: 'デバイスが選択されていません。',
        inactivityTimeout: 'タイムアウト',
        seconds: '{{count}}秒',
        minutes: '{{count}}分',
        hours: '{{count}}時間',
        ssidPlaceholder: 'ネットワーク名（SSID）',
        addSsid: 'ネットワーク名を追加',
        atTime: '時刻',
        hour: '時',
        minute: '分',
        allDays: '毎日',
        day: {
          0: '日曜日',
          1: '月曜日',
          2: '火曜日',
          3: '水曜日',
          4: '木曜日',
          5: '金曜日',
          6: '土曜日'
        },
        metric: '指標',
        comparison: '比較',
        threshold: 'しきい値',
        thresholdPercent: 'しきい値（%）',
        durationSeconds: '時間（秒）',
        cooldownSeconds: 'クールダウン（秒）',
        chargeFilter: '充電フィルター',
        deviceInstanceId: 'デバイスインスタンスID'
      }
    },
    macro: {
      title: 'キーボードマクロ',
      enable: 'マクロを有効化',
      enableDesc: 'マクロを機能させるにはUniversal Device Toolkitが起動している必要があります。',
      subtitle: '一連のキー入力を記録し、キーボードのテンキーで呼び出すことができます。',
      numpad: 'テンキー',
      sequence: 'シーケンス',
      repeat: '繰り返し回数',
      events: 'イベント',
      save: '保存',
      clear: 'クリア',
      play: '再生',
      record: '記録',
      recordingOptions: '記録オプション',
      ignoreDelays: '遅延を無視',
      interruptOnOtherKey: '他のキーで中断',
      dontRepeat: '繰り返さない',
      keyboardOnly: 'キーボードのみ',
      keyboardMouse: 'キーボードとマウスボタン',
      allInputs: 'すべての入力',
      recordingInterrupted: '記録が中断されました',
      keyboard: 'キーボード',
      mouse: 'マウス',
      move: 'マウス移動',
      wheelUp: 'ホイール上',
      wheelDown: 'ホイール下',
      wheelLeft: 'ホイール左',
      wheelRight: 'ホイール右',
      leftButton: '左ボタン',
      rightButton: '右ボタン',
      middleButton: '中ボタン',
      xButton: 'Xボタン',
      button: 'マウスボタン',
      empty: 'このキーのマクロシーケンスはまだありません',
      recording: {
        preparing: '3秒後に記録を開始します...',
        title: '記録中...',
        pressEscToStop: 'ESCで停止します。',
        focusHint: '記録中はこのウィンドウにフォーカスを維持してください。'
      }
    },
    plugins: {
      title: 'プラグインと拡張機能',
      search: 'プラグインを検索',
      filterAll: 'すべて',
      filterInstalled: 'インストール済み',
      filterNotInstalled: '未インストール',
      refresh: '更新',
      total: '合計 {{count}}',
      summary: 'インストール済み {{count}}',
      updatable: '{{count}}件の更新あり',
      install: 'インストール',
      update: '更新',
      updateAvailable: '更新あり',
      uninstall: 'アンインストール',
      uninstallConfirm: 'このプラグインをアンインストールしますか？',
      uninstallFailed: 'アンインストールに失敗しました',
      installed: 'インストール済み',
      online: 'オンライン',
      installing: 'インストール中…',
      downloading: 'ダウンロード中…',
      preparingDownload: 'ダウンロードを準備中…',
      downloadCompleted: 'ダウンロードが完了しました',
      offline: 'オンラインストアを利用できません。ローカルにインストール済みのプラグインのみ表示しています',
      empty: 'プラグインが見つかりません',
      dependencies: '依存関係',
      dependenciesBlocked: 'このプラグインは未解決の依存関係があり、アンインストールできません',
      details: '詳細',
      usageGuide: '使用方法',
      changelog: '変更履歴',
      importProgress: 'プラグインパッケージをインポート中…',
      importSuccess: '{{count}}個のプラグインパッケージをインポートしました',
      importFailed: '{{count}}個のプラグインパッケージのインポートに失敗しました',
      installAll: 'すべてインストール',
      installAllComplete: '{{count}}個のプラグインをインストールしました',
      installAllPartial: '{{count}}/{{total}}件のプラグイン操作が完了しました',
      copyId: 'プラグインIDをコピー',
      copied: 'プラグインIDをクリップボードにコピーしました',
      copyFailed: 'プラグインIDをコピーできませんでした',
      local: 'ローカル',
      collapseDetails: '詳細を非表示',
      showDetails: '詳細を表示',
      updateInfo: '更新情報',
      versionLabel: 'バージョン：',
      configure: '設定',
      open: '開く',
      description: 'プラグインをインストール・管理して機能を拡張します',
      storeUnavailable: 'プラグインストアを利用できません',
      summaryTotal: 'プラグイン合計',
      summaryInstalled: 'インストール済み',
      summaryUpdates: '更新あり',
      importFromFiles: 'ファイルからインポート',
      updateAll: 'すべて更新',
      emptyStore: 'プラグインストアは現在空です。今後のプラグイン更新をお待ちください。'
    },
    optimization: {
      title: 'システム最適化',
      info: 'これらの操作はシステムサービスとファイルを変更し、管理者権限が必要な場合があります。',
      tabs: {
        optimization: '最適化',
        cleanup: 'クリーンアップ',
        driverDownload: 'ドライバーをダウンロード',
        networkAcceleration: 'ネットワーク加速'
      },
      recommended: '推奨',
      selected: '選択済み',
      selectedActions: '選択したアクション',
      noSelection: 'アクションが選択されていません',
      selectRecommended: '推奨を選択',
      applyRecommended: '推奨をすべて適用',
      apply: '適用',
      clear: 'クリア（元に戻す）',
      applied: '適用済み',
      applyFailed: '適用に失敗しました（管理者権限が必要な場合があります）',
      reverted: '元に戻しました',
      revertFailed: '元に戻せませんでした（管理者権限が必要な場合があります）',
      estimate: 'サイズを推定',
      estimateResult: '解放可能な容量',
      runCleanup: 'クリーンアップを実行',
      cleanupHint: 'クリーンアップはカスタムクリーンアップルールに従って実行されます。',
      cleanupConfirm: '今すぐクリーンアップを実行しますか？',
      cleanupDone: 'クリーンアップが完了しました',
      cleanupFailed: 'クリーンアップに失敗しました',
      cleanup: {
        custom: {
          header: 'カスタムクリーンアップルール',
          description: '選択したクリーンアップアクションと一緒にクリーンアップされる追加フォルダー。',
          empty: 'カスタムクリーンアップルールはありません',
          add: 'フォルダーを追加',
          edit: 'フォルダーを編集',
          remove: '削除',
          clear: 'すべてクリア',
          added: 'ルールを追加しました',
          updated: 'ルールを更新しました',
          recursive: 'サブフォルダーを含める',
          noExtensions: '拡張子が指定されていません',
          folderPickerFailed: 'フォルダーピッカーを開けませんでした'
        }
      },
      network: {
        status: '状態',
        running: '実行中',
        stopped: '停止中',
        backendReady: 'バックエンド準備完了',
        backendNotReady: 'バックエンド未準備',
        config: '基本設定',
        accelerationEnabled: '加速を有効化',
        mode: 'モード',
        modes: {
          off: 'オフ',
          systemProxy: 'システムプロキシ',
          hosts: 'Hosts',
          diagnosticsOnly: '診断のみ'
        },
        save: '設定を保存',
        saved: '設定を保存しました',
        saveFailed: '設定の保存に失敗しました',
        start: '開始',
        stop: '停止',
        startFailed: '開始に失敗しました',
        stopFailed: '停止に失敗しました',
        modeLabel: 'モード',
        targetsLabel: 'ターゲット',
        portLabel: 'ポート',
        targetsHeading: '加速ターゲット',
        domainGroupsHint: 'ローカルプロキシで加速するサービスを選択します。',
        domainGroupsEmptyTitle: '加速ターゲットがありません',
        domainGroupsEmptyDescription: 'ターゲットリストが空か、検索に一致するものがありません。',
        selectionHint: '選択したターゲットは加速開始時に適用されます。',
        searchTargets: 'ターゲットを検索',
        recommendedMenu: '推奨',
        groupRuntime: '{{selected}}/{{total}}選択中  {{active}}アクティブ',
        trafficHeading: 'トラフィック概要',
        metrics: {
          upload: 'アップロード',
          download: 'ダウンロード',
          connections: '接続数',
          total: '総トラフィック',
          health: '健全性'
        },
        trafficLive: 'ライブプロキシトラフィックを収集中',
        trafficWaiting: '加速を開始するとライブトラフィックを収集します',
        trafficUnavailable: 'トラフィックデータは一時的に利用できません',
        connectionsHeading: '現在と最近の接続',
        destinationsHeading: '宛先統計',
        connectionSummary: '{{active}}アクティブ / {{total}}合計',
        destinationSummary: '{{count}}件の宛先',
        connectionStates: {
          active: 'アクティブ',
          completed: '完了',
          blocked: 'ブロック',
          failed: '失敗',
          stopped: '停止',
          unknown: '不明'
        },
        unknownHost: '不明なホスト',
        destinationRow: '{{count}}接続  {{latency}}',
        health: {
          healthy: '正常',
          degraded: '低下',
          stopped: '停止',
          unknown: '不明'
        },
        modeFull: {
          systemProxy: 'システムプロキシ',
          hosts: 'Hostsファイル',
          diagnosticsOnly: '診断のみ',
          off: 'アイドル'
        },
        backendMissingHint: 'プロキシワーカーを利用できません',
        selectGroupsFirstHint: '少なくとも1つのターゲットを選択してください',
        advancedHeading: '詳細設定',
        advancedBody: '詳細設定とネットワーク復旧。',
        portFormat: 'ポート：{{port}}',
        dangerZoneHeading: '危険ゾーン',
        restoreHint: '加速前に記録したシステムの元のネットワーク状態を復元します。',
        restoreNetwork: 'ネットワークを復元',
        restoreConfirm: 'システムのネットワーク状態を今すぐ復元しますか？',
        restored: 'ネットワーク状態を復元しました',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: '検出',
          unknown: '不明',
          natTypes: {
            OpenInternet: 'オープンNAT',
            Nat: 'NAT',
            UdpBlocked: 'UDPブロック',
            Unknown: '不明'
          },
          internetConnected: '接続済み',
          internetUnreachable: '到達不能',
          natType: 'NATタイプ',
          localIp: 'ローカルIP',
          publicIp: 'パブリックIP',
          internet: 'インターネット',
          dnsDomain: 'ドメイン',
          customDns: 'カスタムDNS',
          enableDoh: 'DoH',
          dohUrl: 'DoH URL',
          latency: 'レイテンシ',
          resolvedAddress: '解決済みアドレス',
          latencyFormat: '{{ms}} ms',
          failed: '失敗',
          ipv6Support: 'IPv6サポート',
          ipv6Address: 'IPv6アドレス',
          ipv6SupportedFull: 'IPv6アクセス対応',
          notSupported: '非対応'
        }
      },
      driverDownload: {
        comingSoon: 'ドライバーのダウンロードは今後のバージョンで利用可能になります'
      },
      driver: {
        machineType: 'マシンタイプ',
        machineTypePlaceholder: '例：82K3',
        os: 'オペレーティングシステム',
        downloadTo: 'ダウンロード先',
        downloadToPlaceholder: 'ダウンロード先のフォルダーを選択',
        browse: '参照',
        openDownloadTo: 'フォルダーを開く',
        source: 'ソース',
        primarySource: 'Vantage',
        primarySourceMessage: 'Vantage経由の公式デバイスデータベース。',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'PC Supportの互換性データベース。',
        scan: 'スキャン',
        scanning: 'スキャン中…',
        scanValidation: '正しい4桁のマシンタイプを入力し、OSを選択してください。',
        disclaimer: 'パッケージは選択したソースから取得されます。インストールは自己責任で行ってください。',
        filter: 'フィルター',
        onlyShowUpdates: '更新のみ表示',
        sort: {
          name: '名前で並べ替え',
          category: 'カテゴリで並べ替え',
          date: '日付で並べ替え'
        },
        selectRecommended: '推奨を選択',
        startAll: 'すべて開始',
        pauseAll: 'すべて一時停止',
        clearSelection: '選択をクリア',
        packagesFound: '{{count}}個のパッケージが見つかりました。',
        packagesFoundOne: '1個のパッケージが見つかりました。',
        status: {
          NotStarted: '',
          Queued: '待機中',
          Downloading: 'ダウンロード中',
          Installing: 'インストール中',
          Completed: '完了',
          Error: 'エラー'
        },
        recommended: '推奨',
        isUpdate: '更新',
        reboot: {
          recommended: '再起動を推奨',
          required: '再起動が必要',
          shutdown: 'シャットダウンが必要'
        },
        oldPackageWarning: 'このパッケージは1年以上前のもので、ドライバーが古い可能性があります。',
        download: 'ダウンロード',
        install: 'インストール',
        uninstall: 'アンインストール',
        pause: '一時停止',
        openReadme: 'Readmeを開く',
        hide: '非表示',
        hideAll: 'すべて非表示',
        showHiddenDownloads: '非表示のダウンロードを表示',
        downloadInProgress: {
          title: 'ダウンロード進行中',
          message: 'ダウンロードタスクが実行中です。再スキャンしますか？',
          confirm: 'スキャン'
        },
        empty: {
          notScanned: {
            title: 'ドライバーパッケージをスキャン',
            message: 'ソースを選択してスキャンし、互換性のあるドライバーのダウンロードを一覧表示します。'
          },
          noResults: {
            title: 'ドライバーのダウンロードが見つかりません',
            message: '別のソース、OS、またはマシンタイプを試してください。'
          },
          noFilterResults: {
            title: '一致するダウンロードが見つかりません',
            message: 'フィルター、更新のみオプション、または非表示ダウンロードリストを調整してください。'
          },
          error: {
            title: 'ドライバースキャンが完了しませんでした',
            message: '選択したソースとネットワーク接続を確認して、再スキャンしてください。'
          }
        },
        osOptions: {
          windows7: 'Windows 7',
          windows8: 'Windows 8',
          windows10: 'Windows 10',
          windows11: 'Windows 11'
        }
      }
    },
    about: {
      title: '情報',
      appName: 'アプリケーション',
      version: 'バージョン',
      build: 'ビルド',
      links: 'プロジェクトリンク',
      projectWebsite: 'GitHubのプロジェクトサイト',
      latestRelease: 'GitHubの最新リリース',
      applicationFolders: 'アプリケーションフォルダー',
      data: 'データ',
      temp: '一時',
      pid: 'プロセスID',
      machine: 'デバイスモデル',
      bios: 'BIOSバージョン',
      compatible: '互換性',
      yes: '互換',
      no: '非互換',
      dataFolder: 'データフォルダー',
      thirdParty: 'サードパーティライブラリ',
      copyright: '著作権'
    },
    statusBanner: {
      updateAvailable: '更新があります！',
      updateAvailableWithVersion: '更新{{version}}が利用可能です！',
      pluginExtensionsDisabled: 'プラグイン拡張機能のナビゲーションが非表示です。設定 → ナビゲーション項目で有効にしてください。'
    },
    wpf: legacy.translation.wpf
  }
}



