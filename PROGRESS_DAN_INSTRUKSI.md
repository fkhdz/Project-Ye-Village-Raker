# PROGRESS & INSTRUKSI — YE VILLAGE RAKER

> Dokumen panduan kerja lanjutan. Dirangkum dari `README.md`, `DOKUMENTASI_TEKNIS.md`,
> hasil pemindaian `Assets/Scripts`, `Assets/Editor`, `Assets/Scenes`, dan status proyek saat ini.
>
> **Terakhir diperbarui:** 22 November 2026

---

## 1. GAMBARAN GAME

| Item | Keterangan |
|---|---|
| **Judul** | Ye Village Raker |
| **Genre** | Third-person 3D trash sorting / edukasi lingkungan |
| **Engine** | Unity 6 (`6000.3.27f1`) + Universal Render Pipeline (URP) 17.3 |
| **Bahasa** | C# |
| **Platform target** | Windows |
| **Input** | Legacy Input Manager (`Input.*`), Active Input Handling = **Both** |
| **Scene** | `MainMenu` (build index 0), `SampleScene` (build index 1) |
| **Repo** | https://github.com/fkhdz/Project-Ye-Village-Raker |

**Premis:** Pemain berperan sebagai **Kades** yang memungut sampah di desa, memilahnya ke tong
**Organik / Anorganik / B3**, mengumpulkan poin, lalu membelanjakan poin di **Toko Tetua**
untuk upgrade permanen. Game selesai (victory) setelah membeli **Renovasi Balai Desa**.

**Alur:** `MainMenu` → MULAI BERMAIN → `SampleScene` (gameplay) → kumpulkan 1500 poin →
Toko Tetua → Renovasi → Victory.

**Kontrol:** `WASD` gerak · Mouse putar kamera · `E` ambil/buang/buka toko ·
`ESC` pause/tutup toko/keluar saat victory.

---

## 2. STRUKTUR PROYEK

```text
Project-Ye-Village-Raker/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Player/PlayerController.cs
│  │  ├─ Camera/ThirdPersonCamera.cs
│  │  ├─ Trash/TrashInventory.cs
│  │  ├─ Trash/TrashBin.cs
│  │  ├─ Managers/ScoreManager.cs
│  │  ├─ Managers/TrashSpawner.cs
│  │  ├─ Managers/ShopManager.cs
│  │  ├─ Managers/UIManager.cs
│  │  ├─ Managers/GameManager.cs
│  │  ├─ Managers/AudioManager.cs
│  │  ├─ UI/ScoreHUD.cs
│  │  ├─ UI/MainMenu.cs
│  │  ├─ UI/PauseMenu.cs
│  │  └─ VFX/VFXManager.cs
│  ├─ Editor/                 ← alat builder & diagnostik (tidak ikut build)
│  │  ├─ YeVillageSetup.cs
│  │  ├─ TrashPrefabBuilder.cs
│  │  ├─ ShopSetupBuilder.cs
│  │  ├─ Phase7SceneBuilder.cs
│  │  ├─ AudioSetupBuilder.cs
│  │  ├─ Phase9SceneBuilder.cs
│  │  ├─ VFXSetupBuilder.cs
│  │  ├─ QuaterniusIntegration.cs      ← integrasi aset 3D pihak ketiga
│  │  └─ MainMenuSmokeTest.cs          ← diagnostik jalur tombol MULAI
│  ├─ Scenes/
│  │  ├─ MainMenu.unity
│  │  └─ SampleScene.unity
│  ├─ Prefabs/Trash/          ← Trash_Organik, Trash_Anorganik, Trash_B3
│  ├─ VFX/                    ← prefab, material, tekstur VFX
│  ├─ Audio/                  ← BGM & SFX placeholder (WAV prosedural)
│  ├─ Materials/, Models/, Animations/, ThirdParty/Quaternius/
│  ├─ UI/, Settings/, TutorialInfo/
│  └─ InputSystem_Actions.inputactions
├─ Packages/manifest.json
├─ ProjectSettings/
├─ README.md
├─ DOKUMENTASI_TEKNIS.md
└─ PROGRESS_DAN_INSTRUKSI.md   ← dokumen ini
```

