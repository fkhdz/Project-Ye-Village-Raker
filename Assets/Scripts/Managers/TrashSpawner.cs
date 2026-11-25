using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memunculkan sampah baru secara berkala agar pemain tidak kehabisan sumber poin.
/// Dipasang pada GameObject kosong "TrashSpawnerManager" (sesuai dokumen 05).
///
/// Modul 16: Sebaran berbasis 3 Zona GDD + variasi prefab per kategori.
/// 100% backward-compatible: field lama (trashPrefabs, spawnPoints, maxTrashOnMap)
/// tetap dipertahankan sebagai fallback agar Phase14VerificationTool & DemoPresenterHelper aman.
/// </summary>
public class TrashSpawner : MonoBehaviour
{
    [Header("Prefab & Titik Spawn (Legacy / Fallback)")]
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

    [Header("Pengaturan Spawn")]
    public float spawnInterval = 5f;
    public int maxTrashOnMap = 15;
    public bool isSpawning = true;

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
        if (!HasAnyPrefab())
        {
            Debug.LogError("[TrashSpawner] Tidak ada prefab sampah. Isi array prefab di Inspector.");
            return;
        }

        if ((spawnPoints == null || spawnPoints.Length == 0) &&
            (zona1SpawnPoints == null || zona1SpawnPoints.Length == 0) &&
            (zona2SpawnPoints == null || zona2SpawnPoints.Length == 0) &&
            (zona3SpawnPoints == null || zona3SpawnPoints.Length == 0))
        {
            Debug.LogError("[TrashSpawner] Tidak ada titik spawn. Isi array titik spawn di Inspector.");
            return;
        }

        // Modul 18A: guard agar populasi awal tidak pernah dijalankan dua kali.
        if (hasInitialSpawned)
        {
            return;
        }

        hasInitialSpawned = true;
        SpawnInitialPopulation();
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
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

            // Respawn tertimbang: 25% Zona 1, 50% Zona 2, 25% Zona 3.
            int zone = PickRespawnZone();
            TrySpawnInZone(zone);
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
    /// Dipanggil saat ending (Fase 7) untuk menghentikan pemunculan sampah.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
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
