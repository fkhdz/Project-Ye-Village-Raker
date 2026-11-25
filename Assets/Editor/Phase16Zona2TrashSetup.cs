using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 16: Membangun 6 prefab sampah custom Zona 2 (material URP permanen, skala presisi,
/// collider seragam) lalu meng-wire TrashSpawner dengan sistem 3 zona.
/// Menu: YeVillage > 16 - Build Zona 2 Trash Prefabs &amp; Wire Zone Spawner
///
/// Idempoten &amp; tidak memakai EditorUtility.DisplayDialog agar aman diotomasi lewat MCP.
/// </summary>
public static class Phase16Zona2TrashSetup
{
    private const string SourceRoot = "Assets/ThirdParty/Custom_Zona2";
    private const string PrefabFolder = "Assets/Prefabs/Trash";
    private const string MaterialFolder = "Assets/Materials/Custom_Zona2";
    private const string SpawnRootName = "SpawnPoints_Expanded";

    private const string OrganikPrefabPath = PrefabFolder + "/Trash_Organik.prefab";

    private class TrashSpec
    {
        public string folder;
        public string prefabName;
        public string category;
        public string displayName;
        public float targetSize;
        public Vector3 visualEuler;
        public string[] fallbackHex;
        public float metallic;
    }

    private class BuildResult
    {
        public string prefabName;
        public string prefabPath;
        public Vector3 finalSize;
        public int materialCount;
    }

    private static readonly TrashSpec[] Specs =
    {
        new TrashSpec
        {
            folder = "Kaleng", prefabName = "Trash_Anorganik_Kaleng", category = "Anorganik",
            displayName = "Kaleng Soda", targetSize = 0.42f, visualEuler = new Vector3(0f, 24f, 82f),
            fallbackHex = new[] { "#D93B3B", "#C8CDD0" }, metallic = 0.2f
        },
        new TrashSpec
        {
            folder = "Kardus", prefabName = "Trash_Anorganik_Kardus", category = "Anorganik",
            displayName = "Kardus Bekas", targetSize = 0.56f, visualEuler = Vector3.zero,
            fallbackHex = new[] { "#C49A6C" }, metallic = 0.05f
        },
        new TrashSpec
        {
            folder = "Botol", prefabName = "Trash_Anorganik_Botol", category = "Anorganik",
            displayName = "Botol Plastik", targetSize = 0.46f, visualEuler = new Vector3(0f, 0f, 86f),
            fallbackHex = new[] { "#8EB8D9" }, metallic = 0.05f
        },
        new TrashSpec
        {
            folder = "Baterai", prefabName = "Trash_B3_Baterai", category = "B3",
            displayName = "Baterai Bekas", targetSize = 0.42f, visualEuler = new Vector3(0f, 0f, 90f),
            fallbackHex = new[] { "#2E2E32", "#B87333" }, metallic = 0.2f
        },
        new TrashSpec
        {
            folder = "Jerigen", prefabName = "Trash_B3_Jerigen", category = "B3",
            displayName = "Jerigen Kimia", targetSize = 0.62f, visualEuler = new Vector3(8f, 0f, 0f),
            fallbackHex = new[] { "#D94B26" }, metallic = 0.05f
        },
        new TrashSpec
        {
            folder = "Bohlam", prefabName = "Trash_B3_Bohlam", category = "B3",
            displayName = "Lampu Bohlam", targetSize = 0.44f, visualEuler = new Vector3(0f, 0f, 80f),
            fallbackHex = new[] { "#E6D36A" }, metallic = 0.05f
        }
    };

    [MenuItem("YeVillage/16 - Build Zona 2 Trash Prefabs & Wire Zone Spawner")]
    public static void Run()
    {
        EnsureFolder("Assets/Prefabs/Trash");
        EnsureFolder("Assets/Materials/Custom_Zona2");

        List<BuildResult> results = new List<BuildResult>();
        List<GameObject> anorganic = new List<GameObject>();
        List<GameObject> b3 = new List<GameObject>();

        for (int i = 0; i < Specs.Length; i++)
        {
            BuildResult result = BuildPrefab(Specs[i]);
            if (result == null)
            {
                continue;
            }

            results.Add(result);

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(result.prefabPath);
            if (asset == null)
            {
                continue;
            }

            if (Specs[i].category == "B3")
            {
                b3.Add(asset);
            }
            else if (Specs[i].category == "Anorganik")
            {
                anorganic.Add(asset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Metadata TrashItem untuk prefab Organik lama (displayName dibiarkan kosong
        // agar prompt tetap memakai format lama "[E] Pungut Sampah Organik").
        EnsureOrganicMetadata();

        // ---------------- Audit titik spawn ----------------
        int corrected = AuditSpawnPoints(out int zona1Count, out int zona2Count, out int zona3Count);

        // ---------------- Wiring TrashSpawner ----------------
        string wiringStatus = WireSpawner(anorganic, b3, zona1Count, zona2Count, zona3Count);

        // ---------------- Simpan scene ----------------
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ---------------- Ringkasan Console ----------------
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[Phase16] ===== ZONA 2 TRASH PREFABS & ZONE SPAWNER =====");
        sb.AppendLine("Prefab dibuat : " + results.Count + "/6");

        for (int i = 0; i < results.Count; i++)
        {
            BuildResult r = results[i];
            sb.AppendLine("  - " + r.prefabName +
                          " | ukuran " + r.finalSize.x.ToString("0.00") + " x " +
                          r.finalSize.y.ToString("0.00") + " x " +
                          r.finalSize.z.ToString("0.00") + " m" +
                          " | material URP: " + r.materialCount);
        }

        sb.AppendLine("Titik spawn dikoreksi dari area jembatan/air : " + corrected);
        sb.AppendLine("Titik per zona : Zona1=" + zona1Count + ", Zona2=" + zona2Count + ", Zona3=" + zona3Count);
        sb.AppendLine("Wiring TrashSpawner : " + wiringStatus);
        sb.AppendLine("Scene tersimpan : " + (scene.IsValid() ? scene.path : "-"));

        Debug.Log(sb.ToString());
    }

    // ------------------------------------------------------------ prefab builder

    private static BuildResult BuildPrefab(TrashSpec spec)
    {
        string folderPath = SourceRoot + "/" + spec.folder;
        string modelPath = FindModelPath(folderPath);
        if (modelPath == null)
        {
            Debug.LogWarning("[Phase16] Model tidak ditemukan di " + folderPath);
            return null;
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null)
        {
            Debug.LogWarning("[Phase16] Gagal memuat model: " + modelPath);
            return null;
        }

        GameObject root = new GameObject(spec.prefabName);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);

        GameObject instance = Object.Instantiate(model);
        if (PrefabUtility.IsPartOfPrefabInstance(instance))
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        instance.name = model.name;
        instance.transform.SetParent(visual.transform, false);
        instance.transform.localPosition = Vector3.zero;

        // 1. Orientasi visual (rebah/tegak) lalu normalisasi skala dari mesh local bounds.
        visual.transform.localRotation = Quaternion.Euler(spec.visualEuler);
        visual.transform.localScale = Vector3.one;

        Bounds bounds;
        if (!TryGetLocalBounds(root.transform, out bounds))
        {
            Debug.LogWarning("[Phase16] Mesh kosong pada " + spec.folder);
            Object.DestroyImmediate(root);
            return null;
        }

        float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float scaleFactor = maxExtent > 0.0001f ? spec.targetSize / maxExtent : 1f;
        visual.transform.localScale = Vector3.one * scaleFactor;

        // 2. Grounding: geser agar bagian terbawah di Y = 0.01 dan pusat X/Z di 0.
        if (!TryGetLocalBounds(root.transform, out bounds))
        {
            Object.DestroyImmediate(root);
            return null;
        }

        Vector3 delta = new Vector3(-bounds.center.x, 0.01f - bounds.min.y, -bounds.center.z);
        visual.transform.localPosition += delta;

        TryGetLocalBounds(root.transform, out bounds);

        // 3. Konversi material ke URP permanen.
        int materialCount = ConvertMaterials(root, spec);

        // 4. Hapus collider bawaan model, lalu pasang SphereCollider trigger di root.
        Collider[] modelColliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < modelColliders.Length; i++)
        {
            Object.DestroyImmediate(modelColliders[i]);
        }

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.28f, 0f);
        trigger.radius = 0.85f;

        // 5. Tag + metadata TrashItem.
        root.tag = spec.category;

        TrashItem item = root.AddComponent<TrashItem>();
        item.displayName = spec.displayName;
        item.category = spec.category;
        item.originZone = 0;

        // 6. Simpan prefab.
        string prefabPath = PrefabFolder + "/" + spec.prefabName + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (saved == null)
        {
            Debug.LogWarning("[Phase16] Gagal menyimpan prefab: " + prefabPath);
            return null;
        }

        return new BuildResult
        {
            prefabName = spec.prefabName,
            prefabPath = prefabPath,
            finalSize = bounds.size,
            materialCount = materialCount
        };
    }

    private static string FindModelPath(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".fbx" || ext == ".obj")
            {
                return path;
            }
        }

        return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
    }

    /// <summary>
    /// Bounding box gabungan dari semua MeshFilter.sharedMesh.bounds yang ditransformasikan
    /// ke ruang lokal <paramref name="space"/> (bukan Renderer.bounds yang rawan stale).
    /// </summary>
    private static bool TryGetLocalBounds(Transform space, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool has = false;

        MeshFilter[] filters = space.GetComponentsInChildren<MeshFilter>(true);
        Matrix4x4 toSpace = space.worldToLocalMatrix;

        for (int i = 0; i < filters.Length; i++)
        {
            Mesh mesh = filters[i].sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            Bounds meshBounds = mesh.bounds;
            Matrix4x4 matrix = toSpace * filters[i].transform.localToWorldMatrix;

            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = new Vector3(
                    (c & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                    (c & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                    (c & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);

                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!has)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return has;
    }

    // ------------------------------------------------------------ materials

    private static int ConvertMaterials(GameObject root, TrashSpec spec)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[Phase16] Shader 'Universal Render Pipeline/Lit' tidak ditemukan.");
            return 0;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        int materialIndex = 0;

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] targetMaterials = new Material[sourceMaterials.Length];

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                targetMaterials[i] = CreateUrpMaterial(sourceMaterials[i], spec, materialIndex);
                materialIndex++;
            }

            renderer.sharedMaterials = targetMaterials;
        }

        return materialIndex;
    }

    private static Material CreateUrpMaterial(Material source, TrashSpec spec, int index)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        Texture texture = null;
        Color color = Color.white;
        bool broken = source == null;

        if (source != null)
        {
            Shader shader = source.shader;
            if (shader == null || shader.name.Contains("InternalErrorShader"))
            {
                broken = true;
            }

            if (source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
            else if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
            else texture = source.mainTexture;

            if (source.HasProperty("_Color")) color = source.GetColor("_Color");
            else if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
            else color = source.color;
        }

        // Warna transparan/rusak atau putih polos tanpa tekstur dianggap tidak valid.
        if (color.a < 0.1f)
        {
            broken = true;
        }

        if (texture == null && color.r > 0.95f && color.g > 0.95f && color.b > 0.95f)
        {
            broken = true;
        }

        Color finalColor = broken ? FallbackColor(spec, index) : color;
        finalColor.a = 1f;

        string path = MaterialFolder + "/Mat_" + spec.prefabName + "_" + index + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(urpLit);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = urpLit;
        }

        if (texture != null)
        {
            material.SetTexture("_BaseMap", texture);
        }

        material.SetColor("_BaseColor", finalColor);
        material.SetFloat("_Smoothness", 0.25f);
        material.SetFloat("_Metallic", spec.metallic);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Color FallbackColor(TrashSpec spec, int index)
    {
        string hex = spec.fallbackHex[index % spec.fallbackHex.Length];
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }

        return Color.gray;
    }

    // ------------------------------------------------------------ organic metadata

    /// <summary>
    /// Menambahkan TrashItem ke prefab Organik lama (idempoten) tanpa mengubah perilaku
    /// prompt: displayName dibiarkan kosong sehingga fallback ke format lama.
    /// </summary>
    private static void EnsureOrganicMetadata()
    {
        GameObject organic = AssetDatabase.LoadAssetAtPath<GameObject>(OrganikPrefabPath);
        if (organic == null || organic.GetComponent<TrashItem>() != null)
        {
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(OrganikPrefabPath);
        try
        {
            if (contents.GetComponent<TrashItem>() == null)
            {
                TrashItem item = contents.AddComponent<TrashItem>();
                item.displayName = "";
                item.category = "Organik";
                PrefabUtility.SaveAsPrefabAsset(contents, OrganikPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // ------------------------------------------------------------ spawn audit

    private static int AuditSpawnPoints(out int zona1Count, out int zona2Count, out int zona3Count)
    {
        zona1Count = 0;
        zona2Count = 0;
        zona3Count = 0;

        GameObject spawnRoot = GameObject.Find(SpawnRootName);
        if (spawnRoot == null)
        {
            Debug.LogWarning("[Phase16] '" + SpawnRootName + "' tidak ditemukan. Jalankan Modul 15 dulu.");
            return 0;
        }

        int corrected = 0;
        zona1Count = AuditZone(spawnRoot.transform.Find("Zona1_TPS"), ref corrected);
        zona2Count = AuditZone(spawnRoot.transform.Find("Zona2_Riverbank"), ref corrected);
        zona3Count = AuditZone(spawnRoot.transform.Find("Zona3_BalaiDesa"), ref corrected);

        return corrected;
    }

    private static int AuditZone(Transform zone, ref int corrected)
    {
        if (zone == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < zone.childCount; i++)
        {
            Transform point = zone.GetChild(i);
            Vector3 position = point.position;

            bool inBridgeZone = Mathf.Abs(position.x) < 3.5f && position.z > -4.0f && position.z < 11.0f;
            bool inWater = position.z > -2.6f && position.z < 9.6f;

            if (inBridgeZone || inWater)
            {
                float side = position.x >= 0f ? 1f : -1f;
                float x = side * Random.Range(4.5f, 8.0f);

                float z;
                if (position.z < 4f)
                {
                    z = Mathf.Min(position.z, -3.2f);
                }
                else
                {
                    z = Mathf.Max(position.z, 10.2f);
                }

                position = new Vector3(x, position.y, z);
                corrected++;
            }

            // Grounding: raycast ke bawah dari Y = 10 agar duduk di permukaan tanah.
            Physics.SyncTransforms();
            RaycastHit hit;
            float groundY = 0f;
            if (Physics.Raycast(new Vector3(position.x, 10f, position.z), Vector3.down, out hit, 60f, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y <= 1.5f)
                {
                    groundY = hit.point.y;
                }
            }

            point.position = new Vector3(position.x, groundY, position.z);
            count++;
        }

        return count;
    }

    // ------------------------------------------------------------ wiring

    private static string WireSpawner(List<GameObject> anorganic, List<GameObject> b3,
        int zona1Count, int zona2Count, int zona3Count)
    {
        GameObject manager = GameObject.Find("TrashSpawnerManager");
        if (manager == null)
        {
            manager = new GameObject("TrashSpawnerManager");
        }

        TrashSpawner spawner = manager.GetComponent<TrashSpawner>();
        if (spawner == null)
        {
            spawner = manager.AddComponent<TrashSpawner>();
        }

        GameObject organic = AssetDatabase.LoadAssetAtPath<GameObject>(OrganikPrefabPath);

        List<GameObject> organikList = new List<GameObject>();
        if (organic != null)
        {
            organikList.Add(organic);
        }

        GameObject kaleng = FindInList(anorganic, "Kaleng");
        GameObject kardus = FindInList(anorganic, "Kardus");
        GameObject botol = FindInList(anorganic, "Botol");
        GameObject baterai = FindInList(b3, "Baterai");
        GameObject jerigen = FindInList(b3, "Jerigen");
        GameObject bohlam = FindInList(b3, "Bohlam");

        spawner.organicPrefabs = organikList.ToArray();
        spawner.anorganicPrefabs = anorganic.ToArray();
        spawner.b3Prefabs = b3.ToArray();

        // Field tunggal lama tetap diisi agar skrip lama / Phase14VerificationTool aman.
        spawner.organicPrefab = organic;
        spawner.anorganicPrefab = botol;
        spawner.b3Prefab = baterai;

        List<GameObject> all = new List<GameObject>();
        all.AddRange(organikList);
        all.AddRange(anorganic);
        all.AddRange(b3);
        spawner.trashPrefabs = all.ToArray();

        Transform[] z1 = CollectPoints("Zona1_TPS");
        Transform[] z2 = CollectPoints("Zona2_Riverbank");
        Transform[] z3 = CollectPoints("Zona3_BalaiDesa");

        spawner.zona1SpawnPoints = z1;
        spawner.zona2SpawnPoints = z2;
        spawner.zona3SpawnPoints = z3;

        List<Transform> legacyPoints = new List<Transform>();
        legacyPoints.AddRange(z1);
        legacyPoints.AddRange(z2);
        legacyPoints.AddRange(z3);
        spawner.spawnPoints = legacyPoints.ToArray();

        spawner.initialSpawnCount = 28;
        spawner.maxActiveTrash = 34;
        spawner.maxTrashOnMap = 34;
        spawner.spawnInterval = 5f;
        spawner.isSpawning = true;

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(manager.scene);

        bool ok = spawner.trashPrefabs.Length >= 7 &&
                  z1.Length > 0 && z2.Length > 0 && z3.Length > 0 &&
                  botol != null && baterai != null;

        return (ok ? "OK" : "WARNING") +
               " (prefab=" + spawner.trashPrefabs.Length +
               ", titik=" + spawner.spawnPoints.Length +
               " [" + zona1Count + "/" + zona2Count + "/" + zona3Count + "]" +
               ", initial=" + spawner.initialSpawnCount +
               ", max=" + spawner.maxActiveTrash + ")";
    }

    private static Transform[] CollectPoints(string zoneName)
    {
        GameObject spawnRoot = GameObject.Find(SpawnRootName);
        if (spawnRoot == null)
        {
            return new Transform[0];
        }

        Transform zone = spawnRoot.transform.Find(zoneName);
        if (zone == null)
        {
            return new Transform[0];
        }

        Transform[] points = new Transform[zone.childCount];
        for (int i = 0; i < zone.childCount; i++)
        {
            points[i] = zone.GetChild(i);
        }

        return points;
    }

    private static GameObject FindInList(List<GameObject> list, string keyword)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].name.Contains(keyword))
            {
                return list[i];
            }
        }

        return null;
    }

    // ------------------------------------------------------------ util

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
