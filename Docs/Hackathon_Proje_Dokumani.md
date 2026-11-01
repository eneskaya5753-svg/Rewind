# İklim Oyunu – Unity Proje Mimarisi ve Entegrasyon Dokümanı

> Bu doküman Claude Code ve projeyi devralacak geliştirici içindir. Proje sıfırdan kurulacaktır.
> Namespace kökü: `EcoGame` (proje adı netleşince toplu değiştirilebilir).

---

## 0. ÇALIŞMA KURALLARI (Claude Code için zorunlu)

1. **Önce mevcut asset'ler.** İşe başlamadan `Assets/_ThirdParty/` altındaki tüm paketleri tara ve uygun model, materyal, prefab ve ses varsa onu kullan.
2. **Bulunamayan her şey blockout olur.** Unity primitive'leri (Cube, Cylinder, Sphere, Capsule) ile yap, adı `PH_` önekiyle başlasın, `M_Placeholder_<Renk>` materyali kullansın.
3. **Her blockout kayıt altına alınır.** Proje kökündeki `Docs/PlaceholderList.md` dosyasına şu satırı ekle: `PH_ adı | Prefab yolu | Neyi temsil ediyor | Önerilen gerçek asset`.
4. **Oyuncuya gösterilen her bilgi ekran UI'ındadır** (Screen Space – Overlay Canvas). Dünya içine yazı, World Space Canvas veya obje üstü metin kullanılmaz. Sebep: top-down ve first person modlarının ikisinde de aynı görünmesi.
5. **Sistemler kameradan bağımsızdır.** Hareket, etkileşim ve toplama hiçbir yerde belirli bir kameraya bağlı yazılmaz. Etkileşim, raycast yerine yakınlık tabanlıdır: oyuncuya en yakın Interactable seçilir.
6. **Ayarlanabilir her sayı ScriptableObject'tedir.** Kod içinde sihirli sayı yoktur. Denge değerleri `SO_EnvironmentSettings` içindedir.
7. **Script'ler model objesine eklenmez.** Her prefab'ın yapısı "Root (script + collider) → Visual (sadece mesh)" şeklindedir (bkz. Bölüm 4).
8. `Assets/_ThirdParty/` içindeki paketler taşınmaz ve düzenlenmez. Değişiklik gerekiyorsa prefab variant `_Project/Prefabs` altında oluşturulur.

---

## 1. STANDART KLASÖR YAPISI

