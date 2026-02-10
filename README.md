# PropHunt - CS2 Plugin

Counter-Strike 2 icin **CounterStrikeSharp v1.0.362** ile yazilmis Prop Hunt oyun modu plugini.

Saklananlar haritadaki proplara donuserek gizlenir, arayicilar onlari bulup vurmaya calisir.

---

## Ozellikler

- **Acma/Kapama Sistemi** - Admin komutu ile (`!prophunt`) mod acilir/kapanir, her zaman calismaz
- **Prop Secimi** - Chat menusu veya rastgele atama ile prop modeli sec
- **Saklanma Fazi** - Ayarlanabilir sure boyunca arayicilar dondurulur, saklananlar gizlenir
- **Prop Hasar Sistemi** - Arayicilar prop'u vurursa saklanan oyuncu olur
- **Boyut-Can Sistemi** - Kucuk/Orta/Buyuk proplar farkli cana sahip (30/100/300)
- **Oransal Can Olcekleme** - Model degistirince can yuzdesi korunur
- **CheckTransmit Gizleme** - Oyuncu pawn'i istemciye hic gonderilmez (gercek gorunmezlik)
- **Ses Gizleme** - Saklanan oyuncunun adim/hareket sesleri diger oyunculara iletilmez
- **Taunt Sesi** - Sol tik veya `!taunt` ile ses calarak arayicilara ipucu ver
- **Tus Atamalari** - Sol tik, sag tik, E, R tuslarina aksiyon atanabilir
- **Dondurma** - Saklananlar kendini dondurarak sabit prop gibi davranabilir
- **Decoy Sistemi** - Sahte prop birakarak arayicilari yanilt
- **Model Degistirme** - Sinirli sayida model degistirme hakki
- **3. Sahis Gorunum** - Saklananlar kendini gorebilir
- **Islik** - `!whistle` ile arayicilara sesli ipucu ver
- **Takim Karistirma** - Her round oncesi takimlar otomatik karistirilir
- **Harita Bazli Modeller** - 10 harita icin 25-30 model/harita
- **Otomatik Model Kesfi** - Haritadaki fizik proplari otomatik eklenir
- **Son Saklanan Bildirimi** - Son kalan saklanan oyuncu herkes tarafindan bildirilir
- **Arayici Miss Cezasi** - Bos propa ates eden arayici HP kaybeder

---

## Gereksinimler

- **CS2 Dedicated Server** (SteamCMD ile kurulmus)
- **.NET 8.0 SDK** (derleme icin)
- **MetaMod:Source** (2.x son surum)
- **CounterStrikeSharp** v1.0.362+

---

## Detayli Kurulum

### Adim 1: CS2 Dedicated Server Kurulumu

Eger henuz bir CS2 sunucunuz yoksa SteamCMD ile kurun:

```
steamcmd +force_install_dir /home/cs2server +login anonymous +app_update 730 validate +quit
```

Windows icin SteamCMD'yi `https://developer.valvesoftware.com/wiki/SteamCMD` adresinden indirin.

### Adim 2: MetaMod:Source Kurulumu

1. `https://www.sourcemm.net/downloads.php?branch=master` adresinden **MetaMod:Source 2.x** son surumunu indirin
2. Platform olarak **CS2 / Counter-Strike 2** secin
3. Indirilen arsivi acin
4. `addons/` klasorunu sunucunuzdaki `csgo/` klasorunun icine kopyalayin

```
csgo/
  addons/
    metamod/
      bin/
        ...
    metamod.vdf
```

5. `csgo/gameinfo.gi` dosyasina MetaMod satirini ekleyin (otomatik eklenmiyorsa):
```
Game    csgo/addons/metamod
```

Bu satiri `SearchPaths` blogu icindeki diger `Game` satirlarinin ustune ekleyin.

### Adim 3: CounterStrikeSharp Kurulumu