> Catatan: `Library/`, `Temp/`, `UserSettings/`, `Packages/mcp-unity/`,
> `ProjectSettings/McpUnitySettings.json`, dan `opencode.json` bersifat lokal/tooling
> dan tidak untuk di-commit.

---

## 3. FITUR YANG SUDAH SELESAI

Status keseluruhan: **10/10 fase selesai dan dikonfirmasi** (18 Nov 2026).

| # | Fase | Fitur | Script/Aset Inti | Status |
|---|---|---|---|---|
| 1 | Fondasi | Tag kustom (`Organik`, `Anorganik`, `B3`, `TongHijau`, `TongKuning`, `TongMerah`, `ShopNPC`), layer `Ground`, struktur folder | `YeVillageSetup` | ✅ |
| 2 | Movement & Camera | Gerak third-person relatif kamera, fisika Rigidbody, kamera orbit 360°, footstep timer | `PlayerController`, `ThirdPersonCamera` | ✅ |
| 3 | Inventori & Debuff B3 | Deteksi sampah via trigger, ambil dengan `E`, kapasitas tas, debuff lambat 40% saat membawa B3 tanpa sarung tangan | `TrashInventory` | ✅ |
| 4 | Pemilahan & Skor | Validasi 3 tong, +10 poin benar (jadi +20 dengan mesin), −5 poin salah (tidak pernah minus) | `ScoreManager`, `TrashBin`, `ScoreHUD` | ✅ |
| 5 | Anti-Softlock | Spawner sampah berkala (interval 5 dtk, maks 15 sampah), 20 spawn point (3 Zona 1 + 3 Zona 2 + 14 Zona 3), 3 prefab sampah | `TrashSpawner`, `TrashPrefabBuilder`, `Zona3Builder` | ✅ |
| 6 | Toko Tetua | NPC Tetua, 7 upgrade (Kantong, Boots, Sarung Tangan, Gerobak, Mesin Kompos, Mesin Press, Renovasi), jeda waktu saat toko buka | `ShopManager`, `ShopSetupBuilder` | ✅ |
| 7 | HUD & Victory | HUD poin + kapasitas tas, tukar bangunan Balai Desa, hentikan spawner, panel victory, bekukan waktu | `UIManager`, `GameManager`, `Phase7SceneBuilder` | ✅ |
| 8 | Audio | BGM gameplay loop, BGM victory, SFX pickup/correct/wrong/buy/footstep (placeholder WAV prosedural), retry BGM | `AudioManager`, `AudioSetupBuilder` | ✅ |
| 9 | Menu & Pause | Main menu (Mulai/Petunjuk/Keluar), pause (Resume/Restart/Menu), build settings 2 scene | `MainMenu`, `PauseMenu`, `Phase9SceneBuilder` | ✅ |
| 10 | VFX & Polish | Konfeti benar, asap salah, kilau upgrade, efek renovasi; material particle URP transparan; auto-destroy VFX | `VFXManager`, `VFXSetupBuilder` | ✅ |

**Pola arsitektur yang dipakai:**

- **Singleton:** `ScoreManager`, `GameManager`, `AudioManager`, `VFXManager`.
- **Trigger + `E`:** `TrashInventory`, `TrashBin`, `ShopManager`.
- **Coroutine:** `TrashSpawner`.
- **Runtime UI Builder:** `ScoreHUD`, `ShopManager`, `MainMenu`, `PauseMenu` (UI dibuat saat runtime).
- **Editor Builder (MenuItem):** 9 builder di `Assets/Editor`, idempotent (aman dijalankan ulang).

**Menu editor bantu:**

