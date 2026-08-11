import legacy from './ru'

export default {
  translation: {
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'Журнал',
      openLogs: 'Открыть папку журналов',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'Информация об устройстве'
    },
    nav: {
      dashboard: 'Главная',
      settings: 'Настройки',
      automation: 'Автоматизация',
      keyboard: 'Клавиатура',
      keyboardBacklight: 'Подсветка клавиатуры',
      macro: 'Пользовательские макросы',
      windowsOptimization: 'Оптимизация системы',
      pluginExtensions: 'Плагины и расширения',
      about: 'О программе'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: 'Добро пожаловать! Выберите раздел ниже, чтобы начать',
      hostReady: 'Бэкенд подключён',
      hostState: 'Состояние бэкенда',
      hostVersion: 'Версия бэкенда',
      initComplete: 'Инициализация завершена',
      safeStart: 'Безопасный запуск, пропущено',
      machine: 'Устройство',
      compatible: 'Совместимость',
      status: 'Статус'
    },
    dashboard: {
      title: 'Главная',
      customize: 'Настроить',
      edit: {
        title: 'Редактировать главную',
        description: 'Выберите разделы и функции, отображаемые на главной странице.',
        showSensors: 'Датчики оборудования',
        groups: 'Группы функций',
        save: 'Сохранить',
        cancel: 'Отмена',
        saved: 'Раскладка главной сохранена',
        error: 'Не удалось сохранить раскладку главной',
        disclaimer: 'Некоторые функции могут не отображаться в зависимости от состояния и конфигурации устройства.',
        addGroup: 'Добавить',
        renameGroup: 'Изменить имя группы',
        deleteGroup: 'Удалить',
        moveUp: 'Вверх',
        moveDown: 'Вниз',
        deleteItem: 'Удалить',
        addItem: 'Добавить',
        groupNamePlaceholder: 'Имя',
        items: {
          discreteGpu: 'Режим дискретного GPU',
          overclockGpu: 'Разгон GPU',
          turnOffMonitors: 'Выключить мониторы'
        }
      },
      addItem: {
        title: 'Добавить',
        searchPlaceholder: 'Поиск',
        empty: 'Все элементы уже добавлены',
        addHint: 'Добавить элемент'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Память',
      temperature: 'Температура',
      usage: 'Использование',
      power: 'Мощность',
      fanSpeed: 'Вентилятор',
      vram: 'VRAM',
      memoryUsed: 'Использовано памяти',
      memoryTotal: 'Всего памяти',
      storageTemp: 'Температура накопителя',
      notAvailable: '--',
      sensor: {
        cpu: 'Процессор',
        gpu: 'Видеокарта',
        memory: 'Память',
        temperature: 'Температура',
        usage: 'Использование',
        power: 'Мощность',
        fanSpeed: 'Вентилятор',
        vram: 'VRAM',
        frequency: 'Частота ядра',
        battery: 'Аккумулятор',
        charge: 'Заряд',
        health: 'Здоровье',
        rate: 'Скорость',
        fan: 'Вентилятор',
        lowPowerAdapter: 'Подключён адаптер низкой мощности',
        batteryLow: 'Низкий заряд аккумулятора',
        acCharging: 'Адаптер подключён, зарядка…',
        acNotCharging: 'Адаптер подключён, зарядка не идёт…',
        remainingTime: 'Расчётное время работы: {0}',
        memoryTemperature: 'Температура памяти',
        ssdTemperature: 'Температура SSD',
        vramTemperature: 'Температура VRAM',
        vramUsage: 'Использование VRAM',
        cycles: 'Циклы',
        capacity: 'Ёмкость',
        fullCapacity: 'Полная ёмкость заряда',
        designCapacity: 'Проектная ёмкость',
        date: 'Дата',
        voltage: 'Напряжение ядра',
        voltageRange: 'Диапазон напряжения',
        powerRange: 'Диапазон мощности',
        details: 'Подробности',
        refreshInterval: 'Интервал обновления',
        detail: {
          power: 'Мощность',
          powerCores: 'Ядра',
          powerMemory: 'Память',
          powerPlatform: 'Платформа',
          pCoreClock: 'Частота P-ядер',
          eCoreClock: 'Частота E-ядер',
          memoryUsage: 'Использование памяти',
          sharedMemoryUsage: 'Использование общей памяти',
          vramUsage: 'Использование VRAM',
          hotSpot: 'Горячая точка GPU',
          pcieThroughput: 'Пропускная способность PCIe',
          designCapacity: 'Проектная ёмкость',
          fullChargeCapacity: 'Полная ёмкость заряда'
        }
      },
      group: {
        power: 'Питание',
        graphics: 'Графика',
        display: 'Дисплей',
        other: 'Другое',
        custom: 'Пользовательский'
      },
      card: {
        error: 'Не удалось применить настройку',
        config: 'Дополнительные настройки',
        configComingSoon: 'Дополнительные настройки появятся в будущей версии'
      }
    },
    balanceMode: {
      title: 'Настройки сбалансированного режима',
      aiEngine: 'Включить ИИ-движок',
      aiEngineDesc: 'Автоматически определяет запущенные игры и настраивает производительность CPU/GPU. Температура и шум вентиляторов могут возрасти.'
    },
    godMode: {
      title: 'Настройки пользовательского режима',
      activePreset: 'Активный профиль',
      presetName: 'Имя профиля',
      name: 'Имя',
      errorLoad: 'Не удалось загрузить настройку.',
      errorApply: 'Не удалось применить настройки',
      applySuccess: 'Настройки пользовательского режима применены.',
      defaultPresetName: 'Профиль',
      cpu: {
        title: 'CPU',
        longTermPL: 'Долгосрочный лимит мощности',
        'longTermPL.desc': 'Постоянное потребление мощности, достижимое процессором.',
        shortTermPL: 'Краткосрочный лимит мощности',
        'shortTermPL.desc': 'Пиковое потребление мощности за короткий промежуток времени.',
        peakPL: 'Пиковый лимит мощности',
        'peakPL.desc': 'Максимальное мгновенное потребление мощности процессором.',
        crossLoading: 'Долгосрочный лимит (перекрёстная нагрузка)',
        'crossLoading.desc': 'Максимальная мощность CPU при полной нагрузке CPU и GPU.',
        pl1Tau: 'Длительность краткосрочного лимита',
        'pl1Tau.desc': 'Время, в течение которого CPU может использовать краткосрочный лимит. По истечении применяется долгосрочный.',
        apuSppt: 'Лимит мощности APU sPPT',
        'apuSppt.desc': 'Пиковое потребление мощности с небольшой задержкой.',
        tempLimit: 'Лимит температуры CPU',
        'tempLimit.desc': 'Максимальная температура CPU до снижения частоты и мощности.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'Динамический буст',
        'dynamicBoost.desc': 'Дополнительная мощность, выделяемая GPU в зависимости от потребления CPU.',
        ctgp: 'Настраиваемый TGP',
        'ctgp.desc': 'Дополнительная мощность для GPU сверх базового потребления.',
        tempLimit: 'Лимит температуры GPU',
        'tempLimit.desc': 'Максимальная температура GPU до снижения частоты и мощности.',
        totalProcessingPowerTarget: 'Целевая мощность процессора от сети',
        'totalProcessingPowerTarget.desc': 'Точка, в которой CPU запускает динамическую регулировку мощности GPU.',
        toCpuDynamicBoost: 'Динамический буст GPU→CPU',
        'toCpuDynamicBoost.desc': 'Максимальная мощность, передаваемая от GPU к CPU в зависимости от загрузки CPU. Чем выше значение, тем лучше производительность CPU.'
      },
      fans: {
        title: 'Вентиляторы',
        curve: 'Кривая вентиляторов',
        curveMessage: 'Скорость вентиляторов следует за наибольшим показателем среди CPU, GPU и радиатора. Наведите курсор на шаг, чтобы увидеть точные значения.',
        maxSpeed: 'Максимальная скорость вентиляторов',
        maxSpeedWarning: 'Длительное использование этой опции изнашивает вентиляторы.\nБудьте осторожны с ней!'
      },
      advanced: {
        title: 'Дополнительно',
        message: 'Не меняйте параметры ниже, если не уверены в том, что делаете.',
        maxOffset: 'Максимальное смещение',
        maxOffsetWarning: 'Большие значения могут вызвать непредсказуемое поведение. Оставьте 0, если сомневаетесь.',
        minOffset: 'Минимальное смещение',
        minOffsetWarning: 'Меньшие значения могут вызвать непредсказуемое поведение. Оставьте 0, если сомневаетесь.',
        invalidOffset: 'Введите целое число перед сохранением.'
      },
      vantageWarning: 'Настройки пользовательского режима не будут применены корректно при работающем Lenovo Vantage или его службах.',
      legionZoneWarning: 'Настройки пользовательского режима не будут применены корректно при работающей Legion Zone или её службах.'
    },
    overclock: {
      title: 'Настройки разгона GPU',
      preset: 'Профиль',
      coreOffset: 'Смещение частоты ядра',
      memoryOffset: 'Смещение частоты памяти',
      namePlaceholder: 'Имя…',
      newProfileName: 'Профиль',
      loadError: 'Не удалось загрузить настройки разгона.'
    },
    feature: {
      powerMode: 'Режим производительности',
      'powerMode.desc': 'Изменить режим производительности.\nТакже можно изменить с помощью Fn+Q.',
      'powerMode.hint': 'Быстрое переключение — сочетание Fn+Q.',
      'powerMode.warning': 'Режим производительности может работать некорректно без подключённого адаптера.',
      battery: 'Режим зарядки аккумулятора',
      'battery.desc': 'Выберите режим зарядки. Режим консервации ограничивает заряд для продления срока службы, быстрая зарядка — заряжает с большей мощностью.',
      batteryNightCharge: 'Ночная зарядка',
      'batteryNightCharge.desc': 'При включении заряжает до 80 % ночью и до 100 % к утру.',
      alwaysOnUsb: 'Постоянное питание USB',
      'alwaysOnUsb.desc': 'Сохраняет питание портов USB при выключенном, спящем или гибернирующем компьютере.',
      instantBoot: 'Мгновенный запуск',
      'instantBoot.desc': 'Включает компьютер сразу при подключении питания.',
      flipToStart: 'Запуск при открытии',
      'flipToStart.desc': 'Открытие крышки автоматически включает ноутбук.',
      fnLock: 'Блокировка Fn',
      'fnLock.desc': 'При включении функции срабатывают без нажатия Fn. Для исходных клавиш F1–F12 нажимайте Fn вместе с ними.',
      gSync: 'G-Sync',
      'gSync.desc': 'Включить или выключить переменную частоту G-Sync',
      hdr: 'HDR',
      'hdr.desc': 'Включает HDR для встроенного дисплея.',
      'hdr.warning': 'Использование HDR заблокировано настройками Windows.',
      hybridMode: 'Гибридный режим',
      'hybridMode.desc': 'Гибридный режим позволяет переключаться между встроенным и дискретным GPU. Отключение включает режим прямого дискретного GPU; требуется перезагрузка.',
      igpuMode: 'Режим дискретного GPU',
      'igpuMode.desc': 'Принудительный вывод через встроенную графику для экономии энергии',
      refreshRate: 'Частота обновления',
      'refreshRate.desc': 'Переключает частоту обновления встроенного дисплея.',
      itsMode: 'Режим ITS',
      'itsMode.desc': 'Интеллектуальное терморешение',
      microphone: 'Микрофон',
      'microphone.desc': 'При выключении все доступные микрофоны будут отключены.',
      overDrive: 'Over Drive',
      'overDrive.desc': 'Улучшает время отклика встроенного дисплея. Может вызывать шлейфы (гостинг).',
      panelLogo: 'Подсветка логотипа Legion',
      'panelLogo.desc': 'Включает или выключает подсветку логотипа Legion на задней панели.',
      portsBacklight: 'Подсветка портов',
      'portsBacklight.desc': 'Включает или выключает подсветку портов на задней панели.',
      resolution: 'Разрешение',
      'resolution.desc': 'Переключает разрешение встроенного дисплея.',
      dpiScale: 'Масштаб DPI',
      'dpiScale.desc': 'Переключает масштаб встроенного дисплея.',
      speaker: 'Динамик',
      touchpadLock: 'Блокировка тачпада',
      'touchpadLock.desc': 'Отключает тачпад. Рекомендуется при использовании мыши.',
      whiteKeyboard: 'Подсветка клавиатуры',
      'whiteKeyboard.desc': 'Используйте Fn + Пробел для переключения и регулировки яркости подсветки.',
      winKey: 'Отключить клавишу Win',
      'winKey.desc': 'Только для встроенной клавиатуры. Клавиша Win перестанет реагировать.',
      oneLevelWhiteKeyboard: 'Подсветка клавиатуры',
      'oneLevelWhiteKeyboard.desc': 'Используйте сочетание Fn + Пробел для переключения подсветки.',
      'hybridMode.states.hybrid': 'Гибридный',
      'hybridMode.states.hybridIGPUOnly': 'Гибрид-iGPU',
      'hybridMode.states.hybridAuto': 'Гибрид-авто',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'О режимах работы GPU',
      'hybridMode.info.hybrid.title': 'Гибридный режим',
      'hybridMode.info.hybrid.message': 'Встроенный и дискретный GPU включены. Система автоматически переключается между ними.',
      'hybridMode.info.hybridIgpu.title': 'Только гибрид-iGPU',
      'hybridMode.info.hybridIgpu.message': 'Используется только встроенный GPU. Минимизирует потребление и шум.',
      'hybridMode.info.hybridIgpu.disclaimer': 'Режим действует только при неработающем дискретном GPU.',
      'hybridMode.info.hybridAuto.title': 'Гибрид-авто режим',
      'hybridMode.info.hybridAuto.message': 'От аккумулятора — только встроенный GPU, от сети — оба. При нестандартном адаптере переключается на режим только iGPU.',
      'hybridMode.info.dgpu.title': 'Режим dGPU',
      'hybridMode.info.dgpu.message': 'Используется только дискретный GPU. Лучшая графика, но выше потребление.',
      'hybridMode.info.dgpu.disclaimer': 'Переход в этот режим и обратно требует перезагрузки.',
      'hybridMode.restartRequired.title': 'Требуется перезагрузка',
      'hybridMode.restartRequired.message': 'Переключение на {{mode}} требует перезагрузки. Перезагрузить сейчас?',
      'hybridMode.restartRequired.now': 'Перезагрузить сейчас',
      'hybridMode.restartRequired.later': 'Перезагружу позже',
      'hybridMode.restartFailed': 'Не удалось перезагрузить автоматически. Перезагрузитесь вручную для завершения.',
      'hybridMode.changeFailed.title': 'Не удалось изменить режим GPU',
      'hybridMode.changeFailed.message': 'Повторите попытку через несколько секунд. Если dGPU не реагирует, перезагрузите ноутбук.',
      batteryModes: {
        conservation: 'Режим консервации',
        normal: 'Обычный режим',
        rapidCharge: 'Быстрая зарядка'
      },
      powerModeOptions: {
        quiet: 'Тихий',
        balance: 'Сбалансированный',
        performance: 'Производительность',
        extreme: 'Экстремальный',
        godMode: 'Пользовательский'
      }
    },
    common: {
      loading: 'Загрузка…',
      error: 'Что-то пошло не так',
      retry: 'Повторить',
      close: 'Закрыть',
      cancel: 'Отмена',
      moreActions: 'Другие действия',
      copied: 'Скопировано в буфер обмена',
      add: 'Добавить',
      save: 'Сохранить',
      saveAndClose: 'Сохранить и закрыть',
      apply: 'Применить',
      applyAndClose: 'Применить и закрыть',
      default: 'По умолчанию',
      rename: 'Переименовать',
      delete: 'Удалить',
      ok: 'ОК'
    },
    colorPicker: {
      hex: 'Hex',
      red: 'Красный',
      green: 'Зелёный',
      blue: 'Синий',
      ok: 'ОК'
    },
    fanCurve: {
      fanSpeed: 'Скорость вентилятора',
      fanSpeedMax: '100 %',
      cpu: 'CPU',
      cpuSensor: 'Датчик CPU',
      gpu: 'GPU',
      gpu2: 'GPU №2',
      rpm: 'об/мин'
    },
    pages: {
      placeholder: 'Скоро появится'
    },
    settings: {
      title: 'Настройки',
      description: 'Настройте внешний вид, поведение и функции приложения.',
      nav: {
        appearance: 'Внешний вид',
        application: 'Приложение',
        power: 'Питание',
        display: 'Дисплей',
        smartKeys: 'Умные клавиши',
        update: 'Обновление',
        integrations: 'Интеграции',
        osd: 'OSD'
      },
      appearance: {
        language: 'Язык',
        languageDesc: 'Выберите язык',
        temperature: 'Температура',
        temperatureDesc: 'Выберите единицу измерения для датчиков температуры.',
        theme: 'Тема',
        accentColor: 'Акцентный цвет',
        accentColorDesc: 'Изменить акцентный цвет приложения.',
        appScale: 'Масштаб интерфейса',
        appScaleDesc: 'Масштабирует текст и весь интерфейс независимо от масштаба Windows.',
        themeOptions: {
          system: 'Система',
          light: 'Светлая',
          dark: 'Тёмная'
        }
      },
      application: {
        minimizeToTray: 'Сворачивать в трей',
        minimizeToTrayDesc: 'Всегда сворачивать в трей вместо панели задач.',
        minimizeOnClose: 'Сворачивать при закрытии',
        minimizeOnCloseDesc: 'Всегда сворачивать в трей. Для выхода щёлкните правой кнопкой по значку трея и выберите «Закрыть».',
        disableUnsupportedWarning: 'Не предупреждать о несовместимых устройствах',
        disableUnsupportedWarningDesc: 'Скрывает предупреждение о несовместимом устройстве при запуске.',
        enableHardwareSensors: 'Датчики оборудования',
        enableHardwareSensorsDesc: 'Включает расширенный опрос оборудования для мониторинга температуры, частоты и лимитов мощности.',
        dontShowNotifications: 'Не показывать уведомления',
        dontShowNotificationsDesc: 'Отключает уведомления в приложении и системе',
        autorun: 'Запуск при входе',
        autorunDesc: 'Запускаться свёрнутым в системный трей после входа в Windows.',
        extensionsEnabled: 'Включить расширения',
        extensionsEnabledDesc: 'Включить загрузку плагинов и расширений',
        sensorSections: 'Разделы датчиков',
        sensorSectionsDesc: 'Выберите отображаемые разделы датчиков и их порядок.',
        disableVantage: 'Отключить Lenovo Vantage',
        disableVantageDesc: 'Отключает Lenovo Vantage и ImController без удаления.\nПосле изменения рекомендуется перезагрузка.',
        disableLegionZone: 'Отключить Legion Zone',
        disableLegionZoneDesc: 'Отключает Legion Zone и её службу без удаления.\nПосле изменения рекомендуется перезагрузка.',
        disableLenovoHotkeys: 'Отключить Lenovo Hotkeys',
        disableLenovoHotkeysDesc: 'Отключает Lenovo Hotkeys и их службу без удаления.\nЕсли отключено, это приложение обрабатывает сочетания Fn.\nПосле изменения рекомендуется перезагрузка.',
        valueOn: 'Вкл',
        valueOff: 'Выкл'
      },
      saved: 'Настройки сохранены',
      saveFailed: 'Не удалось сохранить настройки',
      osd: {
        title: 'OSD',
        showOsd: 'Показать OSD',
        showOsdDesc: 'Немедленно показать экранную панель.',
        style: 'Стиль оверлея',
        styles: {
          panel: 'Панель',
          bar: 'Полоса'
        },
        refreshInterval: 'Интервал обновления',
        snapThreshold: 'Порог привязки',
        lockPosition: 'Закрепить позицию',
        resetPosition: 'Сбросить позицию',
        previewHint: 'Предпросмотр',
        tabs: {
          general: 'Общие',
          appearance: 'Внешний вид',
          thresholds: 'Пороги',
          sensors: 'Датчики'
        },
        opacity: 'Непрозрачность',
        cornerRadius: 'Радиус углов',
        cornerRadiusTop: 'Верх',
        cornerRadiusBottom: 'Низ',
        fontSize: 'Размер шрифта',
        background: 'Цвет фона',
        category: 'Цвет категории',
        label: 'Цвет метки',
        value: 'Цвет значения',
        warning: 'Цвет предупреждения',
        critical: 'Критический цвет',
        separator: 'Цвет разделителя',
        thresholds: {
          performance: 'Производительность',
          fpsRedline: 'Красная линия FPS',
          lowFpsDelta: 'Низкий дельта FPS',
          temperature: 'Температура',
          usage: 'Использование',
          warning: 'Предупреждение',
          critical: 'Критично'
        },
        items: {
          groups: {
            game: 'Игра',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1 % Low',
            FrameTime: 'Время кадра',
            CpuFrequency: 'Частота ядра',
            CpuPCoreFrequency: 'Частота P-ядер',
            CpuECoreFrequency: 'Частота E-ядер',
            CpuUtilization: 'Использование',
            CpuTemperature: 'Температура',
            CpuPower: 'Мощность',
            CpuFan: 'Вентилятор',
            GpuFrequency: 'Частота ядра',
            GpuUtilization: 'Использование',
            GpuTemperature: 'Температура ядра',
            GpuVramUtilization: 'Использование VRAM',
            GpuVramTemperature: 'Температура VRAM',
            GpuPower: 'Мощность',
            GpuFan: 'Вентилятор',
            MemoryUtilization: 'Использование',
            MemoryTemperature: 'Температура',
            Disk1Temperature: 'Температура диска 1',
            Disk2Temperature: 'Температура диска 2',
            PchTemperature: 'Температура PCH',
            PchFan: 'Вентилятор'
          }
        }
      },
      power: {
        powerModeMapping: 'Сопоставление режимов питания',
        powerModeMappingDesc: 'При смене режима производительности синхронно менять план или режим питания Windows.',
        mappingModes: {
          disabled: 'Отключено',
          windowsPowerMode: 'Режим питания Windows',
          windowsPowerPlan: 'План питания Windows'
        },
        windowsPowerModes: 'Режим питания Windows',
        windowsPowerModesDesc: 'Выберите режим питания Windows, применяемый при смене режима.',
        windowsPowerPlans: 'План питания Windows',
        windowsPowerPlansDesc: 'Выберите план питания Windows, применяемый при смене режима.',
        synchronizeBrightness: 'Фиксировать яркость экрана',
        synchronizeBrightnessDesc: 'При включении яркость остаётся одинаковой между планами питания.',
        smartFnLock: 'Клавиши-модификаторы Smart Fn Lock',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: 'Сбрасывать «От батареи с» при запуске',
        resetBatteryOnSinceDesc: 'Сбрасывает счётчик «От батареи с» в разделе аккумулятора при перезагрузке системы.',
        godModeFnQ: 'Переключение в пользовательский режим через Fn+Q',
        godModeFnQDesc: 'Позволяет быстро переключаться в пользовательский режим через Fn+Q.'
      },
      display: {
        navigationItems: 'Видимость элементов навигации',
        navigationKeys: {
          keyboard: 'Подсветка клавиатуры',
          battery: 'Аккумулятор',
          automation: 'Автоматизация',
          macro: 'Макрос',
          windowsOptimization: 'Оптимизация Windows',
          pluginExtensions: 'Плагины и расширения',
          about: 'О программе'
        },
        notificationPosition: 'Позиция уведомлений',
        notificationPositions: {
          bottomRight: 'Внизу справа',
          bottomCenter: 'Внизу по центру',
          bottomLeft: 'Внизу слева',
          centerLeft: 'Слева по центру',
          topLeft: 'Вверху слева',
          topCenter: 'Вверху по центру',
          topRight: 'Вверху справа',
          centerRight: 'Справа по центру',
          center: 'По центру'
        },
        notificationDuration: 'Длительность уведомлений',
        notificationDurations: {
          short: 'Короткая (3 с)',
          normal: 'Обычная (5 с)',
          long: 'Длинная (10 с)'
        },
        excludedRefreshRates: 'Исключённые частоты обновления',
        excludedRefreshRatesDesc: 'Исключите частоты, чтобы ускорить переключение Fn+R.',
        excludedRefreshRatesHint: 'Расширенное редактирование появится в будущей версии',
        excludedRefreshRatesEmpty: 'Нет исключённых частот обновления',
        excludedRefreshRatesManageHint: 'Нажмите, чтобы управлять исключёнными частотами',
        notifications: 'Уведомления',
        notificationsDesc: 'Выберите, какие уведомления показывать.',
        bootLogo: 'Логотип загрузки',
        bootLogoDesc: 'Настройте логотип, отображаемый при запуске.'
      },
      smartKeys: {
        smartFnLock: 'Умная блокировка Fn',
        smartFnLockDesc: 'При нажатии Alt, Ctrl или Shift Fn временно разблокируется.',
        off: 'Выкл',
        hint: 'Клавиши-модификаторы Smart Fn Lock можно изменить в настройках питания.',
        singlePressActionDesc: 'Назначьте быстрое действие на одинарное нажатие Fn+F9.',
        doublePressActionDesc: 'Назначьте быстрое действие на двойное нажатие Fn+F9.'
      },
      update: {
        frequency: 'Проверять обновления автоматически',
        frequencies: {
          perHour: 'Каждый час',
          perThreeHours: 'Каждые 3 часа',
          perTwelveHours: 'Каждые 12 часов',
          perDay: 'Каждый день',
          perWeek: 'Каждую неделю',
          perMonth: 'Каждый месяц'
        },
        includePrerelease: 'Включать предварительные версии',
        includePrereleaseDesc: 'Выкл: только стабильные; вкл: также предварительные (бета) версии.',
        repository: 'Репозиторий обновлений',
        repositoryDesc: 'Настройте репозиторий GitHub для проверки обновлений. Пусто — значение по умолчанию.',
        repositoryOwner: 'Владелец репозитория',
        repositoryOwnerPlaceholder: 'например, SSC-STUDIO',
        repositoryName: 'Имя репозитория',
        repositoryNamePlaceholder: 'например, UniversalDeviceToolkit',
        check: 'Проверить обновления',
        comingSoon: 'Проверка обновлений появится в будущей версии'
      },
      checkResult: {
        available: 'Доступна новая версия: v{{version}}',
        latest: 'У вас актуальная версия'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'Делится скоростью вентиляторов, температурой аккумулятора и другими данными с HWiNFO64. После переключения может потребоваться перезапуск HWiNFO64.',
        cli: 'Интерфейс командной строки',
        cliDesc: 'Включить интерфейс командной строки для управления из командной строки.'
      }
    },
    keyboard: {
      title: 'Подсветка клавиатуры',
      unsupported: 'Подсветка клавиатуры не поддерживается на этом устройстве',
      rgb: {
        preset: 'Профиль',
        settings: 'Настройки подсветки',
        effect: 'Эффект',
        speed: 'Скорость',
        brightness: 'Яркость',
        zones: 'Цвета зон',
        synchroniseZones: 'Синхронизировать зоны',
        presets: {
          off: 'Выкл',
          one: 'Профиль 1',
          two: 'Профиль 2',
          three: 'Профиль 3',
          four: 'Профиль 4'
        },
        effectOptions: {
          static: 'Статичный',
          breath: 'Дыхание',
          smooth: 'Плавный',
          waveRtl: 'Волна (справа→слева)',
          waveLtr: 'Волна (слева→справа)'
        },
        speedOptions: {
          slowest: 'Медленнее',
          slow: 'Медленно',
          fast: 'Быстро',
          fastest: 'Быстрее'
        },
        brightnessOptions: {
          low: 'Низкая',
          high: 'Высокая'
        }
      },
      spectrum: {
        brightness: 'Яркость',
        profile: 'Профиль',
        logo: 'Логотип',
        effects: 'Эффекты',
        colors: 'Цвета',
        addEffect: 'Добавить эффект',
        deleteEffect: 'Удалить',
        noEffects: 'Нет эффектов',
        selectAll: 'Выбрать все зоны',
        deselectAll: 'Снять выбор со всех зон',
        switchLayout: 'Сменить раскладку клавиатуры',
        editEffect: 'Изменить',
        allKeys: 'Все клавиши',
        zonesCount: '{{count}} зон',
        noLayoutHint: 'Не удалось загрузить раскладку клавиатуры.',
        selectEffectHint: 'Выберите эффект ниже, чтобы просмотреть и отредактировать его клавиши.',
        effectEdit: {
          addTitle: 'Добавить эффект',
          editTitle: 'Изменить эффект',
          effect: 'Эффект',
          speed: 'Скорость',
          direction: 'Направление',
          clockwiseDirection: 'Направление',
          color: 'Цвет',
          colors: 'Цвета',
          addColor: 'Добавить цвет',
          keys: 'Клавиши',
          alwaysWarning: 'Этот эффект будет применён ко всей клавиатуре и заменит все остальные эффекты.'
        },
        effectTypes: {
          always: 'Постоянный',
          rainbowScrew: 'Радужный винт',
          rainbowWave: 'Радужная волна',
          colorChange: 'Смена цвета',
          colorWave: 'Цветная волна',
          colorPulse: 'Цветовой пульс',
          smooth: 'Плавный',
          rain: 'Дождь',
          ripple: 'Круги',
          type: 'Печать',
          audioBounce: 'Звуковой отскок',
          audioRipple: 'Звуковые круги',
          auroraSync: 'Синхронизация Aurora'
        }
      }
    },
    automation: {
      title: 'Автоматизация',
      enable: 'Включить автоматизацию',
      enableDesc: 'Universal Device Toolkit должен быть запущен, чтобы автоматические действия работали.',
      subtitle: 'При включении это приложение проверяет и выполняет подходящие действия при изменении состояния устройства.',
      actionsTitle: 'Действия',
      actionsEmpty: 'Автоматических действий пока нет',
      quickActionsTitle: 'Быстрые действия',
      quickActionsEmpty: 'Быстрых действий пока нет. Нажмите «Создать», чтобы добавить.',
      renamePipeline: 'Переименовать конвейер',
      renamePipelineTitle: 'Переименовать конвейер',
      renamePipelinePlaceholder: 'Введите имя конвейера',
      changeIcon: 'Сменить значок',
      empty: 'Скриптов автоматизации пока нет. Нажмите «Создать», чтобы добавить.',
      runNow: 'Выполнить сейчас',
      delete: 'Удалить',
      deleteStep: 'Удалить шаг',
      addPipeline: 'Создать',
      addStep: 'Добавить шаг',
      configure: 'Настроить',
      stepType: 'Тип шага',
      steps: 'Шаги',
      save: 'Сохранить',
      revert: 'Отменить',
      pipelineName: 'Имя конвейера',
      pipelineNamePlaceholder: 'Введите имя конвейера',
      quickAction: 'Быстрое действие',
      optionsLoading: 'Загрузка параметров…',
      stepLabels: {
        rgbKeyboardBacklight: 'Подсветка клавиатуры',
        run: 'Запуск',
        showMainWindow: 'Показать главное окно',
        speaker: 'Динамик',
        spectrumKeyboardBacklightBrightness: 'Яркость подсветки',
        spectrumKeyboardBacklightImportProfile: 'Импорт профиля подсветки',
        spectrumKeyboardBacklightProfile: 'Профиль подсветки',
        touchpadLock: 'Блокировка тачпада',
        turnOffMonitors: 'Выключить дисплеи',
        turnOffWiFi: 'Выключить Wi-Fi',
        turnOnWiFi: 'Включить Wi-Fi',
        whiteKeyboardBacklight: 'Подсветка клавиатуры',
        winKey: 'Блокировка клавиши Windows',
        scriptPath: 'Путь к исполняемому файлу',
        scriptArguments: 'Аргументы',
        runSilently: 'Запуск в фоне',
        runSilentlyDesc: 'Запускает консольные приложения без создания окна консоли.',
        runWaitUntilFinished: 'Ждать завершения',
        runWaitUntilFinishedDesc: 'Ждать завершения программы или скрипта',
        runHint: 'Запускает скрипт или программу.\nУбедитесь, что скрипт работает корректно.',
        importProfilePath: 'Путь',
        browse: 'Обзор',
        off: 'Выкл',
        on: 'Вкл',
        mute: 'Без звука',
        unmute: 'Со звуком',
        low: 'Низко',
        high: 'Высоко',
        presetOne: 'Профиль 1',
        presetTwo: 'Профиль 2',
        presetThree: 'Профиль 3',
        presetFour: 'Профиль 4',
        values: {
          off: 'Выкл',
          on: 'Вкл',
          mute: 'Без звука',
          unmute: 'Со звуком',
          low: 'Низко',
          high: 'Высоко',
          presetOne: 'Профиль 1',
          presetTwo: 'Профиль 2',
          presetThree: 'Профиль 3',
          presetFour: 'Профиль 4'
        }
      },
      state: {
        on: 'Вкл',
        off: 'Выкл',
        hidden: 'Скрыть',
        show: 'Показать',
        toggle: 'Переключить состояние',
        quiet: 'Тихий',
        balance: 'Сбалансированный',
        performance: 'Производительность',
        extreme: 'Экстремальный',
        godMode: 'Пользовательский',
        hybrid: 'Гибридный',
        hybridIgpu: 'Гибрид-iGPU',
        hybridAuto: 'Гибрид-авто',
        dgpu: 'dGPU',
        acAdapter: 'Адаптер питания',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'Адаптер и USB PD',
        hz: '{{frequency}} Гц',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'Режим работы GPU',
          desc: 'Выберите режим работы GPU в зависимости от использования и питания.\nСмена режима может потребовать перезагрузки.'
        },
        instantBoot: {
          title: 'Мгновенный запуск',
          desc: 'Включает ноутбук при подключении зарядного устройства.'
        },
        macro: {
          title: 'Макрос',
          desc: 'Включает или отключает макросы.'
        },
        microphone: {
          title: 'Микрофон',
          desc: 'При выключении микрофоны будут отключены.'
        },
        notification: {
          title: 'Показать уведомление',
          desc: 'Показывает уведомление с введённым текстом.',
          placeholder: 'Текст уведомления'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'Подсветка клавиатуры',
          desc: 'Включает или выключает подсветку.'
        },
        osd: {
          title: 'OSD',
          desc: 'Показать или скрыть OSD'
        },
        overclockDiscreteGPU: {
          title: 'Разгон GPU',
          desc: 'Повышает производительность разгоном дискретного GPU.\n\nВНИМАНИЕ: действие не сработает, если дискретный GPU недоступен.'
        },
        overDrive: {
          title: 'Over Drive',
          desc: 'Улучшает время отклика встроенного дисплея.'
        },
        panelLogoBacklight: {
          title: 'Подсветка логотипа',
          desc: 'Включает или выключает подсветку логотипа на крышке.'
        },
        playSound: {
          title: 'Воспроизвести звук',
          desc: 'Поддерживаются распространённые форматы, такие как wav или mp3.',
          browse: 'Обзор…',
          none: 'Файл не выбран'
        },
        portsBacklight: {
          title: 'Подсветка портов',
          desc: 'Включает или выключает подсветку портов на задней панели.'
        },
        powerMode: {
          title: 'Режим производительности',
          desc: 'Изменить режим производительности.'
        },
        quickAction: {
          title: 'Быстрое действие',
          desc: 'Выполняет сохранённое быстрое действие.',
          placeholder: 'Выберите быстрое действие',
          empty: 'Быстрых действий пока нет. Создайте сначала конвейер без триггера.'
        },
        refreshRate: {
          title: 'Частота обновления',
          desc: 'Изменяет частоту обновления встроенного дисплея.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен.',
          empty: 'Нет доступных частот обновления'
        },
        resolution: {
          title: 'Разрешение',
          desc: 'Изменяет разрешение встроенного дисплея.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен.',
          empty: 'Нет доступных разрешений'
        },
        alwaysOnUsb: {
          title: 'Постоянное питание USB',
          desc: 'Заряжает USB-устройства, когда ноутбук выключен, спит или в гибернации.',
          options: {
            OnWhenSleeping: 'Вкл при сне',
            OnAlways: 'Всегда вкл'
          }
        },
        battery: {
          title: 'Режим аккумулятора',
          desc: 'Выберите, как заряжается аккумулятор.',
          options: {
            Conservation: 'Консервация',
            Normal: 'Обычный',
            RapidCharge: 'Быстрая зарядка'
          }
        },
        batteryNightCharge: {
          title: 'Ночная зарядка',
          desc: 'При включении устройство заряжается до 80 % ночью и до 100 % к утру.'
        },
        deactivateGPU: {
          title: 'Деактивировать GPU',
          desc: 'Отключает дискретный GPU, если он активен без необходимости.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен или гибридный режим неактивен.',
          options: {
            KillApps: 'Закрыть приложения',
            RestartGPU: 'Перезапустить GPU'
          }
        },
        delay: {
          title: 'Задержка',
          desc: 'Добавляет задержку перед следующим шагом.',
          second_one: '{{count}} секунда',
          second_few: '{{count}} секунды',
          second_many: '{{count}} секунд',
          second_other: '{{count}} секунды'
        },
        displayBrightness: {
          title: 'Яркость дисплея',
          desc: 'Изменяет яркость встроенного дисплея.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен.',
          percent: '{{value}} %'
        },
        dpiScale: {
          title: 'DPI',
          desc: 'Изменяет масштаб встроенного дисплея.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен.',
          percent: '{{value}} %'
        },
        flipToStart: {
          title: 'Запуск при открытии',
          desc: 'Включает ноутбук при открытии крышки.'
        },
        fnLock: {
          title: 'Блокировка Fn',
          desc: 'Использует вторичные функции F1-F12 без удержания Fn.'
        },
        godModePreset: {
          title: 'Профиль пользовательского режима',
          desc: 'Активирует профиль пользовательского режима.\nДействует только при включённом пользовательском режиме.'
        },
        hdr: {
          title: 'HDR',
          desc: 'Включает HDR на встроенном дисплее.\n\nВНИМАНИЕ: действие не сработает, если встроенный дисплей выключен.'
        },
        hideMainWindow: {
          title: 'Скрыть главное окно'
        },
        rgbKeyboardBacklight: {
          title: 'Подсветка клавиатуры',
          desc: 'Настраивает профиль подсветки.'
        },
        run: {
          title: 'Запуск',
          desc: 'Запускает скрипт или программу.\nУбедитесь, что скрипт работает корректно.'
        },
        showMainWindow: {
          title: 'Показать главное окно'
        },
        speaker: {
          title: 'Динамик',
          desc: 'При выключении звука все активные устройства вывода будут отключены.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'Яркость подсветки',
          desc: 'Настраивает яркость подсветки.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'Импорт профиля подсветки',
          desc: 'Импортирует и применяет конфигурацию подсветки к текущему профилю.'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'Профиль подсветки',
          desc: 'Настраивает профиль подсветки.'
        },
        touchpadLock: {
          title: 'Блокировка тачпада',
          desc: 'Отключает тачпад.'
        },
        turnOffMonitors: {
          title: 'Выключить дисплеи',
          desc: 'Выключает все доступные дисплеи.'
        },
        turnOffWiFi: {
          title: 'Выключить Wi-Fi'
        },
        turnOnWiFi: {
          title: 'Включить Wi-Fi'
        },
        whiteKeyboardBacklight: {
          title: 'Подсветка клавиатуры',
          desc: 'Настраивает яркость подсветки.'
        },
        winKey: {
          title: 'Блокировка клавиши Windows',
          desc: 'Отключает клавишу Windows на встроенной клавиатуре.'
        }
      },
      moveUp: 'Вверх',
      moveDown: 'Вниз',
      noEditableParameters: 'У этого шага нет редактируемых параметров.',
      addAutomaticPipeline: 'Новое действие',
      addQuickAction: 'Новое быстрое действие',
      quickActionName: 'Имя быстрого действия',
      triggerPicker: {
        title: 'Новое действие — выберите триггер'
      },
      triggerConfig: {
        title: 'Настройка триггера',
        noEditableTriggers: 'У этого триггера нет настраиваемых параметров.'
      },
      triggerNames: {
        aCAdapterConnected: 'Когда подключён адаптер питания',
        lowWattageACAdapterConnected: 'Когда подключён адаптер низкой мощности',
        aCAdapterDisconnected: 'Когда отключён адаптер питания',
        powerMode: 'Когда изменён режим производительности',
        godModePresetChanged: 'Когда изменён профиль пользовательского режима',
        gamesAreRunning: 'Когда запущена игра',
        gamesStop: 'Когда игра закрыта',
        processesAreRunning: 'Когда запущено приложение',
        processesStopRunning: 'Когда закрыто приложение',
        userInactivity: 'Когда пользователь стал неактивен',
        userInactivityZero: 'Когда пользователь стал активен',
        sessionLock: 'Сеанс заблокирован',
        sessionUnlock: 'Сеанс разблокирован',
        lidOpened: 'Крышка открыта',
        lidClosed: 'Крышка закрыта',
        displayOn: 'Когда дисплеи включаются',
        displayOff: 'Когда дисплеи выключаются',
        hdrOn: 'Когда включается HDR',
        hdrOff: 'Когда выключается HDR',
        deviceConnected: 'Когда подключено устройство',
        deviceDisconnected: 'Когда отключено устройство',
        externalDisplayConnected: 'Когда подключён внешний дисплей',
        externalDisplayDisconnected: 'Когда отключён внешний дисплей',
        wiFiConnected: 'Когда подключён Wi-Fi',
        wiFiDisconnected: 'Когда отключён Wi-Fi',
        time: 'В указанное время',
        periodic: 'Периодическое действие',
        hardwareSensor: 'Датчик оборудования',
        batteryPercentage: 'Процент заряда аккумулятора',
        onStartup: 'При запуске',
        onResume: 'При возобновлении'
      },
      triggerEditors: {
        noProcesses: 'Процессы не выбраны.',
        noDevices: 'Устройства не выбраны.',
        inactivityTimeout: 'Тайм-аут',
        seconds: '{{count}} секунд',
        minutes: '{{count}} минут',
        hours: '{{count}} часов',
        ssidPlaceholder: 'Имя сети (SSID)',
        addSsid: 'Добавить имя сети',
        atTime: 'Время',
        hour: 'Час',
        minute: 'Минута',
        allDays: 'Каждый день',
        day: {
          0: 'Воскресенье',
          1: 'Понедельник',
          2: 'Вторник',
          3: 'Среда',
          4: 'Четверг',
          5: 'Пятница',
          6: 'Суббота'
        },
        metric: 'Метрика',
        comparison: 'Сравнение',
        threshold: 'Порог',
        thresholdPercent: 'Порог (%)',
        durationSeconds: 'Длительность (секунды)',
        cooldownSeconds: 'Перезарядка (секунды)',
        chargeFilter: 'Фильтр заряда',
        deviceInstanceId: 'ID экземпляра устройства'
      }
    },
    macro: {
      title: 'Макросы клавиатуры',
      enable: 'Включить макросы',
      enableDesc: 'Universal Device Toolkit должен быть запущен, чтобы макросы работали.',
      subtitle: 'Можно записать серию нажатий и вызывать её с помощью цифровой клавиатуры.',
      numpad: 'Цифровая клавиатура',
      sequence: 'Последовательность',
      repeat: 'Повторов',
      events: 'События',
      save: 'Сохранить',
      clear: 'Очистить',
      play: 'Воспроизвести',
      record: 'Записать',
      recordingOptions: 'Параметры записи',
      ignoreDelays: 'Игнорировать задержки',
      interruptOnOtherKey: 'Прерывать при другой клавише',
      dontRepeat: 'Не повторять',
      keyboardOnly: 'Только клавиатура',
      keyboardMouse: 'Клавиатура и кнопки мыши',
      allInputs: 'Все вводы',
      recordingInterrupted: 'Запись прервана',
      keyboard: 'Клавиатура',
      mouse: 'Мышь',
      move: 'Движение мыши',
      wheelUp: 'Колесо вверх',
      wheelDown: 'Колесо вниз',
      wheelLeft: 'Колесо влево',
      wheelRight: 'Колесо вправо',
      leftButton: 'Левая кнопка',
      rightButton: 'Правая кнопка',
      middleButton: 'Средняя кнопка',
      xButton: 'Кнопка X',
      button: 'Кнопка мыши',
      empty: 'Для этой клавиши ещё нет последовательности макроса',
      recording: {
        preparing: 'Запись начнётся через 3 секунды…',
        title: 'Запись…',
        pressEscToStop: 'Нажмите ESC для остановки.',
        focusHint: 'Держите это окно в фокусе во время записи.'
      }
    },
    plugins: {
      title: 'Плагины и расширения',
      search: 'Поиск плагинов',
      filterAll: 'Все',
      filterInstalled: 'Установлены',
      filterNotInstalled: 'Не установлены',
      refresh: 'Обновить',
      total: 'Всего: {{count}}',
      summary: 'Установлено: {{count}}',
      updatable: 'Доступно обновлений: {{count}}',
      install: 'Установить',
      update: 'Обновить',
      updateAvailable: 'Доступно обновление',
      uninstall: 'Удалить',
      uninstallConfirm: 'Удалить этот плагин?',
      uninstallFailed: 'Не удалось удалить',
      installed: 'Установлен',
      online: 'Онлайн',
      installing: 'Установка…',
      downloading: 'Скачивание…',
      preparingDownload: 'Подготовка скачивания…',
      downloadCompleted: 'Скачивание завершено',
      offline: 'Онлайн-магазин недоступен; показаны только локально установленные плагины',
      empty: 'Плагины не найдены',
      dependencies: 'Зависимости',
      dependenciesBlocked: 'У плагина неудовлетворённые зависимости; удаление невозможно',
      details: 'Подробности',
      usageGuide: 'Инструкция',
      changelog: 'История изменений',
      importProgress: 'Импорт пакетов плагинов…',
      importSuccess: 'Импортировано пакетов: {{count}}',
      importFailed: 'Не удалось импортировать {{count}} пакет(ов)',
      installAll: 'Установить все',
      installAllComplete: 'Установлено плагинов: {{count}}',
      installAllPartial: 'Выполнено {{count}} из {{total}} операций',
      copyId: 'Копировать ID плагина',
      copied: 'ID плагина скопирован в буфер обмена',
      copyFailed: 'Не удалось скопировать ID плагина',
      local: 'Локальный',
      collapseDetails: 'Скрыть подробности',
      showDetails: 'Показать подробности',
      updateInfo: 'Информация об обновлении',
      versionLabel: 'Версия:',
      configure: 'Настроить',
      open: 'Открыть',
      description: 'Устанавливайте и управляйте плагинами для расширения функций',
      storeUnavailable: 'Магазин плагинов недоступен',
      summaryTotal: 'Всего плагинов',
      summaryInstalled: 'Установлено',
      summaryUpdates: 'Доступны обновления',
      importFromFiles: 'Импорт из файлов',
      updateAll: 'Обновить все',
      emptyStore: 'Магазин плагинов сейчас пуст. Следите за будущими обновлениями.'
    },
    optimization: {
      title: 'Оптимизация системы',
      info: 'Эти действия изменяют системные службы и файлы и могут требовать прав администратора.',
      tabs: {
        optimization: 'Оптимизация',
        cleanup: 'Очистка',
        driverDownload: 'Загрузка драйверов',
        networkAcceleration: 'Ускорение сети'
      },
      recommended: 'Рекомендуется',
      selected: 'Выбрано',
      selectedActions: 'Выбранные действия',
      noSelection: 'Действия не выбраны',
      selectRecommended: 'Выбрать рекомендуемые',
      applyRecommended: 'Применить все рекомендуемые',
      apply: 'Применить',
      clear: 'Очистить (отменить)',
      applied: 'Применено',
      applyFailed: 'Не удалось применить (может требоваться права администратора)',
      reverted: 'Отменено',
      revertFailed: 'Не удалось отменить (может требоваться права администратора)',
      estimate: 'Оценить размер',
      estimateResult: 'Освобождаемое место',
      runCleanup: 'Запустить очистку',
      cleanupHint: 'Очистка выполняется по вашим пользовательским правилам.',
      cleanupConfirm: 'Запустить очистку сейчас?',
      cleanupDone: 'Очистка завершена',
      cleanupFailed: 'Не удалось выполнить очистку',
      cleanup: {
        custom: {
          header: 'Пользовательские правила очистки',
          description: 'Дополнительные папки, очищаемые вместе с выбранными действиями.',
          empty: 'Нет пользовательских правил очистки',
          add: 'Добавить папку',
          edit: 'Изменить папку',
          remove: 'Удалить',
          clear: 'Очистить все',
          added: 'Правило добавлено',
          updated: 'Правило обновлено',
          recursive: 'Включая подпапки',
          noExtensions: 'Расширения не указаны',
          folderPickerFailed: 'Не удалось открыть выбор папки'
        }
      },
      network: {
        status: 'Статус',
        running: 'Запущено',
        stopped: 'Остановлено',
        backendReady: 'Бэкенд готов',
        backendNotReady: 'Бэкенд не готов',
        config: 'Базовая конфигурация',
        accelerationEnabled: 'Включить ускорение',
        mode: 'Режим',
        modes: {
          off: 'Выкл',
          systemProxy: 'Системный прокси',
          hosts: 'Hosts',
          diagnosticsOnly: 'Только диагностика'
        },
        save: 'Сохранить конфигурацию',
        saved: 'Конфигурация сохранена',
        saveFailed: 'Не удалось сохранить конфигурацию',
        start: 'Запустить',
        stop: 'Остановить',
        startFailed: 'Не удалось запустить',
        stopFailed: 'Не удалось остановить',
        modeLabel: 'Режим',
        targetsLabel: 'Цели',
        portLabel: 'Порт',
        targetsHeading: 'Цели ускорения',
        domainGroupsHint: 'Выберите сервисы для ускорения через локальный прокси.',
        domainGroupsEmptyTitle: 'Нет целей ускорения',
        domainGroupsEmptyDescription: 'Список целей пуст или не совпадает с поиском.',
        selectionHint: 'Выбранные цели применяются при запуске ускорения.',
        searchTargets: 'Поиск целей',
        recommendedMenu: 'Рекомендуемое',
        groupRuntime: 'Выбрано {{selected}}/{{total}}  активно {{active}}',
        trafficHeading: 'Обзор трафика',
        metrics: {
          upload: 'Отдача',
          download: 'Загрузка',
          connections: 'Соединения',
          total: 'Общий трафик',
          health: 'Состояние'
        },
        trafficLive: 'Сбор живого прокси-трафика',
        trafficWaiting: 'Запустите ускорение для сбора живого трафика',
        trafficUnavailable: 'Данные о трафике временно недоступны',
        connectionsHeading: 'Текущие и недавние соединения',
        destinationsHeading: 'Статистика назначений',
        connectionSummary: '{{active}} активных / {{total}} всего',
        destinationSummary: 'Назначений: {{count}}',
        connectionStates: {
          active: 'Активно',
          completed: 'Завершено',
          blocked: 'Заблокировано',
          failed: 'Ошибка',
          stopped: 'Остановлено',
          unknown: 'Неизвестно'
        },
        unknownHost: 'Неизвестный хост',
        destinationRow: '{{count}} соед.  {{latency}}',
        health: {
          healthy: 'Здорово',
          degraded: 'Деградация',
          stopped: 'Остановлено',
          unknown: 'Неизвестно'
        },
        modeFull: {
          systemProxy: 'Системный прокси',
          hosts: 'Файл hosts',
          diagnosticsOnly: 'Только диагностика',
          off: 'Простой'
        },
        backendMissingHint: 'Прокси-воркер недоступен',
        selectGroupsFirstHint: 'Выберите хотя бы одну цель',
        advancedHeading: 'Дополнительно',
        advancedBody: 'Дополнительные настройки и восстановление сети.',
        portFormat: 'Порт: {{port}}',
        dangerZoneHeading: 'Опасная зона',
        restoreHint: 'Восстанавливает исходное состояние сети, записанное до ускорения.',
        restoreNetwork: 'Восстановить сеть',
        restoreConfirm: 'Восстановить состояние сети системы сейчас?',
        restored: 'Состояние сети восстановлено',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: 'Определить',
          unknown: 'Неизвестно',
          natTypes: {
            OpenInternet: 'Открытый NAT',
            Nat: 'NAT',
            UdpBlocked: 'UDP заблокирован',
            Unknown: 'Неизвестно'
          },
          internetConnected: 'Подключено',
          internetUnreachable: 'Недоступно',
          natType: 'Тип NAT',
          localIp: 'Локальный IP',
          publicIp: 'Публичный IP',
          internet: 'Интернет',
          dnsDomain: 'Домен',
          customDns: 'Пользовательский DNS',
          enableDoh: 'DoH',
          dohUrl: 'URL DoH',
          latency: 'Задержка',
          resolvedAddress: 'Разрешённый адрес',
          latencyFormat: '{{ms}} мс',
          failed: 'Ошибка',
          ipv6Support: 'Поддержка IPv6',
          ipv6Address: 'Адрес IPv6',
          ipv6SupportedFull: 'Доступ по IPv6 поддерживается',
          notSupported: 'Не поддерживается'
        }
      },
      driverDownload: {
        comingSoon: 'Загрузка драйверов появится в будущей версии'
      },
      driver: {
        machineType: 'Тип устройства',
        machineTypePlaceholder: 'например, 82K3',
        os: 'Операционная система',
        downloadTo: 'Скачать в',
        downloadToPlaceholder: 'Выберите папку для скачивания',
        browse: 'Обзор',
        openDownloadTo: 'Открыть папку',
        source: 'Источник',
        primarySource: 'Vantage',
        primarySourceMessage: 'Официальная база устройств через Vantage.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'База совместимости PC Support.',
        scan: 'Сканировать',
        scanning: 'Сканирование…',
        scanValidation: 'Введите корректный 4-значный тип устройства и выберите ОС.',
        disclaimer: 'Пакеты берутся из выбранного источника. Установка на свой риск.',
        filter: 'Фильтр',
        onlyShowUpdates: 'Только обновления',
        sort: {
          name: 'По имени',
          category: 'По категории',
          date: 'По дате'
        },
        selectRecommended: 'Выбрать рекомендуемые',
        startAll: 'Запустить все',
        pauseAll: 'Приостановить все',
        clearSelection: 'Сбросить выбор',
        packagesFound: 'Найдено пакетов: {{count}}.',
        packagesFoundOne: 'Найден 1 пакет.',
        status: {
          NotStarted: '',
          Queued: 'В очереди',
          Downloading: 'Скачивание',
          Installing: 'Установка',
          Completed: 'Завершено',
          Error: 'Ошибка'
        },
        recommended: 'Рекомендуется',
        isUpdate: 'Обновление',
        reboot: {
          recommended: 'Рекомендуется перезагрузка',
          required: 'Требуется перезагрузка',
          shutdown: 'Требуется выключение'
        },
        oldPackageWarning: 'Пакету больше года; драйвер может быть устаревшим.',
        download: 'Скачать',
        install: 'Установить',
        uninstall: 'Удалить',
        pause: 'Пауза',
        openReadme: 'Открыть readme',
        hide: 'Скрыть',
        hideAll: 'Скрыть все',
        showHiddenDownloads: 'Показать скрытые загрузки',
        downloadInProgress: {
          title: 'Идёт скачивание',
          message: 'Скачивание ещё выполняется. Сканировать снова?',
          confirm: 'Сканировать'
        },
        empty: {
          notScanned: {
            title: 'Сканирование пакетов драйверов',
            message: 'Выберите источник и отсканируйте, чтобы получить список совместимых драйверов.'
          },
          noResults: {
            title: 'Загрузки драйверов не найдены',
            message: 'Попробуйте другой источник, ОС или тип устройства.'
          },
          noFilterResults: {
            title: 'Подходящие загрузки не найдены',
            message: 'Измените фильтр, опцию «только обновления» или список скрытых загрузок.'
          },
          error: {
            title: 'Сканирование драйверов не завершено',
            message: 'Проверьте выбранный источник и соединение, затем повторите сканирование.'
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
      title: 'О программе',
      appName: 'Приложение',
      version: 'Версия',
      build: 'Сборка',
      links: 'Ссылки на проект',
      projectWebsite: 'Сайт проекта на GitHub',
      latestRelease: 'Последний релиз на GitHub',
      applicationFolders: 'Папки приложения',
      data: 'Данные',
      temp: 'Временные',
      pid: 'ID процесса',
      machine: 'Модель устройства',
      bios: 'Версия BIOS',
      compatible: 'Совместимость',
      yes: 'Совместимо',
      no: 'Не совместимо',
      dataFolder: 'Папка данных',
      thirdParty: 'Сторонние библиотеки',
      copyright: 'Авторские права'
    },
    statusBanner: {
      updateAvailable: 'Доступно обновление!',
      updateAvailableWithVersion: 'Доступно обновление {{version}}!',
      pluginExtensionsDisabled: 'Навигация «Плагины и расширения» скрыта. Включите её в Настройки → Элементы навигации.'
    },
    wpf: legacy.translation.wpf
  }
}