```
Assets/
├── _Project/                      ← Bize ait HER ŞEY burada. Assets kökünde dosya YASAK.
│   ├── Art/
│   │   ├── Animations/            ← ANIM_ klipleri, AC_ Animator Controller'ları
│   │   ├── Materials/
│   │   │   ├── Environment/
│   │   │   ├── Props/
│   │   │   ├── Buildings/         ← Hologram materyali dahil
│   │   │   ├── Water/
│   │   │   ├── VFX/
│   │   │   └── Placeholder/       ← M_Placeholder_Red, _Blue, _Green, _Grey, _Yellow
│   │   ├── Models/
│   │   │   ├── Environment/       ← Ağaç, kaya, ev, buz kütlesi
│   │   │   ├── Buildings/         ← Güneş paneli, rüzgar türbini, su değirmeni, file
│   │   │   ├── Machines/          ← Jeneratör, geri dönüşüm makinesi, arıtma cihazı
│   │   │   ├── Trash/             ← Cam, metal, plastik, organik çöp modelleri
│   │   │   └── Animals/           ← Penguen, kutup ayısı, ziyaretçi hayvanlar, arı
│   │   ├── Textures/              ← Modellere ait texture'lar, model klasör yapısını taklit eder
│   │   ├── UI/
│   │   │   ├── Icons/             ← UI_Icon_Glass, UI_Icon_Metal ...
│   │   │   ├── Sprites/           ← Panel arka planları, bar dolguları, butonlar
│   │   │   └── Fonts/
│   │   └── VFX/                   ← Duman, sis, su sıçraması, inşa efekti
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Data/                      ← ScriptableObject ÖRNEKLERİ (.asset dosyaları)
│   │   ├── Settings/              ← SO_EnvironmentSettings, SO_PlayerSettings
│   │   ├── Trash/                 ← SO_Trash_Glass, _Metal, _Plastic, _Organic
│   │   ├── Buildings/             ← SO_Building_SolarPanel ...
│   │   └── Quiz/                  ← SO_QuizDatabase + SO_Quiz_Q01 ... Q20
│   ├── Prefabs/
│   │   ├── Managers/
│   │   ├── Player/                ← PF_Player, PF_Camera_FirstPerson, PF_Camera_TopDown
│   │   ├── Environment/
│   │   ├── Machines/              ← PF_Generator, PF_RecyclingMachine, PF_Desalinator
│   │   ├── Buildings/             ← Tamamlanmış yapılar
│   │   ├── BuildSites/            ← Hologram şablonlu inşa alanları
│   │   ├── Trash/                 ← PF_Trash_Glass ... PF_RiverTrash
│   │   ├── Waterables/            ← PF_Tree_Waterable, PF_Garden, PF_AnimalTrough
│   │   ├── Animals/
│   │   ├── VFX/
│   │   └── UI/                    ← PF_UI_QuizPanel, PF_UI_BuildRequirement ...
│   ├── Scenes/
│   │   ├── Main.unity             ← Teslim edilecek tek sahne
│   │   └── Sandbox/               ← Kişisel test sahneleri, build'e dahil EDİLMEZ
│   ├── Scripts/
│   │   ├── Core/                  ← GameManager, ObjectiveManager, GameState
│   │   ├── Player/                ← PlayerController, PlayerInteractor, PlayerInventory, WaterCarrier
│   │   ├── Camera/                ← CameraModeManager, FirstPersonCameraRig, TopDownCameraRig
│   │   ├── Interaction/           ← IInteractable, Interactable (base)
│   │   ├── Environment/           ← EnvironmentManager, IPollutionSource, Fog/TreeColor/WaterColor/Ice controller'ları
│   │   ├── Machines/              ← Generator, RecyclingMachine, Desalinator
│   │   ├── Building/              ← BuildSite, BuildModeController
│   │   ├── Trash/                 ← TrashItem, TrashSpawner, RiverTrashFlow, RiverNet
│   │   ├── Quiz/                  ← QuizManager
│   │   ├── Water/                 ← Waterable, Garden
│   │   ├── Animals/               ← IceAnimalHopper, AnimalVisitorSpawner, BeeSwarm
│   │   ├── UI/                    ← UIManager ve panel script'leri
│   │   ├── Data/                  ← ScriptableObject SINIF tanımları
│   │   └── Utils/
│   └── Settings/                  ← URP asset'leri, Input Actions (.inputactions)
└── _ThirdParty/                   ← Hazır asset paketleri. DOKUNULMAZ.

Docs/                              ← Assets DIŞINDA, proje kökünde
├── PlaceholderList.md
└── (bu doküman)
```

**Kurallar:**
- ScriptableObject **sınıfı** `Scripts/Data/` altında, **örnekleri** (.asset) `Data/` altındadır.
- Bir modelin materyali `Materials/` altındaki aynı kategori klasörüne gider, model klasörüne konmaz.
- Sahnede kullanılan her obje bir prefab'dan gelir. Sahnede prefab'sız obje bırakılmaz (Terrain ve ışıklar hariç).

---

## 2. İSİMLENDİRME STANDARTLARI

### Kod
| Öğe | Kural | Örnek |
|---|---|---|
| Sınıf / Script dosyası | PascalCase, dosya adı = sınıf adı | `RecyclingMachine.cs` |
| Interface | `I` + PascalCase | `IInteractable`, `IPollutionSource` |
| Public metot / property | PascalCase | `AddItem()`, `CurrentPollution` |
| Serialized private alan | camelCase + `[SerializeField]` | `[SerializeField] private float moveSpeed;` |
| Private alan (serialize değil) | `_camelCase` | `_currentTarget` |
| Sabit | UPPER_SNAKE | `MAX_BAG_SIZE` |
| Event | `On` + PascalCase | `OnBuildingCompleted` |
| Enum | PascalCase, tekil | `TrashType.Glass` |
| Namespace | `EcoGame.<Klasör>` | `EcoGame.Machines` |

