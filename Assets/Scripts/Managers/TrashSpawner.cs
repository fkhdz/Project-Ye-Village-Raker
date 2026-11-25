using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem Pemunculan Sampah Ye Village Raker.
/// 1. AreaVolume (Level 1): spawn berbasis volume, bobot kategori, anti-softlock, starter trash, exclusion zone.
/// 2. LegacyPoints (SampleScene lama): spawn dari titik & prefab array lama.
/// 3. ZonaQuota (Modul 16): sebaran 7/14/7 ke 3 zona + variasi prefab & ground-snap (Modul 18A).
/// </summary>
public class TrashSpawner : MonoBehaviour
{
    public enum SpawnerMode
    {
        AreaVolume,
        LegacyPoints,
        ZonaQuota
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
    // PENGATURAN MODE LAMA (Backward Compatibility)
    // ================================================================

    [Header("Legacy Mode (Backward Compatibility)")]
    public GameObject[] trashPrefabs;
    public Transform[] spawnPoints;

    [Header("Variasi Prefab per Kategori (Modul 16)")]
    public GameObject[] organicPrefabs;
    public GameObject[] anorganicPrefabs;
    public GameObject[] b3Prefabs;

    [Header("Prefab Tunggal (Legacy / Fallback)")]
    public GameObject organicPrefab;
    public GameObject anorganicPrefab;
    public GameObject b3Prefab;

    [Header("Titik Spawn 3 Zona (Modul 16)")]
    public Transform[] zona1SpawnPoints;
    public Transform[] zona2SpawnPoints;
    public Transform[] zona3SpawnPoints;
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

    [Header("Kepadatan Map Luas (Modul 16)")]
    [Tooltip("Jumlah objek sampah awal saat Start (Zona 1: 7, Zona 2: 14, Zona 3: 7).")]
    public int initialSpawnCount = 28;
    [Tooltip("Batas maksimum sampah aktif di map. Jika 0, memakai maxTrashOnMap.")]
    public int maxActiveTrash = 34;
    [Tooltip("Radius minimum antar sampah agar tidak menumpuk.")]
    public float occupiedRadius = 1.6f;
    [Tooltip("Offset XZ acak saat spawn agar penempatan terlihat natural.")]
    public float spawnOffsetJitter = 0.35f;

    // ---------------- Hook Modul 17: progres pembersihan Zona 2 ----------------
    public static event System.Action<int> OnZona2TrashCleaned;
    public int Zona2CleanedCount { get; private set; }

    // Bobot kategori per zona sesuai GDD (halaman 5 & 15).
    private static readonly float[] Zona1Weights = { 0.55f, 0.45f, 0.00f }; // Organik, Anorganik, B3
    private static readonly float[] Zona2Weights = { 0.10f, 0.50f, 0.40f };
    private static readonly float[] Zona3Weights = { 0.33f, 0.34f, 0.33f };

    private const string CatOrganik = "Organik";
    private const string CatAnorganik = "Anorganik";
    private const string CatB3 = "B3";

    // Modul 18A (BUG-09/BUG-04): tinggi & jarak raycast ground-snap.
    private const float GroundSnapStartHeight = 5f;
    private const float GroundSnapDistance = 12f;

    private bool hasInitialSpawned;

    private int EffectiveMax => maxActiveTrash > 0 ? maxActiveTrash : maxTrashOnMap;

