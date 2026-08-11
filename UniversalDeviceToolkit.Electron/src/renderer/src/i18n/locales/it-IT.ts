import legacy from './it'

export default {
  translation: {
    ...legacy.translation,
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'Registro',
      openLogs: 'Apri cartella dei registri',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'Informazioni sul dispositivo'
    },
    nav: {
      dashboard: 'Dashboard',
      settings: 'Impostazioni',
      automation: 'Automazione',
      keyboard: 'Tastiera',
      keyboardBacklight: 'Retroilluminazione tastiera',
      macro: 'Macro personalizzate',
      windowsOptimization: 'Ottimizzazione di sistema',
      pluginExtensions: 'Plugin ed estensioni',
      about: 'Informazioni'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: 'Benvenuto! Scegli una sezione qui sotto per iniziare',
      hostReady: 'Backend connesso',
      hostState: 'Stato del backend',
      hostVersion: 'Versione del backend',
      initComplete: 'Inizializzazione completata',
      safeStart: 'Avvio sicuro, saltato',
      machine: 'Dispositivo',
      compatible: 'Compatibilità',
      status: 'Stato'
    },
    dashboard: {
      title: 'Home',
      customize: 'Personalizza',
      edit: {
        title: 'Modifica dashboard',
        description: 'Scegli quali sezioni e funzioni mostrare nella pagina iniziale.',
        showSensors: 'Sensori hardware',
        groups: 'Gruppi di funzioni',
        save: 'Salva',
        cancel: 'Annulla',
        saved: 'Layout dashboard salvato',
        error: 'Impossibile salvare il layout della dashboard',
        disclaimer: 'Alcune funzioni potrebbero non apparire in base allo stato e alla configurazione del dispositivo.',
        addGroup: 'Aggiungi',
        renameGroup: 'Modifica nome gruppo',
        deleteGroup: 'Elimina',
        moveUp: 'Sposta su',
        moveDown: 'Sposta giù',
        deleteItem: 'Elimina',
        addItem: 'Aggiungi',
        groupNamePlaceholder: 'Nome',
        items: {
          discreteGpu: 'Modalità GPU discreta',
          overclockGpu: 'Overclock GPU',
          turnOffMonitors: 'Spegni monitor'
        }
      },
      addItem: {
        title: 'Aggiungi',
        searchPlaceholder: 'Cerca',
        empty: 'Tutti gli elementi sono già stati aggiunti',
        addHint: 'Aggiungi elemento'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Memoria',
      temperature: 'Temperatura',
      usage: 'Utilizzo',
      power: 'Potenza',
      fanSpeed: 'Ventola',
      vram: 'VRAM',
      memoryUsed: 'Memoria usata',
      memoryTotal: 'Memoria totale',
      storageTemp: 'Temp. archiviazione',
      notAvailable: '--',
      sensor: {
        cpu: 'Processore',
        gpu: 'Scheda grafica',
        memory: 'Memoria',
        temperature: 'Temperatura',
        usage: 'Utilizzo',
        power: 'Potenza',
        fanSpeed: 'Ventola',
        vram: 'VRAM',
        frequency: 'Frequenza core',
        battery: 'Batteria',
        charge: 'Carica',
        health: 'Salute',
        rate: 'Velocità',
        fan: 'Ventola',
        lowPowerAdapter: 'Adattatore a bassa potenza collegato',
        batteryLow: 'Batteria scarica',
        acCharging: 'Adattatore collegato, in carica…',
        acNotCharging: 'Adattatore collegato, non in carica…',
        remainingTime: 'Tempo rimanente stimato: {0}',
        memoryTemperature: 'Temperatura memoria',
        ssdTemperature: 'Temperatura SSD',
        vramTemperature: 'Temperatura VRAM',
        vramUsage: 'Utilizzo VRAM',
        cycles: 'Cicli',
        capacity: 'Capacità',
        fullCapacity: 'Capacità di carica completa',
        designCapacity: 'Capacità di progetto',
        date: 'Data',
        voltage: 'Tensione core',
        voltageRange: 'Intervallo tensione',
        powerRange: 'Intervallo potenza',
        details: 'Dettagli',
        refreshInterval: 'Intervallo di aggiornamento',
        detail: {
          power: 'Potenza',
          powerCores: 'Core',
          powerMemory: 'Memoria',
          powerPlatform: 'Piattaforma',
          pCoreClock: 'Frequenza P-Core',
          eCoreClock: 'Frequenza E-Core',
          memoryUsage: 'Utilizzo memoria',
          sharedMemoryUsage: 'Utilizzo memoria condivisa',
          vramUsage: 'Utilizzo VRAM',
          hotSpot: 'Hot spot GPU',
          pcieThroughput: 'Throughput PCIe',
          designCapacity: 'Capacità di progetto',
          fullChargeCapacity: 'Capacità di carica completa'
        }
      },
      group: {
        power: 'Alimentazione',
        graphics: 'Grafica',
        display: 'Display',
        other: 'Altro',
        custom: 'Personalizzato'
      },
      card: {
        error: 'Impossibile applicare l’impostazione',
        config: 'Impostazioni avanzate',
        configComingSoon: 'Le impostazioni avanzate saranno disponibili in una versione futura'
      }
    },
    balanceMode: {
      title: 'Impostazioni modalità bilanciata',
      aiEngine: 'Attiva motore IA',
      aiEngineDesc: 'Rileva automaticamente i giochi in esecuzione e regola le prestazioni di CPU/GPU. Temperatura e rumore delle ventole possono aumentare.'
    },
    godMode: {
      title: 'Impostazioni modalità personalizzata',
      activePreset: 'Profilo attivo',
      presetName: 'Nome profilo',
      name: 'Nome',
      errorLoad: 'Impossibile caricare l’impostazione.',
      errorApply: 'Impossibile applicare le impostazioni',
      applySuccess: 'Impostazioni modalità personalizzata applicate.',
      defaultPresetName: 'Profilo',
      cpu: {
        title: 'CPU',
        longTermPL: 'Limite di potenza a lungo termine',
        'longTermPL.desc': 'Il consumo continuo raggiungibile dalla CPU.',
        shortTermPL: 'Limite di potenza a breve termine',
        'shortTermPL.desc': 'Il consumo di picco raggiungibile dalla CPU in breve tempo.',
        peakPL: 'Limite di potenza di picco',
        'peakPL.desc': 'Il consumo istantaneo massimo raggiungibile dalla CPU.',
        crossLoading: 'Limite a lungo termine (cross loading)',
        'crossLoading.desc': 'La potenza massima della CPU con CPU e GPU al massimo carico.',
        pl1Tau: 'Durata limite a breve termine',
        'pl1Tau.desc': 'Il tempo in cui la CPU può usare il limite a breve termine. Alla scadenza si applica quello a lungo termine.',
        apuSppt: 'Limite di potenza APU sPPT',
        'apuSppt.desc': 'Il consumo di picco raggiungibile dalla CPU con un lieve ritardo.',
        tempLimit: 'Limite temperatura CPU',
        'tempLimit.desc': 'La temperatura massima della CPU prima della riduzione di frequenza e potenza.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'Dynamic Boost',
        'dynamicBoost.desc': 'La potenza aggiuntiva assegnabile alla GPU in base al consumo della CPU.',
        ctgp: 'TGP configurabile',
        'ctgp.desc': 'La potenza aggiuntiva assegnabile alla GPU oltre il consumo di base.',
        tempLimit: 'Limite temperatura GPU',
        'tempLimit.desc': 'La temperatura massima della GPU prima della riduzione di frequenza e potenza.',
        totalProcessingPowerTarget: 'Obiettivo di potenza processore in CA',
        'totalProcessingPowerTarget.desc': 'Il punto in cui la CPU attiva la regolazione dinamica della potenza della GPU.',
        toCpuDynamicBoost: 'Dynamic Boost GPU verso CPU',
        'toCpuDynamicBoost.desc': 'La potenza aggiuntiva assegnabile alla CPU dalla GPU in base all’uso della CPU. Più alto il valore, migliori le prestazioni CPU.'
      },
      fans: {
        title: 'Ventole',
        curve: 'Curva delle ventole',
        curveMessage: 'La velocità segue il sensore più alto tra CPU, GPU e dissipatore. Passa il mouse su ogni punto per i valori esatti.',
        maxSpeed: 'Velocità massima ventole',
        maxSpeedWarning: 'L’uso prolungato di questa opzione danneggia le ventole.\nUsa questa opzione con molta attenzione!'
      },
      advanced: {
        title: 'Avanzate',
        message: 'Non modificare le opzioni seguenti se non sai cosa fai.',
        maxOffset: 'Offset massimo',
        maxOffsetWarning: 'Valori più alti possono causare comportamenti imprevedibili. Lascia a 0 in caso di dubbi.',
        minOffset: 'Offset minimo',
        minOffsetWarning: 'Valori più bassi possono causare comportamenti imprevedibili. Lascia a 0 in caso di dubbi.',
        invalidOffset: 'Inserisci un numero intero prima di salvare.'
      },
      vantageWarning: 'Le impostazioni della modalità personalizzata non verranno applicate correttamente con Lenovo Vantage o i suoi servizi attivi.',
      legionZoneWarning: 'Le impostazioni della modalità personalizzata non verranno applicate correttamente con Legion Zone o i suoi servizi attivi.'
    },
    overclock: {
      title: 'Impostazioni overclock GPU',
      preset: 'Profilo',
      coreOffset: 'Offset frequenza core',
      memoryOffset: 'Offset frequenza memoria',
      namePlaceholder: 'Nome…',
      newProfileName: 'Profilo',
      loadError: 'Impossibile caricare le impostazioni di overclock.'
    },
    feature: {
      powerMode: 'Modalità di alimentazione',
      'powerMode.desc': 'Cambia la modalità prestazioni.\nPuò essere cambiata anche con Fn+Q.',
      'powerMode.hint': 'Puoi cambiarla rapidamente con il collegamento Fn+Q.',
      'powerMode.warning': 'La modalità prestazioni potrebbe non funzionare correttamente senza adattatore.',
      battery: 'Modalità di carica batteria',
      'battery.desc': 'Scegli una modalità di carica. La modalità conservazione limita la carica per prolungare la vita della batteria; la carica rapida carica a potenza maggiore.',
      batteryNightCharge: 'Carica notturna',
      'batteryNightCharge.desc': 'Se attivata, carica all’80 % di notte e completa al 100 % al mattino.',
      alwaysOnUsb: 'USB sempre attiva',
      'alwaysOnUsb.desc': 'Mantiene l’alimentazione delle porte USB a computer spento, in sospensione o ibernazione.',
      instantBoot: 'Avvio istantaneo',
      'instantBoot.desc': 'Accende il computer appena viene collegata l’alimentazione.',
      flipToStart: 'Apri e avvia',
      'flipToStart.desc': 'L’apertura del coperchio accende automaticamente il portatile.',
      fnLock: 'Blocco Fn',
      'fnLock.desc': 'Se attivato, le funzioni si attivano senza premere Fn. Premi Fn con i tasti F1-F12 per le funzioni originali.',
      gSync: 'G-Sync',
      'gSync.desc': 'Attiva o disattiva la frequenza variabile G-Sync',
      hdr: 'HDR',
      'hdr.desc': 'Attiva l’HDR sul display integrato.',
      'hdr.warning': 'L’uso dell’HDR è bloccato dalle impostazioni di Windows.',
      hybridMode: 'Modalità ibrida',
      'hybridMode.desc': 'La modalità ibrida consente di passare tra GPU integrata e discreta. Disattivarla attiva la modalità diretta della GPU discreta; è richiesto un riavvio.',
      igpuMode: 'Modalità GPU discreta',
      'igpuMode.desc': 'Forza l’uscita grafica integrata per risparmiare energia',
      refreshRate: 'Frequenza di aggiornamento',
      'refreshRate.desc': 'Cambia la frequenza di aggiornamento del display integrato.',
      itsMode: 'Modalità ITS',
      'itsMode.desc': 'Soluzione termica intelligente',
      microphone: 'Microfono',
      'microphone.desc': 'Disattivandolo, tutti i microfoni disponibili vengono silenziati.',
      overDrive: 'Over Drive',
      'overDrive.desc': 'Migliora il tempo di risposta del display integrato. Può causare scie (ghosting).',
      panelLogo: 'Logo Legion',
      'panelLogo.desc': 'Accende o spegne il logo Legion sul retro del dispositivo.',
      portsBacklight: 'Retroilluminazione porte',
      'portsBacklight.desc': 'Accende o spegne le luci delle porte sul retro.',
      resolution: 'Risoluzione',
      'resolution.desc': 'Cambia la risoluzione del display integrato.',
      dpiScale: 'Scala DPI',
      'dpiScale.desc': 'Cambia la scala del display integrato.',
      speaker: 'Altoparlante',
      touchpadLock: 'Blocco touchpad',
      'touchpadLock.desc': 'Disattiva il touchpad. Consigliato quando si usa il mouse.',
      whiteKeyboard: 'Retroilluminazione tastiera',
      'whiteKeyboard.desc': 'Usa Fn + Spazio per attivare e regolare la retroilluminazione.',
      winKey: 'Disattiva tasto Windows',
      'winKey.desc': 'Solo tastiera integrata. Il tasto Windows non risponderà più.',
      oneLevelWhiteKeyboard: 'Retroilluminazione tastiera',
      'oneLevelWhiteKeyboard.desc': 'Usa il collegamento Fn + Spazio per attivare la retroilluminazione.',
      'hybridMode.states.hybrid': 'Ibrida',
      'hybridMode.states.hybridIGPUOnly': 'Ibrida-iGPU',
      'hybridMode.states.hybridAuto': 'Ibrida-auto',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'Informazioni sulle modalità GPU',
      'hybridMode.info.hybrid.title': 'Modalità ibrida',
      'hybridMode.info.hybrid.message': 'GPU integrata e discreta attive. Il sistema passa automaticamente dall’una all’altra.',
      'hybridMode.info.hybridIgpu.title': 'Solo modalità ibrida-iGPU',
      'hybridMode.info.hybridIgpu.message': 'Usa solo la GPU integrata. Riduce al minimo consumo e rumore.',
      'hybridMode.info.hybridIgpu.disclaimer': 'Questa modalità ha effetto solo quando la GPU discreta non lavora.',
      'hybridMode.info.hybridAuto.title': 'Modalità ibrida-auto',
      'hybridMode.info.hybridAuto.message': 'A batteria solo GPU integrata; in CA entrambe. Con adattatore non standard passa alla modalità solo iGPU.',
      'hybridMode.info.dgpu.title': 'Modalità dGPU',
      'hybridMode.info.dgpu.message': 'Usa solo la GPU discreta. Massime prestazioni grafiche ma maggior consumo.',
      'hybridMode.info.dgpu.disclaimer': 'Il passaggio da e verso questa modalità richiede un riavvio.',
      'hybridMode.restartRequired.title': 'Riavvio necessario',
      'hybridMode.restartRequired.message': 'Il passaggio a {{mode}} richiede un riavvio. Riavviare ora?',
      'hybridMode.restartRequired.now': 'Riavvia ora',
      'hybridMode.restartRequired.later': 'Riavvierò più tardi',
      'hybridMode.restartFailed': 'Impossibile riavviare automaticamente. Riavvia manualmente per completare.',
      'hybridMode.changeFailed.title': 'Impossibile cambiare la modalità GPU',
      'hybridMode.changeFailed.message': 'Riprova tra qualche secondo. Se la dGPU non risponde, riavvia il portatile.',
      batteryModes: {
        conservation: 'Modalità conservazione',
        normal: 'Modalità normale',
        rapidCharge: 'Carica rapida'
      },
      powerModeOptions: {
        quiet: 'Silenziosa',
        balance: 'Bilanciata',
        performance: 'Prestazioni',
        extreme: 'Estrema',
        godMode: 'Personalizzata'
      }
    },
    common: {
      loading: 'Caricamento…',
      error: 'Qualcosa è andato storto',
      retry: 'Riprova',
      close: 'Chiudi',
      cancel: 'Annulla',
      moreActions: 'Altre azioni',
      copied: 'Copiato negli appunti',
      add: 'Aggiungi',
      save: 'Salva',
      saveAndClose: 'Salva e chiudi',
      apply: 'Applica',
      applyAndClose: 'Applica e chiudi',
      default: 'Predefinito',
      rename: 'Rinomina',
      delete: 'Elimina',
      ok: 'OK'
    },
    colorPicker: {
      hex: 'Hex',
      red: 'Rosso',
      green: 'Verde',
      blue: 'Blu',
      ok: 'OK'
    },
    fanCurve: {
      fanSpeed: 'Velocità ventola',
      fanSpeedMax: '100 %',
      cpu: 'CPU',
      cpuSensor: 'Sensore CPU',
      gpu: 'GPU',
      gpu2: 'GPU #2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: 'Prossimamente'
    },
    settings: {
      title: 'Impostazioni',
      description: 'Configura l’aspetto, il comportamento e le funzioni dell’applicazione.',
      nav: {
        appearance: 'Aspetto',
        application: 'Applicazione',
        power: 'Alimentazione',
        display: 'Display',
        smartKeys: 'Smart Keys',
        update: 'Aggiornamento',
        integrations: 'Integrazioni',
        osd: 'OSD'
      },
      appearance: {
        language: 'Lingua',
        languageDesc: 'Scegli la lingua',
        temperature: 'Temperatura',
        temperatureDesc: 'Scegli l’unità usata dai sensori di temperatura.',
        theme: 'Tema',
        accentColor: 'Colore accento',
        accentColorDesc: 'Cambia il colore di accento dell’applicazione.',
        appScale: 'Scala interfaccia',
        appScaleDesc: 'Scala testo e intera interfaccia, indipendentemente dalla scala di Windows.',
        themeOptions: {
          system: 'Sistema',
          light: 'Chiaro',
          dark: 'Scuro'
        }
      },
      application: {
        minimizeToTray: 'Riduci nell’area di notifica',
        minimizeToTrayDesc: 'Riduci sempre nell’area di notifica invece della barra delle applicazioni.',
        minimizeOnClose: 'Riduci alla chiusura',
        minimizeOnCloseDesc: 'Riduci sempre nell’area di notifica. Per uscire, fai clic destro sull’icona e scegli Chiudi.',
        disableUnsupportedWarning: 'Non avvisare sui dispositivi incompatibili',
        disableUnsupportedWarningDesc: 'Nasconde l’avviso di dispositivo incompatibile all’avvio.',
        enableHardwareSensors: 'Sensori hardware',
        enableHardwareSensorsDesc: 'Attiva il polling hardware avanzato per monitorare temperatura, frequenza e limiti di potenza.',
        dontShowNotifications: 'Non mostrare notifiche',
        dontShowNotificationsDesc: 'Disattiva le notifiche dell’app e di sistema',
        autorun: 'Avvio all’accesso',
        autorunDesc: 'Avvia ridotto nell’area di notifica dopo l’accesso a Windows.',
        extensionsEnabled: 'Attiva estensioni',
        extensionsEnabledDesc: 'Abilita il caricamento di plugin ed estensioni',
        sensorSections: 'Sezioni sensori',
        sensorSectionsDesc: 'Scegli quali sezioni sensori mostrare e in quale ordine.',
        disableVantage: 'Disattiva Lenovo Vantage',
        disableVantageDesc: 'Disattiva Lenovo Vantage e ImController senza disinstallarli.\nDopo la modifica si consiglia un riavvio.',
        disableLegionZone: 'Disattiva Legion Zone',
        disableLegionZoneDesc: 'Disattiva Legion Zone e il suo servizio senza disinstallarlo.\nDopo la modifica si consiglia un riavvio.',
        disableLenovoHotkeys: 'Disattiva Lenovo Hotkeys',
        disableLenovoHotkeysDesc: 'Disattiva Lenovo Hotkeys e il suo servizio senza disinstallarlo.\nSe disattivati, questa app gestisce i collegamenti Fn.\nDopo la modifica si consiglia un riavvio.',
        valueOn: 'Attivo',
        valueOff: 'Disattivo'
      },
      saved: 'Impostazioni salvate',
      saveFailed: 'Impossibile salvare le impostazioni',
      osd: {
        title: 'OSD',
        showOsd: 'Mostra OSD',
        showOsdDesc: 'Mostra immediatamente la sovrapposizione a schermo.',
        style: 'Stile sovrapposizione',
        styles: {
          panel: 'Pannello',
          bar: 'Barra'
        },
        refreshInterval: 'Intervallo di aggiornamento',
        snapThreshold: 'Soglia di aggancio',
        lockPosition: 'Blocca posizione',
        resetPosition: 'Reimposta posizione',
        previewHint: 'Anteprima',
        tabs: {
          general: 'Generale',
          appearance: 'Aspetto',
          thresholds: 'Soglie',
          sensors: 'Sensori'
        },
        opacity: 'Opacità',
        cornerRadius: 'Raggio angoli',
        cornerRadiusTop: 'Alto',
        cornerRadiusBottom: 'Basso',
        fontSize: 'Dimensione carattere',
        background: 'Colore sfondo',
        category: 'Colore categoria',
        label: 'Colore etichetta',
        value: 'Colore valore',
        warning: 'Colore avviso',
        critical: 'Colore critico',
        separator: 'Colore separatore',
        thresholds: {
          performance: 'Prestazioni',
          fpsRedline: 'Limite FPS',
          lowFpsDelta: 'Delta FPS basso',
          temperature: 'Temperatura',
          usage: 'Utilizzo',
          warning: 'Avviso',
          critical: 'Critico'
        },
        items: {
          groups: {
            game: 'Gioco',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1 % Low',
            FrameTime: 'Tempo di frame',
            CpuFrequency: 'Frequenza core',
            CpuPCoreFrequency: 'Frequenza P-Core',
            CpuECoreFrequency: 'Frequenza E-Core',
            CpuUtilization: 'Utilizzo',
            CpuTemperature: 'Temperatura',
            CpuPower: 'Potenza',
            CpuFan: 'Ventola',
            GpuFrequency: 'Frequenza core',
            GpuUtilization: 'Utilizzo',
            GpuTemperature: 'Temperatura core',
            GpuVramUtilization: 'Utilizzo VRAM',
            GpuVramTemperature: 'Temp. VRAM',
            GpuPower: 'Potenza',
            GpuFan: 'Ventola',
            MemoryUtilization: 'Utilizzo',
            MemoryTemperature: 'Temperatura',
            Disk1Temperature: 'Temp. disco 1',
            Disk2Temperature: 'Temp. disco 2',
            PchTemperature: 'Temp. PCH',
            PchFan: 'Ventola'
          }
        }
      },
      power: {
        powerModeMapping: 'Associazione modalità di alimentazione',
        powerModeMappingDesc: 'Al cambio di modalità prestazioni, cambia in sincronia il piano o la modalità di alimentazione di Windows.',
        mappingModes: {
          disabled: 'Disattivato',
          windowsPowerMode: 'Modalità di alimentazione Windows',
          windowsPowerPlan: 'Piano di alimentazione Windows'
        },
        windowsPowerModes: 'Modalità di alimentazione Windows',
        windowsPowerModesDesc: 'Scegli la modalità di alimentazione Windows applicata al cambio di modalità.',
        windowsPowerPlans: 'Piano di alimentazione Windows',
        windowsPowerPlansDesc: 'Scegli il piano di alimentazione Windows applicato al cambio di modalità.',
        synchronizeBrightness: 'Blocca luminosità display',
        synchronizeBrightnessDesc: 'Se attivato, la luminosità resta identica tra i piani di alimentazione.',
        smartFnLock: 'Tasti modificatori Smart Fn Lock',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: 'Reimposta «Da batteria dal» all’avvio',
        resetBatteryOnSinceDesc: 'Reimposta il contatore «Da batteria dal» nella sezione batteria al riavvio del sistema.',
        godModeFnQ: 'Passa alla modalità personalizzata con Fn+Q',
        godModeFnQDesc: 'Consente di passare rapidamente alla modalità personalizzata con Fn+Q.'
      },
      display: {
        navigationItems: 'Visibilità elementi di navigazione',
        navigationKeys: {
          keyboard: 'Retroilluminazione tastiera',
          battery: 'Batteria',
          automation: 'Automazione',
          macro: 'Macro',
          windowsOptimization: 'Ottimizzazione Windows',
          pluginExtensions: 'Plugin ed estensioni',
          about: 'Informazioni'
        },
        notificationPosition: 'Posizione notifiche',
        notificationPositions: {
          bottomRight: 'In basso a destra',
          bottomCenter: 'In basso al centro',
          bottomLeft: 'In basso a sinistra',
          centerLeft: 'Centro sinistra',
          topLeft: 'In alto a sinistra',
          topCenter: 'In alto al centro',
          topRight: 'In alto a destra',
          centerRight: 'Centro destra',
          center: 'Centro'
        },
        notificationDuration: 'Durata notifiche',
        notificationDurations: {
          short: 'Breve (3 s)',
          normal: 'Normale (5 s)',
          long: 'Lunga (10 s)'
        },
        excludedRefreshRates: 'Frequenze escluse',
        excludedRefreshRatesDesc: 'Escludi frequenze per velocizzare il cambio con Fn+R.',
        excludedRefreshRatesHint: 'La modifica avanzata sarà disponibile in una versione futura',
        excludedRefreshRatesEmpty: 'Nessuna frequenza esclusa',
        excludedRefreshRatesManageHint: 'Fai clic per gestire le frequenze escluse',
        notifications: 'Notifiche',
        notificationsDesc: 'Scegli quali notifiche mostrare.',
        bootLogo: 'Logo di avvio',
        bootLogoDesc: 'Personalizza il logo mostrato all’avvio.'
      },
      smartKeys: {
        smartFnLock: 'Smart Fn Lock',
        smartFnLockDesc: 'Quando viene premuto Alt, Ctrl o Shift, Fn viene temporaneamente sbloccato.',
        off: 'Disattivato',
        hint: 'I tasti modificatori di Smart Fn Lock possono essere cambiati nelle impostazioni di alimentazione.',
        singlePressActionDesc: 'Assegna un’azione rapida alla singola pressione di Fn+F9.',
        doublePressActionDesc: 'Assegna un’azione rapida alla doppia pressione di Fn+F9.'
      },
      update: {
        frequency: 'Controlla aggiornamenti automaticamente',
        frequencies: {
          perHour: 'Ogni ora',
          perThreeHours: 'Ogni 3 ore',
          perTwelveHours: 'Ogni 12 ore',
          perDay: 'Ogni giorno',
          perWeek: 'Ogni settimana',
          perMonth: 'Ogni mese'
        },
        includePrerelease: 'Includi versioni preliminari',
        includePrereleaseDesc: 'Disattivato: solo versioni stabili; attivato: vengono ricevute anche le versioni beta.',
        repository: 'Repository aggiornamenti',
        repositoryDesc: 'Configura il repository GitHub per gli aggiornamenti. Lascia vuoto per il valore predefinito.',
        repositoryOwner: 'Proprietario repository',
        repositoryOwnerPlaceholder: 'es. SSC-STUDIO',
        repositoryName: 'Nome repository',
        repositoryNamePlaceholder: 'es. UniversalDeviceToolkit',
        check: 'Controlla aggiornamenti',
        comingSoon: 'Il controllo aggiornamenti sarà disponibile in una versione futura'
      },
      checkResult: {
        available: 'Nuova versione disponibile: v{{version}}',
        latest: 'Sei aggiornato'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'Condividi velocità ventole, temperatura batteria e altri dati con HWiNFO64. Dopo l’attivazione potrebbe servire riavviare HWiNFO64.',
        cli: 'Interfaccia a riga di comando',
        cliDesc: 'Attiva l’interfaccia a riga di comando per il controllo dalla riga di comando.'
      }
    },
    keyboard: {
      title: 'Retroilluminazione tastiera',
      unsupported: 'La retroilluminazione della tastiera non è supportata su questo dispositivo',
      rgb: {
        preset: 'Profilo',
        settings: 'Impostazioni retroilluminazione',
        effect: 'Effetto',
        speed: 'Velocità',
        brightness: 'Luminosità',
        zones: 'Colori delle zone',
        synchroniseZones: 'Sincronizza le zone',
        presets: {
          off: 'Spento',
          one: 'Profilo 1',
          two: 'Profilo 2',
          three: 'Profilo 3',
          four: 'Profilo 4'
        },
        effectOptions: {
          static: 'Statico',
          breath: 'Respirazione',
          smooth: 'Fluido',
          waveRtl: 'Onda (destra→sinistra)',
          waveLtr: 'Onda (sinistra→destra)'
        },
        speedOptions: {
          slowest: 'Lenta',
          slow: 'Lenta',
          fast: 'Veloce',
          fastest: 'Veloce'
        },
        brightnessOptions: {
          low: 'Bassa',
          high: 'Alta'
        }
      },
      spectrum: {
        brightness: 'Luminosità',
        profile: 'Profilo',
        logo: 'Logo',
        effects: 'Effetti',
        colors: 'Colori',
        addEffect: 'Aggiungi effetto',
        deleteEffect: 'Elimina',
        noEffects: 'Nessun effetto',
        selectAll: 'Seleziona tutte le zone',
        deselectAll: 'Deseleziona tutte le zone',
        switchLayout: 'Cambia layout tastiera',
        editEffect: 'Modifica',
        allKeys: 'Tutti i tasti',
        zonesCount: '{{count}} zone',
        noLayoutHint: 'Impossibile caricare il layout della tastiera.',
        selectEffectHint: 'Seleziona un effetto qui sotto per visualizzare e modificare i suoi tasti.',
        effectEdit: {
          addTitle: 'Aggiungi effetto',
          editTitle: 'Modifica effetto',
          effect: 'Effetto',
          speed: 'Velocità',
          direction: 'Direzione',
          clockwiseDirection: 'Direzione',
          color: 'Colore',
          colors: 'Colori',
          addColor: 'Aggiungi colore',
          keys: 'Tasti',
          alwaysWarning: 'Questo effetto verrà applicato a tutta la tastiera e sostituirà tutti gli altri effetti.'
        },
        effectTypes: {
          always: 'Permanente',
          rainbowScrew: 'Vite arcobaleno',
          rainbowWave: 'Onda arcobaleno',
          colorChange: 'Cambio colore',
          colorWave: 'Onda di colore',
          colorPulse: 'Pulsazione colore',
          smooth: 'Fluido',
          rain: 'Pioggia',
          ripple: 'Ondulazione',
          type: 'Digitazione',
          audioBounce: 'Rimbalzo audio',
          audioRipple: 'Ondulazione audio',
          auroraSync: 'Sincronizzazione Aurora'
        }
      }
    },
    automation: {
      title: 'Automazione',
      enable: 'Attiva automazione',
      enableDesc: 'Universal Device Toolkit deve essere in esecuzione perché le azioni automatiche funzionino.',
      subtitle: 'Se attivata, questa app controlla ed esegue le azioni corrispondenti quando lo stato del dispositivo cambia.',
      actionsTitle: 'Azioni',
      actionsEmpty: 'Nessuna azione automatica per ora',
      quickActionsTitle: 'Azioni rapide',
      quickActionsEmpty: 'Nessuna azione rapida per ora. Fai clic su «Nuova» per crearne una.',
      renamePipeline: 'Rinomina pipeline',
      renamePipelineTitle: 'Rinomina pipeline',
      renamePipelinePlaceholder: 'Inserisci il nome della pipeline',
      changeIcon: 'Cambia icona',
      empty: 'Nessuno script di automazione per ora. Fai clic su «Nuova» per crearne uno.',
      runNow: 'Esegui ora',
      delete: 'Elimina',
      deleteStep: 'Elimina passaggio',
      addPipeline: 'Nuova',
      addStep: 'Aggiungi passaggio',
      configure: 'Configura',
      stepType: 'Tipo di passaggio',
      steps: 'Passaggi',
      save: 'Salva',
      revert: 'Annulla',
      pipelineName: 'Nome pipeline',
      pipelineNamePlaceholder: 'Inserisci il nome della pipeline',
      quickAction: 'Azione rapida',
      optionsLoading: 'Caricamento opzioni…',
      stepLabels: {
        rgbKeyboardBacklight: 'Retroilluminazione tastiera',
        run: 'Esegui',
        showMainWindow: 'Mostra finestra principale',
        speaker: 'Altoparlante',
        spectrumKeyboardBacklightBrightness: 'Luminosità retroilluminazione',
        spectrumKeyboardBacklightImportProfile: 'Importa profilo retroilluminazione',
        spectrumKeyboardBacklightProfile: 'Profilo retroilluminazione',
        touchpadLock: 'Blocco touchpad',
        turnOffMonitors: 'Spegni display',
        turnOffWiFi: 'Disattiva Wi-Fi',
        turnOnWiFi: 'Attiva Wi-Fi',
        whiteKeyboardBacklight: 'Retroilluminazione tastiera',
        winKey: 'Blocco tasto Windows',
        scriptPath: 'Percorso eseguibile',
        scriptArguments: 'Argomenti',
        runSilently: 'Esegui in silenzio',
        runSilentlyDesc: 'Esegue le applicazioni console senza creare una finestra console.',
        runWaitUntilFinished: 'Attendi il termine',
        runWaitUntilFinishedDesc: 'Attende che il programma o lo script termini',
        runHint: 'Esegue uno script o un programma.\nAssicurati prima che lo script funzioni.',
        importProfilePath: 'Percorso',
        browse: 'Sfoglia',
        off: 'Spento',
        on: 'Attivo',
        mute: 'Silenzia',
        unmute: 'Riattiva audio',
        low: 'Basso',
        high: 'Alto',
        presetOne: 'Profilo 1',
        presetTwo: 'Profilo 2',
        presetThree: 'Profilo 3',
        presetFour: 'Profilo 4',
        values: {
          off: 'Spento',
          on: 'Attivo',
          mute: 'Silenzia',
          unmute: 'Riattiva audio',
          low: 'Basso',
          high: 'Alto',
          presetOne: 'Profilo 1',
          presetTwo: 'Profilo 2',
          presetThree: 'Profilo 3',
          presetFour: 'Profilo 4'
        }
      },
      state: {
        on: 'Attivo',
        off: 'Spento',
        hidden: 'Nascondi',
        show: 'Mostra',
        toggle: 'Cambia stato',
        quiet: 'Silenziosa',
        balance: 'Bilanciata',
        performance: 'Prestazioni',
        extreme: 'Estrema',
        godMode: 'Personalizzata',
        hybrid: 'Ibrida',
        hybridIgpu: 'Ibrida-iGPU',
        hybridAuto: 'Ibrida-auto',
        dgpu: 'dGPU',
        acAdapter: 'Adattatore CA',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'CA e USB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'Modalità GPU',
          desc: 'Seleziona la modalità GPU in base all’uso e all’alimentazione del computer.\nIl cambio di modalità può richiedere un riavvio.'
        },
        instantBoot: {
          title: 'Avvio istantaneo',
          desc: 'Accende il portatile quando viene collegato un caricatore.'
        },
        macro: {
          title: 'Macro',
          desc: 'Attiva o disattiva le macro.'
        },
        microphone: {
          title: 'Microfono',
          desc: 'Se spento, i microfoni verranno silenziati.'
        },
        notification: {
          title: 'Mostra notifica',
          desc: 'Mostra una notifica con il testo inserito.',
          placeholder: 'Testo della notifica'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'Retroilluminazione tastiera',
          desc: 'Accende o spegne la retroilluminazione.'
        },
        osd: {
          title: 'OSD',
          desc: 'Mostra o nascondi l’OSD'
        },
        overclockDiscreteGPU: {
          title: 'Overclock GPU',
          desc: 'Migliora le prestazioni con l’overclock della GPU discreta.\n\nAVVISO: questa azione non funzionerà se la GPU discreta non è disponibile.'
        },
        overDrive: {
          title: 'Over Drive',
          desc: 'Migliora il tempo di risposta del display integrato.'
        },
        panelLogoBacklight: {
          title: 'Retroilluminazione logo',
          desc: 'Accende o spegne la retroilluminazione del logo sul coperchio.'
        },
        playSound: {
          title: 'Riproduci suono',
          desc: 'Sono supportati formati comuni come wav o mp3.',
          browse: 'Sfoglia…',
          none: 'Nessun file selezionato'
        },
        portsBacklight: {
          title: 'Retroilluminazione porte',
          desc: 'Accende o spegne la retroilluminazione delle porte sul retro.'
        },
        powerMode: {
          title: 'Modalità di alimentazione',
          desc: 'Cambia la modalità prestazioni.'
        },
        quickAction: {
          title: 'Azione rapida',
          desc: 'Esegue un’azione rapida salvata.',
          placeholder: 'Seleziona un’azione rapida',
          empty: 'Nessuna azione rapida. Crea prima una pipeline senza trigger.'
        },
        refreshRate: {
          title: 'Frequenza di aggiornamento',
          desc: 'Cambia la frequenza di aggiornamento del display integrato.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento.',
          empty: 'Nessuna frequenza disponibile'
        },
        resolution: {
          title: 'Risoluzione',
          desc: 'Cambia la risoluzione del display integrato.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento.',
          empty: 'Nessuna risoluzione disponibile'
        },
        alwaysOnUsb: {
          title: 'USB sempre attiva',
          desc: 'Carica i dispositivi USB quando il portatile è spento, in sospensione o ibernazione.',
          options: {
            OnWhenSleeping: 'Attiva in sospensione',
            OnAlways: 'Sempre attiva'
          }
        },
        battery: {
          title: 'Modalità batteria',
          desc: 'Scegli come viene caricata la batteria.',
          options: {
            Conservation: 'Conservazione',
            Normal: 'Normale',
            RapidCharge: 'Carica rapida'
          }
        },
        batteryNightCharge: {
          title: 'Carica notturna',
          desc: 'Se attivato, il dispositivo carica all’80 % di notte e completa al 100 % al mattino.'
        },
        deactivateGPU: {
          title: 'Disattiva GPU',
          desc: 'Disattiva la GPU discreta se è attiva inutilmente.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento o la modalità ibrida inattiva.',
          options: {
            KillApps: 'Chiudi app',
            RestartGPU: 'Riavvia GPU'
          }
        },
        delay: {
          title: 'Ritardo',
          desc: 'Aggiunge un ritardo prima del passaggio successivo.',
          second_one: '{{count}} secondo',
          second_other: '{{count}} secondi'
        },
        displayBrightness: {
          title: 'Luminosità display',
          desc: 'Cambia la luminosità del display integrato.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento.',
          percent: '{{value}} %'
        },
        dpiScale: {
          title: 'DPI',
          desc: 'Cambia la scala del display integrato.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento.',
          percent: '{{value}} %'
        },
        flipToStart: {
          title: 'Apri e avvia',
          desc: 'Accende il portatile all’apertura del coperchio.'
        },
        fnLock: {
          title: 'Blocco Fn',
          desc: 'Usa le funzioni secondarie di F1-F12 senza tenere premuto Fn.'
        },
        godModePreset: {
          title: 'Profilo modalità personalizzata',
          desc: 'Attiva un profilo della modalità personalizzata.\nHa effetto solo se la modalità personalizzata è attiva.'
        },
        hdr: {
          title: 'HDR',
          desc: 'Attiva l’HDR sul display integrato.\n\nAVVISO: questa azione non funzionerà se il display integrato è spento.'
        },
        hideMainWindow: {
          title: 'Nascondi finestra principale'
        },
        rgbKeyboardBacklight: {
          title: 'Retroilluminazione tastiera',
          desc: 'Regola il profilo di retroilluminazione.'
        },
        run: {
          title: 'Esegui',
          desc: 'Esegue uno script o un programma.\nAssicurati prima che lo script funzioni.'
        },
        showMainWindow: {
          title: 'Mostra finestra principale'
        },
        speaker: {
          title: 'Altoparlante',
          desc: 'Se silenziato, tutti i dispositivi audio attivi verranno silenziati.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'Luminosità retroilluminazione',
          desc: 'Regola la luminosità della retroilluminazione.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'Importa profilo retroilluminazione',
          desc: 'Importa e applica una configurazione di retroilluminazione al profilo attuale.'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'Profilo retroilluminazione',
          desc: 'Regola il profilo di retroilluminazione.'
        },
        touchpadLock: {
          title: 'Blocco touchpad',
          desc: 'Disattiva il touchpad.'
        },
        turnOffMonitors: {
          title: 'Spegni display',
          desc: 'Spegne tutti i display disponibili.'
        },
        turnOffWiFi: {
          title: 'Disattiva Wi-Fi'
        },
        turnOnWiFi: {
          title: 'Attiva Wi-Fi'
        },
        whiteKeyboardBacklight: {
          title: 'Retroilluminazione tastiera',
          desc: 'Regola la luminosità della retroilluminazione.'
        },
        winKey: {
          title: 'Blocco tasto Windows',
          desc: 'Disattiva il tasto Windows sulla tastiera integrata.'
        }
      },
      moveUp: 'Sposta su',
      moveDown: 'Sposta giù',
      noEditableParameters: 'Questo passaggio non ha parametri modificabili.',
      addAutomaticPipeline: 'Nuova azione',
      addQuickAction: 'Nuova azione rapida',
      quickActionName: 'Nome azione rapida',
      triggerPicker: {
        title: 'Nuova azione — scegli un trigger'
      },
      triggerConfig: {
        title: 'Configura trigger',
        noEditableTriggers: 'Questo trigger non ha parametri configurabili.'
      },
      triggerNames: {
        aCAdapterConnected: 'Quando l’adattatore CA è collegato',
        lowWattageACAdapterConnected: 'Quando è collegato un adattatore a bassa potenza',
        aCAdapterDisconnected: 'Quando l’adattatore CA è scollegato',
        powerMode: 'Quando cambia la modalità di alimentazione',
        godModePresetChanged: 'Quando cambia il profilo della modalità personalizzata',
        gamesAreRunning: 'Quando un gioco è in esecuzione',
        gamesStop: 'Quando un gioco si chiude',
        processesAreRunning: 'Quando un’app si avvia',
        processesStopRunning: 'Quando un’app si chiude',
        userInactivity: 'Quando l’utente diventa inattivo',
        userInactivityZero: 'Quando l’utente torna attivo',
        sessionLock: 'Sessione bloccata',
        sessionUnlock: 'Sessione sbloccata',
        lidOpened: 'Coperchio aperto',
        lidClosed: 'Coperchio chiuso',
        displayOn: 'Quando i display si accendono',
        displayOff: 'Quando i display si spengono',
        hdrOn: 'Quando l’HDR si attiva',
        hdrOff: 'Quando l’HDR si disattiva',
        deviceConnected: 'Quando un dispositivo viene collegato',
        deviceDisconnected: 'Quando un dispositivo viene scollegato',
        externalDisplayConnected: 'Quando viene collegato un display esterno',
        externalDisplayDisconnected: 'Quando viene scollegato un display esterno',
        wiFiConnected: 'Quando il Wi-Fi è connesso',
        wiFiDisconnected: 'Quando il Wi-Fi si disconnette',
        time: 'A un’ora specificata',
        periodic: 'Azione periodica',
        hardwareSensor: 'Sensore hardware',
        batteryPercentage: 'Percentuale batteria',
        onStartup: 'All’avvio',
        onResume: 'Alla ripresa'
      },
      triggerEditors: {
        noProcesses: 'Nessun processo selezionato.',
        noDevices: 'Nessun dispositivo selezionato.',
        inactivityTimeout: 'Timeout',
        seconds: '{{count}} secondi',
        minutes: '{{count}} minuti',
        hours: '{{count}} ore',
        ssidPlaceholder: 'Nome rete (SSID)',
        addSsid: 'Aggiungi nome rete',
        atTime: 'All’ora',
        hour: 'Ora',
        minute: 'Minuto',
        allDays: 'Ogni giorno',
        day: {
          0: 'Domenica',
          1: 'Lunedì',
          2: 'Martedì',
          3: 'Mercoledì',
          4: 'Giovedì',
          5: 'Venerdì',
          6: 'Sabato'
        },
        metric: 'Metrica',
        comparison: 'Confronto',
        threshold: 'Soglia',
        thresholdPercent: 'Soglia (%)',
        durationSeconds: 'Durata (secondi)',
        cooldownSeconds: 'Ricarica (secondi)',
        chargeFilter: 'Filtro di carica',
        deviceInstanceId: 'ID istanza dispositivo'
      }
    },
    macro: {
      title: 'Macro tastiera',
      enable: 'Attiva macro',
      enableDesc: 'Universal Device Toolkit deve essere in esecuzione perché le macro funzionino.',
      subtitle: 'Puoi registrare sequenze di tasti e invocarle con il tastierino numerico.',
      numpad: 'Tastierino numerico',
      sequence: 'Sequenza',
      repeat: 'Ripetizioni',
      events: 'Eventi',
      save: 'Salva',
      clear: 'Cancella',
      play: 'Riproduci',
      record: 'Registra',
      recordingOptions: 'Opzioni di registrazione',
      ignoreDelays: 'Ignora ritardi',
      interruptOnOtherKey: 'Interrompi con altro tasto',
      dontRepeat: 'Non ripetere',
      keyboardOnly: 'Solo tastiera',
      keyboardMouse: 'Tastiera e pulsanti mouse',
      allInputs: 'Tutti gli input',
      recordingInterrupted: 'Registrazione interrotta',
      keyboard: 'Tastiera',
      mouse: 'Mouse',
      move: 'Movimento mouse',
      wheelUp: 'Rotella su',
      wheelDown: 'Rotella giù',
      wheelLeft: 'Rotella sinistra',
      wheelRight: 'Rotella destra',
      leftButton: 'Pulsante sinistro',
      rightButton: 'Pulsante destro',
      middleButton: 'Pulsante centrale',
      xButton: 'Pulsante X',
      button: 'Pulsante del mouse',
      empty: 'Nessuna sequenza di macro per questo tasto',
      recording: {
        preparing: 'La registrazione inizierà tra 3 secondi…',
        title: 'Registrazione…',
        pressEscToStop: 'Premi ESC per fermare.',
        focusHint: 'Mantieni questa finestra a fuoco durante la registrazione.'
      }
    },
    plugins: {
      title: 'Plugin ed estensioni',
      search: 'Cerca plugin',
      filterAll: 'Tutti',
      filterInstalled: 'Installati',
      filterNotInstalled: 'Non installati',
      refresh: 'Aggiorna',
      total: '{{count}} totali',
      summary: '{{count}} installati',
      updatable: '{{count}} aggiornamento/i disponibile/i',
      install: 'Installa',
      update: 'Aggiorna',
      updateAvailable: 'Aggiornamento disponibile',
      uninstall: 'Disinstalla',
      uninstallConfirm: 'Disinstallare questo plugin?',
      uninstallFailed: 'Disinstallazione non riuscita',
      installed: 'Installato',
      online: 'Online',
      installing: 'Installazione…',
      downloading: 'Download…',
      preparingDownload: 'Preparazione download…',
      downloadCompleted: 'Download completato',
      offline: 'Il negozio online non è disponibile; vengono mostrati solo i plugin locali',
      empty: 'Nessun plugin trovato',
      dependencies: 'Dipendenze',
      dependenciesBlocked: 'Questo plugin ha dipendenze non soddisfatte e non può essere disinstallato',
      details: 'Dettagli',
      usageGuide: 'Guida all’uso',
      changelog: 'Registro modifiche',
      importProgress: 'Importazione pacchetti plugin…',
      importSuccess: 'Importati {{count}} pacchetti plugin',
      importFailed: 'Impossibile importare {{count}} pacchetti plugin',
      installAll: 'Installa tutto',
      installAllComplete: 'Installati {{count}} plugin',
      installAllPartial: '{{count}} di {{total}} operazioni completate',
      copyId: 'Copia ID plugin',
      copied: 'ID plugin copiato negli appunti',
      copyFailed: 'Impossibile copiare l’ID del plugin',
      local: 'Locale',
      collapseDetails: 'Nascondi dettagli',
      showDetails: 'Mostra dettagli',
      updateInfo: 'Informazioni aggiornamento',
      versionLabel: 'Versione:',
      configure: 'Configura',
      open: 'Apri',
      description: 'Installa e gestisci plugin per estendere le funzionalità',
      storeUnavailable: 'Negozio plugin non disponibile',
      summaryTotal: 'Totale plugin',
      summaryInstalled: 'Installati',
      summaryUpdates: 'Aggiornamenti disponibili',
      importFromFiles: 'Importa da file',
      updateAll: 'Aggiorna tutto',
      emptyStore: 'Il negozio di plugin è attualmente vuoto. Resta sintonizzato per i futuri aggiornamenti.'
    },
    optimization: {
      title: 'Ottimizzazione di sistema',
      info: 'Queste azioni modificano servizi e file di sistema e potrebbero richiedere privilegi di amministratore.',
      tabs: {
        optimization: 'Ottimizzazione',
        cleanup: 'Pulizia',
        driverDownload: 'Download driver',
        networkAcceleration: 'Accelerazione rete'
      },
      recommended: 'Consigliato',
      selected: 'Selezionato',
      selectedActions: 'Azioni selezionate',
      noSelection: 'Nessuna azione selezionata',
      selectRecommended: 'Seleziona consigliate',
      applyRecommended: 'Applica tutte le consigliate',
      apply: 'Applica',
      clear: 'Cancella (annulla)',
      applied: 'Applicato',
      applyFailed: 'Applicazione non riuscita (potrebbero servire privilegi di amministratore)',
      reverted: 'Annullato',
      revertFailed: 'Annullamento non riuscito (potrebbero servire privilegi di amministratore)',
      estimate: 'Stima dimensione',
      estimateResult: 'Spazio recuperabile',
      runCleanup: 'Esegui pulizia',
      cleanupHint: 'La pulizia esegue le tue regole personalizzate.',
      cleanupConfirm: 'Eseguire la pulizia ora?',
      cleanupDone: 'Pulizia completata',
      cleanupFailed: 'Pulizia non riuscita',
      cleanup: {
        custom: {
          header: 'Regole di pulizia personalizzate',
          description: 'Cartelle aggiuntive pulite insieme alle azioni selezionate.',
          empty: 'Nessuna regola di pulizia personalizzata',
          add: 'Aggiungi cartella',
          edit: 'Modifica cartella',
          remove: 'Rimuovi',
          clear: 'Cancella tutto',
          added: 'Regola aggiunta',
          updated: 'Regola aggiornata',
          recursive: 'Includi sottocartelle',
          noExtensions: 'Nessuna estensione specificata',
          folderPickerFailed: 'Impossibile aprire il selettore di cartelle'
        }
      },
      network: {
        status: 'Stato',
        running: 'In esecuzione',
        stopped: 'Fermato',
        backendReady: 'Backend pronto',
        backendNotReady: 'Backend non pronto',
        config: 'Configurazione base',
        accelerationEnabled: 'Attiva accelerazione',
        mode: 'Modalità',
        modes: {
          off: 'Spento',
          systemProxy: 'Proxy di sistema',
          hosts: 'Hosts',
          diagnosticsOnly: 'Solo diagnostica'
        },
        save: 'Salva configurazione',
        saved: 'Configurazione salvata',
        saveFailed: 'Salvataggio configurazione non riuscito',
        start: 'Avvia',
        stop: 'Ferma',
        startFailed: 'Avvio non riuscito',
        stopFailed: 'Arresto non riuscito',
        modeLabel: 'Modalità',
        targetsLabel: 'Obiettivi',
        portLabel: 'Porta',
        targetsHeading: 'Obiettivi di accelerazione',
        domainGroupsHint: 'Seleziona i servizi da accelerare tramite il proxy locale.',
        domainGroupsEmptyTitle: 'Nessun obiettivo di accelerazione',
        domainGroupsEmptyDescription: 'L’elenco obiettivi è vuoto o non corrisponde alla ricerca.',
        selectionHint: 'Gli obiettivi selezionati vengono applicati all’avvio dell’accelerazione.',
        searchTargets: 'Cerca obiettivi',
        recommendedMenu: 'Consigliati',
        groupRuntime: '{{selected}}/{{total}} selezionati  {{active}} attivi',
        trafficHeading: 'Panoramica traffico',
        metrics: {
          upload: 'Invio',
          download: 'Ricezione',
          connections: 'Connessioni',
          total: 'Traffico totale',
          health: 'Salute'
        },
        trafficLive: 'Raccolta traffico proxy in tempo reale',
        trafficWaiting: 'Avvia l’accelerazione per raccogliere il traffico in tempo reale',
        trafficUnavailable: 'Dati sul traffico temporaneamente non disponibili',
        connectionsHeading: 'Connessioni attuali e recenti',
        destinationsHeading: 'Statistiche destinazioni',
        connectionSummary: '{{active}} attive / {{total}} totali',
        destinationSummary: '{{count}} destinazioni',
        connectionStates: {
          active: 'Attiva',
          completed: 'Completata',
          blocked: 'Bloccata',
          failed: 'Non riuscita',
          stopped: 'Fermata',
          unknown: 'Sconosciuta'
        },
        unknownHost: 'Host sconosciuto',
        destinationRow: '{{count}} conn  {{latency}}',
        health: {
          healthy: 'Sano',
          degraded: 'Degradato',
          stopped: 'Fermato',
          unknown: 'Sconosciuto'
        },
        modeFull: {
          systemProxy: 'Proxy di sistema',
          hosts: 'File hosts',
          diagnosticsOnly: 'Solo diagnostica',
          off: 'Inattivo'
        },
        backendMissingHint: 'Worker proxy non disponibile',
        selectGroupsFirstHint: 'Seleziona almeno un obiettivo',
        advancedHeading: 'Avanzate',
        advancedBody: 'Impostazioni avanzate e ripristino della rete.',
        portFormat: 'Porta: {{port}}',
        dangerZoneHeading: 'Zona pericolosa',
        restoreHint: 'Ripristina lo stato di rete originale registrato prima dell’accelerazione.',
        restoreNetwork: 'Ripristina rete',
        restoreConfirm: 'Ripristinare ora lo stato di rete del sistema?',
        restored: 'Stato di rete ripristinato',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: 'Rileva',
          unknown: 'Sconosciuto',
          natTypes: {
            OpenInternet: 'NAT aperto',
            Nat: 'NAT',
            UdpBlocked: 'UDP bloccato',
            Unknown: 'Sconosciuto'
          },
          internetConnected: 'Connesso',
          internetUnreachable: 'Non raggiungibile',
          natType: 'Tipo NAT',
          localIp: 'IP locale',
          publicIp: 'IP pubblico',
          internet: 'Internet',
          dnsDomain: 'Dominio',
          customDns: 'DNS personalizzato',
          enableDoh: 'DoH',
          dohUrl: 'URL DoH',
          latency: 'Latenza',
          resolvedAddress: 'Indirizzo risolto',
          latencyFormat: '{{ms}} ms',
          failed: 'Non riuscito',
          ipv6Support: 'Supporto IPv6',
          ipv6Address: 'Indirizzo IPv6',
          ipv6SupportedFull: 'Accesso IPv6 supportato',
          notSupported: 'Non supportato'
        }
      },
      driverDownload: {
        comingSoon: 'Il download dei driver sarà disponibile in una versione futura'
      },
      driver: {
        machineType: 'Tipo di macchina',
        machineTypePlaceholder: 'es. 82K3',
        os: 'Sistema operativo',
        downloadTo: 'Scarica in',
        downloadToPlaceholder: 'Scegli una cartella per i download',
        browse: 'Sfoglia',
        openDownloadTo: 'Apri cartella',
        source: 'Origine',
        primarySource: 'Vantage',
        primarySourceMessage: 'Database ufficiale dei dispositivi tramite Vantage.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'Database di compatibilità PC Support.',
        scan: 'Scansiona',
        scanning: 'Scansione…',
        scanValidation: 'Inserisci un tipo di macchina valido di 4 caratteri e scegli un sistema operativo.',
        disclaimer: 'I pacchetti provengono dall’origine scelta. Installazione a proprio rischio.',
        filter: 'Filtra',
        onlyShowUpdates: 'Solo aggiornamenti',
        sort: {
          name: 'Ordina per nome',
          category: 'Ordina per categoria',
          date: 'Ordina per data'
        },
        selectRecommended: 'Seleziona consigliati',
        startAll: 'Avvia tutto',
        pauseAll: 'Metti in pausa tutto',
        clearSelection: 'Cancella selezione',
        packagesFound: 'Trovati {{count}} pacchetti.',
        packagesFoundOne: 'Trovato 1 pacchetto.',
        status: {
          NotStarted: '',
          Queued: 'In coda',
          Downloading: 'Download',
          Installing: 'Installazione',
          Completed: 'Completato',
          Error: 'Errore'
        },
        recommended: 'Consigliato',
        isUpdate: 'Aggiornamento',
        reboot: {
          recommended: 'Riavvio consigliato',
          required: 'Riavvio necessario',
          shutdown: 'Spegnimento necessario'
        },
        oldPackageWarning: 'Questo pacchetto ha più di un anno; il driver potrebbe essere obsoleto.',
        download: 'Scarica',
        install: 'Installa',
        uninstall: 'Disinstalla',
        pause: 'Pausa',
        openReadme: 'Apri readme',
        hide: 'Nascondi',
        hideAll: 'Nascondi tutto',
        showHiddenDownloads: 'Mostra download nascosti',
        downloadInProgress: {
          title: 'Download in corso',
          message: 'Ci sono ancora download attivi. Scansionare di nuovo?',
          confirm: 'Scansiona'
        },
        empty: {
          notScanned: {
            title: 'Scansiona pacchetti driver',
            message: 'Scegli un’origine e scansiona per elencare i driver compatibili.'
          },
          noResults: {
            title: 'Nessun download di driver trovato',
            message: 'Prova un’altra origine, sistema operativo o tipo di macchina.'
          },
          noFilterResults: {
            title: 'Nessun download corrispondente',
            message: 'Regola il filtro, l’opzione solo aggiornamenti o l’elenco dei download nascosti.'
          },
          error: {
            title: 'Scansione driver non completata',
            message: 'Controlla l’origine selezionata e la connessione, poi scansiona di nuovo.'
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
      title: 'Informazioni',
      appName: 'Applicazione',
      version: 'Versione',
      build: 'Build',
      links: 'Collegamenti al progetto',
      projectWebsite: 'Sito del progetto su GitHub',
      latestRelease: 'Ultima versione su GitHub',
      applicationFolders: 'Cartelle dell’applicazione',
      data: 'Dati',
      temp: 'Temp',
      pid: 'ID processo',
      machine: 'Modello dispositivo',
      bios: 'Versione BIOS',
      compatible: 'Compatibilità',
      yes: 'Compatibile',
      no: 'Non compatibile',
      dataFolder: 'Cartella dati',
      thirdParty: 'Librerie di terze parti',
      copyright: 'Copyright'
    },
    statusBanner: {
      updateAvailable: 'Aggiornamento disponibile!',
      updateAvailableWithVersion: 'Aggiornamento {{version}} disponibile!',
      pluginExtensionsDisabled: 'La navigazione Plugin ed estensioni è nascosta. Attivala in Impostazioni → Elementi di navigazione.'
    },
    wpf: legacy.translation.wpf
  }
}



