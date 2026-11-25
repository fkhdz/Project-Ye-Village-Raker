using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 18A — Editor tool idempoten untuk menyapu bersih 10 bug teknis & UX:
/// collision solid, anti-double spawn, HUD antrean (runtime), pesan batas peta,
/// akar bola putih silau, material Jerigen, grounding Kardus, dan asap B3.
/// Menu: YeVillage -> 18A - Apply Bugfixes (Collisions, Spawner, HUD, VFX, Materials)
/// </summary>
public static class Phase18ABugfixTool
{
    private const string MenuPath = "YeVillage/18A - Apply Bugfixes (Collisions, Spawner, HUD, VFX, Materials)";

    private const string SkyboxFolder = "Assets/Materials/Ye_Village";
    private const string SkyboxPath = SkyboxFolder + "/Skybox_YeVillage.mat";
    private const string JerigenPrefabPath = "Assets/Prefabs/Trash/Trash_B3_Jerigen.prefab";
    private const string KardusPrefabPath = "Assets/Prefabs/Trash/Trash_Anorganik_Kardus.prefab";
    private const string ToxicMistMatPath = "Assets/Materials/Zona2_VFX/Mat_ToxicMist_URP.mat";
    private const string Zona2PrefabRealPath = "Assets/Prefabs/Zones/Zona2_BantaranSungai.prefab";

    private const string BoundaryMapMessage = "Batas Wilayah Desa Sukamaju - Tidak bisa pergi lebih jauh.";
    private const string BoundaryRiverMessage = "Air sungai terlalu dalam! Gunakan jembatan kayu untuk menyeberang.";

    // Prefab sampah hanya punya SphereCollider trigger; semua pengecekan wajib Collide.
    private static readonly string[] TreePrefixes = { "CommonTree", "Willow" };

    private static readonly string[] BoxPrefixes =
    {
        "rock_largeA", "rock_largeB", "rock_largeE", "rock_smallA",
        "fence_planks", "WoodLog", "TreeStump",
        "House", "Inn", "Blacksmith", "Stable",
        "Crate", "Barrel", "Bags", "sign", "MarketStand", "Bench", "Well", "Hay", "Cart",
        "Tetua_Canopy_Post_A", "Tetua_Canopy_Post_B"
    };

    [MenuItem(MenuPath)]
    public static void ApplyAll()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity")
        {
            Debug.LogWarning("[18A] Scene aktif: " + scene.path + " (disarankan SampleScene.unity).");
        }

        int bloomFixed = FixBloomAndSkybox();
        int binColliders = AddTrashBinSolidColliders();
        int npcColliders = AddNpcAndBuildingColliders();
        int envColliders = AddEnvironmentSolidColliders();
        Physics.SyncTransforms();

        int deckMoved = MoveSpawnPointsOffTpsDeck();
        int spawnNudged = ValidateSpawnPoints();

        int boundaryTriggers = AddBoundaryTriggers();

        FixJerigenPrefab();
        FixKardusPrefab();
        FixToxicMistVfx();

        int zona2Children = ExportZona2Prefab();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int boundaryCount = CountBoundaryTriggers();