### Asset ön ekleri
| Tür | Ön ek | Örnek |
|---|---|---|
| Prefab | `PF_` | `PF_Generator` |
| Prefab Variant | `PF_` + `_Var` | `PF_Tree_Pine_Var` |
| Statik model | `SM_` | `SM_WindTurbine` |
| Rigged model | `SK_` | `SK_PolarBear` |
| Materyal | `M_` | `M_Hologram` |
| Texture | `T_` + `_D/_N/_M` sonek | `T_Ice_D` |
| ScriptableObject örneği | `SO_<Tür>_<Ad>` | `SO_Building_WindTurbine` |
| UI sprite / ikon | `UI_` | `UI_Icon_Plastic` |
| Animasyon / Controller | `ANIM_` / `AC_` | `AC_Penguin` |
| VFX | `VFX_` | `VFX_GeneratorSmoke` |
| Ses | `SFX_` / `MUS_` | `SFX_QuizCorrect` |
| Blockout | `PH_` | `PH_RecyclingMachine` |
| Sahne | PascalCase | `Main` |

### Sahne içi obje adları (UI dahil)
| Tür | Ön ek | Örnek |
|---|---|---|
| Canvas | `Canvas_` | `Canvas_HUD` |
| Panel | `Panel_` | `Panel_Quiz` |
| Buton | `Btn_` | `Btn_AnswerA` |
| Metin | `Txt_` | `Txt_Question` |
| Görsel | `Img_` | `Img_TrashIcon` |
| Slider / bar | `Bar_` | `Bar_Pollution` |
| Boş referans noktası | `Anchor_` | `Anchor_SmokeOut` |
| Görsel kök | `Visual` (sabit isim) | — |

---

## 3. UNITY SAHNE VE ENTEGRASYON REHBERİ

### 3.1 Hierarchy şablonu (`Main.unity`)

Ayraçlar, Tag'i `EditorOnly` olan boş GameObject'lerdir.

```
---MANAGERS---
  GameManager
  EnvironmentManager
  ObjectiveManager
  QuizManager
  CameraModeManager
  AudioManager
---PLAYER---
  PF_Player
    Anchor_Head
      PF_Camera_FirstPerson
  PF_Camera_TopDown
---ENVIRONMENT---
  Terrain
  Sea
  River
    Anchor_RiverPath (Waypoint_01 … Waypoint_N)
  Forest
  House
  IceFloe
  Props (tekne vb. dekor)
---INTERACTABLES---
  Machines
    PF_Generator_House
    PF_Generator_Desalinator
    PF_Generator_Recycling
    PF_RecyclingMachine
    PF_Desalinator
  BuildSites
    PF_BuildSite_SolarPanel
    PF_BuildSite_WindTurbine
    PF_BuildSite_WaterMill
    PF_BuildSite_RiverNet
  Waterables
    PF_Garden
    PF_AnimalTrough
    Trees_Waterable (nehrin karşısındaki ön sıra ağaçlar)
---TRASH---
  TrashSpawner
    SpawnArea_01 … SpawnArea_N (BoxCollider, isTrigger)
  RiverTrashFlow
---ANIMALS---
  IceAnimals
  AnimalVisitorSpawner
  BeeSwarm_Garden
---VFX---
  Fog (Volume veya RenderSettings sürücüsü)
---LIGHTING---
  Directional Light
  Global Volume
---UI---
  Canvas_HUD
  Canvas_Quiz
  Canvas_EndScreens
  EventSystem
```

### 3.2 Manager'lar: ne nerede, ne yapar

| GameObject | Script | Görev |
|---|---|---|
| GameManager | `GameManager` | Oyun durumu (Playing / Paused / Won / Lost), zaman ölçeği, yeniden başlatma |
| EnvironmentManager | `EnvironmentManager` | Hava kirliliği, sıcaklık ve su kirliliği metrikleri (0–100). `IPollutionSource` kaydı ve artış hesabı. Eşik event'leri. |
| ObjectiveManager | `ObjectiveManager` | Kazanma şartlarını izler, tamamlanınca iyileşme fazını başlatır |
| QuizManager | `QuizManager` | Soru havuzu, rastgele ve tekrarsız seçim, soru panelini açar, sonucu callback ile döner |
| CameraModeManager | `CameraModeManager` | FP ↔ Top-down geçişi (V tuşu), aktif kameraya göre hareket yönü referansı sağlar |
| AudioManager | `AudioManager` | SFX ve müzik çalma (opsiyonel, vakit kalırsa) |
| Canvas_HUD | `UIManager` | Tüm HUD elemanlarını yönetir, diğer sistemlerin event'lerini dinler |

