using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem Pemunculan Sampah v2 (Tahap 7 Ye Village Raker).
/// Mendukung 2 mode:
/// 1. AreaVolume (v2 Level 1): Pemunculan berbasis volume area, bobot tipe (Organik/Anorganik),
///    anti-softlock berkala, burst saat sampah rendah, starter trash, dan zona larangan (exclusion).
/// 2. LegacyPoints (v1 SampleScene): Mode titik lama agar map sebelumnya tetap berjalan normal.
/// </summary>
public class TrashSpawner : MonoBehaviour
{
    public enum SpawnerMode
    {
        AreaVolume,
        LegacyPoints
    }

    [Header("Mode Spawner")]
    public SpawnerMode mode = SpawnerMode.AreaVolume;

    // ================================================================
    // PENGATURAN AREA & POOL DATA (Mode Area v2)
    // ================================================================

    [Header("Area Spawn (Volume)")]
    public TrashSpawnArea[] spawnAreas;

    [Header("Pool Data Sampah")]
    public List<TrashData> organicPool = new List<TrashData>();
    public List<TrashData> inorganicPool = new List<TrashData>();
    public List<TrashData> b3Pool = new List<TrashData>();

    [Header("Jumlah & Interval Spawn")]
    [Tooltip("Jumlah sampah yang langsung muncul saat scene mulai (default 15).")]
    [Range(1, 50)] public int initialSpawnCount = 15;

    [Tooltip("Batas maksimal sampah aktif di map pada satu waktu (default 25).")]
    [Range(1, 50)] public int maxActiveTrash = 25;

    [Tooltip("Jika sampah aktif di bawah batas ini, lakukan burst spawn cepat (default 8).")]
    [Range(1, 30)] public int minActiveTrash = 8;

    [Tooltip("Jeda waktu antar pemunculan normal dalam detik (default 6).")]
    public float spawnInterval = 6f;

    [Tooltip("Status apakah pemunculan sedang aktif.")]
    public bool isSpawning = true;

    [Header("Peluang Bobot Sampah")]
    [Tooltip("Peluang muncul sampah Organik (default 50).")]
    [Range(0, 100)] public int organicWeight = 50;

    [Tooltip("Peluang muncul sampah Anorganik (default 50).")]
    [Range(0, 100)] public int inorganicWeight = 50;

    [Tooltip("Peluang muncul sampah B3. HARUS 0 di Level 1. Baru diisi > 0 di Level 2.")]
    [Range(0, 100)] public int b3Weight = 0;

    [Header("Jarak & Physics")]
    [Tooltip("Jarak minimal antar sampah agar tidak bertumpuk (meter).")]
    public float minDistanceBetweenTrash = 1.5f;

    [Tooltip("Layer tanah untuk raycast.")]
    public LayerMask groundLayer;

    [Tooltip("Layer pagar, rumah, dan rintangan untuk deteksi tabrakan.")]
    public LayerMask blockingLayers;

    [Tooltip("Maksimal percobaan mencari posisi valid sebelum menyerah tanpa error.")]
    public int maxPlacementAttempts = 20;

    [Header("Zona Larangan (Exclusion Zones)")]
    public Vector3 tpsCenter = new Vector3(0f, 0f, 4f);
    public float tpsExclusionRadius = 4f;

    public Vector3 tetuaCenter = new Vector3(-9f, 0f, 3f);
    public float tetuaExclusionRadius = 3f;

    public Vector3 playerSpawnCenter = new Vector3(10f, 0f, -12f);
    public float playerSpawnExclusionRadius = 3f;

    [Header("Tutorial Starter Trash")]
    [Tooltip("Munculkan 3 sampah (minimal 1 Organik dan 1 Anorganik) dekat spawn pemain (5-9 meter).")]
    public bool spawnStarterTrash = true;

    [Header("Debug & Seed")]
    public bool useFixedSeed = false;
    public int seed = 0;

    [Header("Hierarchy Container")]
    [Tooltip("Parent transform untuk menampung sampah hasil spawn.")]
    public Transform trashContainer;

    // ================================================================
    // PENGATURAN MODE LAMA (Legacy Backward Compatibility)
    // ================================================================

    [Header("Legacy Mode (Backward Compatibility)")]
    public GameObject[] trashPrefabs;
    public Transform[] spawnPoints;
    public int maxTrashOnMap = 15;