1. `https://docs.cssharp.dev/docs/guides/getting-started.html` adresinden son surumu indirin
   veya dogrudan GitHub Releases sayfasindan:
   `https://github.com/roflmuffin/CounterStrikeSharp/releases`
2. **v1.0.362** veya daha yeni bir surum secin
3. Platformunuza uygun arsivi indirin:
   - `counterstrikesharp-with-runtime-build-XXX-linux-XXX.zip` (Linux)
   - `counterstrikesharp-with-runtime-build-XXX-windows-XXX.zip` (Windows)
4. Arsivi acin ve icindeki `addons/` klasorunu sunucunuzdaki `csgo/` klasorunun icine kopyalayin

```
csgo/
  addons/
    counterstrikesharp/
      api/
      bin/
      configs/
      dotnet/
      gamedata/
      plugins/          <-- pluginler buraya gelecek
      shared/
      counterstrikesharp.dll
    metamod/
```

5. Dogrulama: Sunucuyu baslatin ve konsolda `meta list` yazin. CounterStrikeSharp gorunuyorsa kurulum basarili.

### Adim 4: Plugin Derleme

Bu repo'yu klonlayin ve derleyin. **.NET 8.0 SDK** gereklidir (`https://dotnet.microsoft.com/download/dotnet/8.0`).

```bash
git clone https://github.com/guccukCENEVAR/prophunt-plugin.git
cd prophunt-plugin
dotnet restore
dotnet build -c Release
```

Basarili derleme sonucunda cikti dosyasi:
```
bin/Release/net8.0/PropHunt.dll
```

### Adim 5: Plugin Yukleme

1. Sunucunuzda plugin klasoru olusturun:
```
csgo/addons/counterstrikesharp/plugins/PropHunt/
```

2. Derlenen `PropHunt.dll` dosyasini bu klasore kopyalayin

3. Repo'daki `models/` klasorunu de ayni yere kopyalayin

Son hali:
```
csgo/addons/counterstrikesharp/plugins/PropHunt/
  PropHunt.dll
  models/
    de_mirage.txt
    de_inferno.txt
    de_dust2.txt
    de_nuke.txt
    de_overpass.txt
    de_ancient.txt
    de_anubis.txt
    de_vertigo.txt
    cs_office.txt
    cs_italy.txt
```

### Adim 6: Sunucuyu Baslat ve Yapilandir

1. Sunucuyu yeniden baslatin
2. Ilk calistirmada config dosyasi otomatik olusur:
```
csgo/addons/counterstrikesharp/configs/plugins/PropHunt/PropHunt.json
```
3. Config dosyasini ihtiyaciniza gore duzenleyin (ayarlar icin asagidaki Yapilandirma bolumune bakin)

### Adim 7: Modu Ac

Sunucu icinde su yontemlerden biriyle modu acin:

**Yontem 1 - Oyun ici chat (admin yetkisi gerektirir):**
```
!prophunt
```

**Yontem 2 - Sunucu konsol:**
```
sv_prophunt 1
```

**Yontem 3 - Otomatik baslama (server.cfg):**
`csgo/cfg/server.cfg` dosyasina ekleyin:
```
sv_prophunt 1
```

**Yontem 4 - Config ile otomatik baslama:**
`PropHunt.json` config dosyasinda:
```json
"EnabledByDefault": true
```

### Onerilen Sunucu Ayarlari

`server.cfg` dosyaniza su satirlari eklemeniz onerilir:

```
// PropHunt icin onerilen ayarlar
sv_prophunt 1
mp_roundtime 5
mp_freezetime 0
mp_warmuptime 15
mp_maxrounds 20
mp_autoteambalance 0
mp_limitteams 0
mp_give_player_c4 0
mp_death_drop_gun 0
mp_death_drop_grenade 0
sv_disable_radar 1
```

### Sorun Giderme