**Bağlantı prensibi:** Manager'lar arası referanslar Inspector'dan bağlanır. Sahnede çok sayıda bulunan objeler (çöpler, ağaçlar, jeneratörler) kendilerini `OnEnable`'da ilgili manager'a **runtime'da kaydeder**. Bu objeler için elle referans bağlanmaz.

### 3.3 Inspector bağlantıları (adım adım)

1. **GameManager**
   - `environmentManager` ← EnvironmentManager
   - `objectiveManager` ← ObjectiveManager
   - `uiManager` ← Canvas_HUD
2. **EnvironmentManager**
   - `settings` ← `Data/Settings/SO_EnvironmentSettings`
   - `fogController`, `waterColorController`, `iceFloeController` ← ilgili sahne objeleri
3. **ObjectiveManager**
   - `buildSites` listesi ← 4 adet PF_BuildSite_*
   - `garden` ← PF_Garden
   - `trashSpawner` ← TrashSpawner (yerdeki çöp sayısı = 0 kontrolü için)
4. **QuizManager**
   - `database` ← `Data/Quiz/SO_QuizDatabase`
   - `quizPanel` ← Canvas_Quiz/Panel_Quiz
5. **CameraModeManager**
   - `firstPersonRig` ← PF_Camera_FirstPerson
   - `topDownRig` ← PF_Camera_TopDown
   - `player` ← PF_Player
6. **PF_Player**
   - `PlayerController.cameraModeManager` ← CameraModeManager
   - `PlayerInventory.settings` ← `SO_PlayerSettings` (çanta kapasitesi 20, su kabı 5)
7. **Her PF_Generator_\***
   - `EnvironmentManager`'a kendini otomatik kaydeder.
   - `Anchor_SmokeOut` ← duman efekti noktası
8. **Her PF_BuildSite_\***
   - `data` ← ilgili `SO_Building_*`
   - `linkedGenerator` ← eşleşen jeneratör (RiverNet için boş bırakılır)
   - `ghostVisual` ← hologram child, `builtVisual` ← tamamlanmış model child
9. **PF_RecyclingMachine**
   - `quizManager` ← QuizManager
10. **TrashSpawner**
    - `spawnTable` ← 4 adet `SO_Trash_*` ve adetleri
    - `spawnAreas` ← SpawnArea child'ları
11. **RiverTrashFlow**
    - `waypoints` ← Anchor_RiverPath child'ları
    - `riverNet` ← PF_BuildSite_RiverNet
12. **UIManager (Canvas_HUD)**
    - Bar_Pollution, Bar_Temperature, Txt_BagCount, Panel_RawMaterials, Panel_BuildRequirement, Txt_InteractPrompt, Canvas_EndScreens referansları.

### 3.4 Kontroller (yeni Input System, `Settings/InputActions`)

| Tuş | Eylem |
|---|---|
| WASD | Hareket (aktif kameraya göre yön) |
| Fare | FP'de bakış, top-down'da karakter yönü |
| E | Etkileşim (topla, boşalt, sula, malzeme koy) |
| Tab | İnşa modu: tüm inşa alanlarının hologramlarını göster/gizle |
| V | Kamera modu değiştir |
| Esc | Duraklat |

---

## 4. GÖRSEL VE MODEL DEĞİŞTİRME REHBERİ (ASSET SWAPPING)

### 4.1 Prefab yapısı (zorunlu)

```
PF_Generator                 ← Root: script'ler + Collider + Rigidbody (gerekirse)
├── Visual                   ← SADECE görsel. Model buraya girer. Script YOK.
│   └── PH_Generator (Cube)
└── Anchor_SmokeOut          ← Script'in kullandığı konum noktası
```