| Menu | Fungsi |
|---|---|
| Tools > Ye Village Raker > Phase 1 - Setup Tags, Layers & Folders | Tag, layer, folder |
| YeVillage > Build Trash Prefabs & Setup Spawner | 3 prefab + 9 spawn point + spawner |
| YeVillage > Setup Tetua Shop | NPC Tetua + visual upgrade |
| YeVillage > Setup Phase 7 Scene | HUD, Victory Panel, referensi GameManager |
| YeVillage > Setup Audio System | AudioManager + 7 klip WAV |
| YeVillage > Setup Phase 9 Menus | Scene MainMenu + PauseMenu + Build Settings |
| YeVillage > Setup Phase 10 VFX | Tekstur/material/prefab VFX |
| YeVillage > Build Zona 3 Balai Desa | Prefab `Zona3_BalaiDesa` + wiring scene |
| YeVillage > Validate Zona 3 Balai Desa | Cek referensi GameManager/TrashSpawner & jumlah spawn |
| Tools > Quaternius > 4. Integrate All | Integrasi aset 3D Quaternius |
| Tools > Quaternius > 16. Test MainMenu Start | Diagnostik tombol MULAI (saat Play) |

---

## 4. STATUS SAAT INI & YANG BELUM DIKERJAKAN

### 4.1 Status aktual

- Semua 10 fase implementasi **selesai**; smoke test gameplay pernah lolos.
- Scene yang sedang terbuka di Unity Editor saat dokumen ini dibuat: **`MainMenu.unity`**
  (root object: `Main Camera`, `Canvas_MainMenu`, `EventSystem`).
- Scene gameplay `SampleScene.unity` berisi sistem lengkap (Kades, kamera, tong, spawner,
  Tetua, HUD, AudioManager, VFXManager).
- **Zona 3** dibungkus prefab `Assets/Prefabs/Zona3_BalaiDesa.prefab`: alun-alun, gapura
  bertulisan "BALAI DESA" (terbaca dari arah pemain), 14 titik spawn sampah, serta dua versi
  bangunan (`BalaiDesa_Rusak` aktif + puing, `BalaiDesa_Megah` nonaktif + taman rapi).
- **Alur renovasi** sudah diuji otomatis di Play Mode (24 Nov 2026): poin 1500 → 0,
  rusak nonaktif, megah aktif, VFX renovasi muncul, spawner berhenti, sampah sisa dibersihkan,
  panel victory tampil, fanfare diputar, `Time.timeScale = 0`.
- **Belum ada bukti build final Windows** yang tersimpan di repo (folder `Build/` di-ignore).
- **Play test manual end-to-end final** (menu → gameplay → 1500 poin → renovasi → victory)
  masih perlu dilakukan ulang setelah perubahan terakhir.

### 4.2 Daftar pekerjaan yang belum dikerjakan (rekomendasi)

Prioritas **P0 = wajib sebelum submit/demo**, **P1 = nilai tambah penting**,
**P2 = opsional/polish**.

#### P0 — Finalisasi & verifikasi

- [ ] **Play test end-to-end** dari `MainMenu` sampai Victory (termasuk restart & kembali ke menu).
      *(Baru terverifikasi: `MainMenu` → tombol MULAI → `SampleScene`; sisa alur belum.)*
- [ ] **Build Windows** (`File > Build Settings` → Windows → Build) dan uji build di luar Editor.
- [x] Verifikasi ulang **Build Settings**: `MainMenu` = 0, `SampleScene` = 1. *(22 Nov 2026)*
- [x] Cek **Console bersih** saat masuk Play Mode & transisi menu → gameplay. *(0 error, 0 warning)*
- [ ] Uji **resolusi/rasio layar** berbeda (HUD & panel toko/victory tidak terpotong).
- [x] Pastikan **token/konfigurasi MCP tidak ikut ter-commit** (sudah di-ignore).