        Debug.Log("[18A] ===== RINGKASAN BUGFIX =====");
        Debug.Log("[18A] BUG-06 akar masalah: skybox 'Default-Skybox' (Skybox/Procedural) _SunDisk=1 memancarkan"
                  + " cakram matahari near-white HDR di langit; Bloom global menyebarkannya jadi bola silau."
                  + " Fix: skybox proyek baru _SunDisk=0 + Bloom clamp<=4.0 (bloomFix=" + bloomFixed + ").");
        Debug.Log("[18A] BUG-02/07 solid collider: tong=" + binColliders + ", NPC/gedung=" + npcColliders
                  + ", properti lingkungan=" + envColliders + ".");
        Debug.Log("[18A] BUG-09/04 spawn: titik digeser dari dek TPS=" + deckMoved
                  + ", titik dinudge dari collider padat=" + spawnNudged + ".");
        Debug.Log("[18A] BUG-10 boundary trigger terpasang/terverifikasi=" + boundaryTriggers
                  + " (total di scene=" + boundaryCount + ", layer 2 Ignore Raycast).");
        Debug.Log("[18A] BUG-01/03/04/08: prefab Jerigen, Kardus, dan B3_ToxicMistVFX diperbarui.");
        Debug.Log("[18A] Zona2_BantaranSungai.prefab di-re-export (" + zona2Children + " child).");
        Debug.Log("[18A] Scene disimpan. Selesai.");
    }

    // ================================================================
    //  BUG-06: Bloom clamp + skybox tanpa cakram matahari yang menyilaukan
    // ================================================================

    private static int FixBloomAndSkybox()
    {
        int changes = 0;

        Volume volume = Object.FindFirstObjectByType<Volume>();
        if (volume != null && volume.sharedProfile != null)
        {
            VolumeProfile profile = volume.sharedProfile;
            if (profile.TryGet(out Bloom bloom))
            {
                if (bloom.threshold.value < 1f)
                {
                    bloom.threshold.value = 1f;
                    changes++;
                }

                if (bloom.intensity.value > 0.35f)
                {
                    bloom.intensity.value = 0.35f;
                    changes++;
                }

                // Nilai lama 65472 membuat bloom menyebar sangat jauh.
                if (bloom.clamp.value > 4f)
                {
                    bloom.clamp.value = 4f;
                    changes++;
                }

                EditorUtility.SetDirty(profile);
            }
        }

        if (!AssetDatabase.IsValidFolder(SkyboxFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Ye_Village");
        }

        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
        Shader procedural = Shader.Find("Skybox/Procedural");

        if (procedural == null)
        {
            Debug.LogWarning("[18A] Shader Skybox/Procedural tidak ditemukan.");
            return changes;
        }

        if (skybox == null)
        {
            skybox = new Material(procedural);
            skybox.name = "Skybox_YeVillage";

            Material builtin = RenderSettings.skybox;
            if (builtin != null && builtin.shader == procedural)
            {
                skybox.CopyPropertiesFromMaterial(builtin);
            }

            AssetDatabase.CreateAsset(skybox, SkyboxPath);
            changes++;
        }

        // Akar bola putih silau: cakram matahari procedural (titik hitam di tengahnya
        // adalah artefak pusat cakram). Matikan cakram, pertahankan gradasi langit.
        if (skybox.HasProperty("_SunDisk") && Mathf.Abs(skybox.GetFloat("_SunDisk")) > 0.001f)
        {
            skybox.SetFloat("_SunDisk", 0f);
            changes++;
        }

        if (skybox.HasProperty("_SunSize"))
        {
            skybox.SetFloat("_SunSize", 0.04f);
        }

        if (skybox.HasProperty("_SunSizeConvergence"))
        {
            skybox.SetFloat("_SunSizeConvergence", 5f);
        }

        if (skybox.HasProperty("_Exposure"))
        {
            skybox.SetFloat("_Exposure", 1.3f);
        }

        EditorUtility.SetDirty(skybox);

        if (RenderSettings.skybox != skybox)
        {
            RenderSettings.skybox = skybox;
            changes++;
        }

        DynamicGI.UpdateEnvironment();
        return changes;
    }

    // ================================================================
    //  BUG-02/07: solid collider
    // ================================================================

    private static int AddTrashBinSolidColliders()
    {
        string[] bins = { "TrashBins/TongHijau", "TrashBins/TongKuning", "TrashBins/TongMerah" };
        int added = 0;

        for (int i = 0; i < bins.Length; i++)
        {
            GameObject bin = GameObject.Find(bins[i]);
            if (bin == null)
            {
                continue;
            }

            Transform existing = bin.transform.Find("SolidBodyCollider");
            if (existing != null)
            {
                existing.gameObject.layer = bin.layer;
                continue;
            }

            GameObject solid = new GameObject("SolidBodyCollider");
            solid.layer = bin.layer;
            solid.transform.SetParent(bin.transform, false);
            solid.transform.localPosition = new Vector3(0f, 0.575f, 0f);

            CapsuleCollider capsule = solid.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = 0.42f;
            capsule.height = 1.15f;
            capsule.isTrigger = false;

            added++;
        }

        return added;
    }

    private static int AddNpcAndBuildingColliders()
    {
        int added = 0;

        // TetuaDesa: pertahankan CapsuleCollider trigger untuk [E] toko,
        // tambahkan badan solid terpisah.
        GameObject tetua = GameObject.Find("TetuaDesa");
        if (tetua != null && tetua.transform.Find("SolidBodyCollider") == null)
        {
            GameObject solid = new GameObject("SolidBodyCollider");
            solid.layer = tetua.layer;
            solid.transform.SetParent(tetua.transform, false);
            solid.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            CapsuleCollider capsule = solid.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.radius = 0.4f;
            capsule.height = 1.9f;
            capsule.isTrigger = false;

            added++;
        }

        string[] buildings = { "BalaiDesa_Rusak", "BalaiDesa_Megah", "MesinKomposVisual", "MesinPressVisual" };
        for (int i = 0; i < buildings.Length; i++)
        {
            GameObject building = FindSceneObject(buildings[i]);
            if (building == null || HasSolidCollider(building))
            {
                continue;
            }

            if (!TryGetLocalMeshBounds(building, out Bounds localBounds))
            {
                continue;
            }

            BoxCollider box = building.AddComponent<BoxCollider>();
            box.center = localBounds.center;
            box.size = localBounds.size;
            box.isTrigger = false;
            added++;
        }

        return added;
    }

    private static int AddEnvironmentSolidColliders()
    {
        GameObject environment = GameObject.Find("Environment_Expanded_P15");
        if (environment == null)
        {
            Debug.LogWarning("[18A] Environment_Expanded_P15 tidak ditemukan.");
            return 0;
        }

        int added = 0;
        MeshRenderer[] renderers = environment.GetComponentsInChildren<MeshRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            GameObject go = renderer.gameObject;
            string name = go.name;

            // Pohon ditangani lebih dulu: batang pohon butuh collider khusus dan
            // bisa saja sudah punya CapsuleCollider lama yang salah masak.
            if (StartsWithAny(name, TreePrefixes))
            {
                RemoveLegacyTrunkCapsule(go);
                if (AddTrunkCapsule(go, renderer))
                {
                    added++;
                }

                continue;
            }

            if (go.GetComponent<Collider>() != null)
            {
                continue;
            }

            if (!StartsWithAny(name, BoxPrefixes))
            {
                continue;
            }

            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Bounds meshBounds = filter.sharedMesh.bounds;
            Vector3 lossy = go.transform.lossyScale;
            Vector3 worldSize = new Vector3(
                Mathf.Abs(meshBounds.size.x * lossy.x),
                Mathf.Abs(meshBounds.size.y * lossy.y),
                Mathf.Abs(meshBounds.size.z * lossy.z));

            float maxWorld = Mathf.Max(worldSize.x, Mathf.Max(worldSize.y, worldSize.z));
            if (maxWorld < 0.45f)
            {
                continue;
            }

            bool isRock = name.StartsWith("rock_");
            float shrink = isRock ? 0.88f : 1f;

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = meshBounds.center;
            box.size = meshBounds.size * shrink;
            box.isTrigger = false;
            added++;
        }

        return added;
    }

    /// <summary>
    /// Bersihkan CapsuleCollider lama yang dipasang langsung pada transform pohon
    /// ber-skala ekstrem (bounds-nya salah masak, sehingga tidak berguna).
    /// </summary>
    private static void RemoveLegacyTrunkCapsule(GameObject tree)
    {
        CapsuleCollider legacy = tree.GetComponent<CapsuleCollider>();
        if (legacy != null && legacy.radius < 0.01f)
        {
            Object.DestroyImmediate(legacy);
        }
    }

    private static Transform FindExistingTrunkCollider(Transform parent, Vector3 treePosition)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null || !child.name.StartsWith("TrunkCollider_"))
            {
                continue;
            }

            Vector3 delta = child.position - treePosition;
            if (Mathf.Abs(delta.x) < 0.75f && Mathf.Abs(delta.z) < 0.75f)
            {
                return child;
            }
        }

        return null;
    }

    private static bool AddTrunkCapsule(GameObject go, Renderer renderer)
    {
        Transform parent = go.transform.parent;
        if (parent == null)
        {
            return false;
        }

        // Pohon punya lossyScale ekstrem (ratusan). Collider yang dipasang langsung
        // pada transform itu dimasak Unity dengan bounds salah, sehingga Kades bisa
        // menembus batang. Solusi: collider jadi child dari grup induk (skala 1)
        // sehingga dimensinya berada di ruang dunia yang wajar.
        Vector3 treePosition = go.transform.position;
        if (FindExistingTrunkCollider(parent, treePosition) != null)
        {
            return false;
        }

        Vector3 parentScale = parent.lossyScale;
        if (Mathf.Abs(parentScale.x) < 1e-4f || Mathf.Abs(parentScale.y) < 1e-4f || Mathf.Abs(parentScale.z) < 1e-4f)
        {
            return false;
        }

        float bottomWorld = renderer.bounds.min.y;
        float centerX = treePosition.x;
        float centerZ = treePosition.z;

        GameObject trunk = new GameObject("TrunkCollider_" + go.name);
        trunk.layer = go.layer;
        trunk.transform.SetParent(parent, false);
        trunk.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        trunk.transform.position = new Vector3(centerX, bottomWorld + 1.6f, centerZ);
        trunk.transform.rotation = Quaternion.identity;

        CapsuleCollider capsule = trunk.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.radius = 0.45f;
        capsule.height = 3.2f;
        capsule.isTrigger = false;
        return true;
    }

    private static bool HasSolidCollider(GameObject go)
    {
        Collider[] colliders = go.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetLocalMeshBounds(GameObject root, out Bounds localBounds)
    {
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;
        bool has = false;

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = worldToLocal * filter.transform.localToWorldMatrix;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                    (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                    (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                Vector3 transformed = matrix.MultiplyPoint3x4(point);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            if (!has)
            {
                localBounds = bounds;
                has = true;
            }
            else
            {
                localBounds.Encapsulate(bounds);
            }
        }

        return has;
    }

    // ================================================================
    //  BUG-09 / BUG-04: validasi titik spawn
    // ================================================================

    private static int MoveSpawnPointsOffTpsDeck()
    {
        TrashSpawner spawner = Object.FindFirstObjectByType<TrashSpawner>();
        if (spawner == null || spawner.zona1SpawnPoints == null)
        {
            return 0;
        }

        // Rect dek TPS (sesuai spesifikasi Modul 18A).
        const float minX = -15.5f;
        const float maxX = -4.0f;
        const float minZ = -33.5f;
        const float maxZ = -25.0f;

        int moved = 0;
        for (int i = 0; i < spawner.zona1SpawnPoints.Length; i++)
        {
            Transform point = spawner.zona1SpawnPoints[i];
            if (point == null)
            {
                continue;
            }

            Vector3 p = point.position;
            if (p.x <= minX || p.x >= maxX || p.z <= minZ || p.z >= maxZ)
            {
                continue;
            }

            float dLeft = p.x - minX;
            float dRight = maxX - p.x;
            float dBack = p.z - minZ;
            float dFront = maxZ - p.z;
            float nearest = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBack, dFront));

            if (nearest == dLeft)
            {
                p.x = minX - 0.8f;
            }
            else if (nearest == dRight)
            {
                p.x = maxX + 0.8f;
            }
            else if (nearest == dBack)
            {
                p.z = minZ - 0.8f;
            }
            else
            {
                p.z = maxZ + 0.8f;
            }

            point.position = p;
            EditorUtility.SetDirty(point);
            moved++;
        }

        return moved;
    }

    private static int ValidateSpawnPoints()
    {
        TrashSpawner spawner = Object.FindFirstObjectByType<TrashSpawner>();
        if (spawner == null)
        {
            return 0;
        }

        List<Transform> points = new List<Transform>();
        AppendPoints(points, spawner.zona1SpawnPoints);
        AppendPoints(points, spawner.zona2SpawnPoints);
        AppendPoints(points, spawner.zona3SpawnPoints);
        AppendPoints(points, spawner.spawnPoints);

        int groundLayer = LayerMask.NameToLayer("Ground");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        int solidMask = ~0;
        if (groundLayer >= 0) solidMask &= ~(1 << groundLayer);
        if (ignoreRaycastLayer >= 0) solidMask &= ~(1 << ignoreRaycastLayer);

        int nudged = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Transform point = points[i];
            if (point == null)
            {
                continue;
            }

            Vector3 p = point.position;
            bool moved = false;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                Collider[] hits = Physics.OverlapSphere(
                    p + Vector3.up * 0.55f, 0.38f, solidMask, QueryTriggerInteraction.Ignore);

                bool blocked = false;
                Vector3 away = Vector3.zero;

                for (int h = 0; h < hits.Length; h++)
                {
                    Collider col = hits[h];
                    if (col == null)
                    {
                        continue;
                    }

                    if (col.GetComponentInParent<TrashItem>() != null)
                    {
                        continue;
                    }

                    blocked = true;
                    Vector3 center = col.bounds.center;
                    Vector3 dir = p - center;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.0001f)
                    {
                        dir = Vector3.forward;
                    }

                    away += dir.normalized;
                }

                if (!blocked)
                {
                    break;
                }

                if (away.sqrMagnitude < 0.0001f)
                {
                    away = Vector3.forward;
                }

                p += away.normalized * 1.1f;
                moved = true;
            }

            if (moved)
            {
                point.position = p;
                EditorUtility.SetDirty(point);
                nudged++;
            }
        }

        return nudged;
    }

    private static void AppendPoints(List<Transform> target, Transform[] source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
            {
                target.Add(source[i]);
            }
        }
    }

    // ================================================================
    //  BUG-10: trigger pembatas peta & tepi sungai
    // ================================================================

    private static int AddBoundaryTriggers()
    {
        int count = 0;

        GameObject walls = GameObject.Find("Environment_Expanded_P15/MapBoundary_Walls");
        if (walls == null)
        {
            walls = GameObject.Find("PlayableBounds");
        }

        if (walls != null)
        {
            Transform[] children = walls.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform wall = children[i];
                if (wall == null || wall == walls.transform || wall.name == "BoundaryFeedbackTrigger")
                {
                    continue;
                }

                BoxCollider wallBox = wall.GetComponent<BoxCollider>();
                if (wallBox == null)
                {
                    continue;
                }

                Vector3 inward;
                if (wall.name.Contains("West"))
                {
                    inward = Vector3.right;
                }
                else if (wall.name.Contains("East"))
                {
                    inward = Vector3.left;
                }
                else if (wall.name.Contains("South"))
                {
                    inward = Vector3.forward;
                }
                else if (wall.name.Contains("North"))
                {
                    inward = Vector3.back;
                }
                else
                {
                    inward = Vector3.forward;
                }

                Vector3 size = wallBox.size;
                Vector3 center = wallBox.center;
                float axisExtent = Mathf.Abs(inward.x) > 0.5f ? size.x : size.z;
                float half = axisExtent * 0.5f;

                Vector3 triggerCenter = center + inward * (half + 0.175f);
                Vector3 triggerSize = size;
                if (Mathf.Abs(inward.x) > 0.5f)
                {
                    triggerSize.x = 0.35f;
                }
                else
                {
                    triggerSize.z = 0.35f;
                }

                if (CreateOrUpdateBoundaryTrigger(wall, triggerCenter, triggerSize, BoundaryMapMessage))
                {
                    count++;
                }
            }
        }

        GameObject river = GameObject.Find("Environment_Expanded_P15/RiverChannel/RiverWaterBlockers");
        if (river == null)
        {
            river = GameObject.Find("RiverWaterBlockers");
        }

        if (river != null)
        {
            Transform[] children = river.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform blocker = children[i];
                if (blocker == null || blocker == river.transform || blocker.name == "BoundaryFeedbackTrigger")
                {
                    continue;
                }

                BoxCollider blockerBox = blocker.GetComponent<BoxCollider>();
                if (blockerBox == null)
                {
                    continue;
                }

                // Blocker selatan (z ~ -2) -> sisi darat adalah -z.
                // Blocker utara  (z ~ +9) -> sisi darat adalah +z.
                float worldZ = blocker.position.z;
                Vector3 inward = worldZ < 3.5f ? Vector3.back : Vector3.forward;

                Vector3 size = blockerBox.size;
                Vector3 center = blockerBox.center;
                float half = Mathf.Abs(size.z) * 0.5f;

                Vector3 triggerCenter = center + inward * (half + 0.175f);
                Vector3 triggerSize = new Vector3(size.x, size.y, 0.35f);

                if (CreateOrUpdateBoundaryTrigger(blocker, triggerCenter, triggerSize, BoundaryRiverMessage))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool CreateOrUpdateBoundaryTrigger(Transform parent, Vector3 localCenter, Vector3 size, string message)
    {
        Transform existing = parent.Find("BoundaryFeedbackTrigger");
        GameObject triggerObject;

        if (existing == null)
        {
            triggerObject = new GameObject("BoundaryFeedbackTrigger");
            triggerObject.transform.SetParent(parent, false);
            BoxCollider box = triggerObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            triggerObject.AddComponent<BoundaryFeedbackTrigger>();
        }
        else
        {
            triggerObject = existing.gameObject;
        }

        // KRITIS: Layer 2 (Ignore Raycast) agar tidak mengganggu ThirdPersonCamera.
        triggerObject.layer = 2;
        triggerObject.transform.localPosition = localCenter;
        triggerObject.transform.localRotation = Quaternion.identity;
        triggerObject.transform.localScale = Vector3.one;

        BoxCollider collider = triggerObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = triggerObject.AddComponent<BoxCollider>();
        }

        collider.isTrigger = true;
        collider.center = Vector3.zero;
        collider.size = size;

        BoundaryFeedbackTrigger feedback = triggerObject.GetComponent<BoundaryFeedbackTrigger>();
        if (feedback == null)
        {
            feedback = triggerObject.AddComponent<BoundaryFeedbackTrigger>();
        }

        SerializedObject serialized = new SerializedObject(feedback);
        SerializedProperty messageProperty = serialized.FindProperty("warningMessage");
        if (messageProperty != null)
        {
            messageProperty.stringValue = message;
        }

        SerializedProperty cooldownProperty = serialized.FindProperty("cooldownSeconds");
        if (cooldownProperty != null)
        {
            cooldownProperty.floatValue = 2.5f;
        }

        SerializedProperty durationProperty = serialized.FindProperty("displayDuration");
        if (durationProperty != null)
        {
            durationProperty.floatValue = 2.2f;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(triggerObject);
        return true;
    }

    private static int CountBoundaryTriggers()
    {
        BoundaryFeedbackTrigger[] triggers = Object.FindObjectsByType<BoundaryFeedbackTrigger>(FindObjectsSortMode.None);
        return triggers.Length;
    }

    // ================================================================
    //  BUG-03: material Jerigen
    // ================================================================

    private static void FixJerigenPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(JerigenPrefabPath);
        if (root == null)
        {
            Debug.LogWarning("[18A] Prefab Jerigen tidak ditemukan.");
            return;
        }

        try
        {
            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                // Model OBJ diimpor dengan globalScale 1e-6; sisa scale 9e-7 membuat
                // jerigen nyaris tak terlihat. Kembalikan ke skala meter.
                AlignVisualToGround(root, visual, true, true);
            }

            SetMaterialColor("Assets/Materials/Custom_Zona2/Mat_Trash_B3_Jerigen_0.mat", Hex("#D94B26"));
            SetMaterialColor("Assets/Materials/Custom_Zona2/Mat_Trash_B3_Jerigen_1.mat", Hex("#E5C130"));
            SetMaterialColor("Assets/Materials/Custom_Zona2/Mat_Trash_B3_Jerigen_2.mat", Hex("#2A2A2E"));
            SetMaterialColor("Assets/Materials/Custom_Zona2/Mat_Trash_B3_Jerigen_3.mat", Hex("#E8E8E8"));

            PrefabUtility.SaveAsPrefabAsset(root, JerigenPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetMaterialColor(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Debug.LogWarning("[18A] Material tidak ditemukan: " + path);
            return;
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.28f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.28f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);

        EditorUtility.SetDirty(material);
    }

    // ================================================================
    //  BUG-04: grounding Kardus
    // ================================================================

    private static void FixKardusPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(KardusPrefabPath);
        if (root == null)
        {
            Debug.LogWarning("[18A] Prefab Kardus tidak ditemukan.");
            return;
        }

        try
        {
            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                AlignVisualToGround(root, visual, false, false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, KardusPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>
    /// Geser child Visual sehingga sisi paling bawahnya tepat di Y = +0.01 di atas pivot root.
    /// Memakai world-space renderer bounds agar tahan terhadap skala/rotasi induk.
    /// </summary>
    private static void AlignVisualToGround(GameObject root, Transform visual, bool resetScale, bool centerXZ)
    {
        if (resetScale)
        {
            visual.localScale = Vector3.one;
        }

        Physics.SyncTransforms();

        for (int pass = 0; pass < 3; pass++)
        {
            Bounds world = GetRendererWorldBounds(visual.gameObject);
            if (world.size.sqrMagnitude < 1e-10f)
            {
                return;
            }

            Vector3 rootPosition = root.transform.position;
            Vector3 delta = new Vector3(
                centerXZ ? rootPosition.x - world.center.x : 0f,
                0.01f - world.min.y,
                centerXZ ? rootPosition.z - world.center.z : 0f);

            if (delta.sqrMagnitude < 1e-9f)
            {
                return;
            }

            visual.position += delta;
            Physics.SyncTransforms();
        }
    }

    private static Bounds GetRendererWorldBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        Bounds result = new Bounds(go.transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!has)
            {
                result = renderer.bounds;
                has = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }

        return has ? result : new Bounds(go.transform.position, Vector3.zero);
    }

    // ================================================================
    //  BUG-08: asap toksik B3
    // ================================================================

    private static void FixToxicMistVfx()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ToxicMistMatPath);
        if (material != null)
        {
            // Alpha Blended (bukan Additive) agar asap hijau-gelap terbaca di tanah cokelat.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        ParticleSystem mist = FindParticleSystem("Kades/B3_ToxicMistVFX");
        if (mist == null)
        {
            Debug.LogWarning("[18A] B3_ToxicMistVFX tidak ditemukan.");
            return;
        }

        ParticleSystem.MainModule main = mist.main;
        main.playOnAwake = false;
        main.startSize = new ParticleSystem.MinMaxCurve(0.48f, 0.88f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startColor = Color.white;
        main.maxParticles = 60;

        ParticleSystem.EmissionModule emission = mist.emission;
        emission.enabled = false;
        emission.rateOverTime = 16f;

        ParticleSystem.ShapeModule shape = mist.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        mist.transform.localPosition = new Vector3(0f, 0.55f, 0f);

        Gradient gradient = new Gradient();
        Color toxicGreen = Hex("#4E7D1E");
        Color smokePurple = Hex("#4A385C");
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(toxicGreen, 0f),
                new GradientColorKey(smokePurple, 0.5f),
                new GradientColorKey(smokePurple, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.78f, 0f),
                new GradientAlphaKey(0.38f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = mist.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer mistRenderer = mist.GetComponent<ParticleSystemRenderer>();
        if (mistRenderer != null && material != null)
        {
            mistRenderer.sharedMaterial = material;
        }

        EditorUtility.SetDirty(mist);
        EditorUtility.SetDirty(mist.gameObject);
    }

    private static ParticleSystem FindParticleSystem(string path)
    {
        GameObject go = GameObject.Find(path);
        if (go != null)
        {
            return go.GetComponent<ParticleSystem>();
        }

        return null;
    }

    // ================================================================
    //  Re-export prefab Zona 2
    // ================================================================

    private static int ExportZona2Prefab()
    {
        GameObject environment = GameObject.Find("Environment_Expanded_P15");
        if (environment == null)
        {
            Debug.LogWarning("[18A] Environment_Expanded_P15 tidak ditemukan.");
            return 0;
        }

        GameObject spawnRoot = GameObject.Find("SpawnPoints_Expanded/Zona2_Riverbank");

        GameObject tempRoot = new GameObject("Zona2_BantaranSungai");
        int childCount = 0;

        try
        {
            CloneInto(environment.transform.Find("RiverChannel"), tempRoot.transform, "RiverChannel");
            CloneInto(environment.transform.Find("Zona2_WoodenBridge"), tempRoot.transform, "Zona2_WoodenBridge");
            CloneInto(environment.transform.Find("Zona2_Decor"), tempRoot.transform, "Zona2_Decor");

            if (spawnRoot != null)
            {
                CloneInto(spawnRoot.transform, tempRoot.transform, "Zona2_SpawnPoints");
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(tempRoot, Zona2PrefabRealPath);
            if (saved == null)
            {
                Debug.LogWarning("[18A] Gagal menyimpan " + Zona2PrefabRealPath);
                return 0;
            }

            childCount = saved.GetComponentsInChildren<Transform>(true).Length - 1;
        }
        finally
        {
            Object.DestroyImmediate(tempRoot);
        }

        return childCount;
    }

    private static void CloneInto(Transform source, Transform parent, string name)
    {
        if (source == null)
        {
            return;
        }

        GameObject clone = Object.Instantiate(source.gameObject, parent, true);
        clone.name = name;
        clone.transform.SetParent(parent, true);
    }

    // ================================================================
    //  Utilitas
    // ================================================================

    private static GameObject FindSceneObject(string name)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                return roots[i];
            }

            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int c = 0; c < children.Length; c++)
            {
                if (children[c].name == name)
                {
                    return children[c].gameObject;
                }
            }
        }

        return null;
    }

    private static bool StartsWithAny(string name, string[] prefixes)
    {
        for (int i = 0; i < prefixes.Length; i++)
        {
            if (name.StartsWith(prefixes[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static Color Hex(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }

        return Color.white;
    }
}
