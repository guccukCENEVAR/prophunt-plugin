# Taunt sesleri – .vsnd yollari nasil olusur?

Taunt sesleri **sadece .vsnd** formatinda calisir. Iki yol kullanabilirsiniz:

---

## 1. Oyun icindeki hazir sesleri kullanmak (yol yazmak)

CS2’nin kendi ses dosyalarini kullanirsiniz; **siz dosya olusturmazsiniz**, sadece yol yazarsiniz.

- Bu klasordeki **.txt** dosyalarina (ornegin `default.txt`) her satira bir ses yolu yazin:
  ```
  sounds/ambient/animal/bird15.vsnd
  sounds/radio/go.vsnd
  sounds/ui/menu_accept.vsnd
  ```
- Bu yollar oyun VPK’larinda zaten var. Ekstra dosya eklemenize gerek yok.

**Hazir ses yollari ornekleri:**

| Ses | Yol |
|-----|-----|
| Kus | `sounds/ambient/animal/bird15.vsnd` |
| Radyo | `sounds/radio/go.vsnd`, `sounds/radio/negative.vsnd`, `sounds/radio/roger.vsnd` |
| UI | `sounds/ui/menu_accept.vsnd`, `sounds/ui/menu_focus.vsnd` |
| Silah | `sounds/weapons/flashbang/flashbang_explode1.vsnd` |

Daha fazla yol icin oyun `pak01_dir.vpk` icindeki `sounds/` klasorune GCFScape veya Source 2 Viewer ile bakabilirsiniz.

---

## 2. Kendi sesinizi kullanmak (.vsnd dosyasi olusturmak)

“Herhangi ses dosyasi” (mp3, wav vb.) icin once **.vsnd dosyasi uretmeniz** gerekir. CS2 sadece .vsnd (Source 2 ses formatini) calar.

### Adim 1: Sesi .vsnd’ye cevirme

- **Secenek A – Valve araclari:**  
  Source 2 Asset Manager / ses export araclari ile WAV’i .vsnd’ye cevirebilirsiniz (Valve dokumantasyonu/araç gunceldir).

- **Secenek B – Topluluk araclari:**  
  CS2 icin .vsnd ureten araclari (ornegin “cs2 vsnd converter”, “source 2 sound tool”) aratin; genelde WAV alip .vsnd verir.

- **Secenek C – Oyun seslerini kopyalama:**  
  VPK’dan bir .vsnd cikarip yerine kendi sesinizi koymak ileri duzeydir; sadece yol yazmak daha kolaydir.

### Adim 2: .vsnd dosyasini nereye koyacaksiniz?

**Yontem A – Taunt klasorune koymak (otomatik yuklenir):**

- Plugin’in **taunt** klasorune `.vsnd` dosyasini kopyalayin:
  ```
  …/plugins/PropHunt/taunt/benim_sesim.vsnd
  ```
- Plugin haritada bu klasordeki tum `.vsnd` dosyalarini tarar ve precache eder. Ekstra .txt satiri yazmaniza gerek yok.

**Yontem B – csgo/sounds altina koyup yol yazmak:**

- Sunucuda `csgo/sounds/prophunt_taunt/` gibi bir klasor olusturun.
- .vsnd dosyanizi oraya koyun, ornegin: `csgo/sounds/prophunt_taunt/benim_sesim.vsnd`
- Bir .txt dosyasina (ornegin `default.txt`) su satiri ekleyin:
  ```
  sounds/prophunt_taunt/benim_sesim.vsnd
  ```

---

## Ozet

| Ne yapiyorsunuz? | Ne yapmalisiniz? |
|------------------|------------------|
| Oyun icindeki hazir ses | .txt’ye sadece yol yazin (ornegin `sounds/radio/go.vsnd`). |
| Kendi sesiniz (mp3/wav) | Once .vsnd’ye cevirin; sonra ya **taunt/** klasorune .vsnd koyun ya da **csgo/sounds/** altina koyup .txt’ye yolu yazin. |

“.vsnd yollari” ya oyundaki hazir sesin yolu, ya da sizin koydugunuz .vsnd dosyasinin oyundaki yolu (ornegin `sounds/prophunt_taunt/benim_sesim.vsnd`) olur.
