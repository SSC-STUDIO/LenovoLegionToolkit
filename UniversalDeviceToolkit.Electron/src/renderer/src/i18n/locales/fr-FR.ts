import legacy from './fr'

export default {
  translation: {
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'Journal',
      openLogs: 'Ouvrir le dossier des journaux',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'Informations sur l’appareil'
    },
    nav: {
      dashboard: 'Tableau de bord',
      settings: 'Paramètres',
      automation: 'Automatisation',
      keyboard: 'Clavier',
      keyboardBacklight: 'Rétroéclairage du clavier',
      macro: 'Macro personnalisée',
      windowsOptimization: 'Optimisation du système',
      pluginExtensions: 'Plugins et extensions',
      about: 'À propos'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: 'Bienvenue ! Choisissez une section ci-dessous pour commencer',
      hostReady: 'Backend connecté',
      hostState: 'État du backend',
      hostVersion: 'Version du backend',
      initComplete: 'Initialisation terminée',
      safeStart: 'Démarrage sécurisé, ignoré',
      machine: 'Appareil',
      compatible: 'Compatibilité',
      status: 'État'
    },
    dashboard: {
      title: 'Accueil',
      customize: 'Personnaliser',
      edit: {
        title: 'Modifier le tableau de bord',
        description: 'Choisissez les sections et fonctions affichées sur la page d’accueil.',
        showSensors: 'Capteurs matériels',
        groups: 'Groupes de fonctions',
        save: 'Enregistrer',
        cancel: 'Annuler',
        saved: 'Disposition du tableau de bord enregistrée',
        error: 'Échec de l’enregistrement de la disposition',
        disclaimer: 'Certaines fonctions peuvent ne pas apparaître selon l’état et la configuration de votre ordinateur.',
        addGroup: 'Ajouter',
        renameGroup: 'Renommer le groupe',
        deleteGroup: 'Supprimer',
        moveUp: 'Monter',
        moveDown: 'Descendre',
        deleteItem: 'Supprimer',
        addItem: 'Ajouter',
        groupNamePlaceholder: 'Nom',
        items: {
          discreteGpu: 'Mode GPU discret',
          overclockGpu: 'Overclocker le GPU',
          turnOffMonitors: 'Éteindre les écrans'
        }
      },
      addItem: {
        title: 'Ajouter',
        searchPlaceholder: 'Rechercher',
        empty: 'Tous les éléments sont déjà ajoutés',
        addHint: 'Ajouter un élément'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Mémoire',
      temperature: 'Température',
      usage: 'Utilisation',
      power: 'Puissance',
      fanSpeed: 'Ventilateur',
      vram: 'VRAM',
      memoryUsed: 'Mémoire utilisée',
      memoryTotal: 'Mémoire totale',
      storageTemp: 'Température du stockage',
      notAvailable: '--',
      sensor: {
        cpu: 'Processeur',
        gpu: 'Carte graphique',
        memory: 'Mémoire',
        temperature: 'Température',
        usage: 'Utilisation',
        power: 'Puissance',
        fanSpeed: 'Ventilateur',
        vram: 'VRAM',
        frequency: 'Fréquence du cœur',
        battery: 'Batterie',
        charge: 'Charge',
        health: 'Santé',
        rate: 'Débit',
        fan: 'Ventilateur',
        lowPowerAdapter: 'Adaptateur basse puissance branché',
        batteryLow: 'Batterie faible',
        acCharging: 'Adaptateur branché, chargement…',
        acNotCharging: 'Adaptateur branché, pas de chargement…',
        remainingTime: 'Temps restant estimé : {0}',
        memoryTemperature: 'Température mémoire',
        ssdTemperature: 'Température SSD',
        vramTemperature: 'Température VRAM',
        vramUsage: 'Utilisation VRAM',
        cycles: 'Cycles',
        capacity: 'Capacité',
        fullCapacity: 'Capacité de charge complète',
        designCapacity: 'Capacité de conception',
        date: 'Date',
        voltage: 'Tension du cœur',
        voltageRange: 'Plage de tension',
        powerRange: 'Plage de puissance',
        details: 'Détails',
        refreshInterval: 'Intervalle de rafraîchissement',
        detail: {
          power: 'Puissance',
          powerCores: 'Cœurs',
          powerMemory: 'Mémoire',
          powerPlatform: 'Plateforme',
          pCoreClock: 'Fréquence P-Core',
          eCoreClock: 'Fréquence E-Core',
          memoryUsage: 'Utilisation mémoire',
          sharedMemoryUsage: 'Utilisation mémoire partagée',
          vramUsage: 'Utilisation VRAM',
          hotSpot: 'Point chaud GPU',
          pcieThroughput: 'Débit PCIe',
          designCapacity: 'Capacité de conception',
          fullChargeCapacity: 'Capacité de charge complète'
        }
      },
      group: {
        power: 'Alimentation',
        graphics: 'Graphiques',
        display: 'Affichage',
        other: 'Autres',
        custom: 'Personnalisé'
      },
      card: {
        error: 'Échec de l’application du réglage',
        config: 'Paramètres avancés',
        configComingSoon: 'Les paramètres avancés seront disponibles dans une future version'
      }
    },
    balanceMode: {
      title: 'Paramètres du mode équilibré',
      aiEngine: 'Activer le moteur IA',
      aiEngineDesc: 'Détecte automatiquement certains jeux et ajuste les performances CPU/GPU. La température et le bruit du ventilateur peuvent augmenter.'
    },
    godMode: {
      title: 'Paramètres du mode personnalisé',
      activePreset: 'Préréglage actif',
      presetName: 'Nom du préréglage',
      name: 'Nom',
      errorLoad: 'Impossible de charger le réglage.',
      errorApply: 'Impossible d’appliquer les réglages',
      applySuccess: 'Paramètres du mode personnalisé appliqués.',
      defaultPresetName: 'Préréglage',
      cpu: {
        title: 'CPU',
        longTermPL: 'Limite de puissance à long terme',
        'longTermPL.desc': 'La consommation continue atteignable par le CPU.',
        shortTermPL: 'Limite de puissance à court terme',
        'shortTermPL.desc': 'La consommation de pointe atteignable par le CPU sur une courte durée.',
        peakPL: 'Limite de puissance de crête',
        'peakPL.desc': 'La consommation instantanée maximale atteignable par le CPU.',
        crossLoading: 'Limite à long terme (charge croisée)',
        'crossLoading.desc': 'La consommation maximale du CPU quand CPU et GPU sont tous deux à pleine charge.',
        pl1Tau: 'Durée de la limite à court terme',
        'pl1Tau.desc': 'La durée pendant laquelle le CPU peut utiliser la limite à court terme. Après expiration, la limite à long terme s’applique.',
        apuSppt: 'Limite de puissance APU sPPT',
        'apuSppt.desc': 'La consommation de pointe atteignable par le CPU avec un léger délai.',
        tempLimit: 'Limite de température CPU',
        'tempLimit.desc': 'La température maximale du CPU avant réduction de fréquence et de puissance.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'Dynamic Boost',
        'dynamicBoost.desc': 'La puissance supplémentaire allouable au GPU selon la consommation du CPU.',
        ctgp: 'TGP configurable',
        'ctgp.desc': 'La puissance supplémentaire allouable au GPU en plus de la consommation de base.',
        tempLimit: 'Limite de température GPU',
        'tempLimit.desc': 'La température maximale du GPU avant réduction de fréquence et de puissance.',
        totalProcessingPowerTarget: 'Cible de puissance processeur sur secteur',
        'totalProcessingPowerTarget.desc': 'Le point où le CPU déclenche l’ajustement dynamique de puissance du GPU.',
        toCpuDynamicBoost: 'Dynamic Boost GPU vers CPU',
        'toCpuDynamicBoost.desc': 'La puissance supplémentaire allouable au CPU depuis le GPU selon l’utilisation CPU. Plus la valeur est élevée, meilleures sont les performances CPU.'
      },
      fans: {
        title: 'Ventilateurs',
        curve: 'Courbe du ventilateur',
        curveMessage: 'La vitesse du ventilateur suit la lecture la plus élevée entre CPU, GPU et dissipateur. Survolez chaque étape pour voir les valeurs exactes.',
        maxSpeed: 'Vitesse maximale du ventilateur',
        maxSpeedWarning: 'Une utilisation prolongée de cette option dégradera les ventilateurs.\nSoyez très prudent avec cette option !'
      },
      advanced: {
        title: 'Avancé',
        message: 'Ne modifiez pas les options ci-dessous sans savoir ce que vous faites.',
        maxOffset: 'Offset maximal',
        maxOffsetWarning: 'Des valeurs plus élevées peuvent provoquer un comportement imprévisible. Laissez à 0 en cas de doute.',
        minOffset: 'Offset minimal',
        minOffsetWarning: 'Des valeurs plus basses peuvent provoquer un comportement imprévisible. Laissez à 0 en cas de doute.',
        invalidOffset: 'Saisissez un nombre entier avant d’enregistrer.'
      },
      vantageWarning: 'Les paramètres du mode personnalisé ne seront pas appliqués correctement si Lenovo Vantage ou ses services sont actifs.',
      legionZoneWarning: 'Les paramètres du mode personnalisé ne seront pas appliqués correctement si Legion Zone ou ses services sont actifs.'
    },
    overclock: {
      title: 'Paramètres d’overclocking GPU',
      preset: 'Préréglage',
      coreOffset: 'Offset de fréquence du cœur',
      memoryOffset: 'Offset de fréquence mémoire',
      namePlaceholder: 'Nom…',
      newProfileName: 'Préréglage',
      loadError: 'Impossible de charger les paramètres d’overclocking.'
    },
    feature: {
      powerMode: 'Mode de performance',
      'powerMode.desc': 'Change le mode de performance.\nIl peut aussi être changé avec Fn+Q.',
      'powerMode.hint': 'Changez-le rapidement avec le raccourci Fn+Q.',
      'powerMode.warning': 'Le mode de performance peut ne pas fonctionner correctement sans adaptateur secteur.',
      battery: 'Mode de charge de la batterie',
      'battery.desc': 'Choisissez un mode de charge. Le mode conservation limite la charge pour prolonger la durée de vie, le mode charge rapide charge à plus forte puissance.',
      batteryNightCharge: 'Charge nocturne',
      'batteryNightCharge.desc': 'Une fois activé, charge à 80 % la nuit et complète à 100 % le matin.',
      alwaysOnUsb: 'USB toujours actif',
      'alwaysOnUsb.desc': 'Maintient l’alimentation des ports USB lorsque l’ordinateur est éteint, en veille ou en veille prolongée.',
      instantBoot: 'Démarrage instantané',
      'instantBoot.desc': 'Allume l’ordinateur dès que le secteur est branché.',
      flipToStart: 'Démarrage à l’ouverture',
      'flipToStart.desc': 'L’ouverture du capot allume automatiquement l’ordinateur.',
      fnLock: 'Verrouillage Fn',
      'fnLock.desc': 'Quand il est activé, les fonctions s’activent sans la touche Fn. Appuyez sur Fn avec F1 à F12 pour les fonctions d’origine.',
      gSync: 'G-Sync',
      'gSync.desc': 'Active ou désactive le taux de rafraîchissement variable G-Sync',
      hdr: 'HDR',
      'hdr.desc': 'Active le HDR sur l’écran intégré.',
      'hdr.warning': 'L’utilisation du HDR est bloquée par les paramètres Windows.',
      hybridMode: 'Mode hybride',
      'hybridMode.desc': 'Le mode hybride permet de basculer entre GPU intégré et discret. Le désactiver active le mode GPU discret direct ; un redémarrage est requis.',
      igpuMode: 'Mode GPU discret',
      'igpuMode.desc': 'Force la sortie graphique intégrée pour économiser de l’énergie',
      refreshRate: 'Taux de rafraîchissement',
      'refreshRate.desc': 'Change le taux de rafraîchissement de l’écran intégré.',
      itsMode: 'Mode ITS',
      'itsMode.desc': 'Solution thermique intelligente',
      microphone: 'Microphone',
      'microphone.desc': 'Désactivé, cela coupe tous les microphones disponibles.',
      overDrive: 'Over Drive',
      'overDrive.desc': 'Améliore le temps de réponse de l’écran intégré. Peut provoquer des effets de rémanence.',
      panelLogo: 'Logo Legion',
      'panelLogo.desc': 'Allume ou éteint le logo Legion à l’arrière de l’appareil.',
      portsBacklight: 'Rétroéclairage des ports',
      'portsBacklight.desc': 'Allume ou éteint les lumières des ports à l’arrière de l’appareil.',
      resolution: 'Résolution',
      'resolution.desc': 'Change la résolution de l’écran intégré.',
      dpiScale: 'Échelle DPI',
      'dpiScale.desc': 'Change l’échelle de l’écran intégré.',
      speaker: 'Haut-parleur',
      touchpadLock: 'Verrouillage du pavé tactile',
      'touchpadLock.desc': 'Désactive le pavé tactile. Recommandé avec une souris pour éviter les touches accidentelles.',
      whiteKeyboard: 'Rétroéclairage du clavier',
      'whiteKeyboard.desc': 'Utilisez Fn + Espace pour activer et régler la luminosité du rétroéclairage.',
      winKey: 'Désactiver la touche Windows',
      'winKey.desc': 'Ne concerne que le clavier intégré. La touche Windows ne répond plus.',
      oneLevelWhiteKeyboard: 'Rétroéclairage du clavier',
      'oneLevelWhiteKeyboard.desc': 'Utilisez le raccourci Fn + Espace pour activer le rétroéclairage.',
      'hybridMode.states.hybrid': 'Hybride',
      'hybridMode.states.hybridIGPUOnly': 'Hybride-iGPU',
      'hybridMode.states.hybridAuto': 'Hybride-auto',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'À propos des modes GPU',
      'hybridMode.info.hybrid.title': 'Mode hybride',
      'hybridMode.info.hybrid.message': 'GPU intégré et discret sont activés. Le système bascule automatiquement selon les besoins.',
      'hybridMode.info.hybridIgpu.title': 'Mode hybride-iGPU uniquement',
      'hybridMode.info.hybridIgpu.message': 'Utilise uniquement le GPU intégré. Consommation et bruit minimisés.',
      'hybridMode.info.hybridIgpu.disclaimer': 'Ce mode n’a d’effet que lorsque le GPU discret est inactif.',
      'hybridMode.info.hybridAuto.title': 'Mode hybride-auto',
      'hybridMode.info.hybridAuto.message': 'Sur batterie, utilise le GPU intégré ; sur secteur, les deux. Avec un adaptateur non standard, bascule en hybride-iGPU uniquement.',
      'hybridMode.info.dgpu.title': 'Mode dGPU',
      'hybridMode.info.dgpu.message': 'Utilise uniquement le GPU discret. Meilleures performances graphiques mais consommation accrue.',
      'hybridMode.info.dgpu.disclaimer': 'Le passage vers ou depuis ce mode nécessite un redémarrage.',
      'hybridMode.restartRequired.title': 'Redémarrage requis',
      'hybridMode.restartRequired.message': 'Le passage à {{mode}} nécessite un redémarrage. Voulez-vous redémarrer maintenant ?',
      'hybridMode.restartRequired.now': 'Redémarrer maintenant',
      'hybridMode.restartRequired.later': 'Je redémarrerai plus tard',
      'hybridMode.restartFailed': 'Impossible de redémarrer automatiquement. Redémarrez manuellement pour terminer.',
      'hybridMode.changeFailed.title': 'Impossible de changer le mode GPU',
      'hybridMode.changeFailed.message': 'Réessayez dans quelques secondes. Si le dGPU ne répond pas du tout, redémarrez votre ordinateur portable.',
      batteryModes: {
        conservation: 'Mode conservation',
        normal: 'Mode normal',
        rapidCharge: 'Mode charge rapide'
      },
      powerModeOptions: {
        quiet: 'Silencieux',
        balance: 'Équilibré',
        performance: 'Performance',
        extreme: 'Extrême',
        godMode: 'Personnalisé'
      }
    },
    common: {
      loading: 'Chargement…',
      error: 'Une erreur est survenue',
      retry: 'Réessayer',
      close: 'Fermer',
      cancel: 'Annuler',
      moreActions: 'Plus d’actions',
      copied: 'Copié dans le presse-papiers',
      add: 'Ajouter',
      save: 'Enregistrer',
      saveAndClose: 'Enregistrer et fermer',
      apply: 'Appliquer',
      applyAndClose: 'Appliquer et fermer',
      default: 'Par défaut',
      rename: 'Renommer',
      delete: 'Supprimer',
      ok: 'OK'
    },
    colorPicker: {
      hex: 'Hex',
      red: 'Rouge',
      green: 'Vert',
      blue: 'Bleu',
      ok: 'OK'
    },
    fanCurve: {
      fanSpeed: 'Vitesse du ventilateur',
      fanSpeedMax: '100 %',
      cpu: 'CPU',
      cpuSensor: 'Capteur CPU',
      gpu: 'GPU',
      gpu2: 'GPU n°2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: 'Bientôt disponible'
    },
    settings: {
      title: 'Paramètres',
      description: 'Configurez l’apparence, le comportement et les fonctionnalités de l’application.',
      nav: {
        appearance: 'Apparence',
        application: 'Application',
        power: 'Alimentation',
        display: 'Affichage',
        smartKeys: 'Touches intelligentes',
        update: 'Mise à jour',
        integrations: 'Intégrations',
        osd: 'OSD'
      },
      appearance: {
        language: 'Langue',
        languageDesc: 'Choisissez la langue',
        temperature: 'Température',
        temperatureDesc: 'Choisissez l’unité utilisée par les capteurs de température.',
        theme: 'Thème',
        accentColor: 'Couleur d’accent',
        accentColorDesc: 'Changez la couleur d’accent de l’application.',
        appScale: 'Échelle de l’interface',
        appScaleDesc: 'Met à l’échelle le texte et toute l’interface, indépendamment du zoom Windows.',
        themeOptions: {
          system: 'Système',
          light: 'Clair',
          dark: 'Sombre'
        }
      },
      application: {
        minimizeToTray: 'Réduire dans la barre d’état',
        minimizeToTrayDesc: 'Toujours réduire dans la barre d’état plutôt que la barre des tâches.',
        minimizeOnClose: 'Réduire à la fermeture',
        minimizeOnCloseDesc: 'Toujours réduire dans la barre d’état. Pour quitter, clic droit sur l’icône puis Fermer.',
        disableUnsupportedWarning: 'Ne pas avertir pour les appareils incompatibles',
        disableUnsupportedWarningDesc: 'Masque l’avertissement d’appareil incompatible affiché au démarrage.',
        enableHardwareSensors: 'Capteurs matériels',
        enableHardwareSensorsDesc: 'Active la collecte matérielle avancée pour surveiller température, fréquence et limites de puissance.',
        dontShowNotifications: 'Ne pas afficher les notifications',
        dontShowNotificationsDesc: 'Désactive les notifications dans l’application et le système',
        autorun: 'Démarrage à la connexion',
        autorunDesc: 'Démarre réduit dans la barre d’état après la connexion à Windows.',
        extensionsEnabled: 'Activer les extensions',
        extensionsEnabledDesc: 'Active le chargement des plugins et extensions',
        sensorSections: 'Sections de capteurs',
        sensorSectionsDesc: 'Choisissez les sections de capteurs affichées et leur ordre.',
        disableVantage: 'Désactiver Lenovo Vantage',
        disableVantageDesc: 'Désactive Lenovo Vantage et ImController sans les désinstaller.\nRedémarrage recommandé après modification.',
        disableLegionZone: 'Désactiver Legion Zone',
        disableLegionZoneDesc: 'Désactive Legion Zone et son service sans le désinstaller.\nRedémarrage recommandé après modification.',
        disableLenovoHotkeys: 'Désactiver Lenovo Hotkeys',
        disableLenovoHotkeysDesc: 'Désactive Lenovo Hotkeys et son service sans le désinstaller.\nUne fois désactivé, cette application gère les raccourcis Fn.\nRedémarrage recommandé après modification.',
        valueOn: 'Activé',
        valueOff: 'Désactivé'
      },
      saved: 'Paramètres enregistrés',
      saveFailed: 'Échec de l’enregistrement des paramètres',
      osd: {
        title: 'OSD',
        showOsd: 'Afficher l’OSD',
        showOsdDesc: 'Affiche immédiatement l’affichage à l’écran.',
        style: 'Style de superposition',
        styles: {
          panel: 'Panneau',
          bar: 'Barre'
        },
        refreshInterval: 'Intervalle de rafraîchissement',
        snapThreshold: 'Seuil d’ancrage',
        lockPosition: 'Verrouiller la position',
        resetPosition: 'Réinitialiser la position',
        previewHint: 'Aperçu',
        tabs: {
          general: 'Général',
          appearance: 'Apparence',
          thresholds: 'Seuils',
          sensors: 'Capteurs'
        },
        opacity: 'Opacité',
        cornerRadius: 'Rayon des coins',
        cornerRadiusTop: 'Haut',
        cornerRadiusBottom: 'Bas',
        fontSize: 'Taille de police',
        background: 'Couleur de fond',
        category: 'Couleur de catégorie',
        label: 'Couleur d’étiquette',
        value: 'Couleur de valeur',
        warning: 'Couleur d’avertissement',
        critical: 'Couleur critique',
        separator: 'Couleur de séparateur',
        thresholds: {
          performance: 'Performance',
          fpsRedline: 'Limite FPS',
          lowFpsDelta: 'Delta FPS faible',
          temperature: 'Température',
          usage: 'Utilisation',
          warning: 'Avertissement',
          critical: 'Critique'
        },
        items: {
          groups: {
            game: 'Jeu',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '1 % Low',
            FrameTime: 'Temps de frame',
            CpuFrequency: 'Fréquence du cœur',
            CpuPCoreFrequency: 'Fréquence P-Core',
            CpuECoreFrequency: 'Fréquence E-Core',
            CpuUtilization: 'Utilisation',
            CpuTemperature: 'Température',
            CpuPower: 'Puissance',
            CpuFan: 'Ventilateur',
            GpuFrequency: 'Fréquence du cœur',
            GpuUtilization: 'Utilisation',
            GpuTemperature: 'Température du cœur',
            GpuVramUtilization: 'Utilisation VRAM',
            GpuVramTemperature: 'Température VRAM',
            GpuPower: 'Puissance',
            GpuFan: 'Ventilateur',
            MemoryUtilization: 'Utilisation',
            MemoryTemperature: 'Température',
            Disk1Temperature: 'Température disque 1',
            Disk2Temperature: 'Température disque 2',
            PchTemperature: 'Température PCH',
            PchFan: 'Ventilateur'
          }
        }
      },
      power: {
        powerModeMapping: 'Mappage des modes d’alimentation',
        powerModeMappingDesc: 'Lors du changement de mode, bascule automatiquement le plan ou le mode d’alimentation Windows.',
        mappingModes: {
          disabled: 'Désactivé',
          windowsPowerMode: 'Mode d’alimentation Windows',
          windowsPowerPlan: 'Plan d’alimentation Windows'
        },
        windowsPowerModes: 'Mode d’alimentation Windows',
        windowsPowerModesDesc: 'Choisissez le mode d’alimentation Windows appliqué lors d’un changement de mode.',
        windowsPowerPlans: 'Plan d’alimentation Windows',
        windowsPowerPlansDesc: 'Choisissez le plan d’alimentation Windows appliqué lors d’un changement de mode.',
        synchronizeBrightness: 'Verrouiller la luminosité',
        synchronizeBrightnessDesc: 'Une fois activé, la luminosité reste identique entre les plans d’alimentation.',
        smartFnLock: 'Touches de modification Smart Fn Lock',
        modifierKeys: {
          shift: 'Maj',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: 'Réinitialiser « Sur batterie depuis » au démarrage',
        resetBatteryOnSinceDesc: 'Réinitialise le compteur « Sur batterie depuis » de la section batterie au redémarrage.',
        godModeFnQ: 'Passer en mode personnalisé avec Fn+Q',
        godModeFnQDesc: 'Permet de basculer rapidement en mode personnalisé avec Fn+Q.'
      },
      display: {
        navigationItems: 'Visibilité des éléments de navigation',
        navigationKeys: {
          keyboard: 'Rétroéclairage du clavier',
          battery: 'Batterie',
          automation: 'Automatisation',
          macro: 'Macro',
          windowsOptimization: 'Optimisation Windows',
          pluginExtensions: 'Plugins et extensions',
          about: 'À propos'
        },
        notificationPosition: 'Position des notifications',
        notificationPositions: {
          bottomRight: 'En bas à droite',
          bottomCenter: 'En bas au centre',
          bottomLeft: 'En bas à gauche',
          centerLeft: 'Au centre à gauche',
          topLeft: 'En haut à gauche',
          topCenter: 'En haut au centre',
          topRight: 'En haut à droite',
          centerRight: 'Au centre à droite',
          center: 'Au centre'
        },
        notificationDuration: 'Durée des notifications',
        notificationDurations: {
          short: 'Courte (3 s)',
          normal: 'Normale (5 s)',
          long: 'Longue (10 s)'
        },
        excludedRefreshRates: 'Taux de rafraîchissement exclus',
        excludedRefreshRatesDesc: 'Excluez des taux pour accélérer le basculement Fn+R.',
        excludedRefreshRatesHint: 'L’édition avancée sera disponible dans une future version',
        excludedRefreshRatesEmpty: 'Aucun taux de rafraîchissement exclu',
        excludedRefreshRatesManageHint: 'Cliquez pour gérer les taux exclus',
        notifications: 'Notifications',
        notificationsDesc: 'Choisissez les notifications affichées.',
        bootLogo: 'Logo de démarrage',
        bootLogoDesc: 'Personnalisez le logo affiché au démarrage.'
      },
      smartKeys: {
        smartFnLock: 'Smart Fn Lock',
        smartFnLockDesc: 'Lorsque Alt, Ctrl ou Maj est enfoncée, Fn est temporairement déverrouillé.',
        off: 'Désactivé',
        hint: 'Les touches de modification Smart Fn Lock se changent dans les paramètres d’alimentation.',
        singlePressActionDesc: 'Assignez une action rapide à l’appui simple sur Fn+F9.',
        doublePressActionDesc: 'Assignez une action rapide au double appui sur Fn+F9.'
      },
      update: {
        frequency: 'Vérifier les mises à jour automatiquement',
        frequencies: {
          perHour: 'Toutes les heures',
          perThreeHours: 'Toutes les 3 heures',
          perTwelveHours: 'Toutes les 12 heures',
          perDay: 'Chaque jour',
          perWeek: 'Chaque semaine',
          perMonth: 'Chaque mois'
        },
        includePrerelease: 'Inclure les versions préliminaires',
        includePrereleaseDesc: 'Désactivé : seules les versions stables sont proposées ; activé : les versions bêta sont aussi reçues.',
        repository: 'Dépôt de mise à jour',
        repositoryDesc: 'Configurez le dépôt GitHub pour les mises à jour. Laissez vide pour utiliser la valeur par défaut.',
        repositoryOwner: 'Propriétaire du dépôt',
        repositoryOwnerPlaceholder: 'ex. : SSC-STUDIO',
        repositoryName: 'Nom du dépôt',
        repositoryNamePlaceholder: 'ex. : UniversalDeviceToolkit',
        check: 'Vérifier les mises à jour',
        comingSoon: 'La vérification des mises à jour sera disponible dans une future version'
      },
      checkResult: {
        available: 'Nouvelle version disponible : v{{version}}',
        latest: 'Vous êtes à jour'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'Partagez la vitesse des ventilateurs, la température de la batterie et d’autres données avec HWiNFO64. Un redémarrage de HWiNFO64 peut être nécessaire.',
        cli: 'Interface en ligne de commande',
        cliDesc: 'Active l’interface en ligne de commande pour le contrôle depuis la ligne de commande.'
      }
    },
    keyboard: {
      title: 'Rétroéclairage du clavier',
      unsupported: 'Le rétroéclairage du clavier n’est pas pris en charge sur cet appareil',
      rgb: {
        preset: 'Préréglage',
        settings: 'Paramètres du rétroéclairage',
        effect: 'Effet',
        speed: 'Vitesse',
        brightness: 'Luminosité',
        zones: 'Couleurs des zones',
        synchroniseZones: 'Synchroniser les zones',
        presets: {
          off: 'Désactivé',
          one: 'Préréglage 1',
          two: 'Préréglage 2',
          three: 'Préréglage 3',
          four: 'Préréglage 4'
        },
        effectOptions: {
          static: 'Statique',
          breath: 'Respiration',
          smooth: 'Fluide',
          waveRtl: 'Vague (droite→gauche)',
          waveLtr: 'Vague (gauche→droite)'
        },
        speedOptions: {
          slowest: 'Le plus lent',
          slow: 'Lent',
          fast: 'Rapide',
          fastest: 'Le plus rapide'
        },
        brightnessOptions: {
          low: 'Bas',
          high: 'Haut'
        }
      },
      spectrum: {
        brightness: 'Luminosité',
        profile: 'Profil',
        logo: 'Logo',
        effects: 'Effets',
        colors: 'Couleurs',
        addEffect: 'Ajouter un effet',
        deleteEffect: 'Supprimer',
        noEffects: 'Aucun effet',
        selectAll: 'Sélectionner toutes les zones',
        deselectAll: 'Désélectionner toutes les zones',
        switchLayout: 'Changer la disposition du clavier',
        editEffect: 'Modifier',
        allKeys: 'Toutes les touches',
        zonesCount: '{{count}} zones',
        noLayoutHint: 'Impossible de charger la disposition du clavier.',
        selectEffectHint: 'Sélectionnez un effet ci-dessous pour prévisualiser et modifier ses touches.',
        effectEdit: {
          addTitle: 'Ajouter un effet',
          editTitle: 'Modifier l’effet',
          effect: 'Effet',
          speed: 'Vitesse',
          direction: 'Direction',
          clockwiseDirection: 'Direction',
          color: 'Couleur',
          colors: 'Couleurs',
          addColor: 'Ajouter une couleur',
          keys: 'Touches',
          alwaysWarning: 'Cet effet s’appliquera à tout le clavier et remplacera tous les autres effets.'
        },
        effectTypes: {
          always: 'Permanent',
          rainbowScrew: 'Vis arc-en-ciel',
          rainbowWave: 'Vague arc-en-ciel',
          colorChange: 'Changement de couleur',
          colorWave: 'Vague de couleur',
          colorPulse: 'Pulsation de couleur',
          smooth: 'Fluide',
          rain: 'Pluie',
          ripple: 'Ripple',
          type: 'Frappe',
          audioBounce: 'Rebond audio',
          audioRipple: 'Ripple audio',
          auroraSync: 'Synchronisation Aurora'
        }
      }
    },
    automation: {
      title: 'Automatisation',
      enable: 'Activer l’automatisation',
      enableDesc: 'Universal Device Toolkit doit être en cours d’exécution pour que les actions automatiques fonctionnent.',
      subtitle: 'Une fois activé, cette application vérifie et exécute les actions correspondantes lorsque l’état de l’appareil change.',
      actionsTitle: 'Actions',
      actionsEmpty: 'Aucune action automatique pour l’instant',
      quickActionsTitle: 'Actions rapides',
      quickActionsEmpty: 'Aucune action rapide pour l’instant. Cliquez sur « Nouveau » pour en créer une.',
      renamePipeline: 'Renommer le pipeline',
      renamePipelineTitle: 'Renommer le pipeline',
      renamePipelinePlaceholder: 'Entrez le nom du pipeline',
      changeIcon: 'Changer l’icône',
      empty: 'Aucun script d’automatisation pour l’instant. Cliquez sur « Nouveau » pour en créer un.',
      runNow: 'Exécuter maintenant',
      delete: 'Supprimer',
      deleteStep: 'Supprimer l’étape',
      addPipeline: 'Nouveau',
      addStep: 'Ajouter une étape',
      configure: 'Configurer',
      stepType: 'Type d’étape',
      steps: 'Étapes',
      save: 'Enregistrer',
      revert: 'Annuler',
      pipelineName: 'Nom du pipeline',
      pipelineNamePlaceholder: 'Entrez le nom du pipeline',
      quickAction: 'Action rapide',
      optionsLoading: 'Chargement des options…',
      stepLabels: {
        rgbKeyboardBacklight: 'Rétroéclairage du clavier',
        run: 'Exécuter',
        showMainWindow: 'Afficher la fenêtre principale',
        speaker: 'Haut-parleur',
        spectrumKeyboardBacklightBrightness: 'Luminosité du rétroéclairage',
        spectrumKeyboardBacklightImportProfile: 'Importer un profil de rétroéclairage',
        spectrumKeyboardBacklightProfile: 'Profil de rétroéclairage',
        touchpadLock: 'Verrouillage du pavé tactile',
        turnOffMonitors: 'Éteindre les écrans',
        turnOffWiFi: 'Désactiver le Wi-Fi',
        turnOnWiFi: 'Activer le Wi-Fi',
        whiteKeyboardBacklight: 'Rétroéclairage du clavier',
        winKey: 'Verrouillage de la touche Windows',
        scriptPath: 'Chemin de l’exécutable',
        scriptArguments: 'Arguments',
        runSilently: 'Exécution silencieuse',
        runSilentlyDesc: 'Exécute les applications console sans créer de fenêtre console.',
        runWaitUntilFinished: 'Attendre la fin',
        runWaitUntilFinishedDesc: 'Attend que le programme ou script termine son exécution',
        runHint: 'Exécutez un script ou un programme.\nAssurez-vous que votre script fonctionne correctement.',
        importProfilePath: 'Chemin',
        browse: 'Parcourir',
        off: 'Désactivé',
        on: 'Activé',
        mute: 'Muet',
        unmute: 'Rétablir le son',
        low: 'Bas',
        high: 'Haut',
        presetOne: 'Préréglage 1',
        presetTwo: 'Préréglage 2',
        presetThree: 'Préréglage 3',
        presetFour: 'Préréglage 4',
        values: {
          off: 'Désactivé',
          on: 'Activé',
          mute: 'Muet',
          unmute: 'Rétablir le son',
          low: 'Bas',
          high: 'Haut',
          presetOne: 'Préréglage 1',
          presetTwo: 'Préréglage 2',
          presetThree: 'Préréglage 3',
          presetFour: 'Préréglage 4'
        }
      },
      state: {
        on: 'Activé',
        off: 'Désactivé',
        hidden: 'Masquer',
        show: 'Afficher',
        toggle: 'Basculer l’état',
        quiet: 'Silencieux',
        balance: 'Équilibré',
        performance: 'Performance',
        extreme: 'Extrême',
        godMode: 'Personnalisé',
        hybrid: 'Hybride',
        hybridIgpu: 'Hybride-iGPU',
        hybridAuto: 'Hybride-auto',
        dgpu: 'dGPU',
        acAdapter: 'Adaptateur secteur',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'Secteur et USB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'Mode GPU',
          desc: 'Sélectionnez le mode GPU selon l’utilisation et l’alimentation de votre ordinateur.\nLe changement de mode peut nécessiter un redémarrage.'
        },
        instantBoot: {
          title: 'Démarrage instantané',
          desc: 'Allume l’ordinateur lorsqu’un chargeur est branché.'
        },
        macro: {
          title: 'Macro',
          desc: 'Active ou désactive les macros.'
        },
        microphone: {
          title: 'Microphone',
          desc: 'Désactivé, les microphones seront coupés.'
        },
        notification: {
          title: 'Afficher une notification',
          desc: 'Affiche une notification avec le texte saisi.',
          placeholder: 'Texte de la notification'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'Rétroéclairage du clavier',
          desc: 'Allume ou éteint le rétroéclairage.'
        },
        osd: {
          title: 'OSD',
          desc: 'Affiche ou masque l’OSD'
        },
        overclockDiscreteGPU: {
          title: 'Overclocker le GPU',
          desc: 'Améliore les performances en overclockant le GPU discret.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si le GPU discret est indisponible.'
        },
        overDrive: {
          title: 'Over Drive',
          desc: 'Améliore le temps de réponse de l’écran intégré.'
        },
        panelLogoBacklight: {
          title: 'Logo arrière',
          desc: 'Allume ou éteint le rétroéclairage du logo sur le capot.'
        },
        playSound: {
          title: 'Jouer un son',
          desc: 'Les formats courants comme wav ou mp3 sont pris en charge.',
          browse: 'Parcourir…',
          none: 'Aucun fichier sélectionné'
        },
        portsBacklight: {
          title: 'Rétroéclairage des ports',
          desc: 'Allume ou éteint le rétroéclairage des ports à l’arrière.'
        },
        powerMode: {
          title: 'Mode de performance',
          desc: 'Change le mode de performance.'
        },
        quickAction: {
          title: 'Action rapide',
          desc: 'Exécute une action rapide enregistrée.',
          placeholder: 'Sélectionnez une action rapide',
          empty: 'Aucune action rapide. Créez d’abord un pipeline sans déclencheur.'
        },
        refreshRate: {
          title: 'Taux de rafraîchissement',
          desc: 'Change le taux de rafraîchissement de l’écran intégré.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint.',
          empty: 'Aucun taux de rafraîchissement disponible'
        },
        resolution: {
          title: 'Résolution',
          desc: 'Change la résolution de l’écran intégré.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint.',
          empty: 'Aucune résolution disponible'
        },
        alwaysOnUsb: {
          title: 'USB toujours actif',
          desc: 'Charge les périphériques USB lorsque l’ordinateur est éteint, en veille ou en veille prolongée.',
          options: {
            OnWhenSleeping: 'Activé en veille',
            OnAlways: 'Toujours activé'
          }
        },
        battery: {
          title: 'Mode batterie',
          desc: 'Choisissez comment la batterie est chargée.',
          options: {
            Conservation: 'Conservation',
            Normal: 'Normal',
            RapidCharge: 'Charge rapide'
          }
        },
        batteryNightCharge: {
          title: 'Charge nocturne',
          desc: 'Une fois activé, l’appareil charge à 80 % la nuit et complète à 100 % au matin.'
        },
        deactivateGPU: {
          title: 'Désactiver le GPU',
          desc: 'Désactive le GPU discret s’il est actif inutilement.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint ou si le mode hybride est inactif.',
          options: {
            KillApps: 'Fermer les applications',
            RestartGPU: 'Redémarrer le GPU'
          }
        },
        delay: {
          title: 'Délai',
          desc: 'Ajoute un délai avant d’exécuter l’étape suivante.',
          second_one: '{{count}} seconde',
          second_other: '{{count}} secondes'
        },
        displayBrightness: {
          title: 'Luminosité de l’écran',
          desc: 'Change la luminosité de l’écran intégré.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint.',
          percent: '{{value}} %'
        },
        dpiScale: {
          title: 'DPI',
          desc: 'Change l’échelle de l’écran intégré.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint.',
          percent: '{{value}} %'
        },
        flipToStart: {
          title: 'Démarrage à l’ouverture',
          desc: 'Allume l’ordinateur à l’ouverture du capot.'
        },
        fnLock: {
          title: 'Verrouillage Fn',
          desc: 'Utilise les fonctions secondaires de F1-F12 sans maintenir la touche Fn.'
        },
        godModePreset: {
          title: 'Préréglage du mode personnalisé',
          desc: 'Active un préréglage du mode personnalisé.\nCe réglage n’a d’effet que si le mode personnalisé est activé.'
        },
        hdr: {
          title: 'HDR',
          desc: 'Active le HDR sur l’écran intégré.\n\nAVERTISSEMENT : cette action ne fonctionnera pas si l’écran intégré est éteint.'
        },
        hideMainWindow: {
          title: 'Masquer la fenêtre principale'
        },
        rgbKeyboardBacklight: {
          title: 'Rétroéclairage du clavier',
          desc: 'Règle le préréglage du rétroéclairage.'
        },
        run: {
          title: 'Exécuter',
          desc: 'Exécute un script ou un programme.\nAssurez-vous que votre script fonctionne correctement.'
        },
        showMainWindow: {
          title: 'Afficher la fenêtre principale'
        },
        speaker: {
          title: 'Haut-parleur',
          desc: 'En mode muet, tous les périphériques de sortie audio actifs seront coupés.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'Luminosité du rétroéclairage',
          desc: 'Règle la luminosité du rétroéclairage.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'Importer un profil de rétroéclairage',
          desc: 'Importe et applique une configuration de rétroéclairage au profil actuel.'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'Profil de rétroéclairage',
          desc: 'Règle le profil de rétroéclairage.'
        },
        touchpadLock: {
          title: 'Verrouillage du pavé tactile',
          desc: 'Désactive le pavé tactile.'
        },
        turnOffMonitors: {
          title: 'Éteindre les écrans',
          desc: 'Éteint tous les écrans disponibles.'
        },
        turnOffWiFi: {
          title: 'Désactiver le Wi-Fi'
        },
        turnOnWiFi: {
          title: 'Activer le Wi-Fi'
        },
        whiteKeyboardBacklight: {
          title: 'Rétroéclairage du clavier',
          desc: 'Règle la luminosité du rétroéclairage.'
        },
        winKey: {
          title: 'Verrouillage de la touche Windows',
          desc: 'Désactive la touche Windows sur le clavier intégré.'
        }
      },
      moveUp: 'Monter',
      moveDown: 'Descendre',
      noEditableParameters: 'Cette étape n’a aucun paramètre modifiable.',
      addAutomaticPipeline: 'Nouvelle action',
      addQuickAction: 'Nouvelle action rapide',
      quickActionName: 'Nom de l’action rapide',
      triggerPicker: {
        title: 'Nouvelle action — choisissez un déclencheur'
      },
      triggerConfig: {
        title: 'Configurer le déclencheur',
        noEditableTriggers: 'Ce déclencheur n’a aucun paramètre configurable.'
      },
      triggerNames: {
        aCAdapterConnected: 'Lorsque l’adaptateur secteur est branché',
        lowWattageACAdapterConnected: 'Lorsqu’un adaptateur basse puissance est branché',
        aCAdapterDisconnected: 'Lorsque l’adaptateur secteur est débranché',
        powerMode: 'Lorsque le mode de performance change',
        godModePresetChanged: 'Lorsque le préréglage du mode personnalisé change',
        gamesAreRunning: 'Lorsqu’un jeu est en cours d’exécution',
        gamesStop: 'Lorsqu’un jeu se ferme',
        processesAreRunning: 'Lorsqu’une application démarre',
        processesStopRunning: 'Lorsqu’une application se ferme',
        userInactivity: 'Lorsque l’utilisateur devient inactif',
        userInactivityZero: 'Lorsque l’utilisateur redevient actif',
        sessionLock: 'Session verrouillée',
        sessionUnlock: 'Session déverrouillée',
        lidOpened: 'Capot ouvert',
        lidClosed: 'Capot fermé',
        displayOn: 'Lorsque les écrans s’allument',
        displayOff: 'Lorsque les écrans s’éteignent',
        hdrOn: 'Lorsque le HDR s’active',
        hdrOff: 'Lorsque le HDR se désactive',
        deviceConnected: 'Lorsqu’un appareil est connecté',
        deviceDisconnected: 'Lorsqu’un appareil est déconnecté',
        externalDisplayConnected: 'Lorsqu’un écran externe est connecté',
        externalDisplayDisconnected: 'Lorsqu’un écran externe est déconnecté',
        wiFiConnected: 'Lorsque le Wi-Fi est connecté',
        wiFiDisconnected: 'Lorsque le Wi-Fi est déconnecté',
        time: 'À une heure spécifiée',
        periodic: 'Action périodique',
        hardwareSensor: 'Capteur matériel',
        batteryPercentage: 'Pourcentage de batterie',
        onStartup: 'Au démarrage',
        onResume: 'À la reprise'
      },
      triggerEditors: {
        noProcesses: 'Aucun processus sélectionné.',
        noDevices: 'Aucun appareil sélectionné.',
        inactivityTimeout: 'Délai',
        seconds: '{{count}} secondes',
        minutes: '{{count}} minutes',
        hours: '{{count}} heures',
        ssidPlaceholder: 'Nom du réseau (SSID)',
        addSsid: 'Ajouter un nom de réseau',
        atTime: 'À l’heure',
        hour: 'Heure',
        minute: 'Minute',
        allDays: 'Tous les jours',
        day: {
          0: 'Dimanche',
          1: 'Lundi',
          2: 'Mardi',
          3: 'Mercredi',
          4: 'Jeudi',
          5: 'Vendredi',
          6: 'Samedi'
        },
        metric: 'Métrique',
        comparison: 'Comparaison',
        threshold: 'Seuil',
        thresholdPercent: 'Seuil (%)',
        durationSeconds: 'Durée (secondes)',
        cooldownSeconds: 'Recharge (secondes)',
        chargeFilter: 'Filtre de charge',
        deviceInstanceId: 'ID d’instance de l’appareil'
      }
    },
    macro: {
      title: 'Macro clavier',
      enable: 'Activer les macros',
      enableDesc: 'Universal Device Toolkit doit être en cours d’exécution pour que les macros fonctionnent.',
      subtitle: 'Enregistrez des séquences de touches et déclenchez-les avec le pavé numérique.',
      numpad: 'Pavé numérique',
      sequence: 'Séquence',
      repeat: 'Nombre de répétitions',
      events: 'Événements',
      save: 'Enregistrer',
      clear: 'Effacer',
      play: 'Jouer',
      record: 'Enregistrer',
      recordingOptions: 'Options d’enregistrement',
      ignoreDelays: 'Ignorer les délais',
      interruptOnOtherKey: 'Interrompre sur une autre touche',
      dontRepeat: 'Ne pas répéter',
      keyboardOnly: 'Clavier uniquement',
      keyboardMouse: 'Touches clavier et boutons souris',
      allInputs: 'Toutes les entrées',
      recordingInterrupted: 'Enregistrement interrompu',
      keyboard: 'Clavier',
      mouse: 'Souris',
      move: 'Déplacement de la souris',
      wheelUp: 'Molette vers le haut',
      wheelDown: 'Molette vers le bas',
      wheelLeft: 'Molette vers la gauche',
      wheelRight: 'Molette vers la droite',
      leftButton: 'Bouton gauche',
      rightButton: 'Bouton droit',
      middleButton: 'Bouton du milieu',
      xButton: 'Bouton X',
      button: 'Bouton de souris',
      empty: 'Aucune séquence de macro pour cette touche',
      recording: {
        preparing: 'L’enregistrement commencera dans 3 secondes…',
        title: 'Enregistrement…',
        pressEscToStop: 'Appuyez sur ÉCHAP pour arrêter.',
        focusHint: 'Gardez cette fenêtre au premier plan pendant l’enregistrement.'
      }
    },
    plugins: {
      title: 'Plugins et extensions',
      search: 'Rechercher des plugins',
      filterAll: 'Tous',
      filterInstalled: 'Installés',
      filterNotInstalled: 'Non installés',
      refresh: 'Actualiser',
      total: '{{count}} au total',
      summary: '{{count}} installés',
      updatable: '{{count}} mise(s) à jour disponible(s)',
      install: 'Installer',
      update: 'Mettre à jour',
      updateAvailable: 'Mise à jour disponible',
      uninstall: 'Désinstaller',
      uninstallConfirm: 'Désinstaller ce plugin ?',
      uninstallFailed: 'Échec de la désinstallation',
      installed: 'Installé',
      online: 'En ligne',
      installing: 'Installation…',
      downloading: 'Téléchargement…',
      preparingDownload: 'Préparation du téléchargement…',
      downloadCompleted: 'Téléchargement terminé',
      offline: 'La boutique en ligne est indisponible ; seuls les plugins locaux sont affichés',
      empty: 'Aucun plugin trouvé',
      dependencies: 'Dépendances',
      dependenciesBlocked: 'Ce plugin a des dépendances non satisfaites et ne peut pas être désinstallé',
      details: 'Détails',
      usageGuide: 'Guide d’utilisation',
      changelog: 'Journal des modifications',
      importProgress: 'Importation des paquets de plugins…',
      importSuccess: '{{count}} paquet(s) de plugins importé(s)',
      importFailed: 'Échec de l’importation de {{count}} paquet(s)',
      installAll: 'Tout installer',
      installAllComplete: '{{count}} plugin(s) installé(s)',
      installAllPartial: '{{count}} sur {{total}} opérations terminées',
      copyId: 'Copier l’ID du plugin',
      copied: 'ID du plugin copié dans le presse-papiers',
      copyFailed: 'Impossible de copier l’ID du plugin',
      local: 'Local',
      collapseDetails: 'Masquer les détails',
      showDetails: 'Afficher les détails',
      updateInfo: 'Informations de mise à jour',
      versionLabel: 'Version :',
      configure: 'Configurer',
      open: 'Ouvrir',
      description: 'Installez et gérez des plugins pour étendre les fonctionnalités',
      storeUnavailable: 'Boutique de plugins indisponible',
      summaryTotal: 'Total des plugins',
      summaryInstalled: 'Installés',
      summaryUpdates: 'Mises à jour disponibles',
      importFromFiles: 'Importer depuis des fichiers',
      updateAll: 'Tout mettre à jour',
      emptyStore: 'La boutique de plugins est actuellement vide. Restez à l’écoute pour de futures mises à jour.'
    },
    optimization: {
      title: 'Optimisation du système',
      info: 'Ces actions modifient les services et fichiers système et peuvent nécessiter des droits administrateur.',
      tabs: {
        optimization: 'Optimisation',
        cleanup: 'Nettoyage',
        driverDownload: 'Téléchargement de pilotes',
        networkAcceleration: 'Accélération réseau'
      },
      recommended: 'Recommandé',
      selected: 'Sélectionné',
      selectedActions: 'Actions sélectionnées',
      noSelection: 'Aucune action sélectionnée',
      selectRecommended: 'Sélectionner les recommandés',
      applyRecommended: 'Appliquer tous les recommandés',
      apply: 'Appliquer',
      clear: 'Effacer (annuler)',
      applied: 'Appliqué',
      applyFailed: 'Échec de l’application (droits administrateur requis)',
      reverted: 'Annulé',
      revertFailed: 'Échec de l’annulation (droits administrateur requis)',
      estimate: 'Estimer la taille',
      estimateResult: 'Espace récupérable',
      runCleanup: 'Exécuter le nettoyage',
      cleanupHint: 'Le nettoyage exécute vos règles de nettoyage personnalisées.',
      cleanupConfirm: 'Exécuter le nettoyage maintenant ?',
      cleanupDone: 'Nettoyage terminé',
      cleanupFailed: 'Échec du nettoyage',
      cleanup: {
        custom: {
          header: 'Règles de nettoyage personnalisées',
          description: 'Dossiers supplémentaires nettoyés avec les actions sélectionnées.',
          empty: 'Aucune règle de nettoyage personnalisée',
          add: 'Ajouter un dossier',
          edit: 'Modifier le dossier',
          remove: 'Supprimer',
          clear: 'Tout effacer',
          added: 'Règle ajoutée',
          updated: 'Règle mise à jour',
          recursive: 'Inclure les sous-dossiers',
          noExtensions: 'Aucune extension spécifiée',
          folderPickerFailed: 'Impossible d’ouvrir le sélecteur de dossier'
        }
      },
      network: {
        status: 'État',
        running: 'En cours',
        stopped: 'Arrêté',
        backendReady: 'Backend prêt',
        backendNotReady: 'Backend non prêt',
        config: 'Configuration de base',
        accelerationEnabled: 'Activer l’accélération',
        mode: 'Mode',
        modes: {
          off: 'Désactivé',
          systemProxy: 'Proxy système',
          hosts: 'Hosts',
          diagnosticsOnly: 'Diagnostics uniquement'
        },
        save: 'Enregistrer la configuration',
        saved: 'Configuration enregistrée',
        saveFailed: 'Échec de l’enregistrement',
        start: 'Démarrer',
        stop: 'Arrêter',
        startFailed: 'Échec du démarrage',
        stopFailed: 'Échec de l’arrêt',
        modeLabel: 'Mode',
        targetsLabel: 'Cibles',
        portLabel: 'Port',
        targetsHeading: 'Cibles d’accélération',
        domainGroupsHint: 'Sélectionnez les services à accélérer via le proxy local.',
        domainGroupsEmptyTitle: 'Aucune cible d’accélération',
        domainGroupsEmptyDescription: 'La liste des cibles est vide ou ne correspond pas à la recherche.',
        selectionHint: 'Les cibles sélectionnées sont appliquées au démarrage de l’accélération.',
        searchTargets: 'Rechercher des cibles',
        recommendedMenu: 'Recommandé',
        groupRuntime: '{{selected}}/{{total}} sélectionnés  {{active}} actifs',
        trafficHeading: 'Aperçu du trafic',
        metrics: {
          upload: 'Envoi',
          download: 'Réception',
          connections: 'Connexions',
          total: 'Trafic total',
          health: 'Santé'
        },
        trafficLive: 'Collecte du trafic proxy en direct',
        trafficWaiting: 'Démarrez l’accélération pour collecter le trafic en direct',
        trafficUnavailable: 'Données de trafic temporairement indisponibles',
        connectionsHeading: 'Connexions actuelles et récentes',
        destinationsHeading: 'Statistiques des destinations',
        connectionSummary: '{{active}} actives / {{total}} au total',
        destinationSummary: '{{count}} destinations',
        connectionStates: {
          active: 'Active',
          completed: 'Terminée',
          blocked: 'Bloquée',
          failed: 'Échouée',
          stopped: 'Arrêtée',
          unknown: 'Inconnue'
        },
        unknownHost: 'Hôte inconnu',
        destinationRow: '{{count}} conn  {{latency}}',
        health: {
          healthy: 'Sain',
          degraded: 'Dégradé',
          stopped: 'Arrêté',
          unknown: 'Inconnu'
        },
        modeFull: {
          systemProxy: 'Proxy système',
          hosts: 'Fichier hosts',
          diagnosticsOnly: 'Diagnostics uniquement',
          off: 'Inactif'
        },
        backendMissingHint: 'Le worker proxy est indisponible',
        selectGroupsFirstHint: 'Sélectionnez au moins une cible',
        advancedHeading: 'Avancé',
        advancedBody: 'Paramètres avancés et récupération réseau.',
        portFormat: 'Port : {{port}}',
        dangerZoneHeading: 'Zone dangereuse',
        restoreHint: 'Restaure l’état réseau système d’origine enregistré avant l’accélération.',
        restoreNetwork: 'Restaurer le réseau',
        restoreConfirm: 'Restaurer l’état réseau système maintenant ?',
        restored: 'État réseau restauré',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: 'Détecter',
          unknown: 'Inconnu',
          natTypes: {
            OpenInternet: 'NAT ouvert',
            Nat: 'NAT',
            UdpBlocked: 'UDP bloqué',
            Unknown: 'Inconnu'
          },
          internetConnected: 'Connecté',
          internetUnreachable: 'Injoignable',
          natType: 'Type de NAT',
          localIp: 'IP locale',
          publicIp: 'IP publique',
          internet: 'Internet',
          dnsDomain: 'Domaine',
          customDns: 'DNS personnalisé',
          enableDoh: 'DoH',
          dohUrl: 'URL DoH',
          latency: 'Latence',
          resolvedAddress: 'Adresse résolue',
          latencyFormat: '{{ms}} ms',
          failed: 'Échec',
          ipv6Support: 'Support IPv6',
          ipv6Address: 'Adresse IPv6',
          ipv6SupportedFull: 'Accès IPv6 pris en charge',
          notSupported: 'Non pris en charge'
        }
      },
      driverDownload: {
        comingSoon: 'Le téléchargement de pilotes sera disponible dans une future version'
      },
      driver: {
        machineType: 'Type de machine',
        machineTypePlaceholder: 'ex. 82K3',
        os: 'Système d’exploitation',
        downloadTo: 'Télécharger vers',
        downloadToPlaceholder: 'Choisissez un dossier pour les téléchargements',
        browse: 'Parcourir',
        openDownloadTo: 'Ouvrir le dossier',
        source: 'Source',
        primarySource: 'Vantage',
        primarySourceMessage: 'Base de données officielle via Vantage.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'Base de données de compatibilité PC Support.',
        scan: 'Analyser',
        scanning: 'Analyse…',
        scanValidation: 'Saisissez un type de machine à 4 caractères et choisissez un OS.',
        disclaimer: 'Les paquets proviennent de la source choisie. Installez à vos risques.',
        filter: 'Filtrer',
        onlyShowUpdates: 'Mises à jour uniquement',
        sort: {
          name: 'Trier par nom',
          category: 'Trier par catégorie',
          date: 'Trier par date'
        },
        selectRecommended: 'Sélectionner les recommandés',
        startAll: 'Tout démarrer',
        pauseAll: 'Tout mettre en pause',
        clearSelection: 'Effacer la sélection',
        packagesFound: '{{count}} paquets trouvés.',
        packagesFoundOne: '1 paquet trouvé.',
        status: {
          NotStarted: '',
          Queued: 'En file',
          Downloading: 'Téléchargement',
          Installing: 'Installation',
          Completed: 'Terminé',
          Error: 'Erreur'
        },
        recommended: 'Recommandé',
        isUpdate: 'Mise à jour',
        reboot: {
          recommended: 'Redémarrage recommandé',
          required: 'Redémarrage requis',
          shutdown: 'Arrêt requis'
        },
        oldPackageWarning: 'Ce paquet a plus d’un an ; le pilote peut être obsolète.',
        download: 'Télécharger',
        install: 'Installer',
        uninstall: 'Désinstaller',
        pause: 'Pause',
        openReadme: 'Ouvrir le README',
        hide: 'Masquer',
        hideAll: 'Tout masquer',
        showHiddenDownloads: 'Afficher les téléchargements masqués',
        downloadInProgress: {
          title: 'Téléchargements en cours',
          message: 'Des téléchargements sont encore actifs. Réanalyser ?',
          confirm: 'Analyser'
        },
        empty: {
          notScanned: {
            title: 'Rechercher des paquets de pilotes',
            message: 'Choisissez une source et analysez pour lister les pilotes compatibles.'
          },
          noResults: {
            title: 'Aucun téléchargement de pilote trouvé',
            message: 'Essayez une autre source, un autre OS ou un autre type de machine.'
          },
          noFilterResults: {
            title: 'Aucun téléchargement correspondant',
            message: 'Ajustez le filtre, l’option mises à jour ou la liste masquée.'
          },
          error: {
            title: 'L’analyse de pilotes n’a pas abouti',
            message: 'Vérifiez la source et la connexion, puis réanalysez.'
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
      title: 'À propos',
      appName: 'Application',
      version: 'Version',
      build: 'Build',
      links: 'Liens du projet',
      projectWebsite: 'Site du projet sur GitHub',
      latestRelease: 'Dernière version sur GitHub',
      applicationFolders: 'Dossiers de l’application',
      data: 'Données',
      temp: 'Temp',
      pid: 'ID du processus',
      machine: 'Modèle de l’appareil',
      bios: 'Version du BIOS',
      compatible: 'Compatibilité',
      yes: 'Compatible',
      no: 'Non compatible',
      dataFolder: 'Dossier de données',
      thirdParty: 'Bibliothèques tierces',
      copyright: 'Droits d’auteur'
    },
    statusBanner: {
      updateAvailable: 'Mise à jour disponible !',
      updateAvailableWithVersion: 'Mise à jour {{version}} disponible !',
      pluginExtensionsDisabled: 'La navigation Plugins et extensions est masquée. Activez-la dans Paramètres → Éléments de navigation.'
    },
    wpf: legacy.translation.wpf
  }
}