| Sorun | Cozum |
|-------|-------|
| Plugin yuklenmedi | Konsolda `css_plugins list` yazin. Gorunmuyorsa DLL yolunu kontrol edin |
| `meta list` bos | MetaMod kurulumunu ve `gameinfo.gi` dosyasini kontrol edin |
| Config olusmuyor | Plugin klasorunun dogru yerde oldugunu kontrol edin |
| Modeller yuklenmedi | `models/` klasorunun `PropHunt.dll` ile ayni yerde oldugunu kontrol edin |
| Admin komutlari calismadi | Oyuncunun `@css/rcon` yetkisine sahip oldugunu kontrol edin |
| Derleme hatasi | .NET 8.0 SDK kurulu mu kontrol edin: `dotnet --version` |
| Hasar sistemi calismadi | Config'de `PropDamageKill` degerinin `true` oldugunu kontrol edin |
| Ses gizleme calismadi | CounterStrikeSharp surumunun v1.0.362+ oldugunu kontrol edin |

### Admin Yetkisi Verme

CounterStrikeSharp'ta admin yetkisi vermek icin:
`csgo/addons/counterstrikesharp/configs/admins.json` dosyasina ekleyin:

```json
{
  "AdminIsmi": {
    "identity": "76561198XXXXXXXXX",
    "flags": ["@css/rcon"]
  }
}
```

`identity` degeri oyuncunun **SteamID64** numarasidir. `https://steamid.io` adresinden bulabilirsiniz.

---

## Mod Nasil Calisir?

1. Admin `!prophunt` veya `sv_prophunt 1` ile modu acar
2. Round basladiginda takimlar otomatik karistirilir
3. **Saklananlar** gorunmez olur ve rastgele bir prop modeli atanir
4. **Saklanma fazi** baslar (varsayilan 60 saniye) - arayicilar dondurulur
5. Saklananlar prop modelini secer, gizlenecek yer bulur
6. Sure dolunca arayicilara silah verilir ve av baslar
7. Arayicilar proplari vurarak saklananlari oldurur
8. Tum saklananlar olurse arayicilar, sure bitene kadar saklananlar hayatta kalirsa saklananlar kazanir

---

## Prop Boyut-Can Sistemi

Proplar buyukluklerine gore farkli cana sahiptir:

| Boyut | Varsayilan HP | Ornek Proplar |
|-------|:------------:|---------------|
| **Kucuk** | 30 | Sise, kutu, ayakkabi, top, seramik |
| **Orta** | 100 | Kasa, varil, sandalye, monitor, cop kutusu |
| **Buyuk** | 300 | Kapi, koltuk, kitaplik, otomat, masa |

Model degistirdiginde can yuzdesi oransal olarak korunur:
- Buyuk prop (300 HP) ile basladin, 150 HP'ye dustun (%50)
- Orta propa swap yaptin -> 50 HP (%50 korunur)
- Kucuk propa swap yaptin -> 15 HP (%50 korunur)

---

## Admin Komutlari

| Komut | Yetki | Aciklama |
|-------|-------|----------|
| `!prophunt` / `!ph` | `@css/rcon` | PropHunt modunu ac/kapat (toggle) |
| `!prophunt_enable` | `@css/rcon` | PropHunt modunu ac |
| `!prophunt_disable` | `@css/rcon` | PropHunt modunu kapat |
| `sv_prophunt 1` | Sunucu konsol | Konsoldan ac |
| `sv_prophunt 0` | Sunucu konsol | Konsoldan kapat |
| `sv_prophunt` | Sunucu konsol | Mevcut durumu goster |

> `server.cfg` dosyaniza `sv_prophunt 1` ekleyerek sunucu basladiginda otomatik acilmasini saglayabilirsiniz.

---

## Oyuncu Komutlari (Saklananlar)

| Komut | Aciklama |
|-------|----------|
| `!prop` / `!props` / `!model` | Prop secim menusu |
| `!swap` | Rastgele model degistir |
| `!freeze` / `!don` | Dondur / coz |
| `!decoy` | Sahte prop yerlestir |
| `!tp` / `!thirdperson` | 3. sahis gorunum |
| `!taunt` | Taunt sesi cal |
| `!whistle` | Islik cal |