> **Catatan verifikasi 22 Nov 2026:** Pengujian Play Mode lewat MCP sempat membuat Unity
> hang di `Reloading Domain`. Solusi yang dipakai: `ProjectSettings/EditorSettings.asset` →
> `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 1` (**Disable Domain Reload**).
> Konsekuensi: static field tidak di-reset antar sesi Play. Jika muncul bug state lama
> (mis. `PauseMenu.isPaused` tetap `true`), kembalikan `m_EnterPlayModeOptions` ke `0`
> atau tambahkan reset static saat play mode dimulai.

#### P1 — Fitur & kualitas yang disarankan

- [ ] **Menu Pengaturan (Settings):** volume BGM/SFX, sensitivitas mouse, kualitas grafis,
      tombol kembali. Saat ini belum ada UI pengaturan.
- [ ] **Save/Highscore:** simpan skor tertinggi atau progres upgrade (mis. `PlayerPrefs`/JSON).
      Saat ini progres hanya hidup selama sesi.
- [ ] **Dukungan Gamepad/Controller:** proyek sudah punya Input System, tetapi script masih
      memakai legacy `Input.*`. Perlu skema input baru + rebinding jika ingin gamepad.
- [ ] **Tutorial interaktif:** panel petunjuk sudah ada, tetapi belum ada tutorial langkah-demi-langkah
      di gameplay (mis. highlight tong pertama).
- [ ] **Object pooling** untuk sampah & VFX (saat ini `Instantiate`/`Destroy` berulang) agar
      lebih stabil pada sesi panjang.
- [ ] **Lebih banyak variasi konten:** jenis sampah tambahan, variasi model, dialog NPC Tetua,
      misi/achievement sederhana.
- [ ] **Animasi Kades:** komponen `Animator` bersifat opsional; belum ada animasi jalan/ambil
      yang final.
- [ ] **Ganti aset placeholder:** audio masih WAV prosedural; sebagian visual masih primitif.
- [ ] **Pengujian otomatis:** unit test skor/inventori, PlayMode test alur victory, dan
      smoke test build di CI.
- [ ] **Aksesibilitas:** label tong ramah buta warna (ikon/teks), subtitle, opsi rebind tombol,
      skala UI.

#### P2 — Polish & rilis

- [ ] **Lokalisasi** ID/EN (saat ini teks hardcoded Bahasa Indonesia).
- [ ] **Efek suara/partikel tambahan** untuk feedback halus (UI hover/click, ambient desa).
- [ ] **Optimasi performa:** profil frame time, kurangi `FindObjectsByType` per frame,
      atur LOD/batching, cek ukuran build.
- [ ] **Halaman kredit & versi** di main menu.
- [ ] **Dokumentasi pemain** (cara main, kontrol, tujuan) dan **catatan rilis**.
- [ ] **Pipeline rilis:** versioning, changelog, dan arsip build.

---

## 5. PANDUAN LANGKAH BERIKUTNYA (URUTAN KERJA)

1. **Buka Unity** dengan versi `6000.3.27f1`, buka `Assets/Scenes/MainMenu.unity`.
2. **Jalankan Play test manual** sesuai checklist P0. Catat bug di issue/notes.
3. **Perbaiki bug** yang ditemukan; jalankan ulang smoke test.
4. **Buat build Windows** dan uji di luar Editor (termasuk resolusi lain).
5. **Kerjakan P1** satu per satu, mulai dari **Settings** dan **Save/Highscore**
   karena paling terasa bagi pemain.
6. **Refactor ke object pooling** jika sesi panjang menunjukkan penurunan performa.
7. **Terakhir, P2** (lokalisasi, kredit, optimasi, dokumentasi rilis).

### Cara membangun ulang scene dari nol (jika rusak)

Jalankan berurutan dari menu Unity:

1. **Tools > Ye Village Raker > Phase 1** — tag/layer/folder.
2. **YeVillage > Build Trash Prefabs & Setup Spawner**.
3. **YeVillage > Setup Tetua Shop**.
4. **YeVillage > Setup Phase 7 Scene**.
5. **YeVillage > Setup Audio System**.
6. **YeVillage > Setup Phase 9 Menus**.
7. **YeVillage > Setup Phase 10 VFX**.
8. Simpan scene.