Script'ler hiçbir zaman `Visual` altındaki objelere referans tutmaz. Konum gerekiyorsa `Anchor_` objesi kullanılır, renderer gerekiyorsa çalışma anında `Visual` altından otomatik toplanır.

### 4.2 3D model değiştirme (blockout → gerçek model)

1. Yeni modeli `Art/Models/<Kategori>/` altına `SM_` önekiyle koy.
2. İlgili prefab'ı aç (çift tık, Prefab Mode).
3. `Visual` altındaki `PH_` objesini sil.
4. Yeni modeli `Visual`'ın child'ı olarak sürükle.
5. Ölçeği ve konumu blockout'un kapladığı alana göre ayarla. Root'un transform'una dokunma.
6. Root'taki Collider'ı yeni modelin boyutuna göre yeniden ayarla.
7. `Anchor_` noktalarını yeni modele göre kaydır (örneğin baca ağzı).
8. Prefab'ı kaydet. Sahnedeki tüm örnekler otomatik güncellenir.
9. `Docs/PlaceholderList.md` dosyasından ilgili satırı sil.

**Renk değiştiren objeler için ek adım (ağaçlar, su, buz):**
- `TreeColorController` `Visual` altındaki tüm renderer'ları otomatik bulur ve `MaterialPropertyBlock` ile `_BaseColor` rengini değiştirir.
- Gerçek ağaç modelinde gövde de turuncuya dönüyorsa, Inspector'daki `excludeRenderers` listesine gövde renderer'ını ekle.
- Yeni modelin shader'ı `_BaseColor` desteklemiyorsa (URP/Lit dışı shader), Inspector'daki `colorProperty` alanına doğru property adını yaz.

### 4.3 Hızlı yöntem: GUID'i koruyarak dosya değiştirme

Blockout yerine gerçek **görsel veya model dosyası** varsa ve **dosya adı ve uzantısı aynı** tutulursa:
- Windows Gezgini'nde `Art/` altındaki eski dosyanın üzerine yenisini kopyala.
- Unity, dosyanın GUID'ini korur ve tüm referanslar aynen çalışır.
- Bu yöntem UI sprite'ları için en hızlı yoldur.

### 4.4 2D UI görseli değiştirme

1. Yeni görseli `Art/UI/Icons/` veya `Sprites/` altına koy.
2. Import Settings'te Texture Type'ı `Sprite (2D and UI)` yap ve Apply'a bas.
3. İkon bir veri ise (çöp türü ikonu gibi) ilgili `SO_Trash_*` asset'ini seç ve Inspector'daki `icon` alanına sürükle. Bu ikonu kullanan bütün UI elemanları otomatik güncellenir.
4. İkon sabit bir UI parçası ise ilgili `PF_UI_*` prefab'ını aç, `Img_` objesinin `Source Image` alanına sürükle.

### 4.5 ScriptableObject verilerini düzenleme

**Var olanı düzenleme:**
1. Project penceresinde `_Project/Data/<Kategori>/` klasörüne git.
2. İlgili `SO_` dosyasına tek tık.
3. Inspector'da değerleri değiştir. Değişiklik anında kaydedilir. Play modunda yapılan değişiklikler de kalıcıdır, buna dikkat et.

**Yeni soru ekleme:**
1. `Data/Quiz/` klasöründe sağ tık, sonra `Create > EcoGame > Quiz Question`.
2. Dosyayı `SO_Quiz_Q21` olarak adlandır.
3. Inspector'da alanları doldur.
4. `SO_QuizDatabase` asset'ini seç ve `questions` listesine yeni soruyu sürükle.

**Yapı maliyetini değiştirme:** `Data/Buildings/SO_Building_*` asset'ini seç ve `requirements` listesindeki adetleri değiştir.

**Oyun dengesini değiştirme:** `Data/Settings/SO_EnvironmentSettings` asset'indeki tüm hız, eşik ve düşüş değerleri buradadır.

---

## 5. SİSTEM TEKNİK ÖZETİ (Claude Code'un kuracağı veri ve davranışlar)

> Oynanış anlatısı değil, uygulanacak teknik kurallar.

### 5.1 ScriptableObject sınıfları (`Scripts/Data/`)