---

## Tus Atamalari (Saklananlar)

Saklanan oyuncular silah tasimadigi icin tuslar aksiyonlara atanmistir:

| Tus | Varsayilan Aksiyon | Config Anahtari |
|-----|--------------------|-----------------|
| Sol Tik | Taunt sesi cal | `KeyTaunt` |
| Sag Tik | Model degistir | `KeySwap` |
| E (Use) | Dondur / Coz | `KeyFreeze` |
| R (Reload) | Decoy yerlestir | `KeyDecoy` |

Tus atamalarini config'den degistirebilir veya `"None"` yaparak devre disi birakabilirsiniz.

Kullanilabilir degerler: `Attack`, `Attack2`, `Use`, `Reload`, `None`

---

## Yapilandirma

Ilk calistirmadan sonra config dosyasi olusur:
`csgo/addons/counterstrikesharp/configs/plugins/PropHunt/PropHunt.json`

```json
{
  "EnabledByDefault": false,
  "Prefix": "{lightblue}[PropHunt]",
  "HidingTeam": "CT",
  "HideTime": 60,
  "TeamScramble": true,
  "MinPlayers": 2,
  "PropHealthSmall": 30,
  "PropHealthMedium": 100,
  "PropHealthLarge": 300,
  "SeekerHealth": 150,
  "SwapLimit": 3,
  "DecoyLimit": 2,
  "WhistleLimit": 5,
  "WhistleCooldown": 10,
  "TauntLimit": 5,
  "TauntCooldown": 15,
  "TauntSounds": [
    "sounds/ambient/animal/bird15.vsnd",
    "sounds/ambient/animal/bird14.vsnd",
    "sounds/ambient/animal/bird13.vsnd"
  ],
  "PropDamageKill": true,
  "SeekerDamagePerMiss": 5,
  "SeekerWeapons": [
    "weapon_knife",
    "weapon_p90",
    "weapon_deagle"
  ],
  "DefaultModels": [
    "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl",
    "models/props/de_inferno/claypot03.vmdl",
    "models/props/cs_office/trash_can.vmdl"
  ],
  "KeyTaunt": "Attack",
  "KeySwap": "Attack2",
  "KeyFreeze": "Use",
  "KeyDecoy": "Reload"
}
```

### Ayarlar Tablosu

| Ayar | Varsayilan | Aciklama |
|------|-----------|----------|
| `EnabledByDefault` | `false` | Sunucu basladiginda mod acik mi? |
| `Prefix` | `{lightblue}[PropHunt]` | Chat mesaj oneki |
| `HidingTeam` | `CT` | Saklanan takim (`T` veya `CT`) |
| `HideTime` | `60` | Saklanma suresi (saniye) |
| `TeamScramble` | `true` | Round oncesi takimlari karistir |
| `MinPlayers` | `2` | Minimum oyuncu sayisi |
| `PropHealthSmall` | `30` | Kucuk prop cani (sise, kutu, top...) |
| `PropHealthMedium` | `100` | Orta prop cani (kasa, varil, sandalye...) |
| `PropHealthLarge` | `300` | Buyuk prop cani (koltuk, kapi, kitaplik...) |
| `SeekerHealth` | `150` | Arayici cani |
| `SwapLimit` | `3` | Model degistirme limiti (saklanma fazinda sinirsiz) |
| `DecoyLimit` | `2` | Sahte prop limiti |
| `WhistleLimit` | `5` | Islik limiti |
| `WhistleCooldown` | `10` | Islik bekleme suresi (saniye) |
| `TauntLimit` | `5` | Taunt limiti |
| `TauntCooldown` | `15` | Taunt bekleme suresi (saniye) |
| `TauntSounds` | `bird15, 14, 13` | Taunt ses dosyalari listesi (.vsnd) |
| `PropDamageKill` | `true` | Prop vurulunca oyuncu olsun mu? |
| `SeekerDamagePerMiss` | `5` | Bos propa ates edince kaybedilen HP |
| `SeekerWeapons` | `knife, p90, deagle` | Arayicilara verilecek silahlar |
| `DefaultModels` | `(40 model)` | Harita dosyasi yoksa kullanilacak modeller |
| `KeyTaunt` | `Attack` | Taunt tusu |
| `KeySwap` | `Attack2` | Swap tusu |
| `KeyFreeze` | `Use` | Freeze tusu |
| `KeyDecoy` | `Reload` | Decoy tusu |

