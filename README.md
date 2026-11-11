# Project Ye Village Raker

Tugas besar Computer Graphics: game 3D pemilahan sampah. Pemain berperan sebagai Kades yang memungut sampah, memilahnya ke tong Organik / Anorganik / B3, lalu menukar poin di toko Tetua Desa untuk upgrade permanen.

## Info Proyek

- **Unity**: 6000.3.27f1
- **Render Pipeline**: Universal Render Pipeline (URP) 17.3
- **Scene utama**: `Assets/Scenes/SampleScene.unity`
- **Input**: Input System (Active Input Handling = Both)
- **Platform**: Windows

## Cara Menjalankan

1. Buka folder ini sebagai project Unity (Unity Hub > Add project from disk).
2. Buka `Assets/Scenes/SampleScene.unity`.
3. Tekan Play.

## Kontrol

- `WASD` — bergerak
- Mouse — memutar kamera
- `E` — ambil sampah / buang ke tong / buka toko Tetua
- `X` atau `Escape` — tutup panel toko

## Progres

- [x] Fase 1 — Fondasi & setup aset
- [x] Fase 2 — Movement & camera
- [x] Fase 3 — Pickup & inventory
- [x] Fase 4 — Sorting & scoring
- [x] Fase 5 — Trash spawner (anti-softlock)
- [ ] Fase 6 — Toko Tetua (sudah diimplementasikan, menunggu Play test)
- [ ] Fase 7 — HUD lengkap & GameManager ending
- [ ] Fase 8 — Audio
- [ ] Fase 9 — Main menu & pause menu
- [ ] Fase 10 — Visual effects & polishing

## Catatan

Folder `Packages/mcp-unity/` dan `Packages/packages-lock.json` tidak disertakan karena merupakan tooling development MCP Unity (memiliki `.git` dan `node_modules` sendiri). Project tetap bisa dibuka dan dijalankan tanpa tooling tersebut.
