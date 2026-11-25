# Project Ye Village Raker

Tugas besar Computer Graphics: game 3D pemilahan sampah. Pemain berperan sebagai Kades yang memungut sampah, memilahnya ke tong Organik / Anorganik / B3, lalu menukar poin di toko Tetua Desa untuk upgrade permanen.

## Info Proyek

- **Unity**: 6000.3.27f1
- **Render Pipeline**: Universal Render Pipeline (URP) 17.3
- **Scene utama**: `Assets/Scenes/MainMenu.unity` (Build Index 0)
- **Scene gameplay**: `Assets/Scenes/Level1_Zona1_Pinggiran.unity` (Zona 1) & `Assets/Scenes/SampleScene.unity` (Zona Lanjutan/Balai Desa)
- **Input**: Input System (Active Input Handling = Both)
- **Platform**: Windows

## Cara Menjalankan

1. Buka folder ini sebagai project Unity (Unity Hub > Add project from disk).
2. Buka `Assets/Scenes/MainMenu.unity`.
3. Tekan Play, lalu klik tombol **MULAI BERMAIN**.
4. Pemain akan mulai di **Level 1 (Zona 1: Pinggiran Desa & Pos TPS)**, memungut sampah, membeli upgrade di Toko Tetua Desa, lalu menuju gerbang utara (`Exit_To_Zona2`) untuk berpindah ke zona berikutnya.

## Kontrol

- `WASD` — bergerak
- Mouse — memutar kamera
- `E` — ambil sampah / buang ke tong / buka toko Tetua / masuk pintu keluar zona
- `X` atau `Escape` — tutup panel toko
- `Escape` saat gameplay — menu pause (lanjutkan / ulangi / menu utama)
- `Escape` saat panel kemenangan — keluar dari game

## Progres & Struktur Map

### Fase Inti (1–10)
- [x] Fase 1 — Fondasi & setup aset
- [x] Fase 2 — Movement & camera
- [x] Fase 3 — Pickup & inventory
- [x] Fase 4 — Sorting & scoring
- [x] Fase 5 — Trash spawner (anti-softlock)
- [x] Fase 6 — Toko Tetua
- [x] Fase 7 — HUD lengkap & GameManager ending
- [x] Fase 8 — Audio terpusat (BGM & SFX)
- [x] Fase 9 — Main menu & pause menu
- [x] Fase 10 — Visual effects & polishing

### Pengembangan Map Level 1 (Zona 1: Pinggiran Desa & Pos TPS)
- [x] Tahap 1 — Prefab bersama di `Assets/Prefabs/Shared/`
- [x] Tahap 2 — Scene dasar 60×60 `Level1_Zona1_Pinggiran.unity`
- [x] Tahap 3 — Environment low-poly (pagar miring, rumah, props, rumput kering)
- [x] Tahap 4 — Pos TPS (pallet 8×4, 3 tong, 2 mesin nonaktif, NPC Tetua Desa)
- [x] Tahap 5 — Toko & HUD per zona (target 250 poin, filter mesin & renovasi)
- [x] Tahap 6 — ScriptableObject `TrashData` & variasi prefab sampah (Organik/Anorganik/B3)
- [x] Tahap 7 — Trash Spawner v2 (mode area, starter trash 5-9m, burst spawn, exclusion zones)
- [x] Tahap 8 — Transisi zona (`ZoneExitMarker`) & persistensi state (`GameSession`)
- [x] Tahap 9 — Reposisi map lama (`SampleScene`) sebagai Zona Lanjutan/Balai Desa
- [x] Tahap 10 — QA checklist otomatis (`Level1QAChecklist`) & dokumentasi teknis

## Catatan

Folder `Packages/mcp-unity/` dan `Packages/packages-lock.json` tidak disertakan karena merupakan tooling development MCP Unity (memiliki `.git` dan `node_modules` sendiri). Project tetap bisa dibuka dan dijalankan tanpa tooling tersebut.