---

## Harita Modelleri

Her harita icin ozel model listesi tanimlanabilir. Plugin klasorunde `models/` klasoru olusturun:

```
plugins/PropHunt/
  PropHunt.dll
  models/
    de_mirage.txt
    de_inferno.txt
    de_dust2.txt
    de_nuke.txt
    de_overpass.txt
    de_ancient.txt
    de_anubis.txt
    de_vertigo.txt
    cs_office.txt
    cs_italy.txt
```

Her `.txt` dosyasina satirda bir model yolu yazin:

```
# Bu bir yorum satiridir
models/props/de_inferno/claypot03.vmdl
models/props/cs_office/trash_can.vmdl
models/props/de_dust/hr_dust/dust_food_crate/dust_food_crate001.vmdl
```

JSON formati da desteklenir (`harita.json`):
```json
{
  "Claypot": "models/props/de_inferno/claypot03.vmdl",
  "Trash Can": "models/props/cs_office/trash_can.vmdl"
}
```

**Oncelik sirasi:**
1. `models/harita_adi.txt` dosyasi varsa kullanilir
2. Yoksa `models/harita_adi.json` dosyasi kontrol edilir
3. Hicbiri yoksa config'deki `DefaultModels` listesi kullanilir
4. Ek olarak haritadaki fizik proplari otomatik kesfedilir ve listeye eklenir

---

## Teknik Detaylar

| Ozellik | Uygulama |
|---------|----------|
| Oyuncu Gizleme | `CheckTransmit` listener - pawn verisi istemciye gonderilmez |
| Ses Gizleme | UserMessage hook (ID 208 - CMsgSosStartSoundEvent) |
| Prop Hasar | `HookEntityOutput("prop_dynamic", "OnTakeDamage")` |
| Prop Boyut | Anahtar kelime tabanli siniflandirma (Small/Medium/Large) |
| Oransal Can | `currentHP / oldMaxHP * newMaxHP` swap sirasinda |
| Oyuncu Dondurma | `MoveType_t.MOVETYPE_OBSOLETE` + Schema set |
| Tus Algilama | `PlayerButtons` flag kontrolu (one-press detection) |
| Taunt Sesi | `point_soundevent` entity olusturma |
| 3. Sahis Kamera | `CDynamicProp` kamera entity + `CameraServices.ViewEntity` |
| Takim Karistirma | Fisher-Yates shuffle |

---

## Dosya Yapisi

```
prophunt/
  PropHunt.csproj           # Proje dosyasi (CSSharp v1.0.362)
  PropHuntPlugin.cs         # Ana plugin, state, helper metodlar
  Events.cs                 # Round, spawn, olum, tick, CheckTransmit, ses hook
  Commands.cs               # Admin + oyuncu komutlari, prop menusu
  Config.cs                 # Yapilandirma sinifi
  PlayerPropData.cs         # Oyuncu prop veri modeli
  Utils.cs                  # Yardimci fonksiyonlar (boyut siniflandirma dahil)
  models/                   # Harita bazli model listeleri (10 harita)
    de_mirage.txt
    de_inferno.txt
    de_dust2.txt
    de_nuke.txt
    de_overpass.txt
    de_ancient.txt
    de_anubis.txt
    de_vertigo.txt
    cs_office.txt
    cs_italy.txt
```

---

## Derleme

```
dotnet restore
dotnet build -c Release
```

Cikti: `bin/Release/net8.0/PropHunt.dll`

---

## Lisans

Bu proje acik kaynaktir.