    private void Start()
    {
    private void Start()
    {
        if (useFixedSeed)
        {
            Random.InitState(seed);
        }

        if (trashContainer == null)
        {
            GameObject containerObj = GameObject.Find("TrashContainer");
            if (containerObj == null) containerObj = new GameObject("TrashContainer");
            trashContainer = containerObj.transform;
        }

        if (groundLayer.value == 0)
        {
            int gLayer = LayerMask.NameToLayer("Ground");
            groundLayer = gLayer >= 0 ? (1 << gLayer) : (1 << 0);
        }

        if (blockingLayers.value == 0)
        {
            int oLayer = LayerMask.NameToLayer("Obstacle");
            blockingLayers = oLayer >= 0 ? (1 << oLayer) : 0;
        }

        // Modul 18A (BUG-09): populasi awal hanya dijalankan sekali.
        if (hasInitialSpawned) return;
        hasInitialSpawned = true;

        bool hasZonePoints =
            (zona1SpawnPoints != null && zona1SpawnPoints.Length > 0) ||
            (zona2SpawnPoints != null && zona2SpawnPoints.Length > 0) ||
            (zona3SpawnPoints != null && zona3SpawnPoints.Length > 0);
        bool hasArea = spawnAreas != null && spawnAreas.Length > 0;

        if (mode == SpawnerMode.ZonaQuota || (!hasArea && hasZonePoints))
        {
            StartZonaMode();
            return;
        }

        if (mode == SpawnerMode.LegacyPoints || !hasArea)
        {
            StartLegacyMode();
            return;
        }

        StartAreaMode();
    }

    private void StartZonaMode()
    {
        if (!HasAnyPrefab())
        {
            Debug.LogError("[TrashSpawner] Tidak ada prefab sampah. Isi array prefab di Inspector.");
            return;
        }

        if ((zona1SpawnPoints == null || zona1SpawnPoints.Length == 0) &&
            (zona2SpawnPoints == null || zona2SpawnPoints.Length == 0) &&
            (zona3SpawnPoints == null || zona3SpawnPoints.Length == 0))
        {
            Debug.LogError("[TrashSpawner] Tidak ada titik spawn zona. Isi zona1/2/3SpawnPoints di Inspector.");
            return;
        }

        SpawnInitialPopulation();
        spawnCoroutine = StartCoroutine(LegacySpawnRoutine());
    }
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

            if (CountActiveTrash() >= EffectiveMax)
            {
                continue;
            }

            if (mode == SpawnerMode.ZonaQuota)
            {
                int zone = PickRespawnZone();
                TrySpawnInZone(zone);
            }
            else
            {
                if (spawnPoints == null || spawnPoints.Length == 0 ||
                    trashPrefabs == null || trashPrefabs.Length == 0)
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
    }

    // ------------------------------------------------------------ populasi awal

    private void SpawnInitialPopulation()
    {
        int z1 = SpawnQuota(1, 7);
        int z2 = SpawnQuota(2, 14);
        int z3 = SpawnQuota(3, 7);

        Debug.Log("[TrashSpawner] Populasi awal: Zona1=" + z1 + ", Zona2=" + z2 + ", Zona3=" + z3 +
                  " | total aktif=" + CountActiveTrash() + " / " + EffectiveMax);
        LogPopulationReport();
    }

    /// <summary>
    /// Ringkasan QA Modul 16: sebaran kategori per zona + deteksi sampah nyasar di jembatan/air.
    /// </summary>
    private void LogPopulationReport()
    {
        string[] tags = { CatOrganik, CatAnorganik, CatB3 };
        int[,] counts = new int[4, 3];
        int unsafeZona2 = 0;

        for (int t = 0; t < tags.Length; t++)
        {
            GameObject[] all = GameObject.FindGameObjectsWithTag(tags[t]);
            for (int i = 0; i < all.Length; i++)
            {
                TrashItem item = all[i].GetComponent<TrashItem>();
                int zone = item != null ? item.originZone : 0;
                if (zone >= 1 && zone <= 3)
                {
                    counts[zone, t]++;
                }

                if (zone == 2 && !IsSafeZona2Position(all[i].transform.position))
                {
                    unsafeZona2++;
                }
            }
        }

        for (int zone = 1; zone <= 3; zone++)
        {
            Debug.Log("[TrashSpawner] Zona " + zone + " -> Organik=" + counts[zone, 0] +
                      ", Anorganik=" + counts[zone, 1] + ", B3=" + counts[zone, 2]);
        }

        if (unsafeZona2 > 0)
        {
            Debug.LogWarning("[TrashSpawner] " + unsafeZona2 + " sampah Zona 2 berada di area jembatan/air!");
        }
    }

    private int SpawnQuota(int zone, int count)
    {
        Transform[] points = GetZonePoints(zone);
        if (points == null || points.Length == 0)
        {
            return 0;
        }

        List<Transform> pool = new List<Transform>(points);
        Shuffle(pool);

        int spawned = 0;
        for (int i = 0; i < pool.Count && spawned < count; i++)
        {
            if (CountActiveTrash() >= EffectiveMax)
            {
                break;
            }

            if (TrySpawnAt(pool[i], zone))
            {
                spawned++;
            }
        }

        return spawned;
    }

    // ------------------------------------------------------------ spawn helpers

    private bool TrySpawnInZone(int zone)
    {
        Transform[] points = GetZonePoints(zone);
        if (points == null || points.Length == 0)
        {
            return false;
        }

        int start = Random.Range(0, points.Length);
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[(start + i) % points.Length];
            if (TrySpawnAt(point, zone))
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySpawnAt(Transform point, int zone)
    {
        if (point == null)
        {
            return false;
        }

        Vector3 basePos = point.position;
        if (IsPositionOccupied(basePos, occupiedRadius))
        {
            return false;
        }

        string category = PickCategoryForZone(zone);
        GameObject prefab = GetRandomPrefabForCategory(category);
        if (prefab == null)
        {
            return false;
        }

        // Sedikit offset acak agar natural, tapi tetap aman dari air/jembatan.
        Vector2 jitter = Random.insideUnitCircle * spawnOffsetJitter;
        Vector3 spawnPos = basePos + new Vector3(jitter.x, 0f, jitter.y);
        if (zone == 2 && !IsSafeZona2Position(spawnPos))
        {
            spawnPos = basePos;
        }

        // Modul 18A: jangan spawn menumpuk di posisi final (setelah jitter).
        if (IsPositionOccupied(spawnPos, occupiedRadius))
        {
            return false;
        }

        // Modul 18A (BUG-04): tempelkan root sampah tepat ke permukaan tanah/dek.
        if (TryGetGroundY(spawnPos, out float groundY))
        {
            spawnPos.y = groundY;
        }

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject spawned = Instantiate(prefab, spawnPos, rotation);
        spawned.name = prefab.name + "_Z" + zone;

        TrashItem item = spawned.GetComponent<TrashItem>();
        if (item != null)
        {
            item.originZone = zone;
        }

        return true;
    }

    /// <summary>
    /// Modul 18A (BUG-09): cegah dua sampah muncul di titik yang sama.
    /// Tiga lapis pengecekan: memori XZ, physics trigger (SphereCollider sampah),
    /// dan solid obstacle (batu/pohon/tong).
    /// </summary>
    private bool IsPositionOccupied(Vector3 worldPos, float minClearance = 1.8f)
    {
        float sqrClearance = minClearance * minClearance;
        Vector2 xz = new Vector2(worldPos.x, worldPos.z);

        // Cek 1 (Memori): jarak XZ ke sampah aktif mana pun.
        TrashItem[] activeTrash = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
        for (int i = 0; i < activeTrash.Length; i++)
        {
            TrashItem other = activeTrash[i];
            if (other == null)
            {
                continue;
            }

            Vector3 p = other.transform.position;
            if ((new Vector2(p.x, p.z) - xz).sqrMagnitude < sqrClearance)
            {
                return true;
            }
        }

        // Cek 1b (Tag): sampel sampah bawaan scene (Trash_Samples) bisa belum
        // punya komponen TrashItem, jadi harus dideteksi lewat Tag juga.
        string[] tags = { CatOrganik, CatAnorganik, CatB3 };
        for (int t = 0; t < tags.Length; t++)
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(tags[t]);
            for (int i = 0; i < tagged.Length; i++)
            {
                if (tagged[i] == null)
                {
                    continue;
                }

                Vector3 p = tagged[i].transform.position;
                if ((new Vector2(p.x, p.z) - xz).sqrMagnitude < sqrClearance)
                {
                    return true;
                }
            }
        }

        // Cek 2 (Fisika trigger): sampah hanya punya SphereCollider isTrigger,
        // jadi WAJIB QueryTriggerInteraction.Collide agar tidak terlewat.
        Collider[] triggerHits = Physics.OverlapSphere(
            worldPos + Vector3.up * 0.3f, minClearance * 0.65f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < triggerHits.Length; i++)
        {
            Collider col = triggerHits[i];
            if (col != null && col.GetComponentInParent<TrashItem>() != null)
            {
                return true;
            }
        }

        // Cek 3 (Anti-nembus batu/pohon/tong): abaikan Ground, Ignore Raycast, Player, dan sampah.
        int groundLayer = LayerMask.NameToLayer("Ground");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        int solidMask = ~0;
        if (groundLayer >= 0) solidMask &= ~(1 << groundLayer);
        if (ignoreRaycastLayer >= 0) solidMask &= ~(1 << ignoreRaycastLayer);

        Collider[] solidHits = Physics.OverlapSphere(
            worldPos + Vector3.up * 0.55f, 0.38f, solidMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < solidHits.Length; i++)
        {
            Collider col = solidHits[i];
            if (col == null)
            {
                continue;
            }

            if (col.GetComponentInParent<TrashItem>() != null)
            {
                continue;
            }

            if (col.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Modul 18A (BUG-04): cari permukaan tanah/dek di bawah titik spawn.
    /// Abaikan trigger, sampah, dan Player.
    /// </summary>
    private static bool TryGetGroundY(Vector3 candidate, out float groundY)
    {
        groundY = candidate.y;

        RaycastHit[] hits = Physics.RaycastAll(
            candidate + Vector3.up * GroundSnapStartHeight,
            Vector3.down,
            GroundSnapDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
            {
                continue;
            }

            if (col.gameObject.layer == ignoreRaycastLayer)
            {
                continue;
            }

            if (col.GetComponentInParent<TrashItem>() != null)
            {
                continue;
            }

            if (col.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            groundY = hits[i].point.y;
            return true;
        }

        return false;
    }

    private static int PickRespawnZone()
    {
        float roll = Random.value;
        if (roll < 0.25f) return 1;
        if (roll < 0.75f) return 2;
        return 3;
    }

    private static string PickCategoryForZone(int zone)
    {
        float[] weights = zone == 1 ? Zona1Weights : (zone == 2 ? Zona2Weights : Zona3Weights);
        float roll = Random.value;
        float cumulative = 0f;

        if (roll < (cumulative += weights[0])) return CatOrganik;
        if (roll < (cumulative += weights[1])) return CatAnorganik;
        return CatB3;
    }

    /// <summary>
    /// Memilih prefab acak untuk kategori tertentu. Prioritas:
    /// array kategori baru -> field prefab tunggal lama -> trashPrefabs lama (berdasarkan Tag).
    /// </summary>
    public GameObject GetRandomPrefabForCategory(string category)
    {
        string key = string.IsNullOrEmpty(category) ? "" : category.Trim().ToLowerInvariant();

        GameObject[] pool = null;
        GameObject legacy = null;

        // Penting: cek "anorganik" SEBELUM "organik" karena "anorganik" mengandung kata "organik".
        if (key.Contains("anorganik"))
        {
            pool = anorganicPrefabs;
            legacy = anorganicPrefab;
        }
        else if (key.Contains("organik"))
        {
            pool = organicPrefabs;
            legacy = organicPrefab;
        }
        else if (key.Contains("b3"))
        {
            pool = b3Prefabs;
            legacy = b3Prefab;
        }

        GameObject fromPool = PickRandomNonNull(pool);
        if (fromPool != null)
        {
            return fromPool;
        }

        if (legacy != null)
        {
            return legacy;
        }

        // Fallback terakhir: cari di trashPrefabs lama berdasarkan Tag.
        if (trashPrefabs != null)
        {
            List<GameObject> matches = new List<GameObject>();
            for (int i = 0; i < trashPrefabs.Length; i++)
            {
                GameObject candidate = trashPrefabs[i];
                if (candidate != null && candidate.tag.ToLowerInvariant() == key)
                {
                    matches.Add(candidate);
                }
            }

            GameObject fromLegacyList = PickRandomNonNull(matches.ToArray());
            if (fromLegacyList != null)
            {
                return fromLegacyList;
            }
        }

        return null;
    }

    public bool HasAnyPrefab()
    {
        return PickRandomNonNull(organicPrefabs) != null ||
               PickRandomNonNull(anorganicPrefabs) != null ||
               PickRandomNonNull(b3Prefabs) != null ||
               organicPrefab != null ||
               anorganicPrefab != null ||
               b3Prefab != null ||
               PickRandomNonNull(trashPrefabs) != null;
    }

    private static GameObject PickRandomNonNull(GameObject[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        int start = Random.Range(0, pool.Length);
        for (int i = 0; i < pool.Length; i++)
        {
            GameObject candidate = pool[(start + i) % pool.Length];
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    private Transform[] GetZonePoints(int zone)
    {
        Transform[] points = zone == 1 ? zona1SpawnPoints : (zone == 2 ? zona2SpawnPoints : zona3SpawnPoints);

        if (points != null && points.Length > 0)
        {
            return points;
        }

        // Fallback legacy: semua titik lama dipakai bila zona belum di-wire.
        return spawnPoints;
    }

    // ------------------------------------------------------------ utilitas

    private static int CountActiveTrash()
    {
        return GameObject.FindGameObjectsWithTag(CatOrganik).Length
             + GameObject.FindGameObjectsWithTag(CatAnorganik).Length
             + GameObject.FindGameObjectsWithTag(CatB3).Length;
    }

    private static bool IsSafeZona2Position(Vector3 position)
    {
        // Zona terlarang jembatan kayu.
        if (Mathf.Abs(position.x) < 3.5f && position.z > -4.0f && position.z < 11.0f)
        {
            return false;
        }

        // Palung air sungai.
        if (position.z > -2.6f && position.z < 9.6f)
        {
            return false;
        }

        return true;
    }

    private static void Shuffle(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ------------------------------------------------------------ hook Modul 17

    /// <summary>
    /// Dipanggil saat sampah dipungut pemain. Menaikkan penghitung Zona 2
    /// dan memicu event untuk Modul 17 (Zona2WaterVisual.SetCleanliness).
    /// </summary>
    public void NotifyTrashPickedUp(TrashItem item)
    {
        if (item == null || item.originZone != 2)
        {
            return;
        }

        RegisterZona2TrashCleaned();
    }

    public void RegisterZona2TrashCleaned()
    {
        Zona2CleanedCount++;
        OnZona2TrashCleaned?.Invoke(Zona2CleanedCount);
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

    /// <summary>
    /// Menghapus seluruh sampah yang masih berserakan di scene.
    /// Dipakai saat ending agar desa benar-benar bersih setelah renovasi.
    /// </summary>
    public void ClearAllTrash()
    {
        string[] trashTags = { "Organik", "Anorganik", "B3" };
        foreach (string tag in trashTags)
        {
            GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject trash in trashObjects)
            {
                Destroy(trash);
            }
        }
    }
}
