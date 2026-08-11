import legacy from './es'

export default {
  translation: {
    ...legacy.translation,
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'Registro',
      openLogs: 'Abrir carpeta de registros',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'Información del dispositivo'
    },
    nav: {
      dashboard: 'Panel',
      settings: 'Configuración',
      automation: 'Automatización',
      keyboard: 'Teclado',
      keyboardBacklight: 'Retroiluminación del teclado',
      macro: 'Macro personalizada',
      windowsOptimization: 'Optimización del sistema',
      pluginExtensions: 'Complementos y extensiones',
      about: 'Acerca de'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: '¡Bienvenido! Elija una sección a continuación para comenzar',
      hostReady: 'Backend conectado',
      hostState: 'Estado del backend',
      hostVersion: 'Versión del backend',
      initComplete: 'Inicialización completada',
      safeStart: 'Inicio seguro, omitido',
      machine: 'Dispositivo',
      compatible: 'Compatibilidad',
      status: 'Estado'
    },
    dashboard: {
      title: 'Inicio',
      customize: 'Personalizar',
      edit: {
        title: 'Editar panel',
        description: 'Elija qué secciones y funciones se muestran en la página de inicio.',
        showSensors: 'Sensores de hardware',
        groups: 'Grupos de funciones',
        save: 'Guardar',
        cancel: 'Cancelar',
        saved: 'Disposición del panel guardada',
        error: 'No se pudo guardar la disposición del panel',
        disclaimer: 'Algunas funciones pueden no aparecer según el estado y la configuración de su equipo.',
        addGroup: 'Añadir',
        renameGroup: 'Editar nombre del grupo',
        deleteGroup: 'Eliminar',
        moveUp: 'Subir',
        moveDown: 'Bajar',
        deleteItem: 'Eliminar',
        addItem: 'Añadir',
        groupNamePlaceholder: 'Nombre',
        items: {
          discreteGpu: 'Modo GPU discreta',
          overclockGpu: 'Overclockear GPU',
          turnOffMonitors: 'Apagar monitores'
        }
      },
      addItem: {
        title: 'Añadir',
        searchPlaceholder: 'Buscar',
        empty: 'Todos los elementos ya están añadidos',
        addHint: 'Añadir elemento'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Memoria',
      temperature: 'Temperatura',
      usage: 'Uso',
      power: 'Potencia',
      fanSpeed: 'Ventilador',
      vram: 'VRAM',
      memoryUsed: 'Memoria usada',
      memoryTotal: 'Memoria total',
      storageTemp: 'Temp. de almacenamiento',
      notAvailable: '--',
      sensor: {
        cpu: 'Procesador',
        gpu: 'Tarjeta gráfica',
        memory: 'Memoria',
        temperature: 'Temperatura',
        usage: 'Uso',
        power: 'Potencia',
        fanSpeed: 'Ventilador',
        vram: 'VRAM',
        frequency: 'Frecuencia del núcleo',
        battery: 'Batería',
        charge: 'Carga',
        health: 'Salud',
        rate: 'Ritmo',
        fan: 'Ventilador',
        lowPowerAdapter: 'Adaptador de baja potencia conectado',
        batteryLow: 'Batería baja',
        acCharging: 'Adaptador conectado, cargando…',
        acNotCharging: 'Adaptador conectado, sin cargar…',
        remainingTime: 'Tiempo restante estimado: {0}',
        memoryTemperature: 'Temperatura de memoria',
        ssdTemperature: 'Temperatura del SSD',
        vramTemperature: 'Temperatura de VRAM',
        vramUsage: 'Uso de VRAM',
        cycles: 'Ciclos',
        capacity: 'Capacidad',
        fullCapacity: 'Capacidad de carga completa',
        designCapacity: 'Capacidad de diseño',
        date: 'Fecha',
        voltage: 'Voltaje del núcleo',
        voltageRange: 'Rango de voltaje',
        powerRange: 'Rango de potencia',
        details: 'Detalles',
        refreshInterval: 'Intervalo de actualización',
        detail: {
          power: 'Potencia',
          powerCores: 'Núcleos',
          powerMemory: 'Memoria',
          powerPlatform: 'Plataforma',
          pCoreClock: 'Frecuencia P-Core',
          eCoreClock: 'Frecuencia E-Core',
          memoryUsage: 'Uso de memoria',
          sharedMemoryUsage: 'Uso de memoria compartida',
          vramUsage: 'Uso de VRAM',
          hotSpot: 'Punto caliente GPU',
          pcieThroughput: 'Rendimiento PCIe',
          designCapacity: 'Capacidad de diseño',
          fullChargeCapacity: 'Capacidad de carga completa'
        }
      },
      group: {
        power: 'Energía',
        graphics: 'Gráficos',
        display: 'Pantalla',
        other: 'Otros',
        custom: 'Personalizado'
      },
      card: {
        error: 'No se pudo aplicar el ajuste',
        config: 'Ajustes avanzados',
        configComingSoon: 'Los ajustes avanzados estarán disponibles en una versión futura'
      }
    },
    balanceMode: {
      title: 'Ajustes del modo equilibrado',
      aiEngine: 'Activar motor de IA',
      aiEngineDesc: 'Detecta automáticamente juegos en ejecución y ajusta el rendimiento de CPU/GPU. La temperatura y el ruido pueden aumentar.'
    },
    godMode: {
      title: 'Ajustes del modo personalizado',
      activePreset: 'Perfil activo',
      presetName: 'Nombre del perfil',
      name: 'Nombre',
      errorLoad: 'No se pudo cargar el ajuste.',
      errorApply: 'No se pudieron aplicar los ajustes',
      applySuccess: 'Ajustes del modo personalizado aplicados.',
      defaultPresetName: 'Perfil',
      cpu: {
        title: 'CPU',
        longTermPL: 'Límite de potencia a largo plazo',
        'longTermPL.desc': 'La potencia continua que puede alcanzar la CPU.',
        shortTermPL: 'Límite de potencia a corto plazo',
        'shortTermPL.desc': 'La potencia máxima que la CPU puede alcanzar en poco tiempo.',
        peakPL: 'Límite de potencia máximo',
        'peakPL.desc': 'La potencia instantánea máxima que puede alcanzar la CPU.',
        crossLoading: 'Límite a largo plazo (carga cruzada)',
        'crossLoading.desc': 'La potencia máxima de la CPU con CPU y GPU a plena carga.',
        pl1Tau: 'Duración del límite a corto plazo',
        'pl1Tau.desc': 'El tiempo que la CPU puede usar el límite a corto plazo. Al expirar, se usa el de largo plazo.',
        apuSppt: 'Límite de potencia APU sPPT',
        'apuSppt.desc': 'La potencia máxima que la CPU puede alcanzar con un ligero retraso.',
        tempLimit: 'Límite de temperatura de CPU',
        'tempLimit.desc': 'La temperatura máxima de la CPU antes de reducir frecuencia y potencia.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'Impulso dinámico',
        'dynamicBoost.desc': 'La potencia adicional que se puede asignar a la GPU según el consumo de la CPU.',
        ctgp: 'TGP configurable',
        'ctgp.desc': 'La potencia adicional asignable a la GPU sobre la base.',
        tempLimit: 'Límite de temperatura de GPU',
        'tempLimit.desc': 'La temperatura máxima de la GPU antes de reducir frecuencia y potencia.',
        totalProcessingPowerTarget: 'Objetivo de potencia del procesador en CA',
        'totalProcessingPowerTarget.desc': 'El punto donde la CPU activa el ajuste dinámico de potencia de la GPU.',
        toCpuDynamicBoost: 'Impulso dinámico de GPU a CPU',
        'toCpuDynamicBoost.desc': 'La potencia adicional asignable a la CPU desde la GPU según el uso de CPU. A mayor valor, mejor rendimiento de CPU.'
      },
      fans: {
        title: 'Ventiladores',
        curve: 'Curva del ventilador',
        curveMessage: 'La velocidad sigue el sensor más alto entre CPU, GPU y disipador. Pase el cursor sobre cada paso para ver valores exactos.',
        maxSpeed: 'Velocidad máxima del ventilador',
        maxSpeedWarning: 'El uso prolongado degradará los ventiladores.\n¡Tenga mucho cuidado con esta opción!'
      },
      advanced: {
        title: 'Avanzado',
        message: 'No cambie las opciones siguientes a menos que sepa lo que hace.',
        maxOffset: 'Desplazamiento máximo',
        maxOffsetWarning: 'Valores más altos pueden causar comportamientos impredecibles. Déjelo en 0 si duda.',
        minOffset: 'Desplazamiento mínimo',
        minOffsetWarning: 'Valores más bajos pueden causar comportamientos impredecibles. Déjelo en 0 si duda.',
        invalidOffset: 'Introduzca un número entero antes de guardar.'
      },
      vantageWarning: 'Los ajustes del modo personalizado no se aplicarán correctamente con Lenovo Vantage o sus servicios en ejecución.',
      legionZoneWarning: 'Los ajustes del modo personalizado no se aplicarán correctamente con Legion Zone o sus servicios en ejecución.'
    },
    overclock: {
      title: 'Ajustes de overclock de GPU',
      preset: 'Perfil',
      coreOffset: 'Desplazamiento de frecuencia del núcleo',
      memoryOffset: 'Desplazamiento de frecuencia de memoria',
      namePlaceholder: 'Nombre…',
      newProfileName: 'Perfil',
      loadError: 'No se pudieron cargar los ajustes de overclock.'
    },
    feature: {
      powerMode: 'Modo de energía',
      'powerMode.desc': 'Cambiar el modo de rendimiento.\nTambién puede cambiarse con Fn+Q.',
      'powerMode.hint': 'Puede cambiarlo rápidamente con el atajo Fn+Q.',
      'powerMode.warning': 'El modo de rendimiento puede no funcionar correctamente sin el adaptador conectado.',
      battery: 'Modo de carga de batería',
      'battery.desc': 'Elija un modo de carga. El modo conservación limita la carga para prolongar la batería; el modo carga rápida carga a mayor potencia.',
      batteryNightCharge: 'Carga nocturna',
      'batteryNightCharge.desc': 'Si está activado, carga al 80 % por la noche y completa al 100 % por la mañana.',
      alwaysOnUsb: 'USB siempre activo',
      'alwaysOnUsb.desc': 'Mantiene la alimentación de los puertos USB con el equipo apagado, en reposo o en hibernación.',
      instantBoot: 'Inicio instantáneo',
      'instantBoot.desc': 'Enciende el equipo en cuanto se conecta la alimentación.',
      flipToStart: 'Abrir para iniciar',
      'flipToStart.desc': 'Abrir la tapa enciende automáticamente el portátil.',
      fnLock: 'Bloqueo de Fn',
      'fnLock.desc': 'Si está activado, las funciones se activan sin pulsar Fn. Pulse Fn con F1-F12 para las funciones originales.',
      gSync: 'G-Sync',
      'gSync.desc': 'Activar o desactivar la frecuencia variable G-Sync',
      hdr: 'HDR',
      'hdr.desc': 'Activa el HDR en la pantalla integrada.',
      'hdr.warning': 'El uso de HDR está bloqueado por la configuración de Windows.',
      hybridMode: 'Modo híbrido',
      'hybridMode.desc': 'El modo híbrido permite cambiar entre GPU integrada y discreta. Desactivarlo activa el modo directo de GPU discreta; se requiere reinicio.',
      igpuMode: 'Modo GPU discreta',
      'igpuMode.desc': 'Forzar la salida gráfica integrada para ahorrar energía',
      refreshRate: 'Frecuencia de refresco',
      'refreshRate.desc': 'Cambia la frecuencia de refresco de la pantalla integrada.',
      itsMode: 'Modo ITS',
      'itsMode.desc': 'Solución térmica inteligente',
      microphone: 'Micrófono',
      'microphone.desc': 'Al desactivarlo, se silencian todos los micrófonos disponibles.',
      overDrive: 'Over Drive',
      'overDrive.desc': 'Mejora el tiempo de respuesta de la pantalla integrada. Puede causar estelas.',
      panelLogo: 'Logotipo Legion',
      'panelLogo.desc': 'Enciende o apaga el logotipo Legion en la parte trasera.',
      portsBacklight: 'Retroiluminación de puertos',
      'portsBacklight.desc': 'Enciende o apaga las luces de los puertos traseros.',
      resolution: 'Resolución',
      'resolution.desc': 'Cambia la resolución de la pantalla integrada.',
      dpiScale: 'Escala DPI',
      'dpiScale.desc': 'Cambia la escala de la pantalla integrada.',
      speaker: 'Altavoz',
      touchpadLock: 'Bloqueo del panel táctil',
      'touchpadLock.desc': 'Desactiva el panel táctil. Recomendado al usar un ratón.',
      whiteKeyboard: 'Retroiluminación del teclado',
      'whiteKeyboard.desc': 'Use Fn + Espacio para alternar y ajustar la retroiluminación.',
      winKey: 'Desactivar tecla Windows',
      'winKey.desc': 'Solo teclado integrado. La tecla Windows dejará de responder.',
      oneLevelWhiteKeyboard: 'Retroiluminación del teclado',
      'oneLevelWhiteKeyboard.desc': 'Use el atajo Fn + Espacio para alternar la retroiluminación.',
      'hybridMode.states.hybrid': 'Híbrido',
      'hybridMode.states.hybridIGPUOnly': 'Híbrido-iGPU',
      'hybridMode.states.hybridAuto': 'Híbrido-auto',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'Acerca de los modos de GPU',
      'hybridMode.info.hybrid.title': 'Modo híbrido',
      'hybridMode.info.hybrid.message': 'GPU integrada y discreta activadas. El sistema cambia automáticamente según sus necesidades.',
      'hybridMode.info.hybridIgpu.title': 'Modo híbrido-iGPU solo',
      'hybridMode.info.hybridIgpu.message': 'Solo GPU integrada. Minimiza consumo y ruido.',
      'hybridMode.info.hybridIgpu.disclaimer': 'Este modo solo tiene efecto cuando la GPU discreta no trabaja.',
      'hybridMode.info.hybridAuto.title': 'Modo híbrido-auto',
      'hybridMode.info.hybridAuto.message': 'Con batería solo GPU integrada; con CA, ambas. Con adaptador no estándar, cambia a solo iGPU.',
      'hybridMode.info.dgpu.title': 'Modo dGPU',
      'hybridMode.info.dgpu.message': 'Solo GPU discreta. Mejor rendimiento gráfico, pero mayor consumo.',
      'hybridMode.info.dgpu.disclaimer': 'Cambiar hacia o desde este modo requiere reinicio.',
      'hybridMode.restartRequired.title': 'Reinicio requerido',
      'hybridMode.restartRequired.message': 'Cambiar a {{mode}} requiere reinicio. ¿Reiniciar ahora?',
      'hybridMode.restartRequired.now': 'Reiniciar ahora',
      'hybridMode.restartRequired.later': 'Reiniciaré más tarde',
      'hybridMode.restartFailed': 'No se pudo reiniciar automáticamente. Reinicie manualmente para completar el cambio.',
      'hybridMode.changeFailed.title': 'No se pudo cambiar el modo de GPU',
      'hybridMode.changeFailed.message': 'Vuelva a intentarlo en unos segundos. Si la dGPU no responde, reinicie el portátil.',
      batteryModes: {
        conservation: 'Modo conservación',
        normal: 'Modo normal',
        rapidCharge: 'Modo carga rápida'
      },
      powerModeOptions: {
        quiet: 'Silencioso',
        balance: 'Equilibrado',
        performance: 'Rendimiento',
        extreme: 'Extremo',
        godMode: 'Personalizado'
      }
    },
    common: {
      loading: 'Cargando…',
      error: 'Algo salió mal',
      retry: 'Reintentar',
      close: 'Cerrar',
      cancel: 'Cancelar',
      moreActions: 'Más acciones',
      copied: 'Copiado al portapapeles',
      add: 'Añadir',
      save: 'Guardar',
      saveAndClose: 'Guardar y cerrar',
      apply: 'Aplicar',
      applyAndClose: 'Aplicar y cerrar',
      default: 'Predeterminado',
      rename: 'Renombrar',
      delete: 'Eliminar',
      ok: 'Aceptar'
    },
    colorPicker: {
      hex: 'Hex',
      red: 'Rojo',
      green: 'Verde',
      blue: 'Azul',
      ok: 'Aceptar'
    },
    fanCurve: {
      fanSpeed: 'Velocidad del ventilador',
      fanSpeedMax: '100 %',
      cpu: 'CPU',
      cpuSensor: 'Sensor de CPU',
      gpu: 'GPU',
      gpu2: 'GPU n.º 2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: 'Próximamente'
    },
    settings: {
      title: 'Configuración',
      description: 'Configure la apariencia, el comportamiento y las funciones de la aplicación.',
      nav: {
        appearance: 'Apariencia',
        application: 'Aplicación',
        power: 'Energía',
        display: 'Pantalla',
        smartKeys: 'Teclas inteligentes',
        update: 'Actualización',
        integrations: 'Integraciones',
        osd: 'OSD'
      },
      appearance: {
        language: 'Idioma',
        languageDesc: 'Elija el idioma',
        temperature: 'Temperatura',
        temperatureDesc: 'Elija la unidad usada por los sensores de temperatura.',
        theme: 'Tema',
        accentColor: 'Color de acento',
        accentColorDesc: 'Cambie el color de acento de la aplicación.',
        appScale: 'Escala de interfaz',
        appScaleDesc: 'Escala el texto y toda la interfaz, independiente de la escala de Windows.',
        themeOptions: {
          system: 'Sistema',
          light: 'Claro',
          dark: 'Oscuro'
        }
      },
      application: {
        minimizeToTray: 'Minimizar a la bandeja',
        minimizeToTrayDesc: 'Minimizar siempre a la bandeja en lugar de la barra de tareas.',
        minimizeOnClose: 'Minimizar al cerrar',
        minimizeOnCloseDesc: 'Minimizar siempre a la bandeja. Para salir, haga clic derecho en el icono y elija Cerrar.',
        disableUnsupportedWarning: 'No advertir sobre dispositivos incompatibles',
        disableUnsupportedWarningDesc: 'Oculta el aviso de dispositivo incompatible al iniciar.',
        enableHardwareSensors: 'Sensores de hardware',
        enableHardwareSensorsDesc: 'Activa el sondeo avanzado para monitorear temperatura, frecuencia y límites de potencia.',
        dontShowNotifications: 'No mostrar notificaciones',
        dontShowNotificationsDesc: 'Desactiva las notificaciones de la aplicación y del sistema',
        autorun: 'Iniciar al iniciar sesión',
        autorunDesc: 'Iniciar minimizado en la bandeja tras iniciar sesión en Windows.',
        extensionsEnabled: 'Activar extensiones',
        extensionsEnabledDesc: 'Activa la carga de complementos y extensiones',
        sensorSections: 'Secciones de sensores',
        sensorSectionsDesc: 'Elija qué secciones de sensores se muestran y en qué orden.',
        disableVantage: 'Desactivar Lenovo Vantage',
        disableVantageDesc: 'Desactiva Lenovo Vantage e ImController sin desinstalarlos.\nSe recomienda reiniciar tras el cambio.',
        disableLegionZone: 'Desactivar Legion Zone',
        disableLegionZoneDesc: 'Desactiva Legion Zone y su servicio sin desinstalarlo.\nSe recomienda reiniciar tras el cambio.',
        disableLenovoHotkeys: 'Desactivar Lenovo Hotkeys',
        disableLenovoHotkeysDesc: 'Desactiva Lenovo Hotkeys y su servicio sin desinstalarlo.\nSi se desactiva, esta app gestionará los atajos Fn.\nSe recomienda reiniciar tras el cambio.',
        valueOn: 'Activado',
        valueOff: 'Desactivado'
      },
      saved: 'Ajustes guardados',
      saveFailed: 'No se pudieron guardar los ajustes',
      osd: {
        title: 'OSD',
        showOsd: 'Mostrar OSD',
        showOsdDesc: 'Mostrar la superposición en pantalla inmediatamente.',
        style: 'Estilo de superposición',
        styles: {
          panel: 'Panel',
          bar: 'Barra'
        },
        refreshInterval: 'Intervalo de actualización',
        snapThreshold: 'Umbral de acople',
        lockPosition: 'Bloquear posición',
        resetPosition: 'Restablecer posición',
        previewHint: 'Vista previa',
        tabs: {
          general: 'General',
          appearance: 'Apariencia',
          thresholds: 'Umbrales',
          sensors: 'Sensores'
        },
        opacity: 'Opacidad',
        cornerRadius: 'Radio de esquinas',
        cornerRadiusTop: 'Superior',
        cornerRadiusBottom: 'Inferior',
        fontSize: 'Tamaño de fuente',
        background: 'Color de fondo',
        category: 'Color de categoría',
        label: 'Color de etiqueta',
        value: 'Color de valor',
        warning: 'Color de aviso',
        critical: 'Color crítico',
        separator: 'Color de separador',
        thresholds: {
          performance: 'Rendimiento',
          fpsRedline: 'Línea roja de FPS',
          lowFpsDelta: 'Delta de FPS bajo',
          temperature: 'Temperatura',
          usage: 'Uso',
          warning: 'Aviso',
          critical: 'Crítico'
        },
        items: {
          groups: {
            game: 'Juego',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1 % Low',
            FrameTime: 'Tiempo de frame',
            CpuFrequency: 'Frecuencia del núcleo',
            CpuPCoreFrequency: 'Frecuencia P-Core',
            CpuECoreFrequency: 'Frecuencia E-Core',
            CpuUtilization: 'Uso',
            CpuTemperature: 'Temperatura',
            CpuPower: 'Potencia',
            CpuFan: 'Ventilador',
            GpuFrequency: 'Frecuencia del núcleo',
            GpuUtilization: 'Uso',
            GpuTemperature: 'Temperatura del núcleo',
            GpuVramUtilization: 'Uso de VRAM',
            GpuVramTemperature: 'Temp. de VRAM',
            GpuPower: 'Potencia',
            GpuFan: 'Ventilador',
            MemoryUtilization: 'Uso',
            MemoryTemperature: 'Temperatura',
            Disk1Temperature: 'Temp. disco 1',
            Disk2Temperature: 'Temp. disco 2',
            PchTemperature: 'Temp. PCH',
            PchFan: 'Ventilador'
          }
        }
      },
      power: {
        powerModeMapping: 'Asignación de modos de energía',
        powerModeMappingDesc: 'Al cambiar de modo, cambia automáticamente el plan o modo de energía de Windows.',
        mappingModes: {
          disabled: 'Desactivado',
          windowsPowerMode: 'Modo de energía de Windows',
          windowsPowerPlan: 'Plan de energía de Windows'
        },
        windowsPowerModes: 'Modo de energía de Windows',
        windowsPowerModesDesc: 'Elija el modo de energía de Windows aplicado al cambiar de modo.',
        windowsPowerPlans: 'Plan de energía de Windows',
        windowsPowerPlansDesc: 'Elija el plan de energía de Windows aplicado al cambiar de modo.',
        synchronizeBrightness: 'Bloquear brillo de pantalla',
        synchronizeBrightnessDesc: 'Si está activado, el brillo permanece igual entre planes de energía.',
        smartFnLock: 'Teclas modificadoras de Smart Fn Lock',
        modifierKeys: {
          shift: 'Mayús',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: 'Restablecer «En batería desde» al iniciar',
        resetBatteryOnSinceDesc: 'Restablece el contador «En batería desde» en la sección de batería al reiniciar el sistema.',
        godModeFnQ: 'Cambiar al modo personalizado con Fn+Q',
        godModeFnQDesc: 'Permite cambiar rápidamente al modo personalizado con Fn+Q.'
      },
      display: {
        navigationItems: 'Visibilidad de elementos de navegación',
        navigationKeys: {
          keyboard: 'Retroiluminación del teclado',
          battery: 'Batería',
          automation: 'Automatización',
          macro: 'Macro',
          windowsOptimization: 'Optimización de Windows',
          pluginExtensions: 'Complementos y extensiones',
          about: 'Acerca de'
        },
        notificationPosition: 'Posición de notificaciones',
        notificationPositions: {
          bottomRight: 'Inferior derecha',
          bottomCenter: 'Inferior centro',
          bottomLeft: 'Inferior izquierda',
          centerLeft: 'Centro izquierda',
          topLeft: 'Superior izquierda',
          topCenter: 'Superior centro',
          topRight: 'Superior derecha',
          centerRight: 'Centro derecha',
          center: 'Centro'
        },
        notificationDuration: 'Duración de notificaciones',
        notificationDurations: {
          short: 'Corta (3 s)',
          normal: 'Normal (5 s)',
          long: 'Larga (10 s)'
        },
        excludedRefreshRates: 'Frecuencias excluidas',
        excludedRefreshRatesDesc: 'Excluya frecuencias para acelerar el cambio con Fn+R.',
        excludedRefreshRatesHint: 'La edición avanzada estará disponible en una versión futura',
        excludedRefreshRatesEmpty: 'No hay frecuencias excluidas',
        excludedRefreshRatesManageHint: 'Haga clic para gestionar las frecuencias excluidas',
        notifications: 'Notificaciones',
        notificationsDesc: 'Elija qué notificaciones se muestran.',
        bootLogo: 'Logotipo de arranque',
        bootLogoDesc: 'Personalice el logotipo mostrado al iniciar.'
      },
      smartKeys: {
        smartFnLock: 'Smart Fn Lock',
        smartFnLockDesc: 'Al pulsar Alt, Ctrl o Mayús, Fn se desbloquea temporalmente.',
        off: 'Desactivado',
        hint: 'Las teclas modificadoras de Smart Fn Lock pueden cambiarse en los ajustes de energía.',
        singlePressActionDesc: 'Asigne una acción rápida a la pulsación simple de Fn+F9.',
        doublePressActionDesc: 'Asigne una acción rápida a la pulsación doble de Fn+F9.'
      },
      update: {
        frequency: 'Buscar actualizaciones automáticamente',
        frequencies: {
          perHour: 'Cada hora',
          perThreeHours: 'Cada 3 horas',
          perTwelveHours: 'Cada 12 horas',
          perDay: 'Cada día',
          perWeek: 'Cada semana',
          perMonth: 'Cada mes'
        },
        includePrerelease: 'Incluir versiones preliminares',
        includePrereleaseDesc: 'Desactivado: solo versiones estables; activado: también se reciben versiones beta.',
        repository: 'Repositorio de actualización',
        repositoryDesc: 'Configure el repositorio de GitHub para las actualizaciones. Vacío para usar el predeterminado.',
        repositoryOwner: 'Propietario del repositorio',
        repositoryOwnerPlaceholder: 'p. ej., SSC-STUDIO',
        repositoryName: 'Nombre del repositorio',
        repositoryNamePlaceholder: 'p. ej., UniversalDeviceToolkit',
        check: 'Buscar actualizaciones',
        comingSoon: 'La búsqueda de actualizaciones estará disponible en una versión futura'
      },
      checkResult: {
        available: 'Nueva versión disponible: v{{version}}',
        latest: 'Está actualizado'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'Comparte velocidad de ventiladores, temperatura de batería y otros datos con HWiNFO64. Puede requerir reiniciar HWiNFO64.',
        cli: 'Interfaz de línea de comandos',
        cliDesc: 'Activa la interfaz de línea de comandos para controlar la aplicación.'
      }
    },
    keyboard: {
      title: 'Retroiluminación del teclado',
      unsupported: 'La retroiluminación del teclado no es compatible con este dispositivo',
      rgb: {
        preset: 'Perfil',
        settings: 'Ajustes de retroiluminación',
        effect: 'Efecto',
        speed: 'Velocidad',
        brightness: 'Brillo',
        zones: 'Colores de zonas',
        synchroniseZones: 'Sincronizar zonas',
        presets: {
          off: 'Apagado',
          one: 'Perfil 1',
          two: 'Perfil 2',
          three: 'Perfil 3',
          four: 'Perfil 4'
        },
        effectOptions: {
          static: 'Estático',
          breath: 'Respiración',
          smooth: 'Fluido',
          waveRtl: 'Ola (derecha→izquierda)',
          waveLtr: 'Ola (izquierda→derecha)'
        },
        speedOptions: {
          slowest: 'Más lenta',
          slow: 'Lenta',
          fast: 'Rápida',
          fastest: 'Más rápida'
        },
        brightnessOptions: {
          low: 'Bajo',
          high: 'Alto'
        }
      },
      spectrum: {
        brightness: 'Brillo',
        profile: 'Perfil',
        logo: 'Logotipo',
        effects: 'Efectos',
        colors: 'Colores',
        addEffect: 'Añadir efecto',
        deleteEffect: 'Eliminar',
        noEffects: 'Sin efectos',
        selectAll: 'Seleccionar todas las zonas',
        deselectAll: 'Deseleccionar todas las zonas',
        switchLayout: 'Cambiar distribución del teclado',
        editEffect: 'Editar',
        allKeys: 'Todas las teclas',
        zonesCount: '{{count}} zonas',
        noLayoutHint: 'No se pudo cargar la distribución del teclado.',
        selectEffectHint: 'Seleccione un efecto abajo para previsualizar y editar sus teclas.',
        effectEdit: {
          addTitle: 'Añadir efecto',
          editTitle: 'Editar efecto',
          effect: 'Efecto',
          speed: 'Velocidad',
          direction: 'Dirección',
          clockwiseDirection: 'Dirección',
          color: 'Color',
          colors: 'Colores',
          addColor: 'Añadir color',
          keys: 'Teclas',
          alwaysWarning: 'Este efecto se aplicará a todo el teclado y reemplazará a los demás efectos.'
        },
        effectTypes: {
          always: 'Permanente',
          rainbowScrew: 'Tornillo arcoíris',
          rainbowWave: 'Ola arcoíris',
          colorChange: 'Cambio de color',
          colorWave: 'Ola de color',
          colorPulse: 'Pulso de color',
          smooth: 'Fluido',
          rain: 'Lluvia',
          ripple: 'Onda',
          type: 'Escritura',
          audioBounce: 'Rebote de audio',
          audioRipple: 'Onda de audio',
          auroraSync: 'Sincronización Aurora'
        }
      }
    },
    automation: {
      title: 'Automatización',
      enable: 'Activar automatización',
      enableDesc: 'Universal Device Toolkit debe estar en ejecución para que las acciones automáticas funcionen.',
      subtitle: 'Si está activado, esta app verifica y ejecuta las acciones coincidentes cuando cambia el estado del dispositivo.',
      actionsTitle: 'Acciones',
      actionsEmpty: 'Aún no hay acciones automáticas',
      quickActionsTitle: 'Acciones rápidas',
      quickActionsEmpty: 'Aún no hay acciones rápidas. Haga clic en «Nueva» para crear una.',
      renamePipeline: 'Renombrar canal',
      renamePipelineTitle: 'Renombrar canal',
      renamePipelinePlaceholder: 'Introduzca el nombre del canal',
      changeIcon: 'Cambiar icono',
      empty: 'Aún no hay scripts de automatización. Haga clic en «Nueva» para crear uno.',
      runNow: 'Ejecutar ahora',
      delete: 'Eliminar',
      deleteStep: 'Eliminar paso',
      addPipeline: 'Nueva',
      addStep: 'Añadir paso',
      configure: 'Configurar',
      stepType: 'Tipo de paso',
      steps: 'Pasos',
      save: 'Guardar',
      revert: 'Revertir',
      pipelineName: 'Nombre del canal',
      pipelineNamePlaceholder: 'Introduzca el nombre del canal',
      quickAction: 'Acción rápida',
      optionsLoading: 'Cargando opciones…',
      stepLabels: {
        rgbKeyboardBacklight: 'Retroiluminación del teclado',
        run: 'Ejecutar',
        showMainWindow: 'Mostrar ventana principal',
        speaker: 'Altavoz',
        spectrumKeyboardBacklightBrightness: 'Brillo de la retroiluminación',
        spectrumKeyboardBacklightImportProfile: 'Importar perfil de retroiluminación',
        spectrumKeyboardBacklightProfile: 'Perfil de retroiluminación',
        touchpadLock: 'Bloqueo del panel táctil',
        turnOffMonitors: 'Apagar pantallas',
        turnOffWiFi: 'Apagar Wi-Fi',
        turnOnWiFi: 'Encender Wi-Fi',
        whiteKeyboardBacklight: 'Retroiluminación del teclado',
        winKey: 'Bloqueo de tecla Windows',
        scriptPath: 'Ruta del ejecutable',
        scriptArguments: 'Argumentos',
        runSilently: 'Ejecutar en silencio',
        runSilentlyDesc: 'Ejecuta aplicaciones de consola sin crear ventana de consola.',
        runWaitUntilFinished: 'Esperar a que termine',
        runWaitUntilFinishedDesc: 'Espera a que el programa o script termine',
        runHint: 'Ejecuta un script o programa.\nAsegúrese de que su script funciona primero.',
        importProfilePath: 'Ruta',
        browse: 'Examinar',
        off: 'Apagado',
        on: 'Encendido',
        mute: 'Silenciar',
        unmute: 'Reactivar sonido',
        low: 'Bajo',
        high: 'Alto',
        presetOne: 'Perfil 1',
        presetTwo: 'Perfil 2',
        presetThree: 'Perfil 3',
        presetFour: 'Perfil 4',
        values: {
          off: 'Apagado',
          on: 'Encendido',
          mute: 'Silenciar',
          unmute: 'Reactivar sonido',
          low: 'Bajo',
          high: 'Alto',
          presetOne: 'Perfil 1',
          presetTwo: 'Perfil 2',
          presetThree: 'Perfil 3',
          presetFour: 'Perfil 4'
        }
      },
      state: {
        on: 'Encendido',
        off: 'Apagado',
        hidden: 'Ocultar',
        show: 'Mostrar',
        toggle: 'Alternar estado',
        quiet: 'Silencioso',
        balance: 'Equilibrado',
        performance: 'Rendimiento',
        extreme: 'Extremo',
        godMode: 'Personalizado',
        hybrid: 'Híbrido',
        hybridIgpu: 'Híbrido-iGPU',
        hybridAuto: 'Híbrido-auto',
        dgpu: 'dGPU',
        acAdapter: 'Adaptador de CA',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'CA y USB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'Modo de GPU',
          desc: 'Seleccione el modo de funcionamiento de la GPU según el uso y la energía.\nCambiar de modo puede requerir reinicio.'
        },
        instantBoot: {
          title: 'Inicio instantáneo',
          desc: 'Enciende el portátil al conectar un cargador.'
        },
        macro: {
          title: 'Macro',
          desc: 'Activa o desactiva macros.'
        },
        microphone: {
          title: 'Micrófono',
          desc: 'Si está apagado, los micrófonos se silenciarán.'
        },
        notification: {
          title: 'Mostrar notificación',
          desc: 'Muestra una notificación con el texto introducido.',
          placeholder: 'Texto de la notificación'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'Retroiluminación del teclado',
          desc: 'Enciende o apaga la retroiluminación.'
        },
        osd: {
          title: 'OSD',
          desc: 'Mostrar u ocultar el OSD'
        },
        overclockDiscreteGPU: {
          title: 'Overclockear GPU',
          desc: 'Mejora el rendimiento overclockeando la GPU discreta.\n\nAVISO: esta acción no funcionará si la GPU discreta no está disponible.'
        },
        overDrive: {
          title: 'Over Drive',
          desc: 'Mejora el tiempo de respuesta de la pantalla integrada.'
        },
        panelLogoBacklight: {
          title: 'Logotipo trasero',
          desc: 'Enciende o apaga el logotipo de la tapa del portátil.'
        },
        playSound: {
          title: 'Reproducir sonido',
          desc: 'Se admiten formatos comunes como wav o mp3.',
          browse: 'Examinar…',
          none: 'Ningún archivo seleccionado'
        },
        portsBacklight: {
          title: 'Retroiluminación de puertos',
          desc: 'Enciende o apaga la retroiluminación de los puertos traseros.'
        },
        powerMode: {
          title: 'Modo de energía',
          desc: 'Cambiar el modo de rendimiento.'
        },
        quickAction: {
          title: 'Acción rápida',
          desc: 'Ejecuta una acción rápida guardada.',
          placeholder: 'Seleccione una acción rápida',
          empty: 'Aún no hay acciones rápidas. Cree primero un canal sin disparador.'
        },
        refreshRate: {
          title: 'Frecuencia de refresco',
          desc: 'Cambia la frecuencia de refresco de la pantalla integrada.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada.',
          empty: 'No hay frecuencias disponibles'
        },
        resolution: {
          title: 'Resolución',
          desc: 'Cambia la resolución de la pantalla integrada.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada.',
          empty: 'No hay resoluciones disponibles'
        },
        alwaysOnUsb: {
          title: 'USB siempre activo',
          desc: 'Carga dispositivos USB cuando el portátil está apagado, en reposo o hibernación.',
          options: {
            OnWhenSleeping: 'Encendido en reposo',
            OnAlways: 'Siempre encendido'
          }
        },
        battery: {
          title: 'Modo de batería',
          desc: 'Elija cómo se carga la batería.',
          options: {
            Conservation: 'Conservación',
            Normal: 'Normal',
            RapidCharge: 'Carga rápida'
          }
        },
        batteryNightCharge: {
          title: 'Carga nocturna',
          desc: 'Si está activado, el dispositivo carga al 80 % por la noche y completa al 100 % por la mañana.'
        },
        deactivateGPU: {
          title: 'Desactivar GPU',
          desc: 'Desactiva la GPU discreta si está activa innecesariamente.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada o el modo híbrido inactivo.',
          options: {
            KillApps: 'Cerrar apps',
            RestartGPU: 'Reiniciar GPU'
          }
        },
        delay: {
          title: 'Retraso',
          desc: 'Añade un retraso antes del siguiente paso.',
          second_one: '{{count}} segundo',
          second_other: '{{count}} segundos'
        },
        displayBrightness: {
          title: 'Brillo de pantalla',
          desc: 'Cambia el brillo de la pantalla integrada.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada.',
          percent: '{{value}} %'
        },
        dpiScale: {
          title: 'DPI',
          desc: 'Cambia la escala de la pantalla integrada.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada.',
          percent: '{{value}} %'
        },
        flipToStart: {
          title: 'Abrir para iniciar',
          desc: 'Enciende el portátil al abrir la tapa.'
        },
        fnLock: {
          title: 'Bloqueo de Fn',
          desc: 'Usa las funciones secundarias de F1-F12 sin mantener Fn.'
        },
        godModePreset: {
          title: 'Perfil del modo personalizado',
          desc: 'Activa un perfil del modo personalizado.\nEste ajuste solo tiene efecto si el modo personalizado está activado.'
        },
        hdr: {
          title: 'HDR',
          desc: 'Activa HDR en la pantalla integrada.\n\nAVISO: esta acción no funcionará si la pantalla integrada está apagada.'
        },
        hideMainWindow: {
          title: 'Ocultar ventana principal'
        },
        rgbKeyboardBacklight: {
          title: 'Retroiluminación del teclado',
          desc: 'Ajusta el perfil de retroiluminación.'
        },
        run: {
          title: 'Ejecutar',
          desc: 'Ejecuta un script o programa.\nAsegúrese de que su script funciona primero.'
        },
        showMainWindow: {
          title: 'Mostrar ventana principal'
        },
        speaker: {
          title: 'Altavoz',
          desc: 'Al silenciar, se silencian todos los dispositivos de salida activos.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'Brillo de la retroiluminación',
          desc: 'Ajusta el brillo de la retroiluminación.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'Importar perfil de retroiluminación',
          desc: 'Importa y aplica una configuración de retroiluminación al perfil actual.'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'Perfil de retroiluminación',
          desc: 'Ajusta el perfil de retroiluminación.'
        },
        touchpadLock: {
          title: 'Bloqueo del panel táctil',
          desc: 'Desactiva el panel táctil.'
        },
        turnOffMonitors: {
          title: 'Apagar pantallas',
          desc: 'Apaga todas las pantallas disponibles.'
        },
        turnOffWiFi: {
          title: 'Apagar Wi-Fi'
        },
        turnOnWiFi: {
          title: 'Encender Wi-Fi'
        },
        whiteKeyboardBacklight: {
          title: 'Retroiluminación del teclado',
          desc: 'Ajusta el brillo de la retroiluminación.'
        },
        winKey: {
          title: 'Bloqueo de tecla Windows',
          desc: 'Desactiva la tecla Windows en el teclado integrado.'
        }
      },
      moveUp: 'Subir',
      moveDown: 'Bajar',
      noEditableParameters: 'Este paso no tiene parámetros editables.',
      addAutomaticPipeline: 'Nueva acción',
      addQuickAction: 'Nueva acción rápida',
      quickActionName: 'Nombre de la acción rápida',
      triggerPicker: {
        title: 'Nueva acción — elija un disparador'
      },
      triggerConfig: {
        title: 'Configurar disparador',
        noEditableTriggers: 'Este disparador no tiene parámetros configurables.'
      },
      triggerNames: {
        aCAdapterConnected: 'Cuando se conecta el adaptador de CA',
        lowWattageACAdapterConnected: 'Cuando se conecta un adaptador de baja potencia',
        aCAdapterDisconnected: 'Cuando se desconecta el adaptador de CA',
        powerMode: 'Cuando cambia el modo de energía',
        godModePresetChanged: 'Cuando cambia el perfil del modo personalizado',
        gamesAreRunning: 'Cuando se ejecuta un juego',
        gamesStop: 'Cuando se cierra un juego',
        processesAreRunning: 'Cuando se inicia una app',
        processesStopRunning: 'Cuando se cierra una app',
        userInactivity: 'Cuando el usuario queda inactivo',
        userInactivityZero: 'Cuando el usuario vuelve a estar activo',
        sessionLock: 'Sesión bloqueada',
        sessionUnlock: 'Sesión desbloqueada',
        lidOpened: 'Tapa abierta',
        lidClosed: 'Tapa cerrada',
        displayOn: 'Cuando las pantallas se encienden',
        displayOff: 'Cuando las pantallas se apagan',
        hdrOn: 'Cuando el HDR se activa',
        hdrOff: 'Cuando el HDR se desactiva',
        deviceConnected: 'Cuando se conecta un dispositivo',
        deviceDisconnected: 'Cuando se desconecta un dispositivo',
        externalDisplayConnected: 'Cuando se conecta una pantalla externa',
        externalDisplayDisconnected: 'Cuando se desconecta una pantalla externa',
        wiFiConnected: 'Cuando el Wi-Fi se conecta',
        wiFiDisconnected: 'Cuando el Wi-Fi se desconecta',
        time: 'A una hora especificada',
        periodic: 'Acción periódica',
        hardwareSensor: 'Sensor de hardware',
        batteryPercentage: 'Porcentaje de batería',
        onStartup: 'Al iniciar',
        onResume: 'Al reanudar'
      },
      triggerEditors: {
        noProcesses: 'No hay procesos seleccionados.',
        noDevices: 'No hay dispositivos seleccionados.',
        inactivityTimeout: 'Tiempo de espera',
        seconds: '{{count}} segundos',
        minutes: '{{count}} minutos',
        hours: '{{count}} horas',
        ssidPlaceholder: 'Nombre de red (SSID)',
        addSsid: 'Añadir nombre de red',
        atTime: 'A la hora',
        hour: 'Hora',
        minute: 'Minuto',
        allDays: 'Todos los días',
        day: {
          0: 'Domingo',
          1: 'Lunes',
          2: 'Martes',
          3: 'Miércoles',
          4: 'Jueves',
          5: 'Viernes',
          6: 'Sábado'
        },
        metric: 'Métrica',
        comparison: 'Comparación',
        threshold: 'Umbral',
        thresholdPercent: 'Umbral (%)',
        durationSeconds: 'Duración (segundos)',
        cooldownSeconds: 'Reutilización (segundos)',
        chargeFilter: 'Filtro de carga',
        deviceInstanceId: 'ID de instancia del dispositivo'
      }
    },
    macro: {
      title: 'Macro de teclado',
      enable: 'Activar macros',
      enableDesc: 'Universal Device Toolkit debe estar en ejecución para que las macros funcionen.',
      subtitle: 'Puede grabar series de pulsaciones e invocarlas con el teclado numérico.',
      numpad: 'Teclado numérico',
      sequence: 'Secuencia',
      repeat: 'Repeticiones',
      events: 'Eventos',
      save: 'Guardar',
      clear: 'Limpiar',
      play: 'Reproducir',
      record: 'Grabar',
      recordingOptions: 'Opciones de grabación',
      ignoreDelays: 'Ignorar retrasos',
      interruptOnOtherKey: 'Interrumpir con otra tecla',
      dontRepeat: 'No repetir',
      keyboardOnly: 'Solo teclado',
      keyboardMouse: 'Teclado y botones del ratón',
      allInputs: 'Todas las entradas',
      recordingInterrupted: 'Grabación interrumpida',
      keyboard: 'Teclado',
      mouse: 'Ratón',
      move: 'Movimiento del ratón',
      wheelUp: 'Rueda arriba',
      wheelDown: 'Rueda abajo',
      wheelLeft: 'Rueda izquierda',
      wheelRight: 'Rueda derecha',
      leftButton: 'Botón izquierdo',
      rightButton: 'Botón derecho',
      middleButton: 'Botón central',
      xButton: 'Botón X',
      button: 'Botón del ratón',
      empty: 'Aún no hay secuencia de macro para esta tecla',
      recording: {
        preparing: 'La grabación comenzará en 3 segundos…',
        title: 'Grabando…',
        pressEscToStop: 'Pulse ESC para detener.',
        focusHint: 'Mantenga esta ventana enfocada mientras graba.'
      }
    },
    plugins: {
      title: 'Complementos y extensiones',
      search: 'Buscar complementos',
      filterAll: 'Todos',
      filterInstalled: 'Instalados',
      filterNotInstalled: 'No instalados',
      refresh: 'Actualizar',
      total: '{{count}} en total',
      summary: '{{count}} instalados',
      updatable: '{{count}} actualización(es) disponible(s)',
      install: 'Instalar',
      update: 'Actualizar',
      updateAvailable: 'Actualización disponible',
      uninstall: 'Desinstalar',
      uninstallConfirm: '¿Desinstalar este complemento?',
      uninstallFailed: 'No se pudo desinstalar',
      installed: 'Instalado',
      online: 'En línea',
      installing: 'Instalando…',
      downloading: 'Descargando…',
      preparingDownload: 'Preparando descarga…',
      downloadCompleted: 'Descarga completada',
      offline: 'La tienda en línea no está disponible; solo se muestran los complementos locales',
      empty: 'No se encontraron complementos',
      dependencies: 'Dependencias',
      dependenciesBlocked: 'Este complemento tiene dependencias sin satisfacer y no puede desinstalarse',
      details: 'Detalles',
      usageGuide: 'Guía de uso',
      changelog: 'Registro de cambios',
      importProgress: 'Importando paquetes…',
      importSuccess: 'Se importaron {{count}} paquete(s)',
      importFailed: 'No se pudieron importar {{count}} paquete(s)',
      installAll: 'Instalar todo',
      installAllComplete: 'Se instalaron {{count}} complemento(s)',
      installAllPartial: '{{count}} de {{total}} operaciones completadas',
      copyId: 'Copiar ID del complemento',
      copied: 'ID del complemento copiado al portapapeles',
      copyFailed: 'No se pudo copiar el ID del complemento',
      local: 'Local',
      collapseDetails: 'Ocultar detalles',
      showDetails: 'Mostrar detalles',
      updateInfo: 'Información de actualización',
      versionLabel: 'Versión:',
      configure: 'Configurar',
      open: 'Abrir',
      description: 'Instale y gestione complementos para ampliar funciones',
      storeUnavailable: 'Tienda de complementos no disponible',
      summaryTotal: 'Total de complementos',
      summaryInstalled: 'Instalados',
      summaryUpdates: 'Actualizaciones disponibles',
      importFromFiles: 'Importar desde archivos',
      updateAll: 'Actualizar todo',
      emptyStore: 'La tienda de complementos está vacía actualmente. Estén atentos a futuras actualizaciones.'
    },
    optimization: {
      title: 'Optimización del sistema',
      info: 'Estas acciones modifican servicios y archivos del sistema y pueden requerir permisos de administrador.',
      tabs: {
        optimization: 'Optimización',
        cleanup: 'Limpieza',
        driverDownload: 'Descarga de controladores',
        networkAcceleration: 'Aceleración de red'
      },
      recommended: 'Recomendado',
      selected: 'Seleccionado',
      selectedActions: 'Acciones seleccionadas',
      noSelection: 'No hay acciones seleccionadas',
      selectRecommended: 'Seleccionar recomendados',
      applyRecommended: 'Aplicar todos los recomendados',
      apply: 'Aplicar',
      clear: 'Limpiar (revertir)',
      applied: 'Aplicado',
      applyFailed: 'Error al aplicar (puede requerir permisos de administrador)',
      reverted: 'Revertido',
      revertFailed: 'Error al revertir (puede requerir permisos de administrador)',
      estimate: 'Estimar tamaño',
      estimateResult: 'Espacio recuperable',
      runCleanup: 'Ejecutar limpieza',
      cleanupHint: 'La limpieza ejecuta sus reglas personalizadas.',
      cleanupConfirm: '¿Ejecutar la limpieza ahora?',
      cleanupDone: 'Limpieza terminada',
      cleanupFailed: 'Error en la limpieza',
      cleanup: {
        custom: {
          header: 'Reglas de limpieza personalizadas',
          description: 'Carpetas adicionales que se limpian con las acciones seleccionadas.',
          empty: 'No hay reglas de limpieza personalizadas',
          add: 'Añadir carpeta',
          edit: 'Editar carpeta',
          remove: 'Quitar',
          clear: 'Limpiar todo',
          added: 'Regla añadida',
          updated: 'Regla actualizada',
          recursive: 'Incluir subcarpetas',
          noExtensions: 'No se especificaron extensiones',
          folderPickerFailed: 'No se pudo abrir el selector de carpetas'
        }
      },
      network: {
        status: 'Estado',
        running: 'En ejecución',
        stopped: 'Detenido',
        backendReady: 'Backend listo',
        backendNotReady: 'Backend no listo',
        config: 'Configuración básica',
        accelerationEnabled: 'Activar aceleración',
        mode: 'Modo',
        modes: {
          off: 'Desactivado',
          systemProxy: 'Proxy del sistema',
          hosts: 'Hosts',
          diagnosticsOnly: 'Solo diagnóstico'
        },
        save: 'Guardar configuración',
        saved: 'Configuración guardada',
        saveFailed: 'No se pudo guardar la configuración',
        start: 'Iniciar',
        stop: 'Detener',
        startFailed: 'No se pudo iniciar',
        stopFailed: 'No se pudo detener',
        modeLabel: 'Modo',
        targetsLabel: 'Objetivos',
        portLabel: 'Puerto',
        targetsHeading: 'Objetivos de aceleración',
        domainGroupsHint: 'Seleccione los servicios a acelerar mediante el proxy local.',
        domainGroupsEmptyTitle: 'No hay objetivos de aceleración',
        domainGroupsEmptyDescription: 'La lista de objetivos está vacía o no coincide con la búsqueda.',
        selectionHint: 'Los objetivos seleccionados se aplican al iniciar la aceleración.',
        searchTargets: 'Buscar objetivos',
        recommendedMenu: 'Recomendado',
        groupRuntime: '{{selected}}/{{total}} seleccionados  {{active}} activos',
        trafficHeading: 'Resumen de tráfico',
        metrics: {
          upload: 'Subida',
          download: 'Descarga',
          connections: 'Conexiones',
          total: 'Tráfico total',
          health: 'Salud'
        },
        trafficLive: 'Recolectando tráfico del proxy en vivo',
        trafficWaiting: 'Inicie la aceleración para recolectar tráfico en vivo',
        trafficUnavailable: 'Datos de tráfico temporalmente no disponibles',
        connectionsHeading: 'Conexiones actuales y recientes',
        destinationsHeading: 'Estadísticas de destinos',
        connectionSummary: '{{active}} activas / {{total}} total',
        destinationSummary: '{{count}} destinos',
        connectionStates: {
          active: 'Activa',
          completed: 'Completada',
          blocked: 'Bloqueada',
          failed: 'Fallida',
          stopped: 'Detenida',
          unknown: 'Desconocida'
        },
        unknownHost: 'Host desconocido',
        destinationRow: '{{count}} conn  {{latency}}',
        health: {
          healthy: 'Sano',
          degraded: 'Degradado',
          stopped: 'Detenido',
          unknown: 'Desconocido'
        },
        modeFull: {
          systemProxy: 'Proxy del sistema',
          hosts: 'Archivo hosts',
          diagnosticsOnly: 'Solo diagnóstico',
          off: 'Inactivo'
        },
        backendMissingHint: 'El worker de proxy no está disponible',
        selectGroupsFirstHint: 'Seleccione al menos un objetivo',
        advancedHeading: 'Avanzado',
        advancedBody: 'Ajustes avanzados y recuperación de red.',
        portFormat: 'Puerto: {{port}}',
        dangerZoneHeading: 'Zona de peligro',
        restoreHint: 'Restaura el estado de red original guardado antes de la aceleración.',
        restoreNetwork: 'Restaurar red',
        restoreConfirm: '¿Restaurar el estado de red del sistema ahora?',
        restored: 'Estado de red restaurado',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: 'Detectar',
          unknown: 'Desconocido',
          natTypes: {
            OpenInternet: 'NAT abierto',
            Nat: 'NAT',
            UdpBlocked: 'UDP bloqueado',
            Unknown: 'Desconocido'
          },
          internetConnected: 'Conectado',
          internetUnreachable: 'Inalcanzable',
          natType: 'Tipo de NAT',
          localIp: 'IP local',
          publicIp: 'IP pública',
          internet: 'Internet',
          dnsDomain: 'Dominio',
          customDns: 'DNS personalizado',
          enableDoh: 'DoH',
          dohUrl: 'URL de DoH',
          latency: 'Latencia',
          resolvedAddress: 'Dirección resuelta',
          latencyFormat: '{{ms}} ms',
          failed: 'Fallido',
          ipv6Support: 'Soporte IPv6',
          ipv6Address: 'Dirección IPv6',
          ipv6SupportedFull: 'Acceso IPv6 compatible',
          notSupported: 'No compatible'
        }
      },
      driverDownload: {
        comingSoon: 'La descarga de controladores estará disponible en una versión futura'
      },
      driver: {
        machineType: 'Tipo de máquina',
        machineTypePlaceholder: 'p. ej., 82K3',
        os: 'Sistema operativo',
        downloadTo: 'Descargar en',
        downloadToPlaceholder: 'Elija una carpeta para las descargas',
        browse: 'Examinar',
        openDownloadTo: 'Abrir carpeta',
        source: 'Fuente',
        primarySource: 'Vantage',
        primarySourceMessage: 'Base de datos oficial mediante Vantage.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'Base de datos de compatibilidad de PC Support.',
        scan: 'Escanear',
        scanning: 'Escaneando…',
        scanValidation: 'Introduzca un tipo de máquina de 4 caracteres y elija un sistema operativo.',
        disclaimer: 'Los paquetes provienen de la fuente elegida. Instale bajo su propio riesgo.',
        filter: 'Filtrar',
        onlyShowUpdates: 'Solo actualizaciones',
        sort: {
          name: 'Ordenar por nombre',
          category: 'Ordenar por categoría',
          date: 'Ordenar por fecha'
        },
        selectRecommended: 'Seleccionar recomendados',
        startAll: 'Iniciar todo',
        pauseAll: 'Pausar todo',
        clearSelection: 'Borrar selección',
        packagesFound: 'Se encontraron {{count}} paquetes.',
        packagesFoundOne: 'Se encontró 1 paquete.',
        status: {
          NotStarted: '',
          Queued: 'En cola',
          Downloading: 'Descargando',
          Installing: 'Instalando',
          Completed: 'Completado',
          Error: 'Error'
        },
        recommended: 'Recomendado',
        isUpdate: 'Actualización',
        reboot: {
          recommended: 'Reinicio recomendado',
          required: 'Reinicio requerido',
          shutdown: 'Apagado requerido'
        },
        oldPackageWarning: 'Este paquete tiene más de un año; el controlador puede estar desactualizado.',
        download: 'Descargar',
        install: 'Instalar',
        uninstall: 'Desinstalar',
        pause: 'Pausar',
        openReadme: 'Abrir readme',
        hide: 'Ocultar',
        hideAll: 'Ocultar todo',
        showHiddenDownloads: 'Mostrar descargas ocultas',
        downloadInProgress: {
          title: 'Descargas en curso',
          message: 'Todavía hay descargas activas. ¿Escanear de nuevo?',
          confirm: 'Escanear'
        },
        empty: {
          notScanned: {
            title: 'Escanear paquetes de controladores',
            message: 'Elija una fuente y escanee para listar descargas compatibles.'
          },
          noResults: {
            title: 'No se encontraron descargas de controladores',
            message: 'Pruebe con otra fuente, sistema operativo o tipo de máquina.'
          },
          noFilterResults: {
            title: 'No se encontraron descargas coincidentes',
            message: 'Ajuste el filtro, la opción de solo actualizaciones o la lista oculta.'
          },
          error: {
            title: 'El escaneo de controladores no se completó',
            message: 'Verifique la fuente y la conexión, y escanee de nuevo.'
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
      title: 'Acerca de',
      appName: 'Aplicación',
      version: 'Versión',
      build: 'Compilación',
      links: 'Enlaces del proyecto',
      projectWebsite: 'Sitio del proyecto en GitHub',
      latestRelease: 'Última versión en GitHub',
      applicationFolders: 'Carpetas de la aplicación',
      data: 'Datos',
      temp: 'Temp',
      pid: 'ID de proceso',
      machine: 'Modelo del dispositivo',
      bios: 'Versión del BIOS',
      compatible: 'Compatibilidad',
      yes: 'Compatible',
      no: 'No compatible',
      dataFolder: 'Carpeta de datos',
      thirdParty: 'Bibliotecas de terceros',
      copyright: 'Derechos de autor'
    },
    statusBanner: {
      updateAvailable: '¡Actualización disponible!',
      updateAvailableWithVersion: '¡Actualización {{version}} disponible!',
      pluginExtensionsDisabled: 'La navegación de Complementos y extensiones está oculta. Actívela en Configuración → Elementos de navegación.'
    },
    wpf: legacy.translation.wpf
  }
}



