# REWIND – Endüstriden Doğaya

> Kirlenmiş bir kıyı kasabasına geliyorsun. Fabrikalar dumanını tütüyor, nehir çöple dolu, ağaçlar kurumuş.
> Çöpleri topla, geri dönüştür, temiz enerjiye geç ve doğayı geri getir. **Zamanı geri sar.**

REWIND, bir iklim hackathon'u için Unity 6 ile geliştirilmiş, eğitici bir 3D keşif ve kaynak yönetimi oyunudur.
Oyuncu; atık toplama, geri dönüşüm, yenilenebilir enerji ve su yönetimi gibi gerçek dünya çözümlerini
oynanış döngüsünün içinde deneyimler. Her geri dönüşümde küçük bir iklim sorusu sorulur.

Hackathon kapsamındaki destekçiler: **HP**, **UNDP** ve **ICPSD** (İstanbul Uluslararası Özel Sektör Kalkınma Merkezi).

---

## İçindekiler

1. [Oyunun Hikâyesi](#1-oyunun-hikâyesi)
2. [Amaç: Nasıl Kazanılır, Nasıl Kaybedilir?](#2-amaç-nasıl-kazanılır-nasıl-kaybedilir)
3. [Oynanış Döngüsü](#3-oynanış-döngüsü)
4. [Mekanikler](#4-mekanikler)
5. [Çevre ve Görsel Geri Bildirim](#5-çevre-ve-görsel-geri-bildirim)
6. [Eğitici Yön: İklim Soruları](#6-eğitici-yön-iklim-soruları)
7. [Kontroller](#7-kontroller)
8. [Arayüz (HUD)](#8-arayüz-hud)
9. [Ses ve Sinematik](#9-ses-ve-sinematik)
10. [Teknik Altyapı](#10-teknik-altyapı)
11. [Oyunu Çalıştırma](#11-oyunu-çalıştırma)
12. [Denge ve Geliştirici Modu](#12-denge-ve-geliştirici-modu)
13. [Proje Klasör Yapısı](#13-proje-klasör-yapısı)
14. [Kullanılan Varlıklar ve Teşekkür](#14-kullanılan-varlıklar-ve-teşekkür)
15. [Sunum İçin Öne Çıkan Noktalar](#15-sunum-için-öne-çıkan-noktalar)

---

## 1. Oyunun Hikâyesi

Oyun, endüstrinin doğayı ele geçirdiği bir kıyı bölgesinde başlar:

- Üç **fosil yakıtlı jeneratör** (ev, su arıtma cihazı ve geri dönüşüm tesisi için) sürekli duman çıkarır, havayı kirletir ve dünyayı ısıtır.
- **Nehir**, denize doğru akan çöplerle doludur. Su bulanık ve kahverengidir, balıklar yok olmuştur.
- **Zemin sararmış**, ormandaki ve kasabadaki **ağaçlar kurumuştur**.
- Hava kirliliği, sıcaklık ve su kirliliği **%50** seviyesinden başlar: durum kötü ama henüz geri döndürülebilir.

Oyuncunun görevi bu bölgeyi adım adım iyileştirmek, fosil yakıtları temiz enerjiyle değiştirmek ve doğanın
geri dönüşüne tanık olmaktır. Başlangıçtaki sinematik bu hikâyeyi anlatır; oyun bittiğinde dünya gözle görülür
şekilde değişmiştir.

---

## 2. Amaç: Nasıl Kazanılır, Nasıl Kaybedilir?

### Kazanmak
**6 yapının tamamı** (2 güneş paneli, 2 rüzgâr türbini, 1 su değirmeni, 1 nehir filesi) inşa edildiğinde doğa
kendi kendine iyileşmeye başlar. **Hava kirliliği, sıcaklık ve su kirliliğinin üçü de %15'in altına** indiğinde
oyun kazanılır: *"Tebrikler! Doğayı kurtardın. Babamız artık gelip burada yaşayabilir."*

Yapılar tamamlandıkça bağlı oldukları **jeneratörler kapanır**, dumanları durur ve kirlilik kaynakları ortadan kalkar.
Çöp toplamak, ağaç sulamak ve bahçeyi gübrelemek değerleri düşürür ve dünyayı yaşanabilir tutar.

### Kaybetmek
**Hava kirliliği** veya **sıcaklık** %100'e ulaşırsa dünya yaşanamaz hale gelir ve oyun kaybedilir.

### Oyun bittiğinde
Hem kazanma hem kaybetme ekranında iki seçenek vardır:
- **Devam Et:** Aynı dünyada oynamaya devam edersin. Kazandıysan iyileşmiş dünyayı gezebilirsin; kaybettiysen görevleri tamamlayıp yine kazanabilirsin.
- **Yeni Oyun:** Oyun baştan başlar.

---

## 3. Oynanış Döngüsü

```
  Çöp topla ──► Geri dönüşüm makinesine götür ──► İklim sorusunu cevapla
      ▲                                                  │
      │                                                  ▼
  Dünya iyileşir ◄── Jeneratörler kapanır ◄── Malzeme torbalarıyla inşa et
      │
      └── Ağaçları sula, bahçeyi gübrele, nehri temizle
```

1. **Topla:** Yerdeki ve nehirdeki çöpleri topla (metal, plastik, cam, organik).
2. **Dönüştür:** Çöpleri geri dönüşüm makinesine yükle, butona bas, soruyu cevapla. Makine her türü renkli bir malzeme torbasına çevirir.
3. **İnşa et:** Torbaları inşaat alanlarına götür. Yapı tamamlanınca bağlı jeneratör kapanır, kirlilik ve sıcaklık düşer.
4. **Bakım yap:** Arıtma cihazından su doldur, kuruyan ağaçları ve bahçeyi sula. Organik torbaları bahçeye gübre olarak koy.
5. **İyileşmeyi izle:** Hava temizlendikçe sis kalkar, su berraklaşır, ağaçlar yeşerir, balıklar ve hayvanlar geri gelir.

---

## 4. Mekanikler

### 4.1 Çöp ve Çanta
- Çöpler dört türdür: **Metal**, **Plastik**, **Cam**, **Organik**. Her biri gerçekçi modellerle temsil edilir (kola kutusu, şarap şişesi, pizza artığı, muz vb.).
- Çanta kapasitesi **20**'dir. **Tab** ile çanta penceresi açılır: her çöp küçük bir resim ve türüyle listelenir.
- Çanta penceresinden bir çöpü **pencerenin dışına sürükleyip bırakmak**, onu yere atar; malzeme torbaları da aynı şekilde yere bırakılarak çantada yer açılabilir ve sonra **E** ile geri alınabilir. **Q** tüm çöpleri yere boşaltır.
- Yerde kalan çöpler **metan saldığı** için dünyayı yavaşça ısıtır.

### 4.2 Geri Dönüşüm Tesisi
- Fabrika görünümlü tesiste giriş bandı, makine, buton ve çıkış bandı bulunur.
- Çöpler banda yüklenir, buton yeşile döner. Butona basınca **tek bir iklim sorusu** sorulur (tüm yük için).
  - **Doğru cevap:** Çöpler türlerine göre **renkli malzeme torbalarına** paketlenir.
  - **Yanlış cevap:** Çöpler olduğu gibi geri verilir.
- Soru ekranı **E** veya sağ üstteki **X** ile cevaplamadan kapatılabilir; çöpler makinede bekler.
- Çalışırken makine sesi duyulur, torbalar çıkış bandından akar.

### 4.3 İnşa Sistemi
Her inşaat alanında yapının **hologramı** görünür; gerekli torbalar getirildikçe ilerleme çubuğu dolar.

| Yapı | Adet | Gereken malzeme | Etki |
|---|---|---|---|
| Güneş Paneli | 2 | 3 cam + 2 metal | Hava kirliliği −8, sıcaklık −5 |
| Rüzgâr Türbini | 2 | 3 metal + 2 plastik | Hava kirliliği −8, sıcaklık −5 |
| Su Değirmeni | 1 | 3 metal + 3 plastik | Hava kirliliği −12, sıcaklık −8 |
| Nehir Filesi | 1 | 6 plastik | Nehir çöplerini yakalar |

- Yapılar tamamlanınca **geri dönüştürülmüş plastik** dokusuyla görünür; türbin pervaneleri ve değirmen çarkı döner.
- Her jeneratör, kendisine bağlı yapılar tamamlanınca **kapanır**.
- **G** tuşu hologramları gizler/gösterir.

### 4.4 Su, Sulama ve Bahçe
- **Su arıtma cihazı** deniz suyunu arıtır; su kabı (5 birim) burada doldurulur.
- Doldurma ve sulama **E'yi basılı tutarak** yapılır: ses çalarken bir çubuk dolar. Erken bırakılırsa işlem iptal olur ve ses kesilir.
- Ağaçlar zamanla susar; sıcaklık arttıkça daha hızlı kururlar. Kuru ağaçlar havayı kirletir.
- **Bahçe:** 27 toprak yatağı vardır. Organik torbalarla **gübrelenip sulanınca çiçek açar**, arılar gelir ve sıcaklık düşer.

### 4.5 Nehir ve Balıklar
- Nehirde çöpler akıntıyla denize sürüklenir. Denize ulaşan her çöp **suyu kirletir**.
- **Su ne kadar kirliyse nehirde o kadar çok çöp** akar (temiz suda 3, tamamen kirli suda 12'ye kadar).
- Çöpler yüzerken toplanabilir. **Nehir filesi** kurulduğunda yeni çöp gelmez; yüzen çöpler fileye takılır ve önünde sıralanır.
- **Zıplayan balıklar:** Su temizlendikçe nehirden balıklar sıçramaya başlar. Su kirliliği %40'ın üstündeyse hiç balık yoktur, %15'in altında 8 balığın hepsi görünür.

### 4.6 Hayvanlar ve Kedi
- Kasabada **geyik, tilki ve civciv** gezer; köprüden geçip yalaktan su içen hayvanlar gelir.
- Kirlilik arttıkça hayvanlar yavaşlar, çok yüksek kirlilikte **ölürler**. Ölüm kalıcıdır: hava sonradan temizlense de geri gelmezler ve sevilemezler.
- Oyuncunun **kendi kedisi** onu her yerde takip eder. Kedi de ölümsüz değildir: hava kirliliği en üst seviyeye (%90) çıkarsa o da kalıcı olarak ölür.
- **F** ile herhangi bir hayvanı (kedi dahil) **kucağa alabilirsin**: hayvan ekranın ortasında yüzü sana dönük durur, arayüz geçici olarak gizlenir. Tekrar **F** ile yere bırakılır.

### 4.7 Kamera
- **Yukarıdan (top-down) kamera:** Denize bakan, açılı bir kuş bakışı.
- **Birinci şahıs kamera:** Fare ile etrafa bakılır; karakterin gövdesi gizlenir, sadece gölgesi kalır.
- **V** ile iki kamera arasında geçiş yapılır.

---

## 5. Çevre ve Görsel Geri Bildirim

Oyundaki üç ana metrik dünyanın görünüşünü doğrudan değiştirir:

| Metrik | Görsel etkisi |
|---|---|
| **Hava kirliliği** | Sis yoğunlaşır, hayvanlar yavaşlar ve yığılır, ortam müziği kararır, kuşlar susar |
| **Sıcaklık** | Zemin ve çimler sararır, orman ağaçları kurur, kutup buzulları küçülür, sulanan ağaçlar daha hızlı susar |
| **Su kirliliği** | Nehir ve deniz bulanıklaşıp kahverengiye döner, balıklar kaybolur, nehirdeki çöp miktarı artar |

Ek olarak:
- Jeneratör bacalarından ve evin bacasından **duman** çıkar; yapılar tamamlanınca duman durur.
- **Gökyüzü:** Panoramik bir skybox kullanılır.
- Evin duvarında **REWIND** ile hackathon destekçilerinin (HP, ICPSD, UNDP) logoları bulunur.

---

## 6. Eğitici Yön: İklim Soruları

Geri dönüşüm makinesi her çalıştığında aşağıdaki gibi sorular sorulur (7 soru, tekrar etmeden karışık sırayla):

- Bir cam şişenin doğada çözünmesi yaklaşık ne kadar sürer?
- Plastik bir şişenin doğada çözünmesi yaklaşık ne kadar sürer?
- Geri dönüştürülen bir alüminyum kutu ne kadar sürede yeniden rafa döner?
- Organik atıklar çöplükte çürürken hangi sera gazını salar?
- Aşağıdakilerden hangisi yenilenebilir enerji kaynağıdır?
- Dünyadaki suyun yaklaşık ne kadarı içilebilir tatlı sudur?
- Deniz buzu eridiğinde hangi hayvanın avlanma alanı daralır?

Doğru cevap verildiğinde cevap yeşile döner ve bir ses çalar; yanlışta kırmızıya döner (doğru cevap açık edilmez).
Ayrıca oyundaki her nesnenin bilgi panelinde, o nesnenin çevreye etkisini anlatan kısa açıklamalar vardır
(ör. "Yerde kaldıkça dünyayı ısıtıyor", "Çürüyor ve metan salıyor").

---

## 7. Kontroller

| Tuş | İşlev |
|---|---|
| **W A S D** | Hareket |
| **Shift** | Koş |
| **Space** | Zıpla |
| **E** | Etkileşim (topla, yükle, inşa et, soru ekranını kapat) |
| **E (basılı tut)** | Su doldur / sula |
| **F** | Hayvanı kucağa al / bırak |
| **Tab** | Çanta penceresi |
| **Q** | Çantadaki tüm çöpleri yere boşalt |
| **V** | Kamera değiştir (yukarıdan / birinci şahıs) |
| **G** | Hologramları gizle / göster |
| **H** | Arayüzü gizle (ekran görüntüsü için) |
| **T** | Tuş rehberini aç / kapat |
| **Esc** | Duraklat (Devam Et / Ana Menüye Dön) |
| **Fare** | Birinci şahısta etrafa bakma, menülerde tıklama |

---

## 8. Arayüz (HUD)

- **Sol alt:** Hava kirliliği, sıcaklık ve su kirliliği çubukları; çanta doluluğu ve su kabı.
- **Sol üst:** Toplanan eşyalar için kısa bildirimler ("+1 Kola kutusu"); birkaç saniye sonra kaybolur.
- **Orta alt:** Yaklaşılan nesnenin bilgi paneli ve **[E]** etkileşim yazısı; basılı tutma işlemlerinde ilerleme çubuğu.
- **Ekranda oklar:** Görev noktalarını gösterir (sarı: inşaat ve bahçe, mavi: sulanacak ağaçlar, camgöbeği: arıtma, yeşil: geri dönüşüm).
- **Sağ alt:** "[T] Tuş rehberi" kutusu; T ile tam tuş listesi açılır.
- **Ortada:** Birinci şahısta küçük, yuvarlak bir nişan noktası.
- **Ana menü:** Logo, "Oyuna Başla" ve "Oyundan Çık" butonları; altta destekçi logoları.

---

## 9. Ses ve Sinematik

- **Giriş sinematiği:** "Oyuna Başla"dan sonra oynar; sağ alttaki "Sinematiği geç [E]" butonuyla (birkaç saniye görünür) veya E ile geçilebilir. Video bitince oyun dünyası siyahtan yavaşça açılır.
- **Dinamik ortam sesi:** Hava temizken aydınlık müzik ve kuş cıvıltıları, kirlendikçe karanlık bir ortam sesi; geçişler yumuşaktır.
- **Nehir ve deniz sesi**, **yürüme sesi**, **3D jeneratör uğultusu** (jeneratör kapanınca susar) ve **geri dönüşüm makinesi sesi**.
- Toplama, inşa, sulama, su doldurma, doğru cevap, kazanma ve kaybetme için ayrı efekt sesleri.

---

## 10. Teknik Altyapı

| | |
|---|---|
| **Motor** | Unity **6000.3.6f1** (Unity 6) |
| **Render** | Universal Render Pipeline (URP) |
| **Girdi** | Yeni Input System |
| **Dil** | C# (namespace `EcoGame`) |
| **Platform** | Windows (64-bit) |

**Mimari öne çıkanlar:**
- **ScriptableObject tabanlı veri:** Denge değerleri (`SO_EnvironmentSettings`), oyuncu ayarları, yapılar, çöp türleri ve quiz soruları kod değiştirmeden düzenlenebilir.
- **Merkezi sistemler:** `GameManager` (oyun durumu, duraklatma), `EnvironmentManager` (metrikler ve kirlilik kaynakları), `ObjectiveManager` (görevler), `UIManager` (arayüz), `AudioManager` (tüm sesler).
- **Arayüz tabanlı tasarım:** Kirleten her şey `IPollutionSource`, bilgi gösteren her şey `IInspectable`, etkileşime girilen her şey `Interactable` sınıfından türer. Yeni bir kirletici eklemek tek bir sınıf yazmak kadar kolaydır.
- **Fiziksiz nehir akışı:** Nehir çöpleri ve balıklar yol noktaları üzerinde hareket eder; performanslı ve öngörülebilirdir.
- **Özel shader:** Geri dönüştürülmüş plastik dokusu için nesne uzayında triplanar shader (dönen parçalarda doku kaymaz).
- **Humanoid animasyon:** Karakter için idle, koşma ve zıplama animasyonları.

---

## 11. Oyunu Çalıştırma

### Hazır oyun (jüri için)
1. `REWIND.zip` dosyasını bir klasöre çıkar.
2. `REWIND.exe`'yi çalıştır. Kurulum gerekmez.
3. Windows "bilinmeyen yayıncı" uyarısı verirse **Ek bilgi → Yine de çalıştır**.

> Klasördeki tüm dosyalar gereklidir; sadece exe'yi kopyalamak yetmez.

### Unity projesini açmak
1. Unity Hub'dan **Unity 6000.3.6f1** sürümünü kur.
2. Repoyu indir:
   ```bash
   git clone https://github.com/eneskaya5753-svg/Rewind.git
   ```
3. Unity Hub → **Add → Add project from disk** → indirilen klasör.
4. `Assets/_Project/Scenes/Main.unity` sahnesini aç ve **Play**'e bas. (İlk açılış birkaç dakika sürebilir.)

### Build almak
**File → Build Profiles → Windows → Build**. Çıktıyı `Builds/` klasörüne almak önerilir (bu klasör repoya dahil değildir).

---

## 12. Denge ve Geliştirici Modu

Tüm denge değerleri `Assets/_Project/Data/Settings/SO_EnvironmentSettings.asset` dosyasındadır.

**Geliştirici (test) modu** sunum ve jüri değerlendirmesi için **açıktır**; oyun böylece birkaç dakikada tamamlanabilir:
- Her yapı, her malzemeden yalnızca **1** torba ister; bahçe için **1** gübre torbası yeter.
- Haritada her türden en fazla **10** çöp çıkar.

Kapatmak için `SO_EnvironmentSettings` → **Test Mode** işaretini kaldırın; bu durumda yukarıdaki tablo maliyetleri ve haritadaki tüm çöpler (24 metal, 24 plastik, 14 cam, 8 organik) devreye girer.

| Değer | Ayar |
|---|---|
| Başlangıç kirliliği / sıcaklık / su kirliliği | %50 / %50 / %50 |
| Kaybetme sınırı | %100 (hava kirliliği veya sıcaklık) |
| Hiçbir şey yapılmazsa kaybetme süresi | Yaklaşık 3,5–4 dakika |

---

## 13. Proje Klasör Yapısı

```
Assets/
  _Project/
    Art/            Malzemeler, shader'lar, dokular, UI görselleri, logolar
    Audio/          Ses klasörü (oyundaki sesler _ThirdParty/sounds altında)
    Data/           ScriptableObject verileri (ayarlar, yapılar, çöpler, quiz)
    Prefabs/        Oyuncu, hayvanlar, makineler, inşaat alanları, çöpler
    Scenes/         Main.unity
    Settings/       URP ve render ayarları
    Scripts/
      Animals/      Gezen hayvanlar, kedi, kucağa alma, balıklar, arılar
      Audio/        AudioManager
      Building/     İnşaat alanları
      Camera/       Kamera modları
      Core/         GameManager, ObjectiveManager
      Data/         ScriptableObject sınıfları
      Environment/  Metrikler ve görsel kontrolcüler (sis, su rengi, kuraklık...)
      Interaction/  Etkileşim ve inceleme altyapısı
      Machines/     Geri dönüşüm tesisi, jeneratörler, su arıtma
      Player/       Hareket, çanta, etkileşim
      Quiz/         Soru seçimi (QuizManager)
      Trash/        Yer ve nehir çöpleri
      UI/           HUD, çanta penceresi, quiz, menüler, sinematik
      Utils/        Bildirimler, nesne hareketi, yardımcılar
      Water/        Sulanabilir ağaçlar ve bahçe
  _ThirdParty/      Hazır varlık paketleri, sesler, sinematik video, logolar
Docs/               Proje mimarisi dokümanı
```

---

## 14. Kullanılan Varlıklar ve Teşekkür

- **3D modeller ve UI:** [Kenney](https://kenney.nl) paketleri (CC0): City Kit, Nature Kit, Survival Kit, Factory Kit, Food Kit, Fantasy Town Kit, Cube Pets, Graveyard Kit, UI Pack (RPG Expansion).
- **Oyuncu karakteri:** Kenney Animated Characters – Survivors.
- **Logolar:** HP, UNDP ve ICPSD logoları, hackathon kapsamında destekçi olarak kullanılmıştır.
- **Sesler, müzik ve sinematik video:** Takım tarafından projeye eklenmiştir.

---

## 15. Sunum İçin Öne Çıkan Noktalar

1. **Gerçek çözümler, oyunlaştırılmış:** Atık ayrıştırma, geri dönüşüm, güneş/rüzgâr/hidroelektrik enerjisi, su arıtma ve kompost (gübre) oyunun temel mekanikleri.
2. **Neden-sonuç ilişkisi görünür:** Oyuncunun her eylemi metriklere yansır ve dünya buna anında tepki verir: sis kalkar, su berraklaşır, ağaçlar yeşerir, balıklar geri döner.
3. **Öğrenerek ilerleme:** Geri dönüşümün başarısı iklim sorusuna verilen cevaba bağlı; bilgi doğrudan oyun ilerlemesine dönüşüyor.
4. **Fosil yakıttan temiz enerjiye geçiş:** Jeneratörlerin tek tek kapanması, enerji dönüşümünü somut bir hedefe çeviriyor.
5. **Zincirleme etkiler:** Kirli su nehre daha çok çöp getirir, sıcaklık ağaçları daha hızlı kurutur, kuru ağaçlar havayı kirletir; sistemler birbirine bağlı.
6. **Duygusal bağ:** Takip eden kedi, kucağa alınabilen hayvanlar ve kirlilik yüzünden kalıcı olarak ölebilen canlılar: geç kalmanın bir bedeli var.
7. **Erişilebilir sunum:** Kısa sürede tamamlanabilen geliştirici modu, tuş rehberi, sinematik giriş ve "Devam Et" ile bitişten sonra da keşif.
8. **Genişletilebilir altyapı:** ScriptableObject verileri sayesinde yeni sorular, yapılar ve çöp türleri kod yazmadan eklenebilir.
