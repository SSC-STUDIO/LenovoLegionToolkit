import legacy from './tr'

export default {
  translation: {
    app: {
      name: 'Universal Device Toolkit'
    },
    titlebar: {
      log: 'Günlük',
      openLogs: 'Günlük klasörünü aç',
      deviceName: 'Legion Y9000P IRX9',
      deviceInfo: 'Cihaz bilgileri'
    },
    nav: {
      dashboard: 'Panel',
      settings: 'Ayarlar',
      automation: 'Otomasyon',
      keyboard: 'Klavye',
      keyboardBacklight: 'Klavye arka aydınlatması',
      macro: 'Özel makro',
      windowsOptimization: 'Sistem optimizasyonu',
      pluginExtensions: 'Eklentiler ve uzantılar',
      about: 'Hakkında'
    },
    home: {
      title: 'Universal Device Toolkit',
      subtitle: 'Hoş geldiniz! Başlamak için aşağıdan bir bölüm seçin',
      hostReady: 'Arka uç bağlandı',
      hostState: 'Arka uç durumu',
      hostVersion: 'Arka uç sürümü',
      initComplete: 'Başlatma tamamlandı',
      safeStart: 'Güvenli başlatma, atlandı',
      machine: 'Cihaz',
      compatible: 'Uyumluluk',
      status: 'Durum'
    },
    dashboard: {
      title: 'Ana Sayfa',
      customize: 'Özelleştir',
      edit: {
        title: 'Paneli düzenle',
        description: 'Ana sayfada gösterilecek bölümleri ve özellikleri seçin.',
        showSensors: 'Donanım sensörleri',
        groups: 'Özellik grupları',
        save: 'Kaydet',
        cancel: 'İptal',
        saved: 'Panel düzeni kaydedildi',
        error: 'Panel düzeni kaydedilemedi',
        disclaimer: 'Cihazınızın durumuna ve yapılandırmasına bağlı olarak bazı özellikler görünmeyebilir.',
        addGroup: 'Ekle',
        renameGroup: 'Grup adını düzenle',
        deleteGroup: 'Sil',
        moveUp: 'Yukarı taşı',
        moveDown: 'Aşağı taşı',
        deleteItem: 'Sil',
        addItem: 'Ekle',
        groupNamePlaceholder: 'Ad',
        items: {
          discreteGpu: 'Ayrık GPU modu',
          overclockGpu: 'GPU hız aşırtma',
          turnOffMonitors: 'Monitörleri kapat'
        }
      },
      addItem: {
        title: 'Ekle',
        searchPlaceholder: 'Ara',
        empty: 'Tüm panel öğeleri zaten eklendi',
        addHint: 'Öğe ekle'
      },
      cpu: 'CPU',
      gpu: 'GPU',
      memory: 'Bellek',
      temperature: 'Sıcaklık',
      usage: 'Kullanım',
      power: 'Güç',
      fanSpeed: 'Fan',
      vram: 'VRAM',
      memoryUsed: 'Kullanılan bellek',
      memoryTotal: 'Toplam bellek',
      storageTemp: 'Depolama sıcaklığı',
      notAvailable: '--',
      sensor: {
        cpu: 'İşlemci',
        gpu: 'Ekran kartı',
        memory: 'Bellek',
        temperature: 'Sıcaklık',
        usage: 'Kullanım',
        power: 'Güç',
        fanSpeed: 'Fan',
        vram: 'VRAM',
        frequency: 'Çekirdek hızı',
        battery: 'Pil',
        charge: 'Şarj',
        health: 'Sağlık',
        rate: 'Hız',
        fan: 'Fan',
        lowPowerAdapter: 'Düşük güçlü adaptör takıldı',
        batteryLow: 'Pil zayıf',
        acCharging: 'Adaptör bağlı, şarj ediliyor…',
        acNotCharging: 'Adaptör bağlı, şarj edilmiyor…',
        remainingTime: 'Tahmini kalan süre: {0}',
        memoryTemperature: 'Bellek sıcaklığı',
        ssdTemperature: 'SSD sıcaklığı',
        vramTemperature: 'VRAM sıcaklığı',
        vramUsage: 'VRAM kullanımı',
        cycles: 'Döngü',
        capacity: 'Kapasite',
        fullCapacity: 'Tam şarj kapasitesi',
        designCapacity: 'Tasarım kapasitesi',
        date: 'Tarih',
        voltage: 'Çekirdek voltajı',
        voltageRange: 'Voltaj aralığı',
        powerRange: 'Güç aralığı',
        details: 'Ayrıntılar',
        refreshInterval: 'Yenileme aralığı',
        detail: {
          power: 'Güç',
          powerCores: 'Çekirdekler',
          powerMemory: 'Bellek',
          powerPlatform: 'Platform',
          pCoreClock: 'P çekirdek hızı',
          eCoreClock: 'E çekirdek hızı',
          memoryUsage: 'Bellek kullanımı',
          sharedMemoryUsage: 'Paylaşımlı bellek kullanımı',
          vramUsage: 'VRAM kullanımı',
          hotSpot: 'GPU sıcak noktası',
          pcieThroughput: 'PCIe verimi',
          designCapacity: 'Tasarım kapasitesi',
          fullChargeCapacity: 'Tam şarj kapasitesi'
        }
      },
      group: {
        power: 'Güç',
        graphics: 'Grafik',
        display: 'Görüntü',
        other: 'Diğer',
        custom: 'Özel'
      },
      card: {
        error: 'Ayar uygulanamadı',
        config: 'Gelişmiş ayarlar',
        configComingSoon: 'Gelişmiş ayarlar gelecekteki bir sürümde sunulacak'
      }
    },
    balanceMode: {
      title: 'Dengeli mod ayarları',
      aiEngine: 'Yapay zekâ motorunu etkinleştir',
      aiEngineDesc: 'Çalışan belirli oyunları otomatik algılar ve CPU/GPU performansını ayarlar. Sıcaklık ve fan sesi artabilir.'
    },
    godMode: {
      title: 'Özel mod ayarları',
      activePreset: 'Etkin profil',
      presetName: 'Profil adı',
      name: 'Ad',
      errorLoad: 'Ayar yüklenemedi.',
      errorApply: 'Ayarlar uygulanamadı',
      applySuccess: 'Özel mod ayarları uygulandı.',
      defaultPresetName: 'Profil',
      cpu: {
        title: 'CPU',
        longTermPL: 'Uzun vadeli güç sınırı',
        'longTermPL.desc': 'CPU’nun ulaşabileceği sürekli güç tüketimi.',
        shortTermPL: 'Kısa vadeli güç sınırı',
        'shortTermPL.desc': 'CPU’nun kısa sürede ulaşabileceği tepe güç tüketimi.',
        peakPL: 'Tepe güç sınırı',
        'peakPL.desc': 'CPU’nun anlık ulaşabileceği maksimum güç tüketimi.',
        crossLoading: 'Uzun vadeli güç sınırı (çapraz yük)',
        'crossLoading.desc': 'CPU ve GPU tam yükteyken CPU’nun maksimum güç tüketimi.',
        pl1Tau: 'Kısa vadeli güç sınırı süresi',
        'pl1Tau.desc': 'CPU’nun kısa vadeli güç sınırını kullanabildiği süre. Süre dolunca uzun vadeli sınır uygulanır.',
        apuSppt: 'APU sPPT güç sınırı',
        'apuSppt.desc': 'CPU’nun hafif bir gecikmeyle ulaşabileceği tepe güç tüketimi.',
        tempLimit: 'CPU sıcaklık sınırı',
        'tempLimit.desc': 'Frekans ve güç düşürülmeden önce CPU’nun maksimum sıcaklığı.'
      },
      gpu: {
        title: 'GPU',
        dynamicBoost: 'Dinamik artırma',
        'dynamicBoost.desc': 'CPU’nun güç tüketimine göre GPU’ya ayrılabilen ek güç.',
        ctgp: 'Yapılandırılabilir TGP',
        'ctgp.desc': 'Temel güç tüketiminin üzerinde GPU’ya ayrılabilen ek güç.',
        tempLimit: 'GPU sıcaklık sınırı',
        'tempLimit.desc': 'Frekans ve güç düşürülmeden önce GPU’nun maksimum sıcaklığı.',
        totalProcessingPowerTarget: 'AC’de toplam işlemci güç hedefi',
        'totalProcessingPowerTarget.desc': 'CPU’nun GPU için dinamik güç ayarını tetiklediği nokta.',
        toCpuDynamicBoost: 'GPU’dan CPU’ya dinamik artırma',
        'toCpuDynamicBoost.desc': 'CPU kullanımına göre GPU’dan CPU’ya ayrılabilen ek güç. Değer yükseldikçe CPU performansı artar.'
      },
      fans: {
        title: 'Fanlar',
        curve: 'Fan eğrisi',
        curveMessage: 'Fan hızı CPU, GPU veya soğutucu sensörlerinden en yüksek olanı takip eder. Tam değerler için her adımın üzerine gelin.',
        maxSpeed: 'Maksimum fan hızı',
        maxSpeedWarning: 'Bu seçeneğin uzun süreli kullanımı fanların ömrünü kısaltır.\nBu seçeneğe gerçekten dikkat edin!'
      },
      advanced: {
        title: 'Gelişmiş',
        message: 'Aşağıdaki seçenekleri ne yaptığınızdan emin değilseniz değiştirmeyin.',
        maxOffset: 'Maksimum kayma',
        maxOffsetWarning: 'Daha yüksek değerler öngörülemeyen davranışlara yol açabilir. Emin değilseniz 0 bırakın.',
        minOffset: 'Minimum kayma',
        minOffsetWarning: 'Daha düşük değerler öngörülemeyen davranışlara yol açabilir. Emin değilseniz 0 bırakın.',
        invalidOffset: 'Kaydetmeden önce tam sayı girin.'
      },
      vantageWarning: 'Lenovo Vantage veya hizmetleri çalışırken özel mod ayarları doğru uygulanmayacaktır.',
      legionZoneWarning: 'Legion Zone veya hizmetleri çalışırken özel mod ayarları doğru uygulanmayacaktır.'
    },
    overclock: {
      title: 'GPU hız aşırtma ayarları',
      preset: 'Profil',
      coreOffset: 'Çekirdek frekans kayması',
      memoryOffset: 'Bellek frekans kayması',
      namePlaceholder: 'Ad…',
      newProfileName: 'Profil',
      loadError: 'Hız aşırtma ayarları yüklenemedi.'
    },
    feature: {
      powerMode: 'Güç modu',
      'powerMode.desc': 'Performans modunu değiştirin.\nFn+Q ile de değiştirilebilir.',
      'powerMode.hint': 'Fn+Q kısayoluyla hızlıca değiştirebilirsiniz.',
      'powerMode.warning': 'Güç adaptörü bağlı değilken performans modu düzgün çalışmayabilir.',
      battery: 'Pil şarj modu',
      'battery.desc': 'Bir pil şarj modu seçin. Koruma modu pil ömrünü uzatmak için şarjı sınırlar; hızlı şarj modu daha yüksek güçle şarj eder.',
      batteryNightCharge: 'Gece pil şarjı',
      'batteryNightCharge.desc': 'Etkinleştirildiğinde gece %80’e kadar şarj eder ve sabaha kadar %100’e tamamlar.',
      alwaysOnUsb: 'Her zaman açık USB',
      'alwaysOnUsb.desc': 'Bilgisayar kapalıyken, uyurken veya hazırda beklerken USB bağlantı noktalarına güç verir.',
      instantBoot: 'Anında başlatma',
      'instantBoot.desc': 'Güç bağlanır bağlanmaz bilgisayarı açar.',
      flipToStart: 'Açınca başlat',
      'flipToStart.desc': 'Kapağın açılması dizüstü bilgisayarı otomatik açar.',
      fnLock: 'Fn kilidi',
      'fnLock.desc': 'Etkinleştirildiğinde işlevler Fn’e basmadan tetiklenir. Orijinal F1-F12 tuşları için Fn ile birlikte basın.',
      gSync: 'G-Sync',
      'gSync.desc': 'G-Sync değişken yenileme hızını etkinleştirin veya devre dışı bırakın',
      hdr: 'HDR',
      'hdr.desc': 'Yerleşik ekranda HDR’yi etkinleştirir.',
      'hdr.warning': 'HDR kullanımı Windows ayarları tarafından engellendi.',
      hybridMode: 'Hibrit mod',
      'hybridMode.desc': 'Hibrit mod, tümleşik ve ayrık GPU arasında geçiş yapmanızı sağlar. Kapatmak, doğrudan ayrık GPU modunu etkinleştirir; yeniden başlatma gerekir.',
      igpuMode: 'Ayrık GPU modu',
      'igpuMode.desc': 'Güç tasarrufu için tümleşik grafik çıkışını zorla',
      refreshRate: 'Yenileme hızı',
      'refreshRate.desc': 'Yerleşik ekranın yenileme hızını değiştirir.',
      itsMode: 'ITS modu',
      'itsMode.desc': 'Akıllı termal çözüm',
      microphone: 'Mikrofon',
      'microphone.desc': 'Kapatıldığında tüm mevcut mikrofonlar sessize alınır.',
      overDrive: 'Over Drive',
      'overDrive.desc': 'Yerleşik ekranın tepki süresini iyileştirir. Hayalet görüntüye neden olabilir.',
      panelLogo: 'Legion logo ışığı',
      'panelLogo.desc': 'Cihazın arkasındaki Legion logosunu açar veya kapatır.',
      portsBacklight: 'Bağlantı noktası ışıkları',
      'portsBacklight.desc': 'Cihazın arkasındaki bağlantı noktası ışıklarını açar veya kapatır.',
      resolution: 'Çözünürlük',
      'resolution.desc': 'Yerleşik ekranın çözünürlüğünü değiştirir.',
      dpiScale: 'DPI ölçeği',
      'dpiScale.desc': 'Yerleşik ekranın ölçeklendirmesini değiştirir.',
      speaker: 'Hoparlör',
      touchpadLock: 'Dokunmatik yüzey kilidi',
      'touchpadLock.desc': 'Dokunmatik yüzeyi devre dışı bırakır. Fare kullanırken önerilir.',
      whiteKeyboard: 'Klavye arka aydınlatması',
      'whiteKeyboard.desc': 'Fn + Boşluk kısayoluyla arka aydınlatmayı açıp parlaklığını ayarlayın.',
      winKey: 'Win tuşunu devre dışı bırak',
      'winKey.desc': 'Yalnızca yerleşik klavyede geçerlidir. Win tuşu yanıt vermez.',
      oneLevelWhiteKeyboard: 'Klavye arka aydınlatması',
      'oneLevelWhiteKeyboard.desc': 'Arka aydınlatmayı açmak için Fn + Boşluk kısayolunu kullanın.',
      'hybridMode.states.hybrid': 'Hibrit',
      'hybridMode.states.hybridIGPUOnly': 'Hibrit-iGPU',
      'hybridMode.states.hybridAuto': 'Hibrit-otomatik',
      'hybridMode.states.off': 'dGPU',
      'hybridMode.info.title': 'GPU çalışma modları hakkında',
      'hybridMode.info.hybrid.title': 'Hibrit mod',
      'hybridMode.info.hybrid.message': 'Tümleşik ve ayrık GPU etkindir. Sistem ihtiyaca göre otomatik geçiş yapar.',
      'hybridMode.info.hybridIgpu.title': 'Yalnızca hibrit-iGPU modu',
      'hybridMode.info.hybridIgpu.message': 'Yalnızca tümleşik GPU kullanılır. Güç tüketimini ve gürültüyü en aza indirir.',
      'hybridMode.info.hybridIgpu.disclaimer': 'Bu mod yalnızca ayrık GPU çalışmıyorken etkilidir.',
      'hybridMode.info.hybridAuto.title': 'Hibrit-otomatik mod',
      'hybridMode.info.hybridAuto.message': 'Pilde yalnızca tümleşik GPU, AC adaptör bağlıyken ikisi birden kullanılır. Standart olmayan adaptörde yalnızca iGPU moduna geçer.',
      'hybridMode.info.dgpu.title': 'dGPU modu',
      'hybridMode.info.dgpu.message': 'Yalnızca ayrık GPU kullanılır. En iyi grafik performansını sağlar ancak güç tüketimini artırır.',
      'hybridMode.info.dgpu.disclaimer': 'Bu moda geçmek ve bu moddan çıkmak yeniden başlatma gerektirir.',
      'hybridMode.restartRequired.title': 'Yeniden başlatma gerekli',
      'hybridMode.restartRequired.message': '{{mode}} moduna geçmek yeniden başlatma gerektirir. Şimdi yeniden başlatılsın mı?',
      'hybridMode.restartRequired.now': 'Şimdi yeniden başlat',
      'hybridMode.restartRequired.later': 'Daha sonra yeniden başlatacağım',
      'hybridMode.restartFailed': 'Otomatik yeniden başlatılamadı. Değişikliği tamamlamak için elle yeniden başlatın.',
      'hybridMode.changeFailed.title': 'GPU çalışma modu değiştirilemedi',
      'hybridMode.changeFailed.message': 'Birkaç saniye sonra tekrar deneyin. dGPU hiç yanıt vermiyorsa dizüstü bilgisayarı yeniden başlatın.',
      batteryModes: {
        conservation: 'Koruma modu',
        normal: 'Normal mod',
        rapidCharge: 'Hızlı şarj modu'
      },
      powerModeOptions: {
        quiet: 'Sessiz',
        balance: 'Dengeli',
        performance: 'Performans',
        extreme: 'Aşırı',
        godMode: 'Özel'
      }
    },
    common: {
      loading: 'Yükleniyor…',
      error: 'Bir şeyler ters gitti',
      retry: 'Yeniden dene',
      close: 'Kapat',
      cancel: 'İptal',
      moreActions: 'Diğer işlemler',
      copied: 'Panoya kopyalandı',
      add: 'Ekle',
      save: 'Kaydet',
      saveAndClose: 'Kaydet ve kapat',
      apply: 'Uygula',
      applyAndClose: 'Uygula ve kapat',
      default: 'Varsayılan',
      rename: 'Yeniden adlandır',
      delete: 'Sil',
      ok: 'Tamam'
    },
    colorPicker: {
      hex: 'Hex',
      red: 'Kırmızı',
      green: 'Yeşil',
      blue: 'Mavi',
      ok: 'Tamam'
    },
    fanCurve: {
      fanSpeed: 'Fan hızı',
      fanSpeedMax: '%100',
      cpu: 'CPU',
      cpuSensor: 'CPU sensörü',
      gpu: 'GPU',
      gpu2: 'GPU #2',
      rpm: 'RPM'
    },
    pages: {
      placeholder: 'Yakında'
    },
    settings: {
      title: 'Ayarlar',
      description: 'Uygulamanın görünümünü, davranışını ve işlevlerini yapılandırın.',
      nav: {
        appearance: 'Görünüm',
        application: 'Uygulama',
        power: 'Güç',
        display: 'Ekran',
        smartKeys: 'Akıllı tuşlar',
        update: 'Güncelleme',
        integrations: 'Entegrasyonlar',
        osd: 'OSD'
      },
      appearance: {
        language: 'Dil',
        languageDesc: 'Dili seçin',
        temperature: 'Sıcaklık',
        temperatureDesc: 'Sıcaklık sensörlerinin kullandığı birimi seçin.',
        theme: 'Tema',
        accentColor: 'Vurgu rengi',
        accentColorDesc: 'Uygulamanın vurgu rengini değiştirin.',
        appScale: 'Arayüz ölçeği',
        appScaleDesc: 'Metni ve tüm arayüzü, Windows ekran ölçeğinden bağımsız olarak ölçeklendirir.',
        themeOptions: {
          system: 'Sistem',
          light: 'Açık',
          dark: 'Koyu'
        }
      },
      application: {
        minimizeToTray: 'Sistem tepsisine küçült',
        minimizeToTrayDesc: 'Görev çubuğu yerine her zaman sistem tepsisine küçült.',
        minimizeOnClose: 'Kapatınca küçült',
        minimizeOnCloseDesc: 'Her zaman tepsiye küçült. Çıkmak için tepsi simgesine sağ tıklayıp Kapat seçin.',
        disableUnsupportedWarning: 'Uyumsuz cihazlar için uyarma',
        disableUnsupportedWarningDesc: 'Başlangıçta gösterilen uyumsuz cihaz uyarısını gizler.',
        enableHardwareSensors: 'Donanım sensörleri',
        enableHardwareSensorsDesc: 'Sıcaklık, frekans ve güç sınırlarını izlemek için gelişmiş donanım yoklamasını etkinleştirir.',
        dontShowNotifications: 'Bildirim gösterme',
        dontShowNotificationsDesc: 'Uygulama içi ve sistem bildirimlerini devre dışı bırakır',
        autorun: 'Oturum açınca başlat',
        autorunDesc: 'Windows oturum açıldıktan sonra sistem tepsisinde küçültülmüş başlat.',
        extensionsEnabled: 'Uzantıları etkinleştir',
        extensionsEnabledDesc: 'Eklenti ve uzantı yüklemeyi etkinleştirir',
        sensorSections: 'Sensör bölümleri',
        sensorSectionsDesc: 'Hangi sensör bölümlerinin hangi sırayla gösterileceğini seçin.',
        disableVantage: 'Lenovo Vantage’ı devre dışı bırak',
        disableVantageDesc: 'Lenovo Vantage ve ImController’ı kaldırmadan devre dışı bırakır.\nBu seçeneği değiştirdikten sonra yeniden başlatma önerilir.',
        disableLegionZone: 'Legion Zone’u devre dışı bırak',
        disableLegionZoneDesc: 'Legion Zone ve hizmetini kaldırmadan devre dışı bırakır.\nBu seçeneği değiştirdikten sonra yeniden başlatma önerilir.',
        disableLenovoHotkeys: 'Lenovo Hotkeys’i devre dışı bırak',
        disableLenovoHotkeysDesc: 'Lenovo Hotkeys ve hizmetini kaldırmadan devre dışı bırakır.\nDevre dışı bırakılırsa Fn kısayollarını bu uygulama yönetir.\nBu seçeneği değiştirdikten sonra yeniden başlatma önerilir.',
        valueOn: 'Açık',
        valueOff: 'Kapalı'
      },
      saved: 'Ayarlar kaydedildi',
      saveFailed: 'Ayarlar kaydedilemedi',
      osd: {
        title: 'OSD',
        showOsd: 'OSD göster',
        showOsdDesc: 'Ekran üstü görüntüyü hemen gösterir.',
        style: 'Katman stili',
        styles: {
          panel: 'Panel',
          bar: 'Çubuk'
        },
        refreshInterval: 'Yenileme aralığı',
        snapThreshold: 'Yapışma eşiği',
        lockPosition: 'Konumu kilitle',
        resetPosition: 'Konumu sıfırla',
        previewHint: 'Önizleme',
        tabs: {
          general: 'Genel',
          appearance: 'Görünüm',
          thresholds: 'Eşikler',
          sensors: 'Sensörler'
        },
        opacity: 'Opaklık',
        cornerRadius: 'Köşe yarıçapı',
        cornerRadiusTop: 'Üst',
        cornerRadiusBottom: 'Alt',
        fontSize: 'Yazı boyutu',
        background: 'Arka plan rengi',
        category: 'Kategori rengi',
        label: 'Etiket rengi',
        value: 'Değer rengi',
        warning: 'Uyarı rengi',
        critical: 'Kritik renk',
        separator: 'Ayırıcı rengi',
        thresholds: {
          performance: 'Performans',
          fpsRedline: 'FPS kırmızı çizgisi',
          lowFpsDelta: 'Düşük FPS farkı',
          temperature: 'Sıcaklık',
          usage: 'Kullanım',
          warning: 'Uyarı',
          critical: 'Kritik'
        },
        items: {
          groups: {
            game: 'Oyun',
            cpu: 'CPU',
            gpu: 'GPU',
            pch: 'PCH'
          },
          names: {
            Fps: 'FPS',
            LowFps: '%1 Low',
            FrameTime: 'Kare süresi',
            CpuFrequency: 'Çekirdek hızı',
            CpuPCoreFrequency: 'P çekirdek hızı',
            CpuECoreFrequency: 'E çekirdek hızı',
            CpuUtilization: 'Kullanım',
            CpuTemperature: 'Sıcaklık',
            CpuPower: 'Güç',
            CpuFan: 'Fan',
            GpuFrequency: 'Çekirdek hızı',
            GpuUtilization: 'Kullanım',
            GpuTemperature: 'Çekirdek sıcaklığı',
            GpuVramUtilization: 'VRAM kullanımı',
            GpuVramTemperature: 'VRAM sıcaklığı',
            GpuPower: 'Güç',
            GpuFan: 'Fan',
            MemoryUtilization: 'Kullanım',
            MemoryTemperature: 'Sıcaklık',
            Disk1Temperature: 'Disk 1 sıcaklığı',
            Disk2Temperature: 'Disk 2 sıcaklığı',
            PchTemperature: 'PCH sıcaklığı',
            PchFan: 'Fan'
          }
        }
      },
      power: {
        powerModeMapping: 'Güç modu eşlemesi',
        powerModeMappingDesc: 'Performans modu değişirken Windows güç planını veya güç modunu senkronize olarak değiştirir.',
        mappingModes: {
          disabled: 'Devre dışı',
          windowsPowerMode: 'Windows güç modu',
          windowsPowerPlan: 'Windows güç planı'
        },
        windowsPowerModes: 'Windows güç modu',
        windowsPowerModesDesc: 'Güç modu değiştiğinde uygulanacak Windows güç modunu seçin.',
        windowsPowerPlans: 'Windows güç planı',
        windowsPowerPlansDesc: 'Güç modu değiştiğinde uygulanacak Windows güç planını seçin.',
        synchronizeBrightness: 'Ekran parlaklığını kilitle',
        synchronizeBrightnessDesc: 'Etkinleştirildiğinde güç planları arasında geçişte parlaklık aynı kalır.',
        smartFnLock: 'Akıllı Fn Kilidi değiştirici tuşları',
        modifierKeys: {
          shift: 'Shift',
          ctrl: 'Ctrl',
          alt: 'Alt'
        },
        resetBatteryOnSince: 'Başlangıçta «Pil süresi» değerini sıfırla',
        resetBatteryOnSinceDesc: 'Sistem yeniden başlatıldığında pil bölümündeki «Pil süresi» sayacını sıfırlar.',
        godModeFnQ: 'Fn+Q ile özel moda geç',
        godModeFnQDesc: 'Fn+Q ile özel moda hızlı geçişi etkinleştirir.'
      },
      display: {
        navigationItems: 'Gezinme öğelerinin görünürlüğü',
        navigationKeys: {
          keyboard: 'Klavye arka aydınlatması',
          battery: 'Pil',
          automation: 'Otomasyon',
          macro: 'Makro',
          windowsOptimization: 'Windows optimizasyonu',
          pluginExtensions: 'Eklentiler ve uzantılar',
          about: 'Hakkında'
        },
        notificationPosition: 'Bildirim konumu',
        notificationPositions: {
          bottomRight: 'Sağ alt',
          bottomCenter: 'Alt orta',
          bottomLeft: 'Sol alt',
          centerLeft: 'Orta sol',
          topLeft: 'Sol üst',
          topCenter: 'Üst orta',
          topRight: 'Sağ üst',
          centerRight: 'Orta sağ',
          center: 'Orta'
        },
        notificationDuration: 'Bildirim süresi',
        notificationDurations: {
          short: 'Kısa (3 sn)',
          normal: 'Normal (5 sn)',
          long: 'Uzun (10 sn)'
        },
        excludedRefreshRates: 'Hariç tutulan yenileme hızları',
        excludedRefreshRatesDesc: 'Fn+R geçişini hızlandırmak için yenileme hızlarını hariç tutun.',
        excludedRefreshRatesHint: 'Gelişmiş düzenleme gelecekteki bir sürümde sunulacak',
        excludedRefreshRatesEmpty: 'Hariç tutulan yenileme hızı yok',
        excludedRefreshRatesManageHint: 'Hariç tutulan yenileme hızlarını yönetmek için tıklayın',
        notifications: 'Bildirimler',
        notificationsDesc: 'Hangi bildirimlerin gösterileceğini seçin.',
        bootLogo: 'Açılış logosu',
        bootLogoDesc: 'Bilgisayar açılırken gösterilen logoyu özelleştirin.'
      },
      smartKeys: {
        smartFnLock: 'Akıllı Fn Kilidi',
        smartFnLockDesc: 'Alt, Ctrl veya Shift basılıyken Fn geçici olarak kilitlenmez.',
        off: 'Kapalı',
        hint: 'Akıllı Fn Kilidi değiştirici tuşları güç ayarlarında değiştirilebilir.',
        singlePressActionDesc: 'Fn+F9’un tek basışına bir Hızlı İşlem atayın.',
        doublePressActionDesc: 'Fn+F9’un çift basışına bir Hızlı İşlem atayın.'
      },
      update: {
        frequency: 'Güncellemeleri otomatik kontrol et',
        frequencies: {
          perHour: 'Her saat',
          perThreeHours: 'Her 3 saat',
          perTwelveHours: 'Her 12 saat',
          perDay: 'Her gün',
          perWeek: 'Her hafta',
          perMonth: 'Her ay'
        },
        includePrerelease: 'Ön sürümleri dahil et',
        includePrereleaseDesc: 'Kapalıyken yalnızca kararlı sürümler; açıkken ön sürüm (beta) güncellemeleri de alınır.',
        repository: 'Güncelleme deposu',
        repositoryDesc: 'Güncellemeleri kontrol etmek için GitHub deposunu yapılandırın. Varsayılan için boş bırakın.',
        repositoryOwner: 'Depo sahibi',
        repositoryOwnerPlaceholder: 'örn. SSC-STUDIO',
        repositoryName: 'Depo adı',
        repositoryNamePlaceholder: 'örn. UniversalDeviceToolkit',
        check: 'Güncellemeleri kontrol et',
        comingSoon: 'Güncelleme kontrolü gelecekteki bir sürümde sunulacak'
      },
      checkResult: {
        available: 'Yeni sürüm mevcut: v{{version}}',
        latest: 'Güncelsiniz'
      },
      integrations: {
        hwinfo: 'HWiNFO64',
        hwinfoDesc: 'Fan hızları, pil sıcaklığı ve diğer verileri HWiNFO64 ile paylaşır. Değişiklikten sonra HWiNFO64 yeniden başlatılmalıdır.',
        cli: 'Komut satırı arayüzü',
        cliDesc: 'Komut satırından kontrol için komut satırı arayüzünü etkinleştirir.'
      }
    },
    keyboard: {
      title: 'Klavye arka aydınlatması',
      unsupported: 'Bu cihazda klavye arka aydınlatması desteklenmiyor',
      rgb: {
        preset: 'Profil',
        settings: 'Arka aydınlatma ayarları',
        effect: 'Efekt',
        speed: 'Hız',
        brightness: 'Parlaklık',
        zones: 'Bölge renkleri',
        synchroniseZones: 'Bölgeleri senkronize et',
        presets: {
          off: 'Kapalı',
          one: 'Profil 1',
          two: 'Profil 2',
          three: 'Profil 3',
          four: 'Profil 4'
        },
        effectOptions: {
          static: 'Statik',
          breath: 'Nefes',
          smooth: 'Akıcı',
          waveRtl: 'Dalga (sağ→sol)',
          waveLtr: 'Dalga (sol→sağ)'
        },
        speedOptions: {
          slowest: 'En yavaş',
          slow: 'Yavaş',
          fast: 'Hızlı',
          fastest: 'En hızlı'
        },
        brightnessOptions: {
          low: 'Düşük',
          high: 'Yüksek'
        }
      },
      spectrum: {
        brightness: 'Parlaklık',
        profile: 'Profil',
        logo: 'Logo ışığı',
        effects: 'Efektler',
        colors: 'Renkler',
        addEffect: 'Efekt ekle',
        deleteEffect: 'Sil',
        noEffects: 'Efekt yok',
        selectAll: 'Tüm bölgeleri seç',
        deselectAll: 'Tüm bölgelerin seçimini kaldır',
        switchLayout: 'Klavye düzenini değiştir',
        editEffect: 'Düzenle',
        allKeys: 'Tüm tuşlar',
        zonesCount: '{{count}} bölge',
        noLayoutHint: 'Klavye düzeni yüklenemedi.',
        selectEffectHint: 'Tuşlarını önizlemek ve düzenlemek için aşağıdan bir efekt seçin.',
        effectEdit: {
          addTitle: 'Efekt ekle',
          editTitle: 'Efekt düzenle',
          effect: 'Efekt',
          speed: 'Hız',
          direction: 'Yön',
          clockwiseDirection: 'Yön',
          color: 'Renk',
          colors: 'Renkler',
          addColor: 'Renk ekle',
          keys: 'Tuşlar',
          alwaysWarning: 'Bu efekt tüm klavyeye uygulanacak ve diğer tüm efektleri değiştirecek.'
        },
        effectTypes: {
          always: 'Sürekli',
          rainbowScrew: 'Gökkuşağı vidası',
          rainbowWave: 'Gökkuşağı dalgası',
          colorChange: 'Renk değişimi',
          colorWave: 'Renk dalgası',
          colorPulse: 'Renk atımı',
          smooth: 'Akıcı',
          rain: 'Yağmur',
          ripple: 'Halka',
          type: 'Yazma',
          audioBounce: 'Ses zıplaması',
          audioRipple: 'Ses halkası',
          auroraSync: 'Aurora senkronizasyonu'
        }
      }
    },
    automation: {
      title: 'Otomasyon',
      enable: 'Otomasyonu etkinleştir',
      enableDesc: 'Otomatik işlemlerin çalışması için Universal Device Toolkit çalışıyor olmalıdır.',
      subtitle: 'Etkinleştirildiğinde cihaz durumu değiştiğinde bu uygulama eşleşen işlemleri sırayla kontrol edip çalıştırır.',
      actionsTitle: 'İşlemler',
      actionsEmpty: 'Henüz otomatik işlem yok',
      quickActionsTitle: 'Hızlı işlemler',
      quickActionsEmpty: 'Henüz hızlı işlem yok. Oluşturmak için «Yeni»ye tıklayın.',
      renamePipeline: 'İşlem hattını yeniden adlandır',
      renamePipelineTitle: 'İşlem hattını yeniden adlandır',
      renamePipelinePlaceholder: 'İşlem hattı adını girin',
      changeIcon: 'Simgeyi değiştir',
      empty: 'Henüz otomasyon betiği yok. Oluşturmak için «Yeni»ye tıklayın.',
      runNow: 'Şimdi çalıştır',
      delete: 'Sil',
      deleteStep: 'Adımı sil',
      addPipeline: 'Yeni',
      addStep: 'Adım ekle',
      configure: 'Yapılandır',
      stepType: 'Adım türü',
      steps: 'Adımlar',
      save: 'Kaydet',
      revert: 'Geri al',
      pipelineName: 'İşlem hattı adı',
      pipelineNamePlaceholder: 'İşlem hattı adını girin',
      quickAction: 'Hızlı işlem',
      optionsLoading: 'Seçenekler yükleniyor…',
      stepLabels: {
        rgbKeyboardBacklight: 'Klavye arka aydınlatması',
        run: 'Çalıştır',
        showMainWindow: 'Ana pencereyi göster',
        speaker: 'Hoparlör',
        spectrumKeyboardBacklightBrightness: 'Klavye arka aydınlatma parlaklığı',
        spectrumKeyboardBacklightImportProfile: 'Klavye arka aydınlatma profilini içe aktar',
        spectrumKeyboardBacklightProfile: 'Klavye arka aydınlatma profili',
        touchpadLock: 'Dokunmatik yüzey kilidi',
        turnOffMonitors: 'Ekranları kapat',
        turnOffWiFi: 'Wi-Fi’yi kapat',
        turnOnWiFi: 'Wi-Fi’yi aç',
        whiteKeyboardBacklight: 'Klavye arka aydınlatması',
        winKey: 'Windows tuşu kilidi',
        scriptPath: 'Yürütülebilir dosya yolu',
        scriptArguments: 'Bağımsız değişkenler',
        runSilently: 'Sessizce çalıştır',
        runSilentlyDesc: 'Konsol penceresi oluşturmadan konsol uygulamalarını çalıştırır.',
        runWaitUntilFinished: 'Bitmesini bekle',
        runWaitUntilFinishedDesc: 'Program veya betiğin bitmesini bekler',
        runHint: 'Bir betik veya program çalıştırır.\nÖnce betiğinizin doğru çalıştığından emin olun.',
        importProfilePath: 'Yol',
        browse: 'Gözat',
        off: 'Kapalı',
        on: 'Açık',
        mute: 'Sessiz',
        unmute: 'Sesi aç',
        low: 'Düşük',
        high: 'Yüksek',
        presetOne: 'Profil 1',
        presetTwo: 'Profil 2',
        presetThree: 'Profil 3',
        presetFour: 'Profil 4',
        values: {
          off: 'Kapalı',
          on: 'Açık',
          mute: 'Sessiz',
          unmute: 'Sesi aç',
          low: 'Düşük',
          high: 'Yüksek',
          presetOne: 'Profil 1',
          presetTwo: 'Profil 2',
          presetThree: 'Profil 3',
          presetFour: 'Profil 4'
        }
      },
      state: {
        on: 'Açık',
        off: 'Kapalı',
        hidden: 'Gizle',
        show: 'Göster',
        toggle: 'Durumu değiştir',
        quiet: 'Sessiz',
        balance: 'Dengeli',
        performance: 'Performans',
        extreme: 'Aşırı',
        godMode: 'Özel',
        hybrid: 'Hibrit',
        hybridIgpu: 'Hibrit-iGPU',
        hybridAuto: 'Hibrit-otomatik',
        dgpu: 'dGPU',
        acAdapter: 'AC adaptör',
        usbPd: 'USB Power Delivery',
        acAndUsbPd: 'AC ve USB PD',
        hz: '{{frequency}} Hz',
        resolution: '{{width}} × {{height}}'
      },
      stepEditors: {
        hybridMode: {
          title: 'GPU çalışma modu',
          desc: 'Bilgisayarınızın kullanımına ve güç koşullarına göre GPU çalışma modunu seçin.\nMod değişimi yeniden başlatma gerektirebilir.'
        },
        instantBoot: {
          title: 'Anında başlatma',
          desc: 'Şarj aleti bağlandığında dizüstü bilgisayarı açar.'
        },
        macro: {
          title: 'Makro',
          desc: 'Makroları etkinleştirir veya devre dışı bırakır.'
        },
        microphone: {
          title: 'Mikrofon',
          desc: 'Kapalıyken mikrofonlar sessize alınır.'
        },
        notification: {
          title: 'Bildirim göster',
          desc: 'Girilen metinle bir bildirim gösterir.',
          placeholder: 'Bildirim metni'
        },
        oneLevelWhiteKeyboardBacklight: {
          title: 'Klavye arka aydınlatması',
          desc: 'Arka aydınlatmayı açar veya kapatır.'
        },
        osd: {
          title: 'OSD',
          desc: 'OSD’yi gösterir veya gizler'
        },
        overclockDiscreteGPU: {
          title: 'GPU hız aşırtma',
          desc: 'Ayrık GPU’yu hız aşırtarak performansı artırır.\n\nUYARI: Ayrık GPU mevcut değilse bu işlem doğru çalışmaz.'
        },
        overDrive: {
          title: 'Over Drive',
          desc: 'Yerleşik ekranın tepki süresini iyileştirir.'
        },
        panelLogoBacklight: {
          title: 'Panel logo arka aydınlatması',
          desc: 'Dizüstü bilgisayarın kapağındaki arka aydınlatmayı açar veya kapatır.'
        },
        playSound: {
          title: 'Ses çal',
          desc: 'wav veya mp3 gibi yaygın ses biçimleri desteklenir.',
          browse: 'Gözat…',
          none: 'Dosya seçilmedi'
        },
        portsBacklight: {
          title: 'Bağlantı noktası ışıkları',
          desc: 'Dizüstü bilgisayarın arkasındaki bağlantı noktası ışıklarını açar veya kapatır.'
        },
        powerMode: {
          title: 'Güç modu',
          desc: 'Performans modunu değiştirir.'
        },
        quickAction: {
          title: 'Hızlı işlem',
          desc: 'Kayıtlı bir hızlı işlemi çalıştırır.',
          placeholder: 'Bir hızlı işlem seçin',
          empty: 'Henüz hızlı işlem yok. Önce tetikleyicisiz bir işlem hattı oluşturun.'
        },
        refreshRate: {
          title: 'Yenileme hızı',
          desc: 'Yerleşik ekranın yenileme hızını değiştirir.\n\nUYARI: Yerleşik ekran kapalıysa bu işlem doğru çalışmaz.',
          empty: 'Mevcut yenileme hızı yok'
        },
        resolution: {
          title: 'Çözünürlük',
          desc: 'Yerleşik ekranın çözünürlüğünü değiştirir.\n\nUYARI: Yerleşik ekran kapalıysa bu işlem doğru çalışmaz.',
          empty: 'Mevcut çözünürlük yok'
        },
        alwaysOnUsb: {
          title: 'Her zaman açık USB',
          desc: 'Dizüstü bilgisayar kapalıyken, uyurken veya hazırda beklerken USB cihazlarını şarj eder.',
          options: {
            OnWhenSleeping: 'Uykuda açık',
            OnAlways: 'Her zaman açık'
          }
        },
        battery: {
          title: 'Pil modu',
          desc: 'Pilin nasıl şarj edileceğini seçin.',
          options: {
            Conservation: 'Koruma',
            Normal: 'Normal',
            RapidCharge: 'Hızlı şarj'
          }
        },
        batteryNightCharge: {
          title: 'Gece pil şarjı',
          desc: 'Etkinleştirildiğinde cihaz gece %80’e kadar şarj olur ve sabah kullanımınıza kadar %100’ü tamamlar.'
        },
        deactivateGPU: {
          title: 'GPU’yu devre dışı bırak',
          desc: 'Gereksiz yere etkinse ayrık GPU’yu devre dışı bırakır.\n\nUYARI: Yerleşik ekran kapalıysa veya hibrit mod etkin değilse bu işlem doğru çalışmaz.',
          options: {
            KillApps: 'Uygulamaları kapat',
            RestartGPU: 'GPU’yu yeniden başlat'
          }
        },
        delay: {
          title: 'Gecikme',
          desc: 'Sonraki adımdan önce gecikme ekler.',
          second_one: '{{count}} saniye',
          second_other: '{{count}} saniye'
        },
        displayBrightness: {
          title: 'Ekran parlaklığı',
          desc: 'Yerleşik ekranın parlaklığını değiştirir.\n\nUYARI: Yerleşik ekran kapalıysa bu işlem doğru çalışmaz.',
          percent: '{{value}}%'
        },
        dpiScale: {
          title: 'DPI',
          desc: 'Yerleşik ekranın ölçeğini değiştirir.\n\nUYARI: Yerleşik ekran kapalıysa bu işlem doğru çalışmaz.',
          percent: '{{value}}%'
        },
        flipToStart: {
          title: 'Açınca başlat',
          desc: 'Kapağı açtığınızda dizüstü bilgisayarı açar.'
        },
        fnLock: {
          title: 'Fn kilidi',
          desc: 'Fn tuşuna basmadan F1-F12’nin ikincil işlevlerini kullanır.'
        },
        godModePreset: {
          title: 'Özel mod profili',
          desc: 'Özel mod profilini etkinleştirir.\nBu ayar yalnızca özel mod etkinken geçerlidir.'
        },
        hdr: {
          title: 'HDR',
          desc: 'Yerleşik ekranda HDR’yi etkinleştirir.\n\nUYARI: Yerleşik ekran kapalıysa bu işlem doğru çalışmaz.'
        },
        hideMainWindow: {
          title: 'Ana pencereyi gizle'
        },
        rgbKeyboardBacklight: {
          title: 'Klavye arka aydınlatması',
          desc: 'Arka aydınlatma profilini ayarlar.'
        },
        run: {
          title: 'Çalıştır',
          desc: 'Bir betik veya program çalıştırır.\nÖnce betiğinizin doğru çalıştığından emin olun.'
        },
        showMainWindow: {
          title: 'Ana pencereyi göster'
        },
        speaker: {
          title: 'Hoparlör',
          desc: 'Sessize alındığında tüm etkin ses çıkış aygıtları sessize alınır.'
        },
        spectrumKeyboardBacklightBrightness: {
          title: 'Klavye arka aydınlatma parlaklığı',
          desc: 'Klavye arka aydınlatma parlaklığını ayarlar.'
        },
        spectrumKeyboardBacklightImportProfile: {
          title: 'Klavye arka aydınlatma profilini içe aktar',
          desc: 'Arka aydınlatma yapılandırmasını geçerli profile içe aktarıp uygular.'
        },
        spectrumKeyboardBacklightProfile: {
          title: 'Klavye arka aydınlatma profili',
          desc: 'Klavye arka aydınlatma profilini ayarlar.'
        },
        touchpadLock: {
          title: 'Dokunmatik yüzey kilidi',
          desc: 'Dokunmatik yüzeyi devre dışı bırakır.'
        },
        turnOffMonitors: {
          title: 'Ekranları kapat',
          desc: 'Tüm mevcut ekranları kapatır.'
        },
        turnOffWiFi: {
          title: 'Wi-Fi’yi kapat'
        },
        turnOnWiFi: {
          title: 'Wi-Fi’yi aç'
        },
        whiteKeyboardBacklight: {
          title: 'Klavye arka aydınlatması',
          desc: 'Arka aydınlatma parlaklığını ayarlar.'
        },
        winKey: {
          title: 'Windows tuşu kilidi',
          desc: 'Yerleşik klavyedeki Windows tuşunu devre dışı bırakır.'
        }
      },
      moveUp: 'Yukarı taşı',
      moveDown: 'Aşağı taşı',
      noEditableParameters: 'Bu adımda düzenlenebilir parametre yok.',
      addAutomaticPipeline: 'Yeni işlem',
      addQuickAction: 'Yeni hızlı işlem',
      quickActionName: 'Hızlı işlem adı',
      triggerPicker: {
        title: 'Yeni işlem — tetikleyici seçin'
      },
      triggerConfig: {
        title: 'Tetikleyiciyi yapılandır',
        noEditableTriggers: 'Bu tetikleyicinin yapılandırılabilir parametresi yok.'
      },
      triggerNames: {
        aCAdapterConnected: 'AC güç adaptörü bağlandığında',
        lowWattageACAdapterConnected: 'Düşük güçlü AC adaptör bağlandığında',
        aCAdapterDisconnected: 'AC güç adaptörü çıkarıldığında',
        powerMode: 'Güç modu değiştiğinde',
        godModePresetChanged: 'Özel mod profili değiştiğinde',
        gamesAreRunning: 'Oyun çalışırken',
        gamesStop: 'Oyun kapandığında',
        processesAreRunning: 'Uygulama başladığında',
        processesStopRunning: 'Uygulama kapandığında',
        userInactivity: 'Kullanıcı etkin değilken',
        userInactivityZero: 'Kullanıcı etkinken',
        sessionLock: 'Oturum kilitlendi',
        sessionUnlock: 'Oturum kilidi açıldı',
        lidOpened: 'Kapak açıldı',
        lidClosed: 'Kapak kapandı',
        displayOn: 'Ekranlar açıldığında',
        displayOff: 'Ekranlar kapandığında',
        hdrOn: 'HDR açıldığında',
        hdrOff: 'HDR kapandığında',
        deviceConnected: 'Cihaz bağlandığında',
        deviceDisconnected: 'Cihaz çıkarıldığında',
        externalDisplayConnected: 'Harici ekran bağlandığında',
        externalDisplayDisconnected: 'Harici ekran çıkarıldığında',
        wiFiConnected: 'Wi-Fi bağlandığında',
        wiFiDisconnected: 'Wi-Fi bağlantısı kesildiğinde',
        time: 'Belirtilen zamanda',
        periodic: 'Periyodik işlem',
        hardwareSensor: 'Donanım sensörü',
        batteryPercentage: 'Pil yüzdesi',
        onStartup: 'Başlangıçta',
        onResume: 'Devam edildiğinde'
      },
      triggerEditors: {
        noProcesses: 'İşlem seçilmedi.',
        noDevices: 'Cihaz seçilmedi.',
        inactivityTimeout: 'Zaman aşımı',
        seconds: '{{count}} saniye',
        minutes: '{{count}} dakika',
        hours: '{{count}} saat',
        ssidPlaceholder: 'Ağ adı (SSID)',
        addSsid: 'Ağ adı ekle',
        atTime: 'Saatte',
        hour: 'Saat',
        minute: 'Dakika',
        allDays: 'Her gün',
        day: {
          0: 'Pazar',
          1: 'Pazartesi',
          2: 'Salı',
          3: 'Çarşamba',
          4: 'Perşembe',
          5: 'Cuma',
          6: 'Cumartesi'
        },
        metric: 'Metrik',
        comparison: 'Karşılaştırma',
        threshold: 'Eşik',
        thresholdPercent: 'Eşik (%)',
        durationSeconds: 'Süre (saniye)',
        cooldownSeconds: 'Bekleme (saniye)',
        chargeFilter: 'Şarj filtresi',
        deviceInstanceId: 'Cihaz örnek kimliği'
      }
    },
    macro: {
      title: 'Klavye makrosu',
      enable: 'Makroları etkinleştir',
      enableDesc: 'Makroların çalışması için Universal Device Toolkit çalışıyor olmalıdır.',
      subtitle: 'Tuş basış dizilerini kaydedebilir ve klavyenizdeki sayısal tuş takımıyla tetikleyebilirsiniz.',
      numpad: 'Sayısal tuş takımı',
      sequence: 'Dizi',
      repeat: 'Tekrar sayısı',
      events: 'Olaylar',
      save: 'Kaydet',
      clear: 'Temizle',
      play: 'Oynat',
      record: 'Kaydet',
      recordingOptions: 'Kayıt seçenekleri',
      ignoreDelays: 'Gecikmeleri yoksay',
      interruptOnOtherKey: 'Başka tuşla kes',
      dontRepeat: 'Tekrarlama',
      keyboardOnly: 'Yalnızca klavye',
      keyboardMouse: 'Klavye tuşları ve fare düğmeleri',
      allInputs: 'Tüm girişler',
      recordingInterrupted: 'Kayıt kesildi',
      keyboard: 'Klavye',
      mouse: 'Fare',
      move: 'Fare hareketi',
      wheelUp: 'Tekerlek yukarı',
      wheelDown: 'Tekerlek aşağı',
      wheelLeft: 'Tekerlek sola',
      wheelRight: 'Tekerlek sağa',
      leftButton: 'Sol düğme',
      rightButton: 'Sağ düğme',
      middleButton: 'Orta düğme',
      xButton: 'X düğmesi',
      button: 'Fare düğmesi',
      empty: 'Bu tuş için henüz makro dizisi yok',
      recording: {
        preparing: 'Kayıt 3 saniye içinde başlayacak…',
        title: 'Kayıt…',
        pressEscToStop: 'Durdurmak için ESC’ye basın.',
        focusHint: 'Kayıt sırasında bu pencereyi odakta tutun.'
      }
    },
    plugins: {
      title: 'Eklentiler ve uzantılar',
      search: 'Eklenti ara',
      filterAll: 'Tümü',
      filterInstalled: 'Yüklü',
      filterNotInstalled: 'Yüklü değil',
      refresh: 'Yenile',
      total: 'Toplam {{count}}',
      summary: '{{count}} yüklü',
      updatable: '{{count}} güncelleme mevcut',
      install: 'Yükle',
      update: 'Güncelle',
      updateAvailable: 'Güncelleme mevcut',
      uninstall: 'Kaldır',
      uninstallConfirm: 'Bu eklenti kaldırılsın mı?',
      uninstallFailed: 'Kaldırılamadı',
      installed: 'Yüklü',
      online: 'Çevrimiçi',
      installing: 'Yükleniyor…',
      downloading: 'İndiriliyor…',
      preparingDownload: 'İndirme hazırlanıyor…',
      downloadCompleted: 'İndirme tamamlandı',
      offline: 'Çevrimiçi mağaza kullanılamıyor; yalnızca yerel olarak yüklenen eklentiler gösteriliyor',
      empty: 'Eklenti bulunamadı',
      dependencies: 'Bağımlılıklar',
      dependenciesBlocked: 'Bu eklentinin karşılanmamış bağımlılıkları var ve kaldırılamaz',
      details: 'Ayrıntılar',
      usageGuide: 'Kullanım kılavuzu',
      changelog: 'Değişiklik günlüğü',
      importProgress: 'Eklenti paketleri içe aktarılıyor…',
      importSuccess: '{{count}} eklenti paketi içe aktarıldı',
      importFailed: '{{count}} eklenti paketi içe aktarılamadı',
      installAll: 'Tümünü yükle',
      installAllComplete: '{{count}} eklenti yüklendi',
      installAllPartial: '{{total}} işlemin {{count}} tanesi tamamlandı',
      copyId: 'Eklenti kimliğini kopyala',
      copied: 'Eklenti kimliği panoya kopyalandı',
      copyFailed: 'Eklenti kimliği kopyalanamadı',
      local: 'Yerel',
      collapseDetails: 'Ayrıntıları gizle',
      showDetails: 'Ayrıntıları göster',
      updateInfo: 'Güncelleme bilgisi',
      versionLabel: 'Sürüm:',
      configure: 'Yapılandır',
      open: 'Aç',
      description: 'İşlevselliği genişletmek için eklenti yükleyin ve yönetin',
      storeUnavailable: 'Eklenti mağazası kullanılamıyor',
      summaryTotal: 'Toplam eklenti',
      summaryInstalled: 'Yüklü',
      summaryUpdates: 'Güncelleme mevcut',
      importFromFiles: 'Dosyalardan içe aktar',
      updateAll: 'Tümünü güncelle',
      emptyStore: 'Eklenti mağazası şu anda boş. Gelecekteki eklenti güncellemeleri için takipte kalın.'
    },
    optimization: {
      title: 'Sistem optimizasyonu',
      info: 'Bu işlemler sistem hizmetlerini ve dosyalarını değiştirir; yönetici hakları gerekebilir.',
      tabs: {
        optimization: 'Optimizasyon',
        cleanup: 'Temizlik',
        driverDownload: 'Sürücü indirme',
        networkAcceleration: 'Ağ hızlandırma'
      },
      recommended: 'Önerilen',
      selected: 'Seçili',
      selectedActions: 'Seçili işlemler',
      noSelection: 'İşlem seçilmedi',
      selectRecommended: 'Önerilenleri seç',
      applyRecommended: 'Tüm önerilenleri uygula',
      apply: 'Uygula',
      clear: 'Temizle (geri al)',
      applied: 'Uygulandı',
      applyFailed: 'Uygulanamadı (yönetici hakları gerekebilir)',
      reverted: 'Geri alındı',
      revertFailed: 'Geri alınamadı (yönetici hakları gerekebilir)',
      estimate: 'Boyutu tahmin et',
      estimateResult: 'Geri kazanılabilir alan',
      runCleanup: 'Temizliği çalıştır',
      cleanupHint: 'Temizlik, özel temizlik kurallarınızı çalıştırır.',
      cleanupConfirm: 'Temizlik şimdi çalıştırılsın mı?',
      cleanupDone: 'Temizlik tamamlandı',
      cleanupFailed: 'Temizlik başarısız oldu',
      cleanup: {
        custom: {
          header: 'Özel temizlik kuralları',
          description: 'Seçilen temizlik işlemleriyle birlikte temizlenen ek klasörler.',
          empty: 'Özel temizlik kuralı yok',
          add: 'Klasör ekle',
          edit: 'Klasörü düzenle',
          remove: 'Kaldır',
          clear: 'Tümünü temizle',
          added: 'Kural eklendi',
          updated: 'Kural güncellendi',
          recursive: 'Alt klasörleri dahil et',
          noExtensions: 'Uzantı belirtilmedi',
          folderPickerFailed: 'Klasör seçici açılamadı'
        }
      },
      network: {
        status: 'Durum',
        running: 'Çalışıyor',
        stopped: 'Durduruldu',
        backendReady: 'Arka uç hazır',
        backendNotReady: 'Arka uç hazır değil',
        config: 'Temel yapılandırma',
        accelerationEnabled: 'Hızlandırmayı etkinleştir',
        mode: 'Mod',
        modes: {
          off: 'Kapalı',
          systemProxy: 'Sistem proxy’si',
          hosts: 'Hosts',
          diagnosticsOnly: 'Yalnızca tanılama'
        },
        save: 'Yapılandırmayı kaydet',
        saved: 'Yapılandırma kaydedildi',
        saveFailed: 'Yapılandırma kaydedilemedi',
        start: 'Başlat',
        stop: 'Durdur',
        startFailed: 'Başlatılamadı',
        stopFailed: 'Durdurulamadı',
        modeLabel: 'Mod',
        targetsLabel: 'Hedefler',
        portLabel: 'Bağlantı noktası',
        targetsHeading: 'Hızlandırma hedefleri',
        domainGroupsHint: 'Yerel proxy üzerinden hızlandırılacak hizmetleri seçin.',
        domainGroupsEmptyTitle: 'Hızlandırma hedefi yok',
        domainGroupsEmptyDescription: 'Hedef listesi boş veya aramayla eşleşen yok.',
        selectionHint: 'Seçilen hedefler hızlandırma başladığında uygulanır.',
        searchTargets: 'Hedefleri ara',
        recommendedMenu: 'Önerilen',
        groupRuntime: '{{selected}}/{{total}} seçili  {{active}} etkin',
        trafficHeading: 'Trafik özeti',
        metrics: {
          upload: 'Yükleme',
          download: 'İndirme',
          connections: 'Bağlantılar',
          total: 'Toplam trafik',
          health: 'Sağlık'
        },
        trafficLive: 'Canlı proxy trafiği toplanıyor',
        trafficWaiting: 'Canlı trafik toplamak için hızlandırmayı başlatın',
        trafficUnavailable: 'Trafik verileri geçici olarak kullanılamıyor',
        connectionsHeading: 'Güncel ve son bağlantılar',
        destinationsHeading: 'Hedef istatistikleri',
        connectionSummary: '{{active}} etkin / {{total}} toplam',
        destinationSummary: '{{count}} hedef',
        connectionStates: {
          active: 'Etkin',
          completed: 'Tamamlandı',
          blocked: 'Engellendi',
          failed: 'Başarısız',
          stopped: 'Durduruldu',
          unknown: 'Bilinmiyor'
        },
        unknownHost: 'Bilinmeyen ana bilgisayar',
        destinationRow: '{{count}} bağlantı  {{latency}}',
        health: {
          healthy: 'Sağlıklı',
          degraded: 'Bozuldu',
          stopped: 'Durduruldu',
          unknown: 'Bilinmiyor'
        },
        modeFull: {
          systemProxy: 'Sistem proxy’si',
          hosts: 'Hosts dosyası',
          diagnosticsOnly: 'Yalnızca tanılama',
          off: 'Boşta'
        },
        backendMissingHint: 'Proxy çalışanı kullanılamıyor',
        selectGroupsFirstHint: 'En az bir hedef seçin',
        advancedHeading: 'Gelişmiş',
        advancedBody: 'Gelişmiş ayarlar ve ağ kurtarma.',
        portFormat: 'Bağlantı noktası: {{port}}',
        dangerZoneHeading: 'Tehlike bölgesi',
        restoreHint: 'Hızlandırmadan önce kaydedilen sistem ağ durumunu geri yükler.',
        restoreNetwork: 'Ağı geri yükle',
        restoreConfirm: 'Sistem ağ durumu şimdi geri yüklensin mi?',
        restored: 'Ağ durumu geri yüklendi',
        diag: {
          natTitle: 'NAT',
          dnsTitle: 'DNS',
          ipv6Title: 'IPv6',
          detect: 'Algıla',
          unknown: 'Bilinmiyor',
          natTypes: {
            OpenInternet: 'Açık NAT',
            Nat: 'NAT',
            UdpBlocked: 'UDP engellendi',
            Unknown: 'Bilinmiyor'
          },
          internetConnected: 'Bağlı',
          internetUnreachable: 'Erişilemiyor',
          natType: 'NAT türü',
          localIp: 'Yerel IP',
          publicIp: 'Genel IP',
          internet: 'İnternet',
          dnsDomain: 'Alan adı',
          customDns: 'Özel DNS',
          enableDoh: 'DoH',
          dohUrl: 'DoH URL’si',
          latency: 'Gecikme',
          resolvedAddress: 'Çözümlenen adres',
          latencyFormat: '{{ms}} ms',
          failed: 'Başarısız',
          ipv6Support: 'IPv6 desteği',
          ipv6Address: 'IPv6 adresi',
          ipv6SupportedFull: 'IPv6 erişimi destekleniyor',
          notSupported: 'Desteklenmiyor'
        }
      },
      driverDownload: {
        comingSoon: 'Sürücü indirme gelecekteki bir sürümde sunulacak'
      },
      driver: {
        machineType: 'Makine türü',
        machineTypePlaceholder: 'örn. 82K3',
        os: 'İşletim sistemi',
        downloadTo: 'İndirilecek yer',
        downloadToPlaceholder: 'İndirmeler için bir klasör seçin',
        browse: 'Gözat',
        openDownloadTo: 'Klasörü aç',
        source: 'Kaynak',
        primarySource: 'Vantage',
        primarySourceMessage: 'Vantage üzerinden resmi cihaz veritabanı.',
        secondarySource: 'PC Support',
        secondarySourceMessage: 'PC Support uyumluluk veritabanı.',
        scan: 'Tara',
        scanning: 'Taranıyor…',
        scanValidation: 'Geçerli bir 4 karakterli makine türü girin ve işletim sistemi seçin.',
        disclaimer: 'Paketler seçilen kaynaktan gelir. Yükleme riski size aittir.',
        filter: 'Filtrele',
        onlyShowUpdates: 'Yalnızca güncellemeler',
        sort: {
          name: 'Ada göre sırala',
          category: 'Kategoriye göre sırala',
          date: 'Tarihe göre sırala'
        },
        selectRecommended: 'Önerilenleri seç',
        startAll: 'Tümünü başlat',
        pauseAll: 'Tümünü duraklat',
        clearSelection: 'Seçimi temizle',
        packagesFound: '{{count}} paket bulundu.',
        packagesFoundOne: '1 paket bulundu.',
        status: {
          NotStarted: '',
          Queued: 'Sırada',
          Downloading: 'İndiriliyor',
          Installing: 'Yükleniyor',
          Completed: 'Tamamlandı',
          Error: 'Hata'
        },
        recommended: 'Önerilen',
        isUpdate: 'Güncelleme',
        reboot: {
          recommended: 'Yeniden başlatma önerilir',
          required: 'Yeniden başlatma gerekli',
          shutdown: 'Kapatma gerekli'
        },
        oldPackageWarning: 'Bu paket bir yıldan eski; sürücü güncel olmayabilir.',
        download: 'İndir',
        install: 'Yükle',
        uninstall: 'Kaldır',
        pause: 'Duraklat',
        openReadme: 'Readme’yi aç',
        hide: 'Gizle',
        hideAll: 'Tümünü gizle',
        showHiddenDownloads: 'Gizli indirmeleri göster',
        downloadInProgress: {
          title: 'İndirme sürüyor',
          message: 'İndirmeler hâlâ çalışıyor. Yeniden taransın mı?',
          confirm: 'Tara'
        },
        empty: {
          notScanned: {
            title: 'Sürücü paketlerini tara',
            message: 'Uyumlu sürücü indirmelerini listelemek için bir kaynak seçip tarayın.'
          },
          noResults: {
            title: 'Sürücü indirmesi bulunamadı',
            message: 'Farklı bir kaynak, işletim sistemi veya makine türü deneyin.'
          },
          noFilterResults: {
            title: 'Eşleşen indirme bulunamadı',
            message: 'Filtreyi, yalnızca güncelleme seçeneğini veya gizli indirme listesini ayarlayın.'
          },
          error: {
            title: 'Sürücü taraması tamamlanamadı',
            message: 'Seçilen kaynağı ve ağ bağlantısını kontrol edip yeniden tarayın.'
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
      title: 'Hakkında',
      appName: 'Uygulama',
      version: 'Sürüm',
      build: 'Derleme',
      links: 'Proje bağlantıları',
      projectWebsite: 'GitHub’da proje sitesi',
      latestRelease: 'GitHub’da son sürüm',
      applicationFolders: 'Uygulama klasörleri',
      data: 'Veri',
      temp: 'Geçici',
      pid: 'İşlem kimliği',
      machine: 'Cihaz modeli',
      bios: 'BIOS sürümü',
      compatible: 'Uyumluluk',
      yes: 'Uyumlu',
      no: 'Uyumsuz',
      dataFolder: 'Veri klasörü',
      thirdParty: 'Üçüncü taraf kitaplıklar',
      copyright: 'Telif hakkı'
    },
    statusBanner: {
      updateAvailable: 'Güncelleme mevcut!',
      updateAvailableWithVersion: '{{version}} güncellemesi mevcut!',
      pluginExtensionsDisabled: 'Eklentiler ve uzantılar gezinmesi gizli. Ayarlar → Gezinme öğeleri altında etkinleştirin.'
    },
    wpf: legacy.translation.wpf
  }
}