Semua builder **idempotent** — aman dijalankan berulang.

---

## 6. GOTCHA PENTING (DARI PENGALAMAN PROYEK)

1. **MCP Unity putus setelah recompile/domain reload** — tunggu 5–10 detik, lalu retry;
   jangan spam perintah.
2. **Batch operasi besar bisa timeout** dan dieksekusi sebagian — chunk ≤30 operasi.
3. **Tambah komponen dan set field harus di call terpisah** (MCP).
4. **`GameObject.Find` tidak menemukan objek nonaktif** — gunakan
   `FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)`.
5. **Jangan membuat GameManager kedua** — `ScoreManager.Awake()` menghancurkan duplikat.
6. **Jarak tong & sampah:** tong ≥1.8 m dari sampah, antar tong 2.4–2.6 m.
7. **Kapasitas tas awal 1** — buang dulu sebelum mengambil sampah lain saat test.
8. **Jangan ubah Active Input Handling ke Input System saja** — script memakai legacy `Input.*`.
9. **Gunakan `rb.linearVelocity`** (Unity 6), bukan `rb.velocity`.
10. **`Time.timeScale = 0` mematikan particle default** — prefab VFX memakai
    `main.useUnscaledTime = true`.
11. **Material Particle URP default opaque** — set `_Surface = 1`, alpha blend,
    `_ZWrite = 0`, `_SURFACE_TYPE_TRANSPARENT`, Render Queue 3000.
12. **UI runtime butuh EventSystem** — sudah dibuat otomatis oleh script UI.
13. **`set_play_mode_status play` sering balas error koneksi palsu** — Play tetap berjalan;
    cek `get_play_mode_status` setelah 7–10 detik.
14. **Crash D3D12 saat smoke test pernah terjadi** — scene sudah tersimpan; restart Unity,
    dismiss "Recovering Scene Backups", ulangi test.
15. **Jangan commit folder generated** (`Library/`, `Temp/`, `UserSettings/`,
    `Packages/mcp-unity/`, `Assets/_Recovery/`).

---

## 7. CATATAN TOOLING MCP UNITY (LOKAL)

- MCP Unity terpasang sebagai package cache:
  `Library/PackageCache/com.gamelovers.mcp-unity@d572dc88f1b9/`.
- Konfigurasi klien memakai **stdio lokal** ke `Server~/build/index.js` (bukan remote/SSE),
  dengan env `MCP_UNITY_SETTINGS_PATH` dan `MCP_UNITY_AUTH_TOKEN_PATH`.
- `opencode.json` dan `.vscode/` bersifat lokal (di-ignore), token ada di
  `Library/McpUnity/bridge-token`.
- **Jika package MCP Unity di-update**, hash `@d572dc88f1b9` pada path bisa berubah —
  perbarui `command`/`args` di `opencode.json` dan `.vscode/mcp.json`, lalu build ulang
  server (`Server~`: `npm install` + `npm run build`).
- Error `'node' is not recognized` diatasi dengan memakai path penuh
  `C:\Program Files\nodejs\node.exe`.

---

## 8. REFERENSI CEPAT

| Kebutuhan | Lokasi |
|---|---|
| Dokumentasi teknis lengkap per fase | `DOKUMENTASI_TEKNIS.md` |
| Ringkasan proyek & kontrol | `README.md` |
| Script gameplay | `Assets/Scripts/` |
| Alat builder scene | `Assets/Editor/` |
| Scene menu | `Assets/Scenes/MainMenu.unity` |
| Scene gameplay | `Assets/Scenes/SampleScene.unity` |
| Build settings | `ProjectSettings/EditorBuildSettings.asset` |
| Prefab sampah | `Assets/Prefabs/Trash/` |
| Prefab & material VFX | `Assets/VFX/` |
| Audio placeholder | `Assets/Audio/BGM/`, `Assets/Audio/SFX/` |