    // Internal tracking
    private readonly List<GameObject> activeTrashList = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private bool b3WarningShown = false;

    /// <summary>
    /// Jumlah sampah aktif saat ini.
    /// </summary>
    public int ActiveTrashCount
    {
        get
        {
            CleanActiveTrashList();
            return activeTrashList.Count;
        }
    }

    private void OnEnable()
    {
        TrashItem.OnTrashSpawned += HandleTrashSpawned;
        TrashItem.OnTrashDestroyed += HandleTrashDestroyed;
    }

    private void OnDisable()
    {
        TrashItem.OnTrashSpawned -= HandleTrashSpawned;
        TrashItem.OnTrashDestroyed -= HandleTrashDestroyed;
    }

    private void Start()
    {
        if (useFixedSeed)
        {
            Random.InitState(seed);
        }

        // Setup container jika belum ditentukan
        if (trashContainer == null)
        {
            GameObject containerObj = GameObject.Find("TrashContainer");
            if (containerObj == null)
            {
                containerObj = new GameObject("TrashContainer");
            }
            trashContainer = containerObj.transform;
        }

        // Setup layer masks jika belum ditentukan di Inspector
        if (groundLayer.value == 0)
        {
            int gLayer = LayerMask.NameToLayer("Ground");
            groundLayer = gLayer >= 0 ? (1 << gLayer) : (1 << 0); // Default ke layer 0 jika tidak ada Ground layer
        }

        if (blockingLayers.value == 0)
        {
            int oLayer = LayerMask.NameToLayer("Obstacle");
            blockingLayers = oLayer >= 0 ? (1 << oLayer) : 0;
        }

        // Deteksi mode: jika tidak ada spawnAreas atau mode diset Legacy, gunakan mode lama
        if (mode == SpawnerMode.LegacyPoints || (spawnAreas == null || spawnAreas.Length == 0))
        {
            StartLegacyMode();
            return;
        }

        // Jalankan Mode v2 Area
        StartAreaMode();
    }

    // ================================================================
    // MODE AREA v2 (LEVEL 1)
    // ================================================================

    private void StartAreaMode()
    {
        // 1. Starter Trash (Tutorial halus)
        int starterCount = 0;
        if (spawnStarterTrash)
        {
            starterCount = SpawnStarterTrashCluster();
        }

        // 2. Initial Spawn awal
        int remainingToSpawn = Mathf.Max(0, initialSpawnCount - starterCount);
        for (int i = 0; i < remainingToSpawn; i++)
        {
            if (ActiveTrashCount >= maxActiveTrash) break;
            SpawnOneTrash();
        }

        Debug.Log("[TrashSpawner v2] Initial spawn selesai. Total aktif: " + ActiveTrashCount + " sampah.");

        // 3. Mulai loop spawner berkala & burst
        spawnCoroutine = StartCoroutine(AreaSpawnRoutine());
    }

    private IEnumerator AreaSpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
            {
                continue;
            }

            // Cek kondisi Burst (saat sampah < minActiveTrash)
            if (ActiveTrashCount < minActiveTrash)
            {
                int needed = minActiveTrash - ActiveTrashCount;
                for (int b = 0; b < needed; b++)
                {
                    if (ActiveTrashCount >= maxActiveTrash) break;
                    SpawnOneTrash();
                    yield return new WaitForSeconds(0.15f);
                }
                continue;
            }

