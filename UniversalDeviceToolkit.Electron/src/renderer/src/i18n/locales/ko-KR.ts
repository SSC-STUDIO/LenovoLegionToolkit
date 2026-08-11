export default {
  translation: {
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: '로그',
      openLogs: '로그 폴더 열기',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: '장치 정보'
    },
    nav: {
      dashboard: '대시보드',
      settings: '설정',
      automation: '자동화',
      keyboard: '키보드',
      keyboardBacklight: '키보드 백라이트',
      macro: '사용자 지정 매크로',
      windowsOptimization: '시스템 최적화',
      pluginExtensions: '플러그인 및 확장',
      about: '정보'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: '환영합니다! 아래에서 시작할 섹션을 선택하세요',
      hostReady: '백엔드 연결됨',
      hostState: '백엔드 상태',
      hostVersion: '백엔드 버전',
      initComplete: '초기화 완료',
      safeStart: '안전 시작, 건너뜀',
      machine: '장치',
      compatible: '호환성',
      status: '상태'
    },
    dashboard: {
      title: '홈',
      customize: '사용자 지정',
      edit: {
        title: '대시보드 편집',
        description: '홈 페이지에 표시할 섹션과 기능을 선택하세요.',
        showSensors: '하드웨어 센서',
        groups: '기능 그룹',
        save: '저장',
        cancel: '취소',
        saved: '대시보드 레이아웃 저장됨',
        error: '대시보드 레이아웃을 저장하지 못했습니다',
        disclaimer: '장치 상태와 구성에 따라 일부 기능이 대시보드에 표시되지 않을 수 있습니다.',
        addGroup: '추가',
        renameGroup: '그룹 이름 편집',
        deleteGroup: '삭제',
        moveUp: '위로 이동',
        moveDown: '아래로 이동',
        deleteItem: '삭제',
        addItem: '추가',
        groupNamePlaceholder: '이름',
        items: {
          discreteGpu: '개별 GPU 모드',
          overclockGpu: 'GPU 오버클럭',
          turnOffMonitors: '모니터 끄기'
        }
      },
      addItem: {
        title: '추가',
        searchPlaceholder: '검색',
        empty: '모든 대시보드 항목이 이미 추가되었습니다',
        addHint: '항목 추가'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: '메모리',
      temperature: '온도',
      usage: '사용률',
      power: '전력',
      fanSpeed: '팬',
      vram: 'VRAM',
      memoryUsed: '사용된 메모리',
      memoryTotal: '전체 메모리',
      storageTemp: '저장소 온도',
      notAvailable: '--',
      sensor: {
        cpu: '프로세서',
        gpu: '그래픽 카드',
        memory: '메모리',
        temperature: '온도',
        usage: '사용률',
        power: '전력',
        fanSpeed: '팬',
        vram: 'VRAM',
        frequency: '코어 클럭',
        battery: '배터리',
        charge: '충전',
        health: '상태',
        rate: '속도',
        fan: '팬',
        lowPowerAdapter: '저전력 어댑터 연결됨',
        batteryLow: '배터리 부족',
        acCharging: '전원 어댑터 연결됨, 충전 중...',
        acNotCharging: '전원 어댑터 연결됨, 충전 안 됨...',
        remainingTime: '예상 남은 시간: {0}',
        memoryTemperature: '메모리 온도',
        ssdTemperature: 'SSD 온도',
        vramTemperature: 'VRAM 온도',
        vramUsage: 'VRAM 사용률',
        cycles: '사이클',
        capacity: '용량',
        fullCapacity: '완충 용량',
        designCapacity: '설계 용량',
        date: '날짜',
        voltage: '코어 전압',
        voltageRange: '전압 범위',
        powerRange: '전력 범위',
        details: '세부 정보',
        refreshInterval: '새로 고침 간격',
        detail: {
          power: '전력',
          powerCores: '코어',
          powerMemory: '메모리',
          powerPlatform: '플랫폼',
          pCoreClock: 'P코어 클럭',
          eCoreClock: 'E코어 클럭',
          memoryUsage: '메모리 사용량',
          sharedMemoryUsage: '공유 메모리 사용량',
          vramUsage: 'VRAM 사용량',
          hotSpot: 'GPU 핫스팟',
          pcieThroughput: 'PCIe 처리량',
          designCapacity: '설계 용량',
          fullChargeCapacity: '완충 용량'
        }
      },
      group: {
        power: '전원',
        graphics: '그래픽',
        display: '디스플레이',
        other: '기타',
        custom: '사용자 지정'
      },
      card: {
        error: '설정을 적용하지 못했습니다',
        config: '고급 설정',
        configComingSoon: '고급 설정은 향후 버전에서 제공될 예정입니다'
      }
    },
    balanceMode: {
      title: '균형 모드 설정',
      aiEngine: 'AI 엔진 활성화',
      aiEngineDesc: '특정 게임이 실행 중일 때 자동 감지하여 CPU/GPU 성능을 조정합니다. 온도와 팬 소음이 증가할 수 있습니다.'
    },
    godMode: {
      title: '사용자 지정 모드 설정',
      activePreset: '활성 프리셋',
      presetName: '프리셋 이름',
      name: '이름',
      errorLoad: '설정을 불러올 수 없습니다.',
      errorApply: '설정을 적용할 수 없습니다',
      applySuccess: '사용자 지정 모드 설정이 적용되었습니다.',
      defaultPresetName: '프리셋',
      cpu: {
        title: 'CPU',
        longTermPL: '장기 전력 제한',
        'longTermPL.desc': 'CPU가 지속적으로 도달할 수 있는 전력 소비량.',
        shortTermPL: '단기 전력 제한',
        'shortTermPL.desc': 'CPU가 짧은 시간에 도달할 수 있는 최고 전력 소비량.',
        peakPL: '피크 전력 제한',
        'peakPL.desc': 'CPU가 순간적으로 도달할 수 있는 최대 전력 소비량.',
        crossLoading: '장기 전력 제한 (교차 부하)',
        'crossLoading.desc': 'CPU와 GPU가 모두 최대 부하일 때 CPU의 최대 전력 소비량.',
        pl1Tau: '단기 전력 제한 지속 시간',
        'pl1Tau.desc': 'CPU가 단기 전력 제한으로 부스트할 수 있는 시간. 시간이 지나면 장기 제한이 적용됩니다.',
        apuSppt: 'APU sPPT 전력 제한',
        'apuSppt.desc': 'CPU가 약간의 지연 후 도달할 수 있는 최고 전력 소비량.',
        tempLimit: 'CPU 온도 제한',
        'tempLimit.desc': '주파수와 전력이 낮아지기 전 CPU의 최대 온도.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: '다이내믹 부스트',
        'dynamicBoost.desc': 'CPU 전력 소비에 따라 GPU에 할당할 수 있는 추가 전력.',
        ctgp: '구성 가능한 TGP',
        'ctgp.desc': '기본 전력 소비에 더해 GPU에 할당할 수 있는 추가 전력.',
        tempLimit: 'GPU 온도 제한',
        'tempLimit.desc': '주파수와 전력이 낮아지기 전 GPU의 최대 온도.',
        totalProcessingPowerTarget: 'AC 연결 시 총 프로세서 전력 목표',
        'totalProcessingPowerTarget.desc': 'CPU가 GPU의 동적 전력 조정을 트리거하는 지점.',
        toCpuDynamicBoost: 'GPU→CPU 다이내믹 부스트',
        'toCpuDynamicBoost.desc': 'CPU 사용량에 따라 GPU에서 CPU로 할당할 수 있는 추가 전력. 값이 높을수록 CPU 성능이 좋아집니다.'
      },
      fans: {
        title: '팬',
        curve: '팬 곡선',
        curveMessage: '팬 속도는 CPU, GPU, 방열판 센서 중 가장 높은 값을 따릅니다. 각 단계에 마우스를 올리면 정확한 값을 볼 수 있습니다.',
        maxSpeed: '최대 팬 속도',
        maxSpeedWarning: '이 옵션을 장기간 사용하면 팬 수명이 단축됩니다.\n정말로 주의해서 사용하세요!'
      },
      advanced: {
        title: '고급',
        message: '정확히 이해하지 못하는 옵션은 변경하지 마세요.',
        maxOffset: '최대 오프셋',
        maxOffsetWarning: '더 높은 값은 예측할 수 없는 동작을 유발할 수 있습니다. 확실하지 않으면 0으로 두세요.',
        minOffset: '최소 오프셋',
        minOffsetWarning: '더 낮은 값은 예측할 수 없는 동작을 유발할 수 있습니다. 확실하지 않으면 0으로 두세요.',
        invalidOffset: '저장하기 전에 정수를 입력하세요.'
      },
      vantageWarning: 'Lenovo Vantage 또는 해당 서비스가 실행 중이면 사용자 지정 모드 설정이 올바르게 적용되지 않습니다.',
      legionZoneWarning: 'Legion Zone 또는 해당 서비스가 실행 중이면 사용자 지정 모드 설정이 올바르게 적용되지 않습니다.'
    },
    overclock: {
      title: 'GPU 오버클럭 설정',
      preset: '프리셋',
      coreOffset: '코어 주파수 오프셋',
      memoryOffset: '메모리 주파수 오프셋',
      namePlaceholder: '이름...',
      newProfileName: '프리셋',
      loadError: '오버클럭 설정을 불러올 수 없습니다.'
    },
    feature: {
      powerMode: '전원 모드',
      'powerMode.desc': '성능 모드를 변경합니다.\nFn+Q로도 변경할 수 있습니다.',
      'powerMode.hint': 'Fn+Q 단축키로 빠르게 변경할 수 있습니다.',
      'powerMode.warning': '전원 어댑터가 연결되지 않으면 성능 모드가 제대로 작동하지 않을 수 있습니다.',
      battery: '배터리 충전 모드',
      'battery.desc': '배터리 충전 모드를 선택하세요. 보존 모드는 배터리 수명을 위해 충전을 제한하고, 급속 충전 모드는 더 높은 전력으로 충전합니다.',
      batteryNightCharge: '야간 배터리 충전',
      'batteryNightCharge.desc': '활성화하면 밤에 80%까지 충전하고 아침까지 100%로 완충합니다.',
      alwaysOnUsb: '상시 USB 전원',
      'alwaysOnUsb.desc': '컴퓨터가 꺼져 있거나 절전/최대 절전 상태일 때 USB 포트에 전원을 공급합니다.',
      instantBoot: '즉시 부팅',
      'instantBoot.desc': '전원이 연결되는 즉시 컴퓨터를 켭니다.',
      flipToStart: '열면 시작',
      'flipToStart.desc': '덮개를 열면 노트북이 자동으로 켜집니다.',
      fnLock: 'Fn 잠금',
      'fnLock.desc': '활성화하면 Fn을 누르지 않고 기능 키를 사용할 수 있습니다. 원래 F1~F12 키를 사용하려면 Fn과 함께 누르세요.',
      gSync: 'G-Sync',
      'gSync.desc': 'G-Sync 가변 주사율을 활성화 또는 비활성화합니다',
      hdr: 'HDR',
      'hdr.desc': '내장 디스플레이에서 HDR을 활성화합니다.',
      'hdr.warning': 'HDR 사용이 Windows 설정에 의해 차단되었습니다.',
      hybridMode: '하이브리드 모드',
      'hybridMode.desc': '하이브리드 모드에서는 내장 GPU와 개별 GPU를 전환할 수 있습니다. 끄면 개별 GPU 직결 모드가 활성화되며 재시작이 필요합니다.',
      igpuMode: '개별 GPU 모드',
      'igpuMode.desc': '절전을 위해 내장 그래픽 출력을 강제합니다',
      refreshRate: '주사율',
      'refreshRate.desc': '내장 디스플레이의 주사율을 전환합니다.',
      itsMode: 'ITS 모드',
      'itsMode.desc': '지능형 열 솔루션',
      microphone: '마이크',
      'microphone.desc': '끄면 사용 가능한 모든 마이크가 음소거됩니다.',
      overDrive: '오버 드라이브',
      'overDrive.desc': '내장 디스플레이의 응답 시간을 개선합니다. 잔상(고스팅)이 발생할 수 있습니다.',
      panelLogo: '레기온 로고 라이트',
      'panelLogo.desc': '장치 뒷면의 레기온 로고 라이트를 켜거나 끕니다.',
      portsBacklight: '포트 백라이트',
      'portsBacklight.desc': '장치 뒷면의 포트 라이트를 켜거나 끕니다.',
      resolution: '해상도',
      'resolution.desc': '내장 디스플레이의 해상도를 전환합니다.',
      dpiScale: 'DPI 배율',
      'dpiScale.desc': '내장 디스플레이의 배율을 전환합니다.',
      speaker: '스피커',
      touchpadLock: '터치패드 잠금',
      'touchpadLock.desc': '터치패드를 비활성화합니다. 마우스를 사용할 때 권장됩니다.',
      whiteKeyboard: '키보드 백라이트',
      'whiteKeyboard.desc': 'Fn + 스페이스 단축키로 백라이트를 켜고 밝기를 조절할 수 있습니다.',
      winKey: 'Win 키 비활성화',
      'winKey.desc': '내장 키보드에만 적용됩니다. 활성화하면 Win 키가 반응하지 않습니다.',
      oneLevelWhiteKeyboard: '키보드 백라이트',
      'oneLevelWhiteKeyboard.desc': 'Fn + 스페이스 단축키로 백라이트를 켜거나 끌 수 있습니다.',
      'hybridMode.states.hybrid': '하이브리드',
      'hybridMode.states.hybridIGPUOnly': '하이브리드-iGPU',
      'hybridMode.states.hybridAuto': '하이브리드-자동',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'GPU 작동 모드 정보',
      'hybridMode.info.hybrid.title': '하이브리드 모드',
      'hybridMode.info.hybrid.message': '내장 GPU와 개별 GPU가 모두 활성화됩니다. 시스템이 필요에 따라 자동으로 전환합니다.',
      'hybridMode.info.hybridIgpu.title': '하이브리드-iGPU 전용 모드',
      'hybridMode.info.hybridIgpu.message': '내장 GPU만 사용합니다. 전력 소비와 소음을 최소화합니다.',
      'hybridMode.info.hybridIgpu.disclaimer': '이 모드는 개별 GPU가 작동하지 않을 때만 적용됩니다.',
      'hybridMode.info.hybridAuto.title': '하이브리드-자동 모드',
      'hybridMode.info.hybridAuto.message': '배터리 사용 시 내장 GPU만, AC 어댑터 연결 시 둘 다 사용합니다. 비표준 어댑터 연결 시 하이브리드-iGPU 전용 모드로 전환됩니다.',
      'hybridMode.info.dgpu.title': 'dGPU 모드',
      'hybridMode.info.dgpu.message': '개별 GPU만 사용합니다. 최고의 그래픽 성능을 제공하지만 전력 소비가 증가합니다.',
      'hybridMode.info.dgpu.disclaimer': '이 모드로의 전환에는 재시작이 필요합니다.',
      'hybridMode.restartRequired.title': '재시작 필요',
      'hybridMode.restartRequired.message': '{{mode}}(으)로 변경하려면 재시작이 필요합니다. 지금 재시작하시겠습니까?',
      'hybridMode.restartRequired.now': '지금 재시작',
      'hybridMode.restartRequired.later': '나중에 재시작하겠습니다',
      'hybridMode.restartFailed': '자동으로 재시작할 수 없습니다. 변경을 완료하려면 수동으로 재시작하세요.',
      'hybridMode.changeFailed.title': 'GPU 작동 모드를 변경할 수 없습니다',
      'hybridMode.changeFailed.message': '몇 초 후 다시 시도하세요. dGPU가 전혀 응답하지 않으면 노트북을 재시작하세요.',
      batteryModes: {
        conservation: '보존 모드',
        normal: '일반 모드',
        rapidCharge: '급속 충전 모드'
      },
      powerModeOptions: {
        quiet: '조용함',
        balance: '균형',
        performance: '성능',
        extreme: '극한',
        godMode: '사용자 지정'
      }
    },
    common: {
      loading: '불러오는 중…',
      error: '문제가 발생했습니다',
      retry: '다시 시도',
      close: '닫기',
      cancel: '취소',
      moreActions: '더 많은 작업',
      copied: '클립보드에 복사됨',
      add: '추가',
      save: '저장',
      saveAndClose: '저장 후 닫기',
      apply: '적용',
      applyAndClose: '적용 후 닫기',
      default: '기본값',
      rename: '이름 바꾸기',
      delete: '삭제',
      ok: '확인'
    },
    colorPicker: {
      hex: 'Hex',
      red: '빨강',
      green: '초록',
      blue: '파랑',
      ok: '확인'
    },
    fanCurve: {
      fanSpeed: '팬 속도',
      fanSpeedMax: '100%',
      cpu: 'CPU',
      cpuSensor: 'CPU 센서',
      gpu: 'GPU',
      gpu2: 'GPU #2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: '곧 제공 예정'
    },
    settings: {
      title: '설정',
      description: '앱의 모양, 동작 및 기능 옵션을 구성합니다.',
      nav: {
        appearance: '모양',
        application: '앱',
        power: '전원',
        display: '디스플레이',
        smartKeys: '스마트 키',
        update: '업데이트',
        integrations: '통합',
        osd: 'OSD'
      },
      appearance: {
        language: '언어',
        languageDesc: '언어 선택',
        temperature: '온도',
        temperatureDesc: '온도 센서에 사용할 단위를 선택하세요.',
        theme: '테마',
        accentColor: '강조 색상',
        accentColorDesc: '앱의 강조 색상을 변경합니다.',
        appScale: 'UI 배율',
        appScaleDesc: 'Windows 디스플레이 배율과 독립적으로 텍스트와 전체 인터페이스를 함께 확대/축소합니다.',
        themeOptions: {
          system: '시스템',
          light: '라이트',
          dark: '다크'
        }
      },
      application: {
        minimizeToTray: '트레이로 최소화',
        minimizeToTrayDesc: '작업 표시줄 대신 항상 트레이로 최소화합니다.',
        minimizeOnClose: '닫을 때 최소화',
        minimizeOnCloseDesc: '항상 트레이로 최소화합니다. 종료하려면 트레이 아이콘을 우클릭하고 닫기를 선택하세요.',
        disableUnsupportedWarning: '호환되지 않는 장치 경고 안 함',
        disableUnsupportedWarningDesc: '시작 시 표시되는 호환되지 않는 장치 경고를 숨깁니다.',
        enableHardwareSensors: '하드웨어 센서',
        enableHardwareSensorsDesc: '상세 온도, 주파수, 전력 제한을 모니터링하는 고급 하드웨어 폴링을 활성화합니다.',
        dontShowNotifications: '알림 표시 안 함',
        dontShowNotificationsDesc: '앱 내 및 시스템 알림을 비활성화합니다',
        autorun: '로그인 시 시작',
        autorunDesc: 'Windows에 로그인한 후 시스템 트레이에 최소화된 상태로 시작합니다.',
        extensionsEnabled: '확장 기능 활성화',
        extensionsEnabledDesc: '플러그인 및 확장 기능 로드를 활성화합니다',
        sensorSections: '센서 섹션',
        sensorSectionsDesc: '표시할 센서 섹션과 순서를 선택합니다.',
        disableVantage: 'Lenovo Vantage 비활성화',
        disableVantageDesc: 'Lenovo Vantage 및 ImController를 제거하지 않고 비활성화합니다.\n변경 후 재시작을 권장합니다.',
        disableLegionZone: 'Legion Zone 비활성화',
        disableLegionZoneDesc: 'Legion Zone 및 해당 서비스를 제거하지 않고 비활성화합니다.\n변경 후 재시작을 권장합니다.',
        disableLenovoHotkeys: 'Lenovo Hotkeys 비활성화',
        disableLenovoHotkeysDesc: 'Lenovo Hotkeys 및 해당 서비스를 제거하지 않고 비활성화합니다.\n비활성화하면 이 앱이 Fn 단축키를 처리합니다.\n변경 후 재시작을 권장합니다.',
        valueOn: '켬',
        valueOff: '끔'
      },
      saved: '설정이 저장되었습니다',
      saveFailed: '설정을 저장하지 못했습니다',
      osd: {
        title: 'OSD',
        showOsd: 'OSD 표시',
        showOsdDesc: '화면 오버레이를 즉시 표시합니다.',
        style: '오버레이 스타일',
        styles: {
          panel: '패널',
          bar: '바'
        },
        refreshInterval: '새로 고침 간격',
        snapThreshold: '스냅 임계값',
        lockPosition: '위치 잠금',
        resetPosition: '위치 재설정',
        previewHint: '미리 보기',
        tabs: {
          general: '일반',
          appearance: '모양',
          thresholds: '임계값',
          sensors: '센서'
        },
        opacity: '불투명도',
        cornerRadius: '모서리 반경',
        cornerRadiusTop: '상단',
        cornerRadiusBottom: '하단',
        fontSize: '글꼴 크기',
        background: '배경 색상',
        category: '카테고리 색상',
        label: '레이블 색상',
        value: '값 색상',
        warning: '경고 색상',
        critical: '심각 색상',
        separator: '구분선 색상',
        thresholds: {
          performance: '성능',
          fpsRedline: 'FPS 레드라인',
          lowFpsDelta: '낮은 FPS 델타',
          temperature: '온도',
          usage: '사용률',
          warning: '경고',
          critical: '심각'
        },
        items: {
          groups: {
            game: '게임',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1% Low',
            FrameTime: '프레임 타임',
            CpuFrequency: '코어 클럭',
            CpuPCoreFrequency: 'P코어 클럭',
            CpuECoreFrequency: 'E코어 클럭',
            CpuUtilization: '사용률',
            CpuTemperature: '온도',
            CpuPower: '전력',
            CpuFan: '팬',
            GpuFrequency: '코어 클럭',
            GpuUtilization: '사용률',
            GpuTemperature: '코어 온도',
            GpuVramUtilization: 'VRAM 사용률',
            GpuVramTemperature: 'VRAM 온도',
            GpuPower: '전력',
            GpuFan: '팬',
            MemoryUtilization: '사용률',
            MemoryTemperature: '온도',
            Disk1Temperature: '디스크 1 온도',
            Disk2Temperature: '디스크 2 온도',
            PchTemperature: 'PCH 온도',
            PchFan: '팬'
          }
        }
      },
      power: {
        powerModeMapping: '전원 모드 매핑',
        powerModeMappingDesc: '성능 모드를 전환할 때 Windows 전원 계획 또는 전원 모드를 동기화하여 자동으로 전환합니다.',
        mappingModes: {
          disabled: '비활성화',
          windowsPowerMode: 'Windows 전원 모드',
          windowsPowerPlan: 'Windows 전원 계획'
        },
        windowsPowerModes: 'Windows 전원 모드',
        windowsPowerModesDesc: '전원 모드가 변경될 때 적용할 Windows 전원 모드를 선택하세요.',
        windowsPowerPlans: 'Windows 전원 계획',
        windowsPowerPlansDesc: '전원 모드가 변경될 때 적용할 Windows 전원 계획을 선택하세요.',
        synchronizeBrightness: '디스플레이 밝기 고정',
        synchronizeBrightnessDesc: '활성화하면 전원 계획 간 전환 시 밝기가 유지됩니다.',
        smartFnLock: '스마트 Fn 잠금 보조 키',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: '시작 시 "배터리 사용 시간" 재설정',
        resetBatteryOnSinceDesc: '시스템이 재부팅될 때 배터리 섹션의 "배터리 사용 시간" 카운터를 재설정합니다.',
        godModeFnQ: 'Fn+Q로 사용자 지정 모드 전환',
        godModeFnQDesc: 'Fn+Q로 사용자 지정 모드로 빠르게 전환할 수 있습니다.'
      },
      display: {
        navigationItems: '탐색 항목 표시 여부',
        navigationKeys: {
          keyboard: '키보드 백라이트',
          battery: '배터리',
          automation: '자동화',
          macro: '매크로',
          windowsOptimization: 'Windows 최적화',
          pluginExtensions: '플러그인 및 확장',
          about: '정보'
        },
        notificationPosition: '알림 위치',
        notificationPositions: {
          bottomRight: '오른쪽 아래',
          bottomCenter: '아래 중앙',
          bottomLeft: '왼쪽 아래',
          centerLeft: '중앙 왼쪽',
          topLeft: '왼쪽 위',
          topCenter: '위 중앙',
          topRight: '오른쪽 위',
          centerRight: '중앙 오른쪽',
          center: '중앙'
        },
        notificationDuration: '알림 지속 시간',
        notificationDurations: {
          short: '짧게 (3초)',
          normal: '보통 (5초)',
          long: '길게 (10초)'
        },
        excludedRefreshRates: '제외된 주사율',
        excludedRefreshRatesDesc: 'Fn+R 전환을 빠르게 하려면 주사율을 제외하세요.',
        excludedRefreshRatesHint: '고급 편집은 향후 버전에서 제공될 예정입니다',
        excludedRefreshRatesEmpty: '제외된 주사율 없음',
        excludedRefreshRatesManageHint: '클릭하여 제외 주사율 관리',
        notifications: '알림',
        notificationsDesc: '표시할 알림을 선택하세요.',
        bootLogo: '부팅 로고',
        bootLogoDesc: '컴퓨터 시작 시 표시되는 로고를 사용자 지정합니다.'
      },
      smartKeys: {
        smartFnLock: '스마트 Fn 잠금',
        smartFnLockDesc: 'Alt, Ctrl 또는 Shift를 누르면 Fn이 일시적으로 잠금 해제됩니다.',
        off: '끔',
        hint: '스마트 Fn 잠금 보조 키는 전원 설정에서 변경할 수 있습니다.',
        singlePressActionDesc: 'Fn+F9 한 번 누름에 빠른 작업을 할당합니다.',
        doublePressActionDesc: 'Fn+F9 두 번 누름에 빠른 작업을 할당합니다.'
      },
      update: {
        frequency: '업데이트 자동 확인',
        frequencies: {
          perHour: '매시간',
          perThreeHours: '3시간마다',
          perTwelveHours: '12시간마다',
          perDay: '매일',
          perWeek: '매주',
          perMonth: '매월'
        },
        includePrerelease: '사전 릴리스 버전 포함',
        includePrereleaseDesc: '끄면 안정 버전만 제공되고, 켜면 사전 릴리스(베타) 업데이트도 받습니다.',
        repository: '업데이트 저장소',
        repositoryDesc: '업데이트를 확인할 GitHub 저장소를 구성합니다. 비워두면 기본값을 사용합니다.',
        repositoryOwner: '저장소 소유자',
        repositoryOwnerPlaceholder: '예: SSC-STUDIO',
        repositoryName: '저장소 이름',
        repositoryNamePlaceholder: '예: UniversalDeviceToolkit',
        check: '업데이트 확인',
        comingSoon: '업데이트 확인은 향후 버전에서 제공될 예정입니다'
      },
      checkResult: {
        available: '새 버전 사용 가능: v{{version}}',
        latest: '최신 버전입니다'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: '팬 속도, 배터리 온도 등 데이터를 HWiNFO64와 공유합니다. 전환 후 HWiNFO64 재시작이 필요할 수 있습니다.',
        cli: '명령줄 인터페이스',
        cliDesc: '명령줄에서 제어할 수 있도록 명령줄 인터페이스를 활성화합니다.'
      }
    },
    keyboard: {
      title: '키보드 백라이트',
      unsupported: '이 장치에서는 키보드 백라이트를 지원하지 않습니다',
      rgb: {
        preset: '프리셋',
        settings: '백라이트 설정',
        effect: '효과',
        speed: '속도',
        brightness: '밝기',
        zones: '존 색상',
        synchroniseZones: '존 동기화',
        presets: {
          off: '끔',
          one: '프리셋 1',
          two: '프리셋 2',
          three: '프리셋 3',
          four: '프리셋 4'
        },
        effectOptions: {
          static: '고정',
          breath: '호흡',
          smooth: '부드러움',
          waveRtl: '웨이브 (오른쪽→왼쪽)',
          waveLtr: '웨이브 (왼쪽→오른쪽)'
        },
        speedOptions: {
          slowest: '가장 느림',
          slow: '느림',
          fast: '빠름',
          fastest: '가장 빠름'
        },
        brightnessOptions: {
          low: '낮음',
          high: '높음'
        }
      },
      spectrum: {
        brightness: '밝기',
        profile: '프로필',
        logo: '로고 라이트',
        effects: '효과',
        colors: '색상',
        addEffect: '효과 추가',
        deleteEffect: '삭제',
        noEffects: '효과 없음',
        selectAll: '모든 존 선택',
        deselectAll: '모든 존 선택 해제',
        switchLayout: '키보드 레이아웃 전환',
        editEffect: '편집',
        allKeys: '모든 키',
        zonesCount: '{{count}}개 존',
        noLayoutHint: '키보드 레이아웃을 불러올 수 없습니다.',
        selectEffectHint: '아래에서 효과를 선택하여 키를 미리 보고 편집하세요.',
        effectEdit: {
          addTitle: '효과 추가',
          editTitle: '효과 편집',
          effect: '효과',
          speed: '속도',
          direction: '방향',
          clockwiseDirection: '방향',
          color: '색상',
          colors: '색상',
          addColor: '색상 추가',
          keys: '키',
          alwaysWarning: '이 효과는 전체 키보드에 적용되며 다른 모든 효과를 대체합니다.'
        },
        effectTypes: {
          always: '항상',
          rainbowScrew: '레인보우 나사',
          rainbowWave: '레인보우 웨이브',
          colorChange: '색상 변경',
          colorWave: '색상 웨이브',
          colorPulse: '색상 펄스',
          smooth: '부드러움',
          rain: '비',
          ripple: '리플',
          type: '타이핑',
          audioBounce: '오디오 바운스',
          audioRipple: '오디오 리플',
          auroraSync: 'Aurora 동기화'
        }
      }
    },
    automation: {
      title: '자동화',
      enable: '자동화 활성화',
      enableDesc: '자동 작업이 적용되려면 Universal Device Toolkit이 실행 중이어야 합니다.',
      subtitle: '활성화하면 장치 상태가 변경될 때 일치하는 작업을 순서대로 확인하고 실행합니다.',
      actionsTitle: '작업',
      actionsEmpty: '아직 자동 작업이 없습니다',
      quickActionsTitle: '빠른 작업',
      quickActionsEmpty: '아직 빠른 작업이 없습니다. "새로 만들기"를 클릭하여 만드세요.',
      renamePipeline: '파이프라인 이름 바꾸기',
      renamePipelineTitle: '파이프라인 이름 바꾸기',
      renamePipelinePlaceholder: '파이프라인 이름 입력',
      changeIcon: '아이콘 변경',
      empty: '아직 자동화 스크립트가 없습니다. "새로 만들기"를 클릭하여 만드세요.',
      runNow: '지금 실행',
      delete: '삭제',
      deleteStep: '단계 삭제',
      addPipeline: '새로 만들기',
      addStep: '단계 추가',
      configure: '구성',
      stepType: '단계 유형',
      steps: '단계',
      save: '저장',
      revert: '되돌리기',
      pipelineName: '파이프라인 이름',
      pipelineNamePlaceholder: '파이프라인 이름 입력',
      quickAction: '빠른 작업',
      optionsLoading: '옵션 불러오는 중…',
      stepLabels: {
        rgbKeyboardBacklight: '키보드 백라이트',
        run: '실행',
        showMainWindow: '메인 창 표시',
        speaker: '스피커',
        spectrumKeyboardBacklightBrightness: '키보드 백라이트 밝기',
        spectrumKeyboardBacklightImportProfile: '키보드 백라이트 프로필 가져오기',
        spectrumKeyboardBacklightProfile: '키보드 백라이트 프로필',
        touchpadLock: '터치패드 잠금',
        turnOffMonitors: '디스플레이 끄기',
        turnOffWiFi: 'Wi-Fi 끄기',
        turnOnWiFi: 'Wi-Fi 켜기',
        whiteKeyboardBacklight: '키보드 백라이트',
        winKey: 'Windows 키 잠금',
        scriptPath: '실행 파일 경로',
        scriptArguments: '인수',
        runSilently: '조용히 실행',
        runSilentlyDesc: '콘솔 창을 만들지 않고 콘솔 응용 프로그램을 실행합니다.',
        runWaitUntilFinished: '완료될 때까지 대기',
        runWaitUntilFinishedDesc: '프로그램 또는 스크립트가 끝날 때까지 대기합니다',
        runHint: '스크립트 또는 프로그램을 실행합니다.\n먼저 스크립트가 올바르게 실행되는지 확인하세요.',
        importProfilePath: '경로',
        browse: '찾아보기',
        off: '끔',
        on: '켬',
        mute: '음소거',
        unmute: '음소거 해제',
        low: '낮음',
        high: '높음',
        presetOne: '프리셋 1',
        presetTwo: '프리셋 2',
        presetThree: '프리셋 3',
        presetFour: '프리셋 4',
        values: {
          off: '끔',
          on: '켬',
          mute: '음소거',
          unmute: '음소거 해제',
          low: '낮음',
          high: '높음',
          presetOne: '프리셋 1',
          presetTwo: '프리셋 2',
          presetThree: '프리셋 3',
          presetFour: '프리셋 4'
        }
      },
      state: {
        on: '켬',
        off: '끔',
        hidden: '숨기기',
        show: '표시',
        toggle: '상태 전환',
        quiet: '조용함',
        balance: '균형',
        performance: '성능',
        extreme: '극한',
        godMode: '사용자 지정',
        hybrid: '하이브리드',
        hybridIgpu: '하이브리드-iGPU',
        hybridAuto: '하이브리드-자동',
        dgpu: 'dGPU',
        acAdapter: 'AC 어댑터',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'AC 및 USB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'GPU 작동 모드',
          desc: '컴퓨터 사용량과 전원 상태에 따라 GPU 작동 모드를 선택하세요.\n모드 전환에는 재시작이 필요할 수 있습니다.'
        },
        instantBoot: {
          title: '즉시 부팅',
          desc: '충전기가 연결되면 노트북을 켭니다.'
        },
        macro: {
          title: '매크로',
          desc: '매크로를 활성화하거나 비활성화합니다.'
        },
        microphone: {
          title: '마이크',
          desc: '끄면 마이크가 음소거됩니다.'
        },
        notification: {
          title: '알림 표시',
          desc: '입력한 텍스트로 알림을 표시합니다.',
          placeholder: '알림 텍스트'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: '키보드 백라이트',
          desc: '백라이트를 켜거나 끕니다.'
        },
        osd: {
          title: 'OSD',
          desc: 'OSD를 표시하거나 숨깁니다'
        },
        overclockDiscreteGPU: {
          title: 'GPU 오버클럭',
          desc: '개별 GPU를 오버클럭하여 성능을 향상시킵니다.\n\n경고: 개별 GPU를 사용할 수 없으면 이 작업이 올바르게 실행되지 않습니다.'
        },
        overDrive: {
          title: '오버 드라이브',
          desc: '내장 디스플레이의 응답 시간을 개선합니다.'
        },
        panelLogoBacklight: {
          title: '패널 로고 백라이트',
          desc: '노트북 덮개의 백라이트를 켜거나 끕니다.'
        },
        playSound: {
          title: '소리 재생',
          desc: 'wav 또는 mp3와 같은 일반적인 오디오 형식을 지원합니다.',
          browse: '찾아보기…',
          none: '선택된 파일 없음'
        },
        portsBacklight: {
          title: '포트 백라이트',
          desc: '노트북 뒷면 포트의 백라이트를 켜거나 끕니다.'
        },
        powerMode: {
          title: '전원 모드',
          desc: '성능 모드를 변경합니다.'
        },
        quickAction: {
          title: '빠른 작업',
          desc: '저장된 빠른 작업을 실행합니다.',
          placeholder: '빠른 작업 선택',
          empty: '아직 빠른 작업이 없습니다. 먼저 트리거 없는 파이프라인을 만드세요.'
        },
        refreshRate: {
          title: '주사율',
          desc: '내장 디스플레이의 주사율을 변경합니다.\n\n경고: 내장 디스플레이가 꺼져 있으면 이 작업이 올바르게 실행되지 않습니다.',
          empty: '사용 가능한 주사율 없음'
        },
        resolution: {
          title: '해상도',
          desc: '내장 디스플레이의 해상도를 변경합니다.\n\n경고: 내장 디스플레이가 꺼져 있으면 이 작업이 올바르게 실행되지 않습니다.',
          empty: '사용 가능한 해상도 없음'
        },
        alwaysOnUsb: {
          title: '상시 USB 전원',
          desc: '노트북이 꺼져 있거나 절전/최대 절전 상태일 때 USB 장치를 충전합니다.',
          options: {
            OnWhenSleeping: '절전 시 켜짐',
            OnAlways: '항상 켜짐'
          }
        },
        battery: {
          title: '배터리 모드',
          desc: '배터리 충전 방식을 선택하세요.',
          options: {
            Conservation: '보존',
            Normal: '일반',
            RapidCharge: '급속 충전'
          }
        },
        batteryNightCharge: {
          title: '야간 배터리 충전',
          desc: '활성화하면 밤새 80%까지 충전하고 아침에 사용할 때까지 100%를 완충합니다.'
        },
        deactivateGPU: {
          title: 'GPU 비활성화',
          desc: '불필요하게 활성화된 개별 GPU를 비활성화합니다.\n\n경고: 내장 디스플레이가 꺼져 있거나 하이브리드 모드가 활성 상태가 아니면 이 작업이 올바르게 실행되지 않습니다.',
          options: {
            KillApps: '앱 종료',
            RestartGPU: 'GPU 재시작'
          }
        },
        delay: {
          title: '지연',
          desc: '다음 단계를 실행하기 전에 지연을 추가합니다.',
          second_one: '{{count}}초',
          second_other: '{{count}}초'
        },
        displayBrightness: {
          title: '디스플레이 밝기',
          desc: '내장 디스플레이의 밝기를 변경합니다.\n\n경고: 내장 디스플레이가 꺼져 있으면 이 작업이 올바르게 실행되지 않습니다.',
          percent: '{{value}}%'
        },
        dpiScale: {
          title: 'DPI',
          desc: '내장 디스플레이의 배율을 변경합니다.\n\n경고: 내장 디스플레이가 꺼져 있으면 이 작업이 올바르게 실행되지 않습니다.',
          percent: '{{value}}%'
        },
        flipToStart: {
          title: '열면 시작',
          desc: '덮개를 열면 노트북을 켭니다.'
        },
        fnLock: {
          title: 'Fn 잠금',
          desc: 'Fn 키를 누르지 않고 F1-F12의 보조 기능을 사용합니다.'
        },
        godModePreset: {
          title: '사용자 지정 모드 프리셋',
          desc: '사용자 지정 모드 프리셋을 활성화합니다.\n이 설정은 사용자 지정 모드가 활성화된 경우에만 적용됩니다.'
        },
        hdr: {
          title: 'HDR',
          desc: '내장 디스플레이에서 HDR을 활성화합니다.\n\n경고: 내장 디스플레이가 꺼져 있으면 이 작업이 올바르게 실행되지 않습니다.'
        },
        hideMainWindow: {
          title: '메인 창 숨기기'
        },
        rgbKeyboardBacklight: {
          title: '키보드 백라이트',
          desc: '키보드 백라이트 프리셋을 조정합니다.'
        },
        run: {
          title: '실행',
          desc: '스크립트 또는 프로그램을 실행합니다.\n먼저 스크립트가 올바르게 실행되는지 확인하세요.'
        },
        showMainWindow: {
          title: '메인 창 표시'
        },
        speaker: {
          title: '스피커',
          desc: '음소거하면 모든 활성 오디오 출력 장치가 음소거됩니다.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: '키보드 백라이트 밝기',
          desc: '키보드 백라이트 밝기를 조정합니다.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: '키보드 백라이트 프로필 가져오기',
          desc: '백라이트 구성을 현재 프로필로 가져와 적용합니다.'
        },
        spectrumKeyboardBacklightProfile: {
          title: '키보드 백라이트 프로필',
          desc: '키보드 백라이트 프로필을 조정합니다.'
        },
        touchpadLock: {
          title: '터치패드 잠금',
          desc: '터치패드를 비활성화합니다.'
        },
        turnOffMonitors: {
          title: '디스플레이 끄기',
          desc: '사용 가능한 모든 디스플레이를 끕니다.'
        },
        turnOffWiFi: {
          title: 'Wi-Fi 끄기'
        },
        turnOnWiFi: {
          title: 'Wi-Fi 켜기'
        },
        whiteKeyboardBacklight: {
          title: '키보드 백라이트',
          desc: '키보드 백라이트 밝기를 조정합니다.'
        },
        winKey: {
          title: 'Windows 키 잠금',
          desc: '내장 키보드의 Windows 키를 비활성화합니다.'
        }
      },
      moveUp: '위로 이동',
      moveDown: '아래로 이동',
      noEditableParameters: '이 단계에는 편집 가능한 매개변수가 없습니다.',
      addAutomaticPipeline: '새 작업',
      addQuickAction: '새 빠른 작업',
      quickActionName: '빠른 작업 이름',
      triggerPicker: {
        title: '새 작업 — 트리거 선택'
      },
      triggerConfig: {
        title: '트리거 구성',
        noEditableTriggers: '이 트리거에는 구성 가능한 매개변수가 없습니다.'
      },
      triggerNames: {
        aCAdapterConnected: 'AC 전원 어댑터가 연결되었을 때',
        lowWattageACAdapterConnected: '저전력 AC 어댑터가 연결되었을 때',
        aCAdapterDisconnected: 'AC 전원 어댑터가 분리되었을 때',
        powerMode: '전원 모드가 변경되었을 때',
        godModePresetChanged: '사용자 지정 모드 프리셋이 변경되었을 때',
        gamesAreRunning: '게임이 실행 중일 때',
        gamesStop: '게임이 닫혔을 때',
        processesAreRunning: '앱이 시작되었을 때',
        processesStopRunning: '앱이 종료되었을 때',
        userInactivity: '사용자가 비활성 상태가 되었을 때',
        userInactivityZero: '사용자가 활성 상태가 되었을 때',
        sessionLock: '세션 잠금됨',
        sessionUnlock: '세션 잠금 해제됨',
        lidOpened: '덮개 열림',
        lidClosed: '덮개 닫힘',
        displayOn: '디스플레이가 켜졌을 때',
        displayOff: '디스플레이가 꺼졌을 때',
        hdrOn: 'HDR이 켜졌을 때',
        hdrOff: 'HDR이 꺼졌을 때',
        deviceConnected: '장치가 연결되었을 때',
        deviceDisconnected: '장치가 분리되었을 때',
        externalDisplayConnected: '외부 디스플레이가 연결되었을 때',
        externalDisplayDisconnected: '외부 디스플레이가 분리되었을 때',
        wiFiConnected: 'Wi-Fi가 연결되었을 때',
        wiFiDisconnected: 'Wi-Fi가 끊겼을 때',
        time: '지정된 시간에',
        periodic: '주기적 작업',
        hardwareSensor: '하드웨어 센서',
        batteryPercentage: '배터리 백분율',
        onStartup: '시작 시',
        onResume: '재개 시'
      },
      triggerEditors: {
        noProcesses: '선택된 프로세스가 없습니다.',
        noDevices: '선택된 장치가 없습니다.',
        inactivityTimeout: '시간 초과',
        seconds: '{{count}}초',
        minutes: '{{count}}분',
        hours: '{{count}}시간',
        ssidPlaceholder: '네트워크 이름 (SSID)',
        addSsid: '네트워크 이름 추가',
        atTime: '시각',
        hour: '시',
        minute: '분',
        allDays: '매일',
        day: {
          0: '일요일',
          1: '월요일',
          2: '화요일',
          3: '수요일',
          4: '목요일',
          5: '금요일',
          6: '토요일'
        },
        metric: '지표',
        comparison: '비교',
        threshold: '임계값',
        thresholdPercent: '임계값 (%)',
        durationSeconds: '지속 시간 (초)',
        cooldownSeconds: '재사용 대기 (초)',
        chargeFilter: '충전 필터',
        deviceInstanceId: '장치 인스턴스 ID'
      }
    },
    macro: {
      title: '키보드 매크로',
      enable: '매크로 활성화',
      enableDesc: '매크로가 작동하려면 Universal Device Toolkit이 실행 중이어야 합니다.',
      subtitle: '일련의 키 입력을 기록하고 키보드의 숫자 키패드로 호출할 수 있습니다.',
      numpad: '숫자 키패드',
      sequence: '시퀀스',
      repeat: '반복 횟수',
      events: '이벤트',
      save: '저장',
      clear: '지우기',
      play: '재생',
      record: '녹화',
      recordingOptions: '녹화 옵션',
      ignoreDelays: '지연 무시',
      interruptOnOtherKey: '다른 키로 중단',
      dontRepeat: '반복 안 함',
      keyboardOnly: '키보드만',
      keyboardMouse: '키보드 및 마우스 버튼',
      allInputs: '모든 입력',
      recordingInterrupted: '녹화 중단됨',
      keyboard: '키보드',
      mouse: '마우스',
      move: '마우스 이동',
      wheelUp: '휠 위로',
      wheelDown: '휠 아래로',
      wheelLeft: '휠 왼쪽으로',
      wheelRight: '휠 오른쪽으로',
      leftButton: '왼쪽 버튼',
      rightButton: '오른쪽 버튼',
      middleButton: '가운데 버튼',
      xButton: 'X 버튼',
      button: '마우스 버튼',
      empty: '이 키에 대한 매크로 시퀀스가 아직 없습니다',
      recording: {
        preparing: '3초 후 녹화가 시작됩니다...',
        title: '녹화 중...',
        pressEscToStop: 'ESC를 눌러 중지하세요.',
        focusHint: '녹화 중에는 이 창을 집중 상태로 유지하세요.'
      }
    },
    plugins: {
      title: '플러그인 및 확장',
      search: '플러그인 검색',
      filterAll: '전체',
      filterInstalled: '설치됨',
      filterNotInstalled: '미설치',
      refresh: '새로 고침',
      total: '총 {{count}}개',
      summary: '{{count}}개 설치됨',
      updatable: '업데이트 {{count}}개 사용 가능',
      install: '설치',
      update: '업데이트',
      updateAvailable: '업데이트 가능',
      uninstall: '제거',
      uninstallConfirm: '이 플러그인을 제거하시겠습니까?',
      uninstallFailed: '제거하지 못했습니다',
      installed: '설치됨',
      online: '온라인',
      installing: '설치 중…',
      downloading: '다운로드 중…',
      preparingDownload: '다운로드 준비 중…',
      downloadCompleted: '다운로드 완료',
      offline: '온라인 스토어를 사용할 수 없습니다. 로컬에 설치된 플러그인만 표시됩니다',
      empty: '플러그인을 찾을 수 없습니다',
      dependencies: '종속성',
      dependenciesBlocked: '이 플러그인은 충족되지 않은 종속성이 있어 제거할 수 없습니다',
      details: '세부 정보',
      usageGuide: '사용 가이드',
      changelog: '변경 로그',
      importProgress: '플러그인 패키지 가져오는 중…',
      importSuccess: '{{count}}개 플러그인 패키지를 가져왔습니다',
      importFailed: '{{count}}개 플러그인 패키지를 가져오지 못했습니다',
      installAll: '모두 설치',
      installAllComplete: '{{count}}개 플러그인 설치됨',
      installAllPartial: '{{total}}개 중 {{count}}개 작업 완료',
      copyId: '플러그인 ID 복사',
      copied: '플러그인 ID가 클립보드에 복사되었습니다',
      copyFailed: '플러그인 ID를 복사할 수 없습니다',
      local: '로컬',
      collapseDetails: '세부 정보 숨기기',
      showDetails: '세부 정보 표시',
      updateInfo: '업데이트 정보',
      versionLabel: '버전:',
      configure: '구성',
      open: '열기',
      description: '플러그인을 설치하고 관리하여 기능을 확장합니다',
      storeUnavailable: '플러그인 스토어를 사용할 수 없습니다',
      summaryTotal: '전체 플러그인',
      summaryInstalled: '설치됨',
      summaryUpdates: '업데이트 가능',
      importFromFiles: '파일에서 가져오기',
      updateAll: '모두 업데이트',
      emptyStore: '플러그인 스토어가 현재 비어 있습니다. 향후 플러그인 업데이트를 기대해 주세요.'
    },
    optimization: {
      title: '시스템 최적화',
      info: '이 작업들은 시스템 서비스와 파일을 수정하며 관리자 권한이 필요할 수 있습니다.',
      tabs: {
        optimization: '최적화',
        cleanup: '정리',
        driverDownload: '드라이버 다운로드',
        networkAcceleration: '네트워크 가속'
      },
      recommended: '권장',
      selected: '선택됨',
      selectedActions: '선택된 작업',
      noSelection: '선택된 작업 없음',
      selectRecommended: '권장 항목 선택',
      applyRecommended: '모든 권장 항목 적용',
      apply: '적용',
      clear: '지우기 (되돌리기)',
      applied: '적용됨',
      applyFailed: '적용하지 못했습니다 (관리자 권한이 필요할 수 있음)',
      reverted: '되돌림',
      revertFailed: '되돌리지 못했습니다 (관리자 권한이 필요할 수 있음)',
      estimate: '크기 예측',
      estimateResult: '확보 가능한 공간',
      runCleanup: '정리 실행',
      cleanupHint: '정리는 사용자 지정 정리 규칙을 실행합니다.',
      cleanupConfirm: '지금 정리를 실행하시겠습니까?',
      cleanupDone: '정리 완료',
      cleanupFailed: '정리 실패',
      cleanup: {
        custom: {
          header: '사용자 지정 정리 규칙',
          description: '선택한 정리 작업과 함께 정리되는 추가 폴더.',
          empty: '사용자 지정 정리 규칙 없음',
          add: '폴더 추가',
          edit: '폴더 편집',
          remove: '제거',
          clear: '모두 지우기',
          added: '규칙이 추가되었습니다',
          updated: '규칙이 업데이트되었습니다',
          recursive: '하위 폴더 포함',
          noExtensions: '확장자가 지정되지 않았습니다',
          folderPickerFailed: '폴더 선택기를 열 수 없습니다'
        }
      },
      network: {
        status: '상태',
        running: '실행 중',
        stopped: '중지됨',
        backendReady: '백엔드 준비됨',
        backendNotReady: '백엔드 준비 안 됨',
        config: '기본 구성',
        accelerationEnabled: '가속 활성화',
        mode: '모드',
        modes: {
          off: '끔',
          systemProxy: '시스템 프록시',
          hosts: 'Hosts',
          diagnosticsOnly: '진단 전용'
        },
        save: '구성 저장',
        saved: '구성이 저장되었습니다',
        saveFailed: '구성을 저장하지 못했습니다',
        start: '시작',
        stop: '중지',
        startFailed: '시작하지 못했습니다',
        stopFailed: '중지하지 못했습니다',
        modeLabel: '모드',
        targetsLabel: '대상',
        portLabel: '포트',
        targetsHeading: '가속 대상',
        domainGroupsHint: '로컬 프록시를 통해 가속할 서비스를 선택하세요.',
        domainGroupsEmptyTitle: '가속 대상 없음',
        domainGroupsEmptyDescription: '대상 목록이 비어 있거나 검색과 일치하는 항목이 없습니다.',
        selectionHint: '선택한 대상은 가속 시작 시 적용됩니다.',
        searchTargets: '대상 검색',
        recommendedMenu: '권장',
        groupRuntime: '{{selected}}/{{total}} 선택됨  {{active}} 활성',
        trafficHeading: '트래픽 개요',
        metrics: {
          upload: '업로드',
          download: '다운로드',
          connections: '연결',
          total: '총 트래픽',
          health: '상태'
        },
        trafficLive: '실시간 프록시 트래픽 수집 중',
        trafficWaiting: '가속을 시작하면 실시간 트래픽을 수집합니다',
        trafficUnavailable: '트래픽 데이터를 일시적으로 사용할 수 없습니다',
        connectionsHeading: '현재 및 최근 연결',
        destinationsHeading: '대상 통계',
        connectionSummary: '{{active}} 활성 / {{total}} 전체',
        destinationSummary: '대상 {{count}}개',
        connectionStates: {
          active: '활성',
          completed: '완료됨',
          blocked: '차단됨',
          failed: '실패',
          stopped: '중지됨',
          unknown: '알 수 없음'
        },
        unknownHost: '알 수 없는 호스트',
        destinationRow: '연결 {{count}}개  {{latency}}',
        health: {
          healthy: '정상',
          degraded: '저하됨',
          stopped: '중지됨',
          unknown: '알 수 없음'
        },
        modeFull: {
          systemProxy: '시스템 프록시',
          hosts: 'Hosts 파일',
          diagnosticsOnly: '진단 전용',
          off: '유휴'
        },
        backendMissingHint: '프록시 워커를 사용할 수 없습니다',
        selectGroupsFirstHint: '대상을 하나 이상 선택하세요',
        advancedHeading: '고급',
        advancedBody: '고급 설정 및 네트워크 복구.',
        portFormat: '포트: {{port}}',
        dangerZoneHeading: '위험 구역',
        restoreHint: '가속 전에 기록된 원래 시스템 네트워크 상태를 복원합니다.',
        restoreNetwork: '네트워크 복원',
        restoreConfirm: '시스템 네트워크 상태를 지금 복원하시겠습니까?',
        restored: '네트워크 상태가 복원되었습니다',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: '감지',
          unknown: '알 수 없음',
          natTypes: {
            OpenInternet: '공개 NAT',
            Nat: 'NAT',
            UdpBlocked: 'UDP 차단됨',
            Unknown: '알 수 없음'
          },
          internetConnected: '연결됨',
          internetUnreachable: '연결할 수 없음',
          natType: 'NAT 유형',
          localIp: '로컬 IP',
          publicIp: '공용 IP',
          internet: '인터넷',
          dnsDomain: '도메인',
          customDns: '사용자 지정 DNS',
          enableDoh: 'DoH',
          dohUrl: 'DoH URL',
          latency: '지연 시간',
          resolvedAddress: '확인된 주소',
          latencyFormat: '{{ms}} ms',
          failed: '실패',
          ipv6Support: 'IPv6 지원',
          ipv6Address: 'IPv6 주소',
          ipv6SupportedFull: 'IPv6 액세스 지원됨',
          notSupported: '지원 안 됨'
        }
      },
      driverDownload: {
        comingSoon: '드라이버 다운로드는 향후 버전에서 제공될 예정입니다'
      },
      driver: {
        machineType: '머신 유형',
        machineTypePlaceholder: '예: 82K3',
        os: '운영 체제',
        downloadTo: '다운로드 위치',
        downloadToPlaceholder: '다운로드할 폴더 선택',
        browse: '찾아보기',
        openDownloadTo: '폴더 열기',
        source: '소스',
        primarySource: 'Vantage',
        primarySourceMessage: 'Vantage를 통한 공식 장치 데이터베이스.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'PC Support 호환성 데이터베이스.',
        scan: '스캔',
        scanning: '스캔 중…',
        scanValidation: '올바른 4자리 머신 유형을 입력하고 운영 체제를 선택하세요.',
        disclaimer: '패키지는 선택한 소스에서 제공됩니다. 설치는 본인 책임입니다.',
        filter: '필터',
        onlyShowUpdates: '업데이트만 표시',
        sort: {
          name: '이름순',
          category: '카테고리순',
          date: '날짜순'
        },
        selectRecommended: '권장 항목 선택',
        startAll: '모두 시작',
        pauseAll: '모두 일시 중지',
        clearSelection: '선택 지우기',
        packagesFound: '{{count}}개 패키지를 찾았습니다.',
        packagesFoundOne: '1개 패키지를 찾았습니다.',
        status: {
          NotStarted: '',
          Queued: '대기 중',
          Downloading: '다운로드 중',
          Installing: '설치 중',
          Completed: '완료됨',
          Error: '오류'
        },
        recommended: '권장',
        isUpdate: '업데이트',
        reboot: {
          recommended: '재시작 권장',
          required: '재시작 필요',
          shutdown: '종료 필요'
        },
        oldPackageWarning: '이 패키지는 1년 이상 지난 것으로 드라이버가 오래되었을 수 있습니다.',
        download: '다운로드',
        install: '설치',
        uninstall: '제거',
        pause: '일시 중지',
        openReadme: 'Readme 열기',
        hide: '숨기기',
        hideAll: '모두 숨기기',
        showHiddenDownloads: '숨겨진 다운로드 표시',
        downloadInProgress: {
          title: '다운로드 진행 중',
          message: '다운로드 작업이 아직 실행 중입니다. 다시 스캔하시겠습니까?',
          confirm: '스캔'
        },
        empty: {
          notScanned: {
            title: '드라이버 패키지 스캔',
            message: '소스를 선택하고 스캔하여 호환되는 드라이버 다운로드를 나열하세요.'
          },
          noResults: {
            title: '드라이버 다운로드를 찾을 수 없습니다',
            message: '다른 소스, 운영 체제 또는 머신 유형을 시도하세요.'
          },
          noFilterResults: {
            title: '일치하는 다운로드를 찾을 수 없습니다',
            message: '필터, 업데이트만 옵션 또는 숨겨진 다운로드 목록을 조정하세요.'
          },
          error: {
            title: '드라이버 스캔이 완료되지 않았습니다',
            message: '선택한 소스와 네트워크 연결을 확인한 후 다시 스캔하세요.'
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
      title: '정보',
      appName: '앱',
      version: '버전',
      build: '빌드',
      links: '프로젝트 링크',
      projectWebsite: 'GitHub의 프로젝트 웹사이트',
      latestRelease: 'GitHub의 최신 릴리스',
      applicationFolders: '앱 폴더',
      data: '데이터',
      temp: '임시',
      pid: '프로세스 ID',
      machine: '장치 모델',
      bios: 'BIOS 버전',
      compatible: '호환성',
      yes: '호환됨',
      no: '호환되지 않음',
      dataFolder: '데이터 폴더',
      thirdParty: '타사 라이브러리',
      copyright: '저작권'
    },
    statusBanner: {
      updateAvailable: '업데이트가 있습니다!',
      updateAvailableWithVersion: '{{version}} 업데이트가 있습니다!',
      pluginExtensionsDisabled: '플러그인 및 확장 탐색이 숨겨져 있습니다. 설정 → 탐색 항목에서 활성화하세요.'
    }
  }
}