```csharp
TrashTypeData      : type (Glass/Metal/Plastic/Organic), displayName, icon, prefab, goesToMachine (Organic=false)
BuildingData       : id, displayName, icon, requirements[] {TrashType, amount}, pollutionDrop, temperatureDrop, ghostPrefab, builtPrefab
QuizQuestionData   : question, answers[4], correctIndex, infoNote
QuizDatabase       : List<QuizQuestionData> questions
EnvironmentSettings: başlangıç değerleri, kaynak başı artış hızları, eşikler, iyileşme hızı
PlayerSettings     : moveSpeed, bagCapacity=20, waterCapacity=5, interactRadius
```

**Örnek soru verisi (takım soruları bu formatta verecek):**
```
question  : "Bir cam şişenin doğada çözünmesi yaklaşık ne kadar sürer?"
answers   : ["10 yıl", "100 yıl", "1.000 yıl", "4.000 yıl ve üzeri"]
correctIndex : 3
infoNote  : "Cam sonsuz kez geri dönüştürülebilir, kalitesi düşmez."
```

### 5.2 Metrikler (`EnvironmentManager`)

- `Pollution` (hava kirliliği), `Temperature` (sıcaklık), `WaterPollution` (su kirliliği): hepsi 0–100 arası.
- **Her saniyedeki artış** = kayıtlı tüm aktif `IPollutionSource` kaynaklarının katkılarının toplamı.

| Kaynak | Etkilediği metrik | Not |
|---|---|---|
| Çalışan jeneratör (×3) | Pollution + Temperature | Duman efekti ve ses |
| Yerdeki her çöp | Temperature (metan) | Toplanınca kaynaktan çıkar |
| Susuz kalan ağaç | Pollution | Sulanınca kaynaktan çıkar |
| Nehirdeki akan çöp | Sadece WaterPollution | Hava ve sıcaklığa ETKİ ETMEZ |

- Yapı tamamlanınca: o yapının `pollutionDrop` ve `temperatureDrop` değerleri **anında** düşülür, ardından artış kalan kaynaklarla devam eder.
- Başlangıç denge değerleri (test edilip ayarlanacak): jürinin 10–15 dakikada bitirebileceği tempo hedeflenir. Başlangıç Pollution=20, Temperature=20.

### 5.3 Görsel eşikler (event tabanlı, ilgili controller dinler)

| Metrik | 50 | 70 | 90 | 100 |
|---|---|---|---|---|
| Pollution | Hafif sis, arılar kaybolur, ziyaretçi hayvan sayısı azalır | Yoğun sis, hayvan sayısı daha da azalır | Çok yoğun sis, hayvanlar yavaşlar, bazıları yere yatar (ölüm) | **Kaybetme** |
| Temperature | Ağaçlar turuncuya kaymaya başlar, susama hızlanır | Daha turuncu, buz belirgin küçülür | Ağaçlar tamamen turuncu, buz minimumda | **Kaybetme** |

- Geçişler kademeli lerp ile yapılır, ani değişim olmaz.
- **Buz kütlesi:** ölçeği Temperature'a göre 1.0'dan 0.3'e lerp edilir. Üstündeki penguen ve kutup ayıları animasyonsuz, hafif zıplama hareketiyle sağa sola gider. Hareket alanları buzun güncel sınırına göre daralır.
- **Su rengi:** WaterPollution'a göre mavi ↔ kahverengi lerp edilir.

### 5.4 Çanta, çöp ve geri dönüşüm

- Çanta en fazla 20 çöp alır, tür fark etmez. Ham maddeler ayrı bir sayaçta tutulur ve limitsizdir.
- Haritada 70 çöp bulunur (`TrashSpawner`): 24 metal, 24 plastik, 14 cam, 8 organik.
- **Geri dönüşüm makinesi:** çantadaki cam, metal ve plastik çöpleri alır. **Her 5 obje için bir soru** sorulur. Soru sorulurken oyun duraklar ve imleç serbest kalır.
  - Doğru cevap: 5 obje ham maddeye dönüşür, ardından `infoNote` gösterilir.
  - Yanlış cevap: 5 obje çantaya geri döner.
- Organik çöp makineye gitmez, bahçeye gübre olarak boşaltılır.