            // Spawn normal berkala
            if (ActiveTrashCount < maxActiveTrash)
            {
                SpawnOneTrash();
            }
        }
    }

    /// <summary>
    /// Men-spawn 3 sampah starter di depan spawn pemain (jarak 5-9 meter).
    /// Dijamin ada minimal 1 Organik dan 1 Anorganik.
    /// </summary>
    private int SpawnStarterTrashCluster()
    {
        int spawned = 0;

        // Arah dari Player Spawn (10, 0, -12) menuju ke arah Pos TPS (0, 0, 4)
        Vector3 forwardDir = new Vector3(-10f, 0f, 16f).normalized;
        Vector3 rightDir = new Vector3(forwardDir.z, 0f, -forwardDir.x);

        // 3 titik di jarak 5-9 meter di depan spawn pemain
        Vector3[] starterOffsets =
        {
            forwardDir * 5.5f + rightDir * -1.2f, // Titik 1 (~5.6m) - Organik
            forwardDir * 7.2f + rightDir * 1.5f,  // Titik 2 (~7.4m) - Anorganik
            forwardDir * 8.8f + rightDir * -0.5f   // Titik 3 (~8.8m) - Acak
        };

        TrashType[] starterTypes =
        {
            TrashType.Organic,
            TrashType.Inorganic,
            Random.value > 0.5f ? TrashType.Organic : TrashType.Inorganic
        };

        for (int i = 0; i < 3; i++)
        {
            Vector3 testPos = playerSpawnCenter + starterOffsets[i];

            // Raycast ke bawah untuk menempelkan ke tanah
            Vector3 rayStart = new Vector3(testPos.x, 10f, testPos.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                testPos = hit.point + Vector3.up * 0.05f;
            }
            else
            {
                testPos.y = 0.05f;
            }

            TrashData data = PickDataForType(starterTypes[i]);
            if (data != null && data.prefab != null)
            {
                Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject instance = Instantiate(data.prefab, testPos, rot, trashContainer);
                activeTrashList.Add(instance);
                spawned++;
            }
        }

        Debug.Log("[TrashSpawner v2] Spawned " + spawned + " starter trash dekat pemain.");
        return spawned;
    }

    /// <summary>
    /// Mencoba menempatkan 1 sampah pada salah satu area acak dengan validasi physics & exclusion.
    /// </summary>
    private bool SpawnOneTrash()
    {
        if (spawnAreas == null || spawnAreas.Length == 0) return false;

        TrashData data = SelectRandomTrashData();
        if (data == null || data.prefab == null) return false;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            TrashSpawnArea area = SelectRandomArea();
            if (area == null) continue;

            Vector3 candidatePoint = area.GetRandomPointInside();

            // Raycast ke bawah untuk mencari permukaan tanah
            Vector3 rayOrigin = new Vector3(candidatePoint.x, 15f, candidatePoint.z);
            Vector3 spawnPosition;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, groundLayer))
            {
                spawnPosition = hit.point + Vector3.up * 0.05f;
            }
            else
            {
                spawnPosition = new Vector3(candidatePoint.x, 0.05f, candidatePoint.z);
            }

            // Validasi: Zona Larangan (Exclusion)
            if (IsInExclusionZone(spawnPosition))
            {
                continue;
            }

            // Validasi: Menabrak obstacle (pagar, rumah, props)
            if (blockingLayers.value != 0 && Physics.CheckSphere(spawnPosition + Vector3.up * 0.25f, 0.35f, blockingLayers))
            {
                continue;
            }

            // Validasi: Jarak minimal dengan sampah lain
            if (IsTooCloseToExistingTrash(spawnPosition))
            {
                continue;
            }

            // Posisi valid! Instantiate sampah
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject trashObj = Instantiate(data.prefab, spawnPosition, rot, trashContainer);
            activeTrashList.Add(trashObj);
            return true;
        }

        // Melewati maxPlacementAttempts tanpa error/crash
        return false;
    }

    private bool IsInExclusionZone(Vector3 pos)
    {
        // 1. Dekat Pos TPS
        Vector2 pos2D = new Vector2(pos.x, pos.z);
        if (Vector2.Distance(pos2D, new Vector2(tpsCenter.x, tpsCenter.z)) < tpsExclusionRadius)
        {
            return true;
        }

        // 2. Dekat Tetua Desa
        if (Vector2.Distance(pos2D, new Vector2(tetuaCenter.x, tetuaCenter.z)) < tetuaExclusionRadius)
        {
            return true;
        }

        // 3. Dekat Player Spawn (agar tidak muncul menumpuk di tempat pemain mulai)
        if (Vector2.Distance(pos2D, new Vector2(playerSpawnCenter.x, playerSpawnCenter.z)) < playerSpawnExclusionRadius)
        {
            return true;
        }

        return false;
    }

    private bool IsTooCloseToExistingTrash(Vector3 pos)
    {
        CleanActiveTrashList();
        float minDstSq = minDistanceBetweenTrash * minDistanceBetweenTrash;

        for (int i = 0; i < activeTrashList.Count; i++)
        {
            if (activeTrashList[i] == null) continue;
            Vector3 diff = activeTrashList[i].transform.position - pos;
            diff.y = 0f; // Bandingkan pada bidang horizontal
            if (diff.sqrMagnitude < minDstSq)
            {
                return true;
            }
        }

        return false;
    }

    private TrashSpawnArea SelectRandomArea()
    {
        int totalWeight = 0;
        for (int i = 0; i < spawnAreas.Length; i++)
        {
            if (spawnAreas[i] != null) totalWeight += Mathf.Max(1, spawnAreas[i].weight);
        }

        if (totalWeight <= 0) return spawnAreas[0];

        int roll = Random.Range(0, totalWeight);
        int current = 0;
        for (int i = 0; i < spawnAreas.Length; i++)
        {
            if (spawnAreas[i] == null) continue;
            current += Mathf.Max(1, spawnAreas[i].weight);
            if (roll < current)
            {
                return spawnAreas[i];
            }
        }

        return spawnAreas[0];
    }

    private TrashData SelectRandomTrashData()
    {
        // Validasi Guard B3
        if (b3Weight > 0 && (b3Pool == null || b3Pool.Count == 0))
        {
            if (!b3WarningShown)
            {
                Debug.LogWarning("[TrashSpawner] b3Weight > 0 namun b3Pool kosong! B3 akan diabaikan.");
                b3WarningShown = true;
            }
        }

        int activeB3Weight = (b3Pool != null && b3Pool.Count > 0) ? b3Weight : 0;
        int activeOrgWeight = (organicPool != null && organicPool.Count > 0) ? organicWeight : 0;
        int activeInorgWeight = (inorganicPool != null && inorganicPool.Count > 0) ? inorganicWeight : 0;

        int totalWeight = activeOrgWeight + activeInorgWeight + activeB3Weight;
        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        if (roll < activeOrgWeight)
        {
            return PickRandomFromList(organicPool);
        }
        else if (roll < activeOrgWeight + activeInorgWeight)
        {
            return PickRandomFromList(inorganicPool);
        }
        else
        {
            return PickRandomFromList(b3Pool);
        }
    }

    private TrashData PickDataForType(TrashType type)
    {
        switch (type)
        {
            case TrashType.Organic:
                return PickRandomFromList(organicPool);
            case TrashType.Inorganic:
                return PickRandomFromList(inorganicPool);
            case TrashType.B3:
                return PickRandomFromList(b3Pool);
            default:
                return null;
        }
    }

    private TrashData PickRandomFromList(List<TrashData> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    private void CleanActiveTrashList()
    {
        activeTrashList.RemoveAll(item => item == null);
    }

    private void HandleTrashSpawned(TrashItem item)
    {
        if (item != null && !activeTrashList.Contains(item.gameObject))
        {
            activeTrashList.Add(item.gameObject);
        }
    }

    private void HandleTrashDestroyed(TrashItem item)
    {
        if (item != null)
        {
            activeTrashList.Remove(item.gameObject);
        }
    }

    // ================================================================
    // MODE LAMA (Backward Compatibility)
    // ================================================================

    private void StartLegacyMode()
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogError("[TrashSpawner] Mode Legacy: trashPrefabs kosong di Inspector.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[TrashSpawner] Mode Legacy: spawnPoints kosong di Inspector.");
            return;
        }

        spawnCoroutine = StartCoroutine(LegacySpawnRoutine());
    }

    private IEnumerator LegacySpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
            {
                continue;
            }

            int currentTrashCount = GameObject.FindGameObjectsWithTag("Organik").Length
                                  + GameObject.FindGameObjectsWithTag("Anorganik").Length
                                  + GameObject.FindGameObjectsWithTag("B3").Length;

            if (currentTrashCount >= maxTrashOnMap)
            {
                continue;
            }

            int randomPoint = Random.Range(0, spawnPoints.Length);
            int randomTrash = Random.Range(0, trashPrefabs.Length);

            if (spawnPoints[randomPoint] == null || trashPrefabs[randomTrash] == null)
            {
                continue;
            }

            Instantiate(trashPrefabs[randomTrash], spawnPoints[randomPoint].position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Dipanggil saat Balai Desa direnovasi atau Victory untuk menghentikan spawn.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        Debug.Log("[TrashSpawner] Spawning dihentikan.");
    }
}
