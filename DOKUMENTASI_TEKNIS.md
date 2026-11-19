# DOKUMENTASI TEKNIS — YE VILLAGE RAKER

> **Proyek:** Tugas Besar Komputer Grafis
> **Genre:** Third-person 3D trash sorting / edukasi lingkungan
> **Engine:** Unity 6 (6000.3.27f1) + Universal Render Pipeline (URP)
> **Bahasa:** C#
> **Status:** 10/10 fase selesai dan dikonfirmasi (18 Nov 2026)
> **Repo:** https://github.com/fkhdz/Project-Ye-Village-Raker

Dokumen ini menjelaskan **seluruh implementasi teknis per fase**: tujuan, file yang dibuat, GameObject/komponen yang dipasang, variabel penting, potongan kode aktual, alur kerja, dan integrasi antar-sistem.

---

## DAFTAR ISI

1. [Gambaran Arsitektur](#1-gambaran-arsitektur)
2. [Fase 1 — Fondasi: Tag, Layer, Folder](#2-fase-1--fondasi-tag-layer-folder)
3. [Fase 2 — Pergerakan Kades & Kamera](#3-fase-2--pergerakan-kades--kamera)
4. [Fase 3 — Inventori & Debuff B3](#4-fase-3--inventori--debuff-b3)
5. [Fase 4 — Pemilahan Sampah & Skor](#5-fase-4--pemilahan-sampah--skor)
6. [Fase 5 — Anti-Softlock: Trash Spawner](#6-fase-5--anti-softlock-trash-spawner)
7. [Fase 6 — Toko Tetua & Ekonomi](#7-fase-6--toko-tetua--ekonomi)
8. [Fase 7 — HUD & Kondisi Menang](#8-fase-7--hud--kondisi-menang)
9. [Fase 8 — Audio](#9-fase-8--audio)
10. [Fase 9 — Menu Utama & Pause](#10-fase-9--menu-utama--pause)
11. [Fase 10 — VFX & Polish](#11-fase-10--vfx--polish)
12. [Matriks Integrasi Antar-Script](#12-matriks-integrasi-antar-script)
13. [Menu Editor & Alat Bantu](#13-menu-editor--alat-bantu)
14. [Inventaris Aset & Scene](#14-inventaris-aset--scene)
15. [Cara Menjalankan & Build](#15-cara-menjalankan--build)
16. [Pelajaran Teknis & Gotcha](#16-pelajaran-teknis--gotcha)

---

## 1. GAMBARAN ARSITEKTUR

### 1.1 Struktur Scene

| Scene | Build Index | Isi |
|---|---|---|
| `Assets/Scenes/MainMenu.unity` | 0 | Main Camera, `Canvas_MainMenu` + `MainMenu`, EventSystem |
| `Assets/Scenes/SampleScene.unity` | 1 | Gameplay: Kades, kamera, tong, spawner, Tetua, HUD, AudioManager, VFXManager |

### 1.2 Pola Desain yang Dipakai

| Pola | Script | Alasan |
|---|---|---|
| **Singleton** | `ScoreManager`, `GameManager`, `AudioManager`, `VFXManager` | Script lain cukup memanggil `X.instance.Method()` tanpa `Find` berulang |
| **Trigger + `[E]`** | `TrashInventory`, `TrashBin`, `ShopManager` | Interaksi jarak dekat yang sederhana dan konsisten |
| **Coroutine** | `TrashSpawner` | Spawn berkala tanpa membebani `Update` |
| **Runtime UI Builder** | `ScoreHUD`, `ShopManager`, `MainMenu`, `PauseMenu` | UI dibuat saat runtime agar tidak perlu setup Canvas manual dan listener selalu valid |
| **Editor Builder (MenuItem)** | 7 script di `Assets/Editor` | Setup scene/aset otomatis, idempotent, bisa dijalankan ulang |

### 1.3 Alur Ketergantungan

```text
PlayerController  <── dibaca/diubah ──  TrashInventory, ShopManager
TrashInventory    <── dibaca ─────────  TrashBin, UIManager, ShopManager
TrashBin          ── memanggil ──────►  ScoreManager, AudioManager, VFXManager
ShopManager       ── memanggil ──────►  ScoreManager, AudioManager, VFXManager, GameManager
GameManager       ── memanggil ──────►  TrashSpawner, AudioManager, VFXManager, ShopManager
PauseMenu         ── membaca ────────►  GameManager.IsVictory, ShopManager.IsShopOpen
MainMenu          ── memuat scene ───►  SceneManager (build index + 1)
```

### 1.4 Tag & Layer

**Tag kustom** (`ProjectSettings/TagManager.asset`):
`Organik`, `Anorganik`, `B3`, `TongHijau`, `TongKuning`, `TongMerah`, `ShopNPC` — plus tag bawaan `Player`.

**Layer kustom:** `Ground` (slot layer kosong pertama ≥ 8).

### 1.5 Input

Proyek memakai **legacy Input Manager** (`Input.GetAxisRaw`, `Input.GetKeyDown`). Project Settings → Player → Active Input Handling = **Both** agar tidak konflik dengan paket Input System.

| Aksi | Tombol |
|---|---|
| Gerak | `W A S D` |
| Ambil / buang / buka toko | `E` |
| Pause / tutup toko / keluar saat victory | `ESC` |
| Putar kamera | Mouse |

### 1.6 Peta Folder & Lokasi Potongan Kode

Akar proyek Unity ada di folder **`My project`**. Semua potongan kode di dokumen ini kini punya baris **File:** tepat di bawah judulnya. Berikut peta foldernya:

```text
uas\
├─ DOKUMENTASI_TEKNIS.md          ← dokumen ini
├─ HANDOVER.md
├─ 01..11 guide .md
└─ My project\                    ← buka folder INI di VS Code
   ├─ Assets\
   │  ├─ Editor\                  ← alat builder, tidak ikut build game
   │  │  ├─ YeVillageSetup.cs
   │  │  ├─ TrashPrefabBuilder.cs
   │  │  ├─ ShopSetupBuilder.cs
   │  │  ├─ Phase7SceneBuilder.cs
   │  │  ├─ AudioSetupBuilder.cs
   │  │  ├─ Phase9SceneBuilder.cs
   │  │  └─ VFXSetupBuilder.cs
   │  ├─ Scripts\
   │  │  ├─ Player\PlayerController.cs
   │  │  ├─ Camera\ThirdPersonCamera.cs
   │  │  ├─ Trash\TrashInventory.cs
   │  │  ├─ Trash\TrashBin.cs
   │  │  ├─ Managers\ScoreManager.cs
   │  │  ├─ Managers\TrashSpawner.cs
   │  │  ├─ Managers\ShopManager.cs
   │  │  ├─ Managers\UIManager.cs
   │  │  ├─ Managers\GameManager.cs
   │  │  ├─ Managers\AudioManager.cs
   │  │  ├─ UI\ScoreHUD.cs
   │  │  ├─ UI\MainMenu.cs
   │  │  ├─ UI\PauseMenu.cs
   │  │  └─ VFX\VFXManager.cs
   │  ├─ Scenes\MainMenu.unity
   │  ├─ Scenes\SampleScene.unity
   │  ├─ Prefabs\Trash\Trash_Organik.prefab, Trash_Anorganik.prefab, Trash_B3.prefab
   │  ├─ VFX\Prefabs\VFX_Correct.prefab, VFX_Wrong.prefab, VFX_Upgrade.prefab, VFX_Renovation.prefab
   │  ├─ VFX\Materials\VFX_Correct.mat, VFX_Wrong.mat, VFX_Upgrade.mat, VFX_Renovation.mat
   │  ├─ VFX\Textures\VFX_SoftCircle.png
   │  ├─ Audio\BGM\BGM_Gameplay.wav, BGM_Victory.wav
   │  └─ Audio\SFX\SFX_Pickup.wav, SFX_Correct.wav, SFX_Wrong.wav, SFX_Buy.wav, SFX_Footstep.wav
   └─ ProjectSettings\EditorBuildSettings.asset
```

**Peta cepat potongan kode → file:**

| Bagian di dokumen | File sumber |
|---|---|
| 2.4–2.6 | `My project/Assets/Editor/YeVillageSetup.cs` |
| 3.4–3.6 | `My project/Assets/Scripts/Player/PlayerController.cs` |
| 3.7–3.8 | `My project/Assets/Scripts/Camera/ThirdPersonCamera.cs` |
| 4.3–4.7 | `My project/Assets/Scripts/Trash/TrashInventory.cs` |
| 5.5, 5.7 | `My project/Assets/Scripts/Managers/ScoreManager.cs` |
| 5.6 | `My project/Assets/Scripts/Trash/TrashBin.cs` |
| 6.4–6.5 | `My project/Assets/Scripts/Managers/TrashSpawner.cs` |
| 6.6–6.7 | `My project/Assets/Editor/TrashPrefabBuilder.cs` |
| 7.4–7.7, 7.9 | `My project/Assets/Scripts/Managers/ShopManager.cs` |
| 8.4 | `My project/Assets/Scripts/Managers/UIManager.cs` |
| 8.5–8.6 | `My project/Assets/Scripts/Managers/GameManager.cs` |
| 9.4–9.7 | `My project/Assets/Scripts/Managers/AudioManager.cs` |
| 9.8 | `My project/Assets/Editor/AudioSetupBuilder.cs` |
| 10.5 | `My project/Assets/Scripts/UI/MainMenu.cs` |
| 10.7–10.8 | `My project/Assets/Scripts/UI/PauseMenu.cs` |
| 11.4 | `My project/Assets/Scripts/VFX/VFXManager.cs` |
| 11.6–11.8 | `My project/Assets/Editor/VFXSetupBuilder.cs` |

> **Catatan:** potongan di bagian 6.6 (posisi spawn point) dan 7.4 (daftar item/harga) adalah data yang ditulis di dalam file builder/manager terkait, bukan file terpisah.

---

## 2. FASE 1 — FONDASI: TAG, LAYER, FOLDER

### 2.1 Tujuan
Menyiapkan standar proyek sebelum ada gameplay: tag untuk mendeteksi jenis sampah, layer `Ground`, dan struktur folder yang rapi.

### 2.2 File
- `My project/Assets/Editor/YeVillageSetup.cs`

### 2.3 Yang Digunakan
- `SerializedObject` + `SerializedProperty` untuk mengedit `ProjectSettings/TagManager.asset` (karena API tag resmi tidak ada).
- `AssetDatabase.CreateFolder` / `IsValidFolder` untuk struktur folder.
- `GameObject.Find` + rekursi `SetLayerRecursively` untuk menata layer objek tanah.
- Menu editor: **Tools > Ye Village Raker > Phase 1 - Setup Tags, Layers & Folders**.

### 2.4 Daftar Tag, Layer, Folder

**File:** `My project/Assets/Editor/YeVillageSetup.cs`

```csharp
private static readonly string[] RequiredTags =
{
    "Organik", "Anorganik", "B3",
    "TongHijau", "TongKuning", "TongMerah", "ShopNPC"
};

private static readonly string[] RequiredFolders =
{
    "Assets/Scripts",
    "Assets/Scripts/Player",
    "Assets/Scripts/Trash",
    "Assets/Scripts/Managers",
    "Assets/Scripts/UI",
    "Assets/Scripts/Audio",
    "Assets/Scripts/VFX",
    "Assets/Prefabs",
    "Assets/Prefabs/Trash",
    "Assets/Prefabs/Environment",
    "Assets/Prefabs/Characters",
    "Assets/UI",
    "Assets/Audio",
    "Assets/Audio/BGM",
    "Assets/Audio/SFX",
    "Assets/VFX"
};
```

### 2.5 Potongan Kode: Menambah Tag

**File:** `My project/Assets/Editor/YeVillageSetup.cs`

```csharp
private static void EnsureTag(string tagName)
{
    SerializedObject tagManager = GetTagManager();
    if (tagManager == null) return;

    SerializedProperty tags = tagManager.FindProperty("tags");
    for (int i = 0; i < tags.arraySize; i++)
    {
        if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
            return;
    }

    tags.InsertArrayElementAtIndex(tags.arraySize);
    tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
    tagManager.ApplyModifiedProperties();
}
```

### 2.6 Potongan Kode: Menambah Layer `Ground`

**File:** `My project/Assets/Editor/YeVillageSetup.cs`

```csharp
private static void EnsureLayer(string layerName)
{
    SerializedObject tagManager = GetTagManager();
    SerializedProperty layers = tagManager.FindProperty("layers");

    for (int i = 8; i < layers.arraySize; i++)
    {
        SerializedProperty slot = layers.GetArrayElementAtIndex(i);
        if (string.IsNullOrEmpty(slot.stringValue))
        {
            slot.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return;
        }
    }
}
```

### 2.7 Layer Ground ke Objek Tanah

Objek yang di-set ke layer `Ground`: `Ground`, `YeVillage_Nature/Path_1` s.d. `Path_5`. Layer ini berguna agar raycast/ground check dan fisika tidak mengganggu objek lain.

### 2.8 Status
**Dikonfirmasi.** Fondasi dipakai oleh semua fase berikutnya.

---

## 3. FASE 2 — PERGERAKAN KADES & KAMERA

### 3.1 Tujuan
Kades bergerak third-person relatif arah kamera, dengan kamera orbit 360° dan fisika Rigidbody.

### 3.2 File
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Camera/ThirdPersonCamera.cs`

### 3.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `Kades` (tag `Player`) | `Rigidbody` (freeze rotation X/Z, gravity aktif), `CapsuleCollider`, `PlayerController`, `TrashInventory`, `Animator` (opsional) |
| `Main Camera` | `Camera`, `AudioListener`, `ThirdPersonCamera` |

### 3.4 Variabel `PlayerController`

**File:** `My project/Assets/Scripts/Player/PlayerController.cs`

```csharp
[Header("Referensi Kamera")]
public Transform cam;

[Header("Status Gerak")]
public float moveSpeed = 5f;
public float turnSmoothTime = 0.1f;
public bool isSlowed = false;

[Header("Audio Langkah Kaki")]
public float footstepInterval = 0.45f;
```

### 3.5 Potongan Kode: Gerak Relatif Kamera (FixedUpdate)

**File:** `My project/Assets/Scripts/Player/PlayerController.cs`

```csharp
private void FixedUpdate()
{
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");
    Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
    isMoving = direction.magnitude >= 0.1f && cam != null;

    // Debuff B3: kecepatan turun 40%.
    float currentSpeed = isSlowed ? moveSpeed * 0.6f : moveSpeed;

    if (direction.magnitude >= 0.1f && cam != null)
    {
        // Arah gerak = input + yaw kamera.
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
            ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        // Y dipertahankan agar gravitasi tetap bekerja.
        rb.linearVelocity = new Vector3(moveDir.x * currentSpeed, rb.linearVelocity.y, moveDir.z * currentSpeed);
    }
    else
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    if (anim != null)
        anim.SetBool("isWalking", direction.magnitude >= 0.1f);
}
```

### 3.6 Potongan Kode: Footstep Timer (Update)

**File:** `My project/Assets/Scripts/Player/PlayerController.cs`

```csharp
private void Update()
{
    if (PauseMenu.isPaused || Time.timeScale == 0f) return;

    if (!isMoving)
    {
        footstepTimer = 0f;
        return;
    }

    footstepTimer -= Time.deltaTime;
    if (footstepTimer <= 0f)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayFootstep();

        footstepTimer = footstepInterval;
    }
}
```

### 3.7 Variabel `ThirdPersonCamera`

**File:** `My project/Assets/Scripts/Camera/ThirdPersonCamera.cs`

```csharp
public Transform target;
public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);
public float distance = 6f;
public float mouseSensitivity = 3f;
public float minPitch = -20f;
public float maxPitch = 60f;
public float followSmoothTime = 0.05f;

private float yaw;
private float pitch = 18f;
private Vector3 currentVelocity;
```

### 3.8 Potongan Kode: Orbit Kamera (LateUpdate)

**File:** `My project/Assets/Scripts/Camera/ThirdPersonCamera.cs`

```csharp
private void LateUpdate()
{
    if (target == null) return;

    // Kamera tidak berputar saat pause/toko/victory.
    if (PauseMenu.isPaused || Time.timeScale == 0f) return;

    yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
    pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    Vector3 pivot = target.position + targetOffset;
    Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
    Vector3 desiredPosition = pivot + orbitRotation * new Vector3(0f, 0f, -distance);

    transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
        ref currentVelocity, followSmoothTime);
    transform.rotation = Quaternion.LookRotation(pivot - transform.position);
}
```

### 3.9 Catatan Teknis
- **Cinemachine tidak dipakai** karena paket belum ada di proyek; `ThirdPersonCamera` meniru perilaku FreeLook (follow + orbit).
- Kursor dikunci di `Start()` (`Cursor.lockState = Locked`).
- Memakai `rb.linearVelocity` (Unity 6), bukan `rb.velocity` yang obsolete.

### 3.10 Status
**Dikonfirmasi.**

---

## 4. FASE 3 — INVENTORI & DEBUFF B3

### 4.1 Tujuan
Kades bisa mendeteksi sampah di dekatnya, mengambilnya dengan `[E]`, menyimpan tag di tas, dan terkena debuff lambat jika membawa B3 tanpa sarung tangan.

### 4.2 File
- `Assets/Scripts/Trash/TrashInventory.cs` (dipasang di `Kades`)

### 4.3 Variabel

**File:** `My project/Assets/Scripts/Trash/TrashInventory.cs`

```csharp
[Header("Kapasitas Tas")]
public int maxCapacity = 1;
public List<string> carriedTrash = new List<string>();

[Header("Upgrade")]
public bool hasThickGloves = false;

private PlayerController playerController;
private GameObject currentTargetTrash = null;
```

### 4.4 Potongan Kode: Deteksi Sampah (Trigger)

**File:** `My project/Assets/Scripts/Trash/TrashInventory.cs`

```csharp
private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Organik") || other.CompareTag("Anorganik") || other.CompareTag("B3"))
    {
        currentTargetTrash = other.gameObject;
        Debug.Log("Tekan [E] untuk mengambil sampah " + currentTargetTrash.tag + ".");
    }
}

private void OnTriggerExit(Collider other)
{
    if (other.gameObject == currentTargetTrash)
        currentTargetTrash = null;
}
```

### 4.5 Potongan Kode: Ambil Sampah

**File:** `My project/Assets/Scripts/Trash/TrashInventory.cs`

```csharp
private void Update()
{
    if (currentTargetTrash != null && Input.GetKeyDown(KeyCode.E))
    {
        if (carriedTrash.Count < maxCapacity)
        {
            string trashTag = currentTargetTrash.tag;
            carriedTrash.Add(trashTag);
            Destroy(currentTargetTrash);
            currentTargetTrash = null;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPickup);

            EvaluateB3Debuff();
        }
        else
        {
            Debug.Log("Tas Penuh!");
        }
    }
}
```

### 4.6 Potongan Kode: Debuff B3

**File:** `My project/Assets/Scripts/Trash/TrashInventory.cs`

```csharp
public void EvaluateB3Debuff()
{
    bool carryingB3 = carriedTrash.Contains("B3");

    // B3 tanpa sarung tangan = lambat 40%.
    playerController.isSlowed = carryingB3 && !hasThickGloves;
}
```

### 4.7 Potongan Kode: Mengeluarkan Sampah

**File:** `My project/Assets/Scripts/Trash/TrashInventory.cs`

```csharp
public string RemoveTopTrash()
{
    if (carriedTrash.Count > 0)
    {
        string topTrash = carriedTrash[0];
        carriedTrash.RemoveAt(0);

        EvaluateB3Debuff(); // isi tas berubah → evaluasi ulang debuff
        return topTrash;
    }

    return "";
}
```

### 4.8 Catatan
- Sampah disimpan sebagai **tag string**, bukan referensi GameObject, karena objeknya di-`Destroy`.
- Kapasitas awal 1; upgrade Kantong → 3; Gerobak → 7.
- `RemoveTopTrash` dipanggil oleh `TrashBin` agar logika inventori tetap terpusat.

### 4.9 Status
**Dikonfirmasi.**

---

## 5. FASE 4 — PEMILAHAN SAMPAH & SKOR

### 5.1 Tujuan
Tiga tong (Hijau/Kuning/Merah) memvalidasi sampah yang dibuang. Benar = +10 poin (atau +20 jika mesin upgrade), salah = −5 poin (tidak pernah minus).

### 5.2 File
- `Assets/Scripts/Managers/ScoreManager.cs`
- `Assets/Scripts/Trash/TrashBin.cs`
- `Assets/Scripts/UI/ScoreHUD.cs` (HUD pratinjau, fallback)

### 5.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `GameManager` | `ScoreManager` (+ `GameManager`, `UIManager` tidak di sini) |
| `TongHijau` | `BoxCollider` (trigger), `TrashBin` (`acceptedTrashTag = "Organik"`) |
| `TongKuning` | `TrashBin` (`acceptedTrashTag = "Anorganik"`) |
| `TongMerah` | `TrashBin` (`acceptedTrashTag = "B3"`) |

### 5.4 Layout Tong (Zona 1)
- Tong berada di satu stasiun di `z = -7`, jarak antar pusat **2.6 m**.
- Sampah minimal **1.8 m** dari tong; spawn point minimal **2 m** dari tong.
- Trigger radius 0.7 agar dua tong tidak memproses `[E]` bersamaan.

### 5.5 Potongan Kode: `ScoreManager` (Singleton + Skor)

**File:** `My project/Assets/Scripts/Managers/ScoreManager.cs`

```csharp
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    public int currentPoin = 0;
    public bool hasCompostMachine = false;
    public bool hasPressMachine = false;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public void AddScore(string trashType)
    {
        if (trashType == "Organik")
            currentPoin += hasCompostMachine ? 20 : 10;
        else if (trashType == "Anorganik")
            currentPoin += hasPressMachine ? 20 : 10;
        else if (trashType == "B3")
            currentPoin += 10;
    }

    public void DeductScore(int penaltyAmount = 5)
    {
        // Poin tidak pernah minus.
        currentPoin = Mathf.Max(0, currentPoin - penaltyAmount);
    }
}
```

### 5.6 Potongan Kode: Validasi Tong (`TrashBin`)

**File:** `My project/Assets/Scripts/Trash/TrashBin.cs`

```csharp
private void Update()
{
    if (!isPlayerInRange || !Input.GetKeyDown(KeyCode.E)) return;
    if (playerInventory == null) return;

    string thrownTrash = playerInventory.RemoveTopTrash();
    if (string.IsNullOrEmpty(thrownTrash)) return;

    if (thrownTrash == acceptedTrashTag)
    {
        ScoreManager.instance.AddScore(thrownTrash);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCorrect);

        if (VFXManager.instance != null)
            VFXManager.instance.PlayCorrectVFX(transform.position + Vector3.up * 2f);
    }
    else
    {
        ScoreManager.instance.DeductScore(5);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxWrong);

        if (VFXManager.instance != null)
            VFXManager.instance.PlayWrongVFX(transform.position + Vector3.up * 2f);
    }
}
```

### 5.7 `ScoreHUD` — HUD Pratinjau (Fallback)

**File:** `My project/Assets/Scripts/Managers/ScoreManager.cs`
Dibuat otomatis oleh `ScoreManager.Start()` **hanya jika `UIManager` tidak ada**:

```csharp
private void Start()
{
    if (Object.FindFirstObjectByType<UIManager>() == null && GetComponent<ScoreHUD>() == null)
        gameObject.AddComponent<ScoreHUD>();
}
```

`ScoreHUD` membuat Canvas `HUD_Canvas` runtime (`sortingOrder = 10`) dengan teks `Poin: N` di kiri atas. Setelah Fase 7, komponen ini dinonaktifkan agar tidak dobel dengan `UIManager`.

### 5.8 Status
**Dikonfirmasi.**

---

## 6. FASE 5 — ANTI-SOFTLOCK: TRASH SPAWNER

### 6.1 Tujuan
Sampah baru muncul berkala di titik acak agar pemain tidak kehabisan sumber poin (anti-softlock), dengan batas maksimum 15 sampah aktif.

### 6.2 File
- `Assets/Scripts/Managers/TrashSpawner.cs`
- `Assets/Editor/TrashPrefabBuilder.cs`
- Prefab: `Assets/Prefabs/Trash/Trash_Organik.prefab`, `Trash_Anorganik.prefab`, `Trash_B3.prefab`

### 6.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `TrashSpawnerManager` | `TrashSpawner` |
| `SpawnPoints` (parent) | 9 child: `SP_Zona1_01..03`, `SP_Zona2_01..03`, `SP_Zona3_01..03` |
| Prefab sampah | Semua `Collider` di-set `isTrigger = true`, tag sesuai jenis |

### 6.4 Variabel

**File:** `My project/Assets/Scripts/Managers/TrashSpawner.cs`

```csharp
public GameObject[] trashPrefabs;
public Transform[] spawnPoints;
public float spawnInterval = 5f;
public int maxTrashOnMap = 15;
public bool isSpawning = true;
```

### 6.5 Potongan Kode: Coroutine Spawn

**File:** `My project/Assets/Scripts/Managers/TrashSpawner.cs`

```csharp
private IEnumerator SpawnRoutine()
{
    while (true)
    {
        yield return new WaitForSeconds(spawnInterval);

        if (!isSpawning) continue;

        // Hitung sampah aktif berdasarkan tag.
        int currentTrashCount = GameObject.FindGameObjectsWithTag("Organik").Length
                              + GameObject.FindGameObjectsWithTag("Anorganik").Length
                              + GameObject.FindGameObjectsWithTag("B3").Length;

        if (currentTrashCount >= maxTrashOnMap) continue;

        int randomPoint = Random.Range(0, spawnPoints.Length);
        int randomTrash = Random.Range(0, trashPrefabs.Length);

        if (spawnPoints[randomPoint] == null || trashPrefabs[randomTrash] == null) continue;

        Instantiate(trashPrefabs[randomTrash],
            spawnPoints[randomPoint].position, Quaternion.identity);
    }
}

public void StopSpawning()
{
    isSpawning = false; // dipanggil GameManager saat victory
}
```

### 6.6 Posisi 9 Spawn Point

**File:** `My project/Assets/Editor/TrashPrefabBuilder.cs`

```csharp
new Vector3(-7f, 0.3f, 0f),   // Zona 1
new Vector3(7f, 0.3f, 0f),
new Vector3(0f, 0.3f, 7f),
new Vector3(5f, 0.3f, -5f),   // Zona 2
new Vector3(7f, 0.3f, 5f),
new Vector3(-5f, 0.3f, -5f),
new Vector3(0f, 0.3f, -9.2f), // Zona 3
new Vector3(-7f, 0.3f, 5f),
new Vector3(-7f, 0.3f, -5f)
```

### 6.7 Potongan Kode: Membuat Prefab dari Objek Contoh

**File:** `My project/Assets/Editor/TrashPrefabBuilder.cs`

```csharp
private static GameObject CreatePrefabFromSample(string sampleName, string prefabName)
{
    GameObject source = GameObject.Find(sampleName);
    if (source == null) return null;

    GameObject temp = Object.Instantiate(source);
    temp.name = prefabName;
    temp.transform.SetParent(null);
    temp.transform.position = Vector3.zero;
    temp.transform.rotation = Quaternion.identity;

    // Semua collider sampah harus trigger agar terdeteksi TrashInventory.
    foreach (Collider col in temp.GetComponentsInChildren<Collider>())
        col.isTrigger = true;

    string path = "Assets/Prefabs/Trash/" + prefabName + ".prefab";
    GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(temp, path);
    Object.DestroyImmediate(temp);
    return prefabAsset;
}
```

### 6.8 Menu Editor
**YeVillage > Build Trash Prefabs & Setup Spawner** — membuat 3 prefab, 9 spawn point, dan `TrashSpawnerManager` sekaligus. Idempotent (aman dijalankan ulang).

### 6.9 Status
**Dikonfirmasi.**

---

## 7. FASE 6 — TOKO TETUA & EKONOMI

### 7.1 Tujuan
NPC Tetua membuka toko `[E]`, menjeda waktu (`Time.timeScale = 0`), dan menjual 7 upgrade permanen dengan poin.

### 7.2 File
- `Assets/Scripts/Managers/ShopManager.cs`
- `Assets/Editor/ShopSetupBuilder.cs`

### 7.3 GameObject & Komponen
| Objek | Komponen/Keterangan |
|---|---|
| `TetuaDesa` | `ShopManager`, `CapsuleCollider` trigger (radius 2, height 2.4, center (0,1.2,0)), visual primitif |
| `GerobakVisual` | Child `Kades`, nonaktif sampai dibeli |
| `MesinKomposVisual` | Di `(4.5, 0, -9)`, nonaktif |
| `MesinPressVisual` | Di `(7, 0, -9)`, nonaktif |

### 7.4 Daftar Item & Harga

**File:** `My project/Assets/Scripts/Managers/ShopManager.cs`

```csharp
private static readonly string[] ItemNames =
{
    "Kantong (Kapasitas 3)",
    "Sepatu Boots (Kecepatan 7.5)",
    "Sarung Tangan (Kebal Slow B3)",
    "Gerobak (Kapasitas 7)",
    "Mesin Kompos (+10 Organik)",
    "Mesin Press (+10 Anorganik)",
    "Renovasi Balai Desa"
};

private static readonly int[] ItemPrices = { 200, 250, 300, 400, 500, 500, 1500 };
```

### 7.5 Potongan Kode: Buka/Tutup Toko

**File:** `My project/Assets/Scripts/Managers/ShopManager.cs`

```csharp
public void OpenShop()
{
    if (isShopOpen) return;

    shopUIPanel.SetActive(true);
    isShopOpen = true;

    // Jeda gameplay agar pemain bisa memilih dengan tenang.
    Time.timeScale = 0f;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    RefreshShopUI();
}

public void CloseShop()
{
    if (!isShopOpen) return;

    isShopOpen = false;
    shopUIPanel.SetActive(false);

    Time.timeScale = 1f;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
```

### 7.6 Potongan Kode: Pembayaran & Upgrade

**File:** `My project/Assets/Scripts/Managers/ShopManager.cs`

```csharp
private bool TryPay(int price)
{
    if (ScoreManager.instance == null) return false;

    if (ScoreManager.instance.currentPoin < price)
    {
        Debug.Log("Uang Tidak Cukup!");
        return false;
    }

    ScoreManager.instance.currentPoin -= price;

    if (AudioManager.Instance != null)
        AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxBuy);

    return true;
}

public void BuySarungTangan()
{
    if (playerInventory.hasThickGloves) return;
    if (!TryPay(300)) return;

    playerInventory.hasThickGloves = true;

    // Debuff B3 langsung dievaluasi ulang.
    playerInventory.EvaluateB3Debuff();

    PlayUpgradeVFX(playerController != null ? playerController.transform : null);
    RefreshShopUI();
}
```

### 7.7 Potongan Kode: Renovasi → Victory

**File:** `My project/Assets/Scripts/Managers/ShopManager.cs`

```csharp
public void BuyRenovasiBalaiDesa()
{
    if (renovasiBought) return;
    if (!TryPay(1500)) return;

    renovasiBought = true;

    if (GameManager.instance != null)
        GameManager.instance.TriggerVictory();
    else
        Debug.LogError("[Shop] GameManager.instance tidak ditemukan.");
}
```

### 7.8 UI Toko Runtime
- Canvas `Shop_Canvas` (`sortingOrder = 50`, ScreenSpaceOverlay, ScaleWithScreenSize 1920×1080).
- Panel `ShopPanel` 760×620, judul, teks poin, tombol X, dan 7 tombol item.
- Tombol kuning khusus untuk Renovasi.
- `RefreshShopUI()` menonaktifkan tombol yang sudah dibeli dan mengubah label menjadi `"- Sudah dibeli"`.
- `Escape` di `Update()` juga menutup toko sebagai jalan keluar darurat anti-softlock.

### 7.9 Potongan Kode: VFX Upgrade (Fase 10)

**File:** `My project/Assets/Scripts/Managers/ShopManager.cs`

```csharp
private void PlayUpgradeVFX(Transform anchor)
{
    if (VFXManager.instance == null) return;

    Vector3 position = anchor != null ? anchor.position : transform.position;
    VFXManager.instance.PlayUpgradeVFX(position + Vector3.up * 1.2f);
}
```

### 7.10 `ShopSetupBuilder`
Menu **YeVillage > Setup Tetua Shop**:
- Membuat `TetuaDesa` di `(-5.5, 0, -7.5)`, rotasi Y 90°.
- Membuat visual primitif (body, head, topi, tongkat) dari material `Assets/Materials/Ye_Village`.
- Membuat `GerobakVisual`, `MesinKomposVisual`, `MesinPressVisual` (nonaktif).
- Memasang `ShopManager` dan mengisi referensi `playerController`/`playerInventory`/3 visual.

### 7.11 Status
**Dikonfirmasi.**

---

## 8. FASE 7 — HUD & KONDISI MENANG

### 8.1 Tujuan
HUD permanen (poin + kapasitas tas) dan kondisi menang saat renovasi dibeli: tukar bangunan, hentikan spawner, tampilkan panel victory, bekukan waktu.

### 8.2 File
- `Assets/Scripts/Managers/UIManager.cs`
- `Assets/Scripts/Managers/GameManager.cs`
- `Assets/Editor/Phase7SceneBuilder.cs`

### 8.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `Canvas_HUD` | `Canvas` (sortingOrder 20), `CanvasScaler`, `GraphicRaycaster`, `UIManager` |
| `HUDPanel/ScoreText` | `Text` "Poin: 0" (32 px, kiri atas) |
| `HUDPanel/CapacityText` | `Text` "Tas: 0 / 1" (28 px) |
| `VictoryPanel` | `Image` overlay hijau gelap, teks **KEMENANGAN!**, nonaktif |
| `BalaiDesa_Rusak` | Bangunan lama (aktif) |
| `BalaiDesa_Megah` | Bangunan baru (nonaktif) |

### 8.4 Potongan Kode: `UIManager`

**File:** `My project/Assets/Scripts/Managers/UIManager.cs`

```csharp
public class UIManager : MonoBehaviour
{
    public Text scoreText;
    public Text capacityText;
    public TrashInventory playerInventory;

    private void Update()
    {
        if (scoreText != null && ScoreManager.instance != null)
            scoreText.text = "Poin: " + ScoreManager.instance.currentPoin;

        if (capacityText != null && playerInventory != null)
            capacityText.text = "Tas: " + playerInventory.carriedTrash.Count
                              + " / " + playerInventory.maxCapacity;
    }
}
```

### 8.5 Potongan Kode: `GameManager.TriggerVictory()`

**File:** `My project/Assets/Scripts/Managers/GameManager.cs`

```csharp
public void TriggerVictory()
{
    if (victoryTriggered) return;
    victoryTriggered = true;

    // Tutup toko agar panel victory tidak tertutup.
    ShopManager shop = Object.FindFirstObjectByType<ShopManager>();
    if (shop != null) shop.CloseShop();

    // 1. Tukar bangunan.
    if (balaiDesaRusak != null) balaiDesaRusak.SetActive(false);
    if (balaiDesaMegah != null) balaiDesaMegah.SetActive(true);

    // 2. VFX renovasi (Fase 10).
    if (VFXManager.instance != null && balaiDesaMegah != null)
        VFXManager.instance.PlayRenovationVFX(
            balaiDesaMegah.transform.position + Vector3.up * 3f);

    // 3. Stop spawner.
    if (trashSpawner != null) trashSpawner.StopSpawning();

    // 4. Panel victory.
    if (victoryPanelUI != null) victoryPanelUI.SetActive(true);

    // 5. Bekukan waktu & bebaskan kursor.
    Time.timeScale = 0f;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;

    // 6. Fanfare.
    if (AudioManager.Instance != null) AudioManager.Instance.PlayVictoryMusic();
}
```

### 8.6 Potongan Kode: Keluar Saat Victory

**File:** `My project/Assets/Scripts/Managers/GameManager.cs`

```csharp
private void Update()
{
    if (victoryTriggered && Input.GetKeyDown(KeyCode.Escape))
        QuitGame();
}

public void QuitGame()
{
    Time.timeScale = 1f;
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
```

### 8.7 `Phase7SceneBuilder`
Menu **YeVillage > Setup Phase 7 Scene**:
- Memasang `GameManager` pada GameObject `GameManager` yang **sudah ada** (tidak membuat kedua).
- Membuat `Canvas_HUD` + 2 teks HUD.
- Membuat `VictoryPanel` runtime dan mengisi referensi `GameManager`.
- Menonaktifkan `ScoreHUD` pratinjau.
- `FindIncludingInactive()` memakai `FindObjectsInactive.Include` agar `BalaiDesa_Megah` (nonaktif) tetap ditemukan.
- Menu **YeVillage > Test Victory** memicu victory saat Play mode.

### 8.8 Status
**Dikonfirmasi.**

---

## 9. FASE 8 — AUDIO

### 9.1 Tujuan
Satu pusat audio: BGM gameplay loop, BGM victory, dan SFX pickup/correct/wrong/buy/footstep. Klip placeholder dibuat prosedural (WAV) agar proyek bisa diuji tanpa aset eksternal.

### 9.2 File
- `Assets/Scripts/Managers/AudioManager.cs`
- `Assets/Editor/AudioSetupBuilder.cs`
- Klip: `Assets/Audio/BGM/BGM_Gameplay.wav`, `BGM_Victory.wav`, `Assets/Audio/SFX/SFX_Pickup.wav`, `SFX_Correct.wav`, `SFX_Wrong.wav`, `SFX_Buy.wav`, `SFX_Footstep.wav`

### 9.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `AudioManager` | `AudioManager`, 2 × `AudioSource` (BGM + SFX) |

### 9.4 Variabel

**File:** `My project/Assets/Scripts/Managers/AudioManager.cs`

```csharp
public static AudioManager Instance;

public AudioSource bgmSource;
public AudioSource sfxSource;

public AudioClip bgmGameplay;
public AudioClip bgmVictory;
public AudioClip sfxPickup;
public AudioClip sfxCorrect;
public AudioClip sfxWrong;
public AudioClip sfxBuy;
public AudioClip sfxFootstep;
```

### 9.5 Potongan Kode: Inisialisasi

**File:** `My project/Assets/Scripts/Managers/AudioManager.cs`

```csharp
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;

    if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
    if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

    bgmSource.loop = true;
    bgmSource.playOnAwake = true;
    sfxSource.loop = false;
    sfxSource.playOnAwake = false;
}
```

### 9.6 Potongan Kode: SFX & Victory Music

**File:** `My project/Assets/Scripts/Managers/AudioManager.cs`

```csharp
public void PlaySFX(AudioClip clip)
{
    if (clip == null || sfxSource == null) return;
    sfxSource.PlayOneShot(clip); // tidak memotong SFX lain
}

public void PlayVictoryMusic()
{
    if (bgmSource == null || bgmVictory == null) return;

    victoryMusicActive = true;
    bgmSource.Stop();
    bgmSource.clip = bgmVictory;
    bgmSource.loop = false;
    bgmSource.Play();
}
```

### 9.7 Retry BGM (Audio Device Belum Siap)

**File:** `My project/Assets/Scripts/Managers/AudioManager.cs`

```csharp
private void Update()
{
    if (victoryMusicActive || bgmSource == null || bgmSource.clip == null) return;

    // Jika BGM berhenti di frame awal, coba putar lagi.
    if (!bgmSource.isPlaying) bgmSource.Play();
}
```

### 9.8 Potongan Kode: Generator WAV Placeholder

**File:** `My project/Assets/Editor/AudioSetupBuilder.cs`

```csharp
private static void WriteWav(string path, float[] samples)
{
    using (FileStream stream = new FileStream(path, FileMode.Create))
    using (BinaryWriter writer = new BinaryWriter(stream))
    {
        int dataSize = samples.Length * 2;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);          // PCM
        writer.Write((short)1);          // mono
        writer.Write(44100);             // sample rate
        writer.Write(44100 * 2);         // byte rate
        writer.Write((short)2);          // block align
        writer.Write((short)16);         // bits
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        foreach (float sample in samples)
            writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), -32768, 32767));
    }
}
```

Durasi placeholder: BGM gameplay 8 s (loop), BGM victory 3.2 s, pickup 0.22 s, correct 0.55 s, wrong 0.5 s, buy 0.4 s, footstep 0.12 s. Semua disintesis dengan sinus/noise + envelope.

### 9.9 Titik Integrasi SFX
| Script | Kejadian | Clip |
|---|---|---|
| `TrashInventory` | Ambil sampah | `sfxPickup` |
| `TrashBin` | Tong benar | `sfxCorrect` |
| `TrashBin` | Tong salah | `sfxWrong` |
| `ShopManager.TryPay` | Pembelian berhasil | `sfxBuy` |
| `PlayerController` | Berjalan (timer 0.45 s) | `sfxFootstep` |
| `GameManager` | Victory | `bgmVictory` |

### 9.10 Menu Editor
- **YeVillage > Setup Audio System** — buat folder, 7 klip placeholder, `AudioManager`, 2 AudioSource, isi referensi.
- **YeVillage > Test Audio State** — log status BGM + putar SFX pickup.

### 9.11 Status
**Dikonfirmasi.**

---

## 10. FASE 9 — MENU UTAMA & PAUSE

### 10.1 Tujuan
Alur game lengkap: mulai dari menu, masuk gameplay, pause/resume, restart, kembali ke menu, dan keluar.

### 10.2 File
- `Assets/Scripts/UI/MainMenu.cs`
- `Assets/Scripts/UI/PauseMenu.cs`
- `Assets/Editor/Phase9SceneBuilder.cs`
- Scene: `Assets/Scenes/MainMenu.unity`

### 10.3 Build Settings
```text
Index 0: Assets/Scenes/MainMenu.unity
Index 1: Assets/Scenes/SampleScene.unity
```

### 10.4 `MainMenu`
Dipasang di `Canvas_MainMenu`. UI dibangun runtime saat `Start()` jika `tutorialPanel` kosong:
- Judul `YE VILLAGE RAKER`, subtitle, tombol **MULAI BERMAIN / PETUNJUK / KELUAR**.
- Panel petunjuk overlay dengan teks cara bermain dan tombol **TUTUP**.

### 10.5 Potongan Kode: `MainMenu.PlayGame()`

**File:** `My project/Assets/Scripts/UI/MainMenu.cs`

```csharp
public void PlayGame()
{
    Time.timeScale = 1f;
    PauseMenu.isPaused = false;

    int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
    if (nextIndex <= 0 || nextIndex >= SceneManager.sceneCountInBuildSettings)
    {
        Debug.LogError("[MainMenu] Scene gameplay belum terdaftar di Build Settings.");
        return;
    }

    SceneManager.LoadScene(nextIndex);
}
```

### 10.6 `PauseMenu`
- `[DefaultExecutionOrder(-100)]` agar `Update`-nya berjalan lebih dulu.
- `public static bool isPaused` dibaca `PlayerController` dan `ThirdPersonCamera`.
- Dipasang di `Canvas_HUD` SampleScene; UI dibuat runtime.

### 10.7 Potongan Kode: Logika ESC

**File:** `My project/Assets/Scripts/UI/PauseMenu.cs`

```csharp
private void Update()
{
    if (!Input.GetKeyDown(KeyCode.Escape)) return;

    // Victory: ESC milik GameManager (keluar game).
    if (GameManager.instance != null && GameManager.instance.IsVictory) return;

    if (isPaused) { Resume(); return; }

    // Toko terbuka: ESC milik ShopManager (tutup toko).
    ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
    if (shop != null && shop.IsShopOpen) return;

    Pause();
}
```

### 10.8 Potongan Kode: Pause/Resume/Restart/Menu

**File:** `My project/Assets/Scripts/UI/PauseMenu.cs`

```csharp
public void Pause()
{
    pauseMenuPanel.SetActive(true);
    pauseMenuPanel.transform.SetAsLastSibling(); // selalu di atas panel lain
    Time.timeScale = 0f;
    isPaused = true;
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

public void Resume()
{
    pauseMenuPanel.SetActive(false);
    Time.timeScale = 1f;
    isPaused = false;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

public void RestartGame()
{
    Time.timeScale = 1f; // wajib sebelum reload
    isPaused = false;
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}

public void LoadMainMenu()
{
    Time.timeScale = 1f;
    isPaused = false;
    SceneManager.LoadScene(0);
}
```

### 10.9 `Phase9SceneBuilder`
Menu **YeVillage > Setup Phase 9 Menus**:
- Simpan scene terbuka.
- Pasang `PauseMenu` di `Canvas_HUD` SampleScene.
- Buat/buka `MainMenu.unity`, buat kamera menu (solid color hijau gelap), `Canvas_MainMenu` + `MainMenu`, EventSystem.
- Set `EditorBuildSettings.scenes` = MainMenu (0), SampleScene (1).

Menu test:
- **YeVillage > Test Play Game** → memanggil `PlayGame()`.
- **YeVillage > Test Pause Menu** → toggle `Pause()`/`Resume()`.

### 10.10 Status
**Dikonfirmasi user 18 Nov 2026.**

---

## 11. FASE 10 — VFX & POLISH

### 11.1 Tujuan
Menambah "juice": konfeti saat benar, asap saat salah, kilau saat upgrade, dan efek besar saat renovasi. Semua particle di-`Instantiate` lalu di-`Destroy` otomatis agar tidak menumpuk.

### 11.2 File
- `Assets/Scripts/VFX/VFXManager.cs`
- `Assets/Editor/VFXSetupBuilder.cs`
- Prefab: `Assets/VFX/Prefabs/VFX_Correct.prefab`, `VFX_Wrong.prefab`, `VFX_Upgrade.prefab`, `VFX_Renovation.prefab`
- Material: `Assets/VFX/Materials/VFX_*.mat`
- Tekstur: `Assets/VFX/Textures/VFX_SoftCircle.png`

### 11.3 GameObject & Komponen
| Objek | Komponen |
|---|---|
| `VFXManager` (SampleScene) | `VFXManager` + 4 referensi prefab |
| Prefab VFX | `ParticleSystem` + `ParticleSystemRenderer` |

### 11.4 Potongan Kode: `VFXManager`

**File:** `My project/Assets/Scripts/VFX/VFXManager.cs`

```csharp
public class VFXManager : MonoBehaviour
{
    public static VFXManager instance;

    public GameObject vfxCorrect;
    public GameObject vfxWrong;
    public GameObject vfxUpgrade;
    public GameObject vfxRenovation;

    public void PlayVFX(GameObject vfxPrefab, Vector3 spawnPosition)
    {
        if (vfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
        ParticleSystem particle = vfxInstance.GetComponent<ParticleSystem>();

        if (particle != null)
        {
            particle.Play();
            Destroy(vfxInstance, particle.main.duration); // auto-destroy
        }
        else
        {
            Destroy(vfxInstance, 2f);
        }
    }

    public void PlayCorrectVFX(Vector3 position) => PlayVFX(vfxCorrect, position);
    public void PlayWrongVFX(Vector3 position) => PlayVFX(vfxWrong, position);
    public void PlayUpgradeVFX(Vector3 position) => PlayVFX(vfxUpgrade, position);
    public void PlayRenovationVFX(Vector3 position) => PlayVFX(vfxRenovation, position);
}
```

### 11.5 Parameter Prefab VFX

| Prefab | Partikel | Durasi | Lifetime | Speed | Size | Gravity | Sudut Cone | Radius | Warna A → B |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `VFX_Correct` | 40 | 1.0 s | 0.8 s | 4 | 0.18 | 0.8 | 35° | 0.15 | hijau → kuning-hijau |
| `VFX_Wrong` | 20 | 1.2 s | 1.0 s | 1.8 | 0.35 | −0.15 | 25° | 0.25 | merah → merah gelap |
| `VFX_Upgrade` | 32 | 1.5 s | 1.2 s | 2.5 | 0.16 | −0.4 | 30° | 0.2 | emas → putih |
| `VFX_Renovation` | 120 | 3.0 s | 2.2 s | 8 | 0.3 | 1.2 | 40° | 0.4 | hijau → emas |

### 11.6 Potongan Kode: Konfigurasi Particle

**File:** `My project/Assets/Editor/VFXSetupBuilder.cs`

```csharp
ParticleSystem.MainModule main = particle.main;
main.duration = duration;
main.loop = false;
main.prewarm = false;
main.startLifetime = lifetime;
main.startSpeed = speed;
main.startSize = size;
main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
main.gravityModifier = gravity;
main.simulationSpace = ParticleSystemSimulationSpace.World;
main.playOnAwake = true;
main.maxParticles = count;
main.useUnscaledTime = true; // tetap beranimasi saat Time.timeScale = 0

// Burst semua partikel di awal.
ParticleSystem.EmissionModule emission = particle.emission;
emission.rateOverTime = 0f;
emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
```

### 11.7 Potongan Kode: Material Transparan

**File:** `My project/Assets/Editor/VFXSetupBuilder.cs`

Material `Universal Render Pipeline/Particles/Unlit` default-nya **opaque**. Harus dipaksa alpha blend:

```csharp
if (material.HasProperty("_Surface"))
{
    material.SetFloat("_Surface", 1f);
    material.SetFloat("_Blend", 0f);
    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
    material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
    material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
    material.SetFloat("_ZWrite", 0f);
    material.SetFloat("_AlphaClip", 0f);
    material.SetOverrideTag("RenderType", "Transparent");
    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    material.DisableKeyword("_ALPHATEST_ON");
    material.renderQueue = (int)RenderQueue.Transparent; // 3000
}
```

### 11.8 Tekstur `VFX_SoftCircle.png`

**File:** `My project/Assets/Editor/VFXSetupBuilder.cs`
Dibuat prosedural 64×64: alpha = `(1 − jarak/radius)²`, sehingga partikel berbentuk bulatan lembut dengan tepi transparan.

```csharp
float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
float alpha = Mathf.Clamp01(1f - distance);
alpha *= alpha; // tepi lebih lembut
pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
```

Import settings: `alphaIsTransparency = true`, `mipmapEnabled = false`, `wrapMode = Clamp`, `Uncompressed`.

### 11.9 Titik Integrasi VFX

| Script | Kejadian | Efek | Posisi |
|---|---|---|---|
| `TrashBin` | Tong benar | `PlayCorrectVFX` | tong + (0, 2, 0) |
| `TrashBin` | Tong salah | `PlayWrongVFX` | tong + (0, 2, 0) |
| `ShopManager` | 6 upgrade dibeli | `PlayUpgradeVFX` | player/anchor + (0, 1.2, 0) |
| `GameManager` | Victory | `PlayRenovationVFX` | Balai Desa + (0, 3, 0) |

### 11.10 Menu Editor
- **YeVillage > Setup Phase 10 VFX** — buat folder/tekstur/material/prefab, pasang `VFXManager` di SampleScene, kembali ke MainMenu. Idempotent (material lama diperbarui transparansinya).
- **YeVillage > Test VFX** — saat Play mode, picu 4 efek di sekitar Kades.

### 11.11 Status
**Dikonfirmasi user 18 Nov 2026.**

---

## 12. MATRIKS INTEGRASI ANTAR-SCRIPT

| Pemanggil | Method/Field yang Dipakai | Penerima |
|---|---|---|
| `PlayerController` | `AudioManager.Instance.PlayFootstep()` | `AudioManager` |
| `PlayerController` | `PauseMenu.isPaused` | `PauseMenu` |
| `ThirdPersonCamera` | `PauseMenu.isPaused` | `PauseMenu` |
| `TrashInventory` | `AudioManager.Instance.PlaySFX(sfxPickup)` | `AudioManager` |
| `TrashInventory` | `PlayerController.isSlowed` | `PlayerController` |
| `TrashBin` | `TrashInventory.RemoveTopTrash()` | `TrashInventory` |
| `TrashBin` | `ScoreManager.instance.AddScore/DeductScore` | `ScoreManager` |
| `TrashBin` | `AudioManager.Instance.PlaySFX` | `AudioManager` |
| `TrashBin` | `VFXManager.instance.PlayCorrect/WrongVFX` | `VFXManager` |
| `ShopManager` | `ScoreManager.instance.currentPoin` | `ScoreManager` |
| `ShopManager` | `TrashInventory.maxCapacity/hasThickGloves/EvaluateB3Debuff` | `TrashInventory` |
| `ShopManager` | `PlayerController.moveSpeed` | `PlayerController` |
| `ShopManager` | `GameManager.instance.TriggerVictory()` | `GameManager` |
| `ShopManager` | `VFXManager.instance.PlayUpgradeVFX` | `VFXManager` |
| `ShopManager` | `AudioManager.Instance.PlaySFX(sfxBuy)` | `AudioManager` |
| `GameManager` | `TrashSpawner.StopSpawning()` | `TrashSpawner` |
| `GameManager` | `AudioManager.Instance.PlayVictoryMusic()` | `AudioManager` |
| `GameManager` | `VFXManager.instance.PlayRenovationVFX` | `VFXManager` |
| `GameManager` | `ShopManager.CloseShop()` | `ShopManager` |
| `PauseMenu` | `GameManager.instance.IsVictory` | `GameManager` |
| `PauseMenu` | `ShopManager.IsShopOpen` | `ShopManager` |
| `MainMenu` | `PauseMenu.isPaused` | `PauseMenu` |
| `UIManager` | `ScoreManager.instance.currentPoin` | `ScoreManager` |
| `UIManager` | `TrashInventory.carriedTrash/maxCapacity` | `TrashInventory` |
| `ScoreManager` | `UIManager` (deteksi) / `ScoreHUD` fallback | `UIManager` / `ScoreHUD` |

---

## 13. MENU EDITOR & ALAT BANTU

| Menu | Script | Fungsi |
|---|---|---|
| **Tools > Ye Village Raker > Phase 1 - Setup Tags, Layers & Folders** | `YeVillageSetup` | Tag, layer Ground, folder |
| **YeVillage > Build Trash Prefabs & Setup Spawner** | `TrashPrefabBuilder` | 3 prefab sampah + 9 spawn point + spawner |
| **YeVillage > Setup Tetua Shop** | `ShopSetupBuilder` | NPC Tetua + visual upgrade + ShopManager |
| **YeVillage > Setup Phase 7 Scene** | `Phase7SceneBuilder` | Canvas HUD, Victory Panel, GameManager refs |
| **YeVillage > Test Victory** | `Phase7SceneBuilder` | Picu victory saat Play |
| **YeVillage > Setup Audio System** | `AudioSetupBuilder` | AudioManager + 7 klip WAV placeholder |
| **YeVillage > Test Audio State** | `AudioSetupBuilder` | Log status BGM + SFX |
| **YeVillage > Setup Phase 9 Menus** | `Phase9SceneBuilder` | Scene MainMenu + PauseMenu + Build Settings |
| **YeVillage > Test Play Game** | `Phase9SceneBuilder` | Pindah ke gameplay saat Play |
| **YeVillage > Test Pause Menu** | `Phase9SceneBuilder` | Toggle pause saat Play |
| **YeVillage > Setup Phase 10 VFX** | `VFXSetupBuilder` | Tekstur/material/prefab VFX + VFXManager |
| **YeVillage > Test VFX** | `VFXSetupBuilder` | Picu 4 efek saat Play |

Semua builder **idempotent**: aman dijalankan berulang, hanya membuat yang belum ada.

---

## 14. INVENTARIS ASET & SCENE

### 14.1 Script Runtime

| File | GameObject | Fungsi |
|---|---|---|
| `Scripts/Player/PlayerController.cs` | Kades | Gerak WASD, rotasi, fisika, footstep |
| `Scripts/Camera/ThirdPersonCamera.cs` | Main Camera | Follow + orbit 360° |
| `Scripts/Trash/TrashInventory.cs` | Kades | Pickup, kapasitas tas, debuff B3 |
| `Scripts/Trash/TrashBin.cs` | 3 tong | Validasi pembuangan |
| `Scripts/Managers/ScoreManager.cs` | GameManager | Poin global, bonus mesin |
| `Scripts/Managers/TrashSpawner.cs` | TrashSpawnerManager | Spawn berkala |
| `Scripts/Managers/ShopManager.cs` | TetuaDesa | Toko 7 upgrade |
| `Scripts/Managers/UIManager.cs` | Canvas_HUD | HUD poin + tas |
| `Scripts/Managers/GameManager.cs` | GameManager | Victory + keluar |
| `Scripts/Managers/AudioManager.cs` | AudioManager | BGM & SFX |
| `Scripts/UI/ScoreHUD.cs` | runtime fallback | HUD pratinjau |
| `Scripts/UI/MainMenu.cs` | Canvas_MainMenu | Menu utama |
| `Scripts/UI/PauseMenu.cs` | Canvas_HUD | Pause/resume/restart/menu |
| `Scripts/VFX/VFXManager.cs` | VFXManager | Spawn particle |

### 14.2 Script Editor

`YeVillageSetup`, `TrashPrefabBuilder`, `ShopSetupBuilder`, `Phase7SceneBuilder`, `AudioSetupBuilder`, `Phase9SceneBuilder`, `VFXSetupBuilder`.

### 14.3 Aset

| Aset | Lokasi |
|---|---|
| 3 prefab sampah | `Assets/Prefabs/Trash/` |
| 4 prefab VFX | `Assets/VFX/Prefabs/` |
| 4 material VFX + tekstur soft circle | `Assets/VFX/Materials/`, `Assets/VFX/Textures/` |
| 7 klip audio placeholder | `Assets/Audio/BGM/`, `Assets/Audio/SFX/` |
| Material Ye Village | `Assets/Materials/Ye_Village/` |

---

## 15. CARA MENJALANKAN & BUILD

### 15.1 Menjalankan di Editor
1. Buka `My project` dengan Unity 6000.3.27f1.
2. Buka scene `Assets/Scenes/MainMenu.unity`.
3. Tekan **Play**.
4. Alur: MULAI BERMAIN → gameplay → kumpulkan 1500 poin → Toko Tetua → Renovasi → Victory.

### 15.2 Kontrol
| Aksi | Tombol |
|---|---|
| Gerak | WASD |
| Ambil/buang sampah, buka toko | E |
| Pause / tutup toko | ESC |
| Putar kamera | Mouse |
| Keluar saat victory | ESC |

### 15.3 Build
1. `File > Build Settings`.
2. Pastikan urutan: **MainMenu (0)**, **SampleScene (1)**.
3. Pilih platform **Windows** (atau sesuai kebutuhan).
4. **Build**.

### 15.4 Setup Ulang dari Nol (Opsional)
Jika scene rusak atau ingin membangun ulang:
1. **Tools > Ye Village Raker > Phase 1** — tag/layer/folder.
2. **YeVillage > Build Trash Prefabs & Setup Spawner**.
3. **YeVillage > Setup Tetua Shop**.
4. **YeVillage > Setup Phase 7 Scene**.
5. **YeVillage > Setup Audio System**.
6. **YeVillage > Setup Phase 9 Menus**.
7. **YeVillage > Setup Phase 10 VFX**.
8. Simpan scene.

---

## 16. PELAJARAN TEKNIS & GOTCHA

1. **MCP Unity putus setelah recompile/domain reload.**
   Tunggu 5–10 detik lalu retry `get_scene_info`; jangan spam perintah.
2. **Batch operasi besar bisa timeout dan dieksekusi sebagian.**
   Gunakan chunk ≤30 operasi dan verifikasi state sebelum lanjut.
3. **Tambah komponen dan set field harus di call terpisah.**
4. **`GameObject.Find` tidak menemukan objek nonaktif.**
   Gunakan `FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)`.
5. **Jangan membuat GameManager kedua.**
   `ScoreManager.Awake()` menghancurkan GameObject duplikat dan bisa ikut menghapus komponen lain.
6. **Tong & sampah harus berjarak** (tong ≥1.8 m dari sampah, antar tong 2.4–2.6 m) agar trigger `[E]` tidak tumpang tindih.
7. **Kapasitas tas awal 1** — saat test, buang dulu sebelum mengambil sampah lain.
8. **Legacy Input** — jangan ubah Active Input Handling ke Input System saja; script memakai `Input.*`.
9. **Gunakan `rb.linearVelocity`** (Unity 6), bukan `rb.velocity` yang obsolete.
10. **`Time.timeScale = 0` mematikan particle default.**
    Semua prefab VFX memakai `main.useUnscaledTime = true`.
11. **Material Particle URP default opaque.**
    Set `_Surface = 1`, alpha blend, `_ZWrite = 0`, `_SURFACE_TYPE_TRANSPARENT`, Render Queue 3000.
12. **UI runtime harus punya EventSystem** — `MainMenu`/`PauseMenu`/`ShopManager` membuatnya otomatis jika belum ada.
13. **`set_play_mode_status play` sering balas error koneksi palsu** — Play tetap berjalan; cek `get_play_mode_status` setelah 7–10 detik.
14. **Crash D3D12 saat smoke test pernah terjadi** — scene sudah tersimpan; restart Unity, dismiss "Recovering Scene Backups", ulangi test.
15. **Jangan commit folder generated** (`Library/`, `Temp/`, `UserSettings/`, `Packages/mcp-unity/`, `Assets/_Recovery/`).

---

## LAMPIRAN: RINGKASAN PROGRESS 10 FASE

| # | Fase | Script Inti | Status |
|---|---|---|---|
| 1 | Fondasi (tag/layer/folder) | `YeVillageSetup` | ✅ Dikonfirmasi |
| 2 | Movement & Camera | `PlayerController`, `ThirdPersonCamera` | ✅ Dikonfirmasi |
| 3 | Inventori & Debuff B3 | `TrashInventory` | ✅ Dikonfirmasi |
| 4 | Pemilahan & Skor | `ScoreManager`, `TrashBin`, `ScoreHUD` | ✅ Dikonfirmasi |
| 5 | Anti-Softlock / Spawner | `TrashSpawner`, `TrashPrefabBuilder` | ✅ Dikonfirmasi |
| 6 | Toko Tetua | `ShopManager`, `ShopSetupBuilder` | ✅ Dikonfirmasi |
| 7 | HUD & Victory | `UIManager`, `GameManager`, `Phase7SceneBuilder` | ✅ Dikonfirmasi |
| 8 | Audio | `AudioManager`, `AudioSetupBuilder` | ✅ Dikonfirmasi |
| 9 | Menu Utama & Pause | `MainMenu`, `PauseMenu`, `Phase9SceneBuilder` | ✅ Dikonfirmasi |
| 10 | VFX & Polish | `VFXManager`, `VFXSetupBuilder` | ✅ Dikonfirmasi |

**Total: 10/10 fase selesai. Proyek siap build/finalisasi.**