### 5.5 İnşa sistemi

- **Tab** inşa modunu açar ve tüm `BuildSite` hologramları görünür olur (`M_Hologram`, yarı saydam).
- Oyuncu bir BuildSite'a yaklaşınca sol üstte `Panel_BuildRequirement` açılır: gereken ve mevcut ham madde miktarları ikonlarla gösterilir.
- **E** ile envanterdeki uygun ham maddeler yapıya aktarılır. Kısmi teslim mümkündür.
- Yapı tamamlanınca: hologram gizlenir, tamamlanmış model aktif olur, `linkedGenerator.Shutdown()` çağrılır (duman ve ses kesilir), metrik düşüşü uygulanır.

| BuildingData | Gereken | linkedGenerator |
|---|---|---|
| SO_Building_SolarPanel | 10 cam + 5 metal | Generator_House (Jen. 1, ev) |
| SO_Building_WindTurbine | 10 metal + 5 plastik | Generator_Recycling (Jen. 3, geri dönüşüm) |
| SO_Building_WaterMill | 5 metal + 5 plastik | Generator_Desalinator (Jen. 2, su arıtma) |
| SO_Building_RiverNet | 10 plastik | — |

### 5.6 Su ve sulama

- `Desalinator` noktasında **E** ile su kabı 5 kullanıma dolar.
- `Waterable` objelerinin (ağaçlar, PF_AnimalTrough, PF_Garden) her birinde 0–100 arası bir susuzluk değeri vardır. Bu değer zamanla azalır, azalma hızı Temperature ile çarpılır.
- Değer 0'a inince: ağaç kuruma görseline geçer ve `IPollutionSource` olarak kaydolur, suluğa hayvan gelmez, bahçede arı olmaz.
- **Kalıcı ölüm yoktur.** Sulanınca değer 100'e döner, görsel ve kaynak kaydı geri alınır.
- Sulama küçük bir Temperature düşüşü verir.

### 5.7 Bahçe

- 5 organik gübre ve su ister.
- Gübrelenmiş ve sulanmış bahçede çiçekler açar, `BeeSwarm` aktif olur (Pollution < 50 ise) ve kalıcı bir Temperature düşüşü uygulanır.

### 5.8 Nehir çöpleri ve file

- `RiverTrashFlow`: belirli sayıda `PF_RiverTrash` waypoint'ler boyunca **fiziksiz** hareket eder. Akarken toplanamaz.
- File yokken: çöp denize ulaşınca yok olur, yerine yenisi doğar ve WaterPollution artar.
- File kurulunca: çöpler filenin konumunda durur, WaterPollution yavaşça 0'a iner. Takılan çöpler **E** ile toplanabilir, çantaya plastik olarak eklenir ve sahneden silinir.

### 5.9 Kazanma ve kaybetme

- **Kaybetme:** Pollution veya Temperature 100'e ulaşırsa `Canvas_EndScreens/Panel_Lose` açılır ve yeniden başlat butonu gelir.
- **Kazanma şartı:** 4 yapı tamamlanmış + bahçe gübrelenmiş + yerde çöp kalmamış.
- Şart sağlanınca **iyileşme fazı** başlar: artış durur, metrikler yavaşça 0'a iner, sis kalkar, ağaçlar yeşillenir, buz büyür, hayvanlar ve arılar geri gelir. Faz bitince `Panel_Win` açılır.

### 5.10 HUD yerleşimi (Screen Space – Overlay)

| Konum | Eleman |
|---|---|
| Sağ üst | Bar_Pollution, Bar_Temperature |
| Sol üst | Panel_BuildRequirement (inşa alanı yakınındayken) |
| Sol alt | Çanta (x/20, türlere göre ikonlu) + ham madde sayaçları + su kabı (x/5) |
| Alt orta | Txt_InteractPrompt ("E – Topla" vb.) |
| Ekran ortası (modal) | Panel_Quiz |
| Tam ekran | Panel_Win / Panel_Lose |

### 5.11 Kapsam dışı (şimdilik)
- Tekne: sadece dekor, etkileşim yok.
- Kaydetme / yükleme sistemi yok.
- Açlık mekaniği yok.
