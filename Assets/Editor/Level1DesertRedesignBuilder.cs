using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Redesign visual Level 1 mengikuti referensi "Level 1" (gurun gersang + Pos TPS).
/// Semua aset memakai model yang SUDAH ada di proyek (tanpa download):
/// - Kenney Nature Kit (pagar, batu/tebing, kaktus, rumput kering)
/// - Quaternius Medieval Village (pondok pasar, gerobak, peti, tong, karung)
/// Menu: YeVillage/Level1/Redesign Desert (Reference)
/// </summary>
public static class Level1DesertRedesignBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string KenneyModels = "Assets/ThirdParty/KenneyNatureKit/Models/";
    private const string QuaternEnv = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/";
    private const string MatYe = "Assets/Materials/Ye_Village/";
    private const string MatLevel1 = "Assets/Materials/Level1/";

    private const string ObstacleLayer = "Obstacle";
    private const string GroundLayer = "Ground";

    private static Material matSand;
    private static Material matWood;
    private static Material matWoodDark;
    private static Material matSandstone;
    private static Material matSandstoneLight;
    private static Material matCracked;
    private static Material matDryPlant;
    private static Material matTrunk;
    private static Material matHouseWall;
    private static Material matRoof;

    [MenuItem("YeVillage/Level1/Redesign Desert (Reference)")]
    public static void RedesignDesert()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[Desert] Scene '" + open.name + "' belum disimpan. Simpan dulu sebelum redesign.");
                return;
            }
        }

        BackupScene();
        EnsureFolder("Assets/Materials/Level1");
        CreateMaterials();

        UnityEngine.Random.InitState(20261124);

        DestroyIfExists("--- ENVIRONMENT ---");
        DestroyIfExists("BoundaryWalls");

        GameObject env = new GameObject("--- ENVIRONMENT ---");
        BuildGroundAndWalls(env.transform);
        BuildCrackedPatches(env.transform);
        BuildFences(env.transform);
        BuildCliffs(env.transform);
        BuildRocks(env.transform);
        BuildVegetation(env.transform);
        BuildProps(env.transform);
        BuildHouses(env.transform);

        RedesignTpsStation();
        BuildTrashPiles();
        UpdateSpawnerAndAreas();
        MovePlayerSpawn();
        SetupAtmosphere();

        InvokeBuildLandscapeAndDecor(env.transform);
        MarkStatic(env);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Desert] Redesign Level 1 selesai. Pos TPS di sudut barat laut, tebing batu pasir di timur, vegetasi kering tersebar, tumpukan sampah di 5 titik.");
    }

    // ================================================================
    // MATERIAL
    // ================================================================
    private static void CreateMaterials()
    {
        matSand = LoadMaterial(MatYe + "M_Sand.mat");
        matWood = LoadMaterial(MatYe + "M_Wood.mat");
        matTrunk = LoadMaterial(MatYe + "M_Trunk.mat");

        matWoodDark = TintedCopy("M_Wood_Dark.mat", MatYe + "M_Wood.mat", new Color(0.38f, 0.25f, 0.12f));
        matSandstone = TintedCopy("M_Sandstone.mat", MatYe + "M_Rock.mat", new Color(0.76f, 0.54f, 0.32f));
        matSandstoneLight = TintedCopy("M_Sandstone_Light.mat", MatYe + "M_Rock.mat", new Color(0.88f, 0.69f, 0.46f));
        matCracked = TintedCopy("M_CrackedEarth.mat", MatYe + "M_Sand.mat", new Color(0.64f, 0.50f, 0.33f));
        matDryPlant = TintedCopy("M_DryPlant.mat", MatYe + "M_LeafYellow.mat", new Color(0.58f, 0.46f, 0.19f));
        matHouseWall = LoadMaterial(MatYe + "M_Wood.mat");
        matRoof = TintedCopy("M_Clay.mat", MatYe + "M_Trunk.mat", new Color(0.65f, 0.30f, 0.15f));
    }

    private static Material LoadMaterial(string path)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) Debug.LogWarning("[Desert] Material tidak ditemukan: " + path);
        return mat;
    }

    private static Material TintedCopy(string fileName, string sourcePath, Color color)
    {
        string path = MatLevel1 + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            if (!AssetDatabase.CopyAsset(sourcePath, path))
            {
                Debug.LogWarning("[Desert] Gagal membuat material turunan: " + path);
                return LoadMaterial(sourcePath);
            }
            AssetDatabase.ImportAsset(path);
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    // ================================================================
    // HELPER METHODS
    // ================================================================

    private static GameObject LoadModel(string path)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) Debug.LogWarning("[Desert] Model tidak ditemukan: " + path);
        return model;
    }

    private static GameObject PlaceSized(Transform parent, GameObject model, string name, Vector3 position,
        float yRotation, float targetHeight, float targetWidth, Material material, bool addCollider,
        float tiltZ = 0f, Material roofMaterial = null)
    {
        if (model == null)
        {
            Debug.LogWarning("[Desert] Model null untuk: " + name);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null) instance = UnityEngine.Object.Instantiate(model);

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Bounds localBounds = MeasureWorldBounds(instance);
        float scale = 1f;
        if (localBounds.size.y > 0.00001f && targetHeight > 0f)
        {
            scale = targetHeight / localBounds.size.y;
        }
        if (targetWidth > 0f)
        {
            float horizontal = Mathf.Max(localBounds.size.x, localBounds.size.z);
            if (horizontal > 0.00001f)
            {
                scale = Mathf.Min(scale, targetWidth / horizontal);
            }
        }
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale)) scale = 1f;

        instance.transform.localScale = Vector3.one * scale;
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yRotation, tiltZ);

        if (roofMaterial != null)
        {
            Renderer[] roofRenderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in roofRenderers)
            {
                Material[] mats = renderer.sharedMaterials;
                bool replaced = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && IsRoofName(mats[i].name))
                    {
                        mats[i] = roofMaterial;
                        replaced = true;
                    }
                }
                if (replaced) renderer.sharedMaterials = mats;
            }
        }

        if (material != null)
        {
            ApplyAllMaterials(instance, material, roofMaterial);
        }

        if (addCollider)
        {
            SetLayer(instance, ObstacleLayer);
            EnsureCollider(instance);
        }

        return instance;
    }

    private static bool IsRoofName(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return false;
        string n = materialName.ToLowerInvariant();
        return n.Contains("roof") || n.Contains("thatch") || n.Contains("tile");
    }

    private static void ApplyAllMaterials(GameObject obj, Material material, Material keep = null)
    {
        if (material == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (keep != null && mats[i] == keep) continue;
                if (mats[i] != material)
                {
                    mats[i] = material;
                    changed = true;
                }
            }
            if (changed) renderer.sharedMaterials = mats;
        }
    }

    private static Bounds MeasureWorldBounds(GameObject instance)
    {
        Renderer[] r = instance.GetComponentsInChildren<Renderer>(true);
        if (r.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        Bounds b = r[0].bounds;
        for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
        return b;
    }

    private static void EnsureCollider(GameObject obj)
    {
        if (obj.GetComponentInChildren<Collider>() != null) return;
        BoxCollider c = obj.AddComponent<BoxCollider>();
        Bounds b = MeasureWorldBounds(obj);
        if (b.size == Vector3.zero) return;
        c.center = obj.transform.InverseTransformPoint(b.center);
        c.size = new Vector3(Mathf.Abs(obj.transform.InverseTransformVector(b.size).x),
            Mathf.Abs(obj.transform.InverseTransformVector(b.size).y),
            Mathf.Abs(obj.transform.InverseTransformVector(b.size).z));
    }

    private static void SetLayer(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) layer = 0;
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    private static void ApplyMaterial(GameObject obj, Material material)
    {
        if (material == null) return;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = material;
    }

    private static void MarkStatic(GameObject root)
    {
        if (root == null) return;
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic
            | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic
            | StaticEditorFlags.ReflectionProbeStatic;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }
    }

    // ================================================================
    // 0. GROUND, INVISIBLE WALLS
    // ================================================================
    private static void BuildGroundAndWalls(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(parent, false);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(60f, 1f, 60f);
        ground.layer = LayerMask.NameToLayer(GroundLayer);
        ApplyMaterial(ground, matSand);

        GameObject group = new GameObject("InvisibleWalls");
        group.transform.SetParent(parent, false);
        CreateWall(group.transform, "Wall_South", new Vector3(0f, 1.5f, -30.5f), new Vector3(62f, 4f, 1f));
        CreateWall(group.transform, "Wall_West", new Vector3(-30.5f, 1.5f, 0f), new Vector3(1f, 4f, 62f));
        CreateWall(group.transform, "Wall_East", new Vector3(30.5f, 1.5f, 0f), new Vector3(1f, 4f, 62f));
        CreateWall(group.transform, "Wall_North_Left", new Vector3(-17f, 1.5f, 30.5f), new Vector3(27f, 4f, 1f));
        CreateWall(group.transform, "Wall_North_Right", new Vector3(17f, 1.5f, 30.5f), new Vector3(27f, 4f, 1f));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    // ================================================================
    // 0b. TANAH RETAK
    // ================================================================
    private static void BuildCrackedPatches(Transform parent)
    {
        GameObject group = new GameObject("CrackedPatches");
        group.transform.SetParent(parent, false);

        Vector3[] patches = {
            new Vector3(-4f, 0f, -4f), new Vector3(9f, 0f, -6f),
            new Vector3(-12f, 0f, 12f), new Vector3(13f, 0f, 16f),
            new Vector3(-3f, 0f, 24f), new Vector3(-20f, 0f, -18f) };
        float[] sizes = { 5.5f, 4.2f, 6.0f, 4.8f, 5.2f, 4.4f };

        for (int i = 0; i < patches.Length; i++)
        {
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "CrackedGround_" + (i + 1);
            patch.transform.SetParent(group.transform, false);
            patch.transform.position = patches[i] + new Vector3(0f, 0.015f, 0f);
            patch.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            patch.transform.localScale = new Vector3(sizes[i], 0.03f, sizes[i] * UnityEngine.Random.Range(0.6f, 1.1f));
            ApplyMaterial(patch, matCracked);
            UnityEngine.Object.DestroyImmediate(patch.GetComponent<Collider>());
        }
    }

    // ================================================================
    // 1. PAGAR
    // ================================================================
    private static void BuildFences(Transform parent)
    {
        GameObject group = new GameObject("Fences");
        group.transform.SetParent(parent, false);

        GameObject planks = LoadModel(KenneyModels + "fence_planks.fbx");
        GameObject gate = LoadModel(KenneyModels + "fence_gate.fbx");

        const float spacing = 3.85f;
        const float height = 1.35f;
        const float width = 3.9f;

        for (float z = -29f; z <= 29.2f; z += spacing)
            FencePiece(group.transform, planks, new Vector3(-29.5f, 0f, z), 90f, height, width);
        for (float x = -29f; x <= 29.2f; x += spacing)
            FencePiece(group.transform, planks, new Vector3(x, 0f, -29.5f), 0f, height, width);
        for (float x = -29f; x <= 29.2f; x += spacing)
            if (Mathf.Abs(x) > 4.6f)
                FencePiece(group.transform, planks, new Vector3(x, 0f, 29.5f), 180f, height, width);
        for (float z = -29f; z <= 29.2f; z += spacing)
            FencePiece(group.transform, planks, new Vector3(29.5f, 0f, z), -90f, height, width);

        PlaceSized(group.transform, gate, "Fence_Gate_Exit", new Vector3(0f, 0f, 29.5f),
            180f, 1.55f, 4.4f, matWood, true, 0f, null);

        int index = 1;
        foreach (Transform child in group.transform)
            if (child.name != "Fence_Gate_Exit") child.name = "Fence_" + (index++);
    }

    private static void FencePiece(Transform parent, GameObject planks, Vector3 position, float yRotation, float height, float width)
    {
        if (planks == null) return;
        if (UnityEngine.Random.value < 0.22f) return;
        float tilt = UnityEngine.Random.value < 0.35f ? UnityEngine.Random.Range(-2.5f, 2.5f) : 0f;
        PlaceSized(parent, planks, "Fence_Piece", position, yRotation, height, width, matWood, true, tilt);
    }

    // ================================================================
    // 2. TEBING & BATU
    // ================================================================
    private static void BuildCliffs(Transform parent)
    {
        GameObject group = new GameObject("Cliffs");
        group.transform.SetParent(parent, false);

        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(26.5f, 0f, 25f), 25f, 6.5f);
        PlaceCliff(group.transform, "cliff_blockSlope_rock", new Vector3(24.5f, 0f, 20f), -35f, 5.4f);
        PlaceCliff(group.transform, "cliff_cornerLarge_rock", new Vector3(27f, 0f, 15f), 40f, 6.8f);
        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(25f, 0f, 10f), 5f, 5.2f);
        PlaceCliff(group.transform, "cliff_corner_rock", new Vector3(27f, 0f, 5f), 25f, 6.0f);
        PlaceCliff(group.transform, "cliff_blockSlope_rock", new Vector3(24.5f, 0f, 0f), 70f, 4.6f);
        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(26.5f, 0f, -6f), 15f, 5.8f);
        PlaceCliff(group.transform, "cliff_cornerLarge_rock", new Vector3(24f, 0f, -12f), -30f, 4.8f);
        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(26.5f, 0f, -18f), 10f, 6.2f);
        PlaceCliff(group.transform, "cliff_blockSlope_rock", new Vector3(24.5f, 0f, -24f), 55f, 4.4f);
        PlaceCliff(group.transform, "cliff_corner_rock", new Vector3(25.5f, 0f, -28f), 0f, 5.0f);
        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(-25.5f, 0f, 27.5f), 20f, 5.0f);
        PlaceCliff(group.transform, "cliff_cornerLarge_rock", new Vector3(-21f, 0f, 28f), 60f, 4.0f);
        PlaceCliff(group.transform, "cliff_blockSlope_rock", new Vector3(-28.5f, 0f, 22f), -20f, 4.4f);
        PlaceCliff(group.transform, "cliff_corner_rock", new Vector3(-28.5f, 0f, 16f), 0f, 3.6f);
        PlaceCliff(group.transform, "cliff_large_rock", new Vector3(-27.5f, 0f, -20f), 35f, 4.2f);
    }

    private static void PlaceCliff(Transform parent, string modelName, Vector3 position, float yRotation, float height)
    {
        GameObject model = LoadModel(KenneyModels + modelName + ".fbx");
        if (model == null) return;
        Material mat = UnityEngine.Random.value < 0.4f ? matSandstoneLight : matSandstone;
        PlaceSized(parent, model, "Cliff_" + modelName, position, yRotation, height, 0f, mat, true);
    }

    private static void BuildRocks(Transform parent)
    {
        GameObject group = new GameObject("Rocks");
        group.transform.SetParent(parent, false);

        PlaceRock(group.transform, "rock_largeA", new Vector3(22f, 0f, 22f), 25f, 1.8f);
        PlaceRock(group.transform, "rock_largeB", new Vector3(21f, 0f, 12f), -35f, 1.5f);
        PlaceRock(group.transform, "rock_largeC", new Vector3(22.5f, 0f, 3f), 10f, 2.0f);
        PlaceRock(group.transform, "rock_largeD", new Vector3(20.5f, 0f, -7f), 60f, 1.6f);
        PlaceRock(group.transform, "rock_largeE", new Vector3(22f, 0f, -17f), 0f, 2.1f);
        PlaceRock(group.transform, "rock_largeF", new Vector3(21f, 0f, -26f), 45f, 1.5f);
        PlaceRock(group.transform, "rock_tallA", new Vector3(23.5f, 0f, 18f), 15f, 2.2f);
        PlaceRock(group.transform, "rock_tallB", new Vector3(23f, 0f, -2f), -25f, 2.0f);
        PlaceRock(group.transform, "rock_tallC", new Vector3(23.5f, 0f, -22f), 40f, 1.9f);
        PlaceRock(group.transform, "rock_largeA", new Vector3(-24f, 0f, -6f), 45f, 1.4f);
        PlaceRock(group.transform, "rock_largeC", new Vector3(-21f, 0f, 14f), 70f, 1.2f);

        Vector3[] small = { new Vector3(15f, 0f, 8f), new Vector3(-18f, 0f, -13f),
            new Vector3(4f, 0f, 10f), new Vector3(-13f, 0f, -22f),
            new Vector3(18f, 0f, -13f), new Vector3(-24f, 0f, 18f),
            new Vector3(9f, 0f, 26f), new Vector3(16f, 0f, -24f) };
        string[] smallModels = { "rock_smallA", "rock_smallB", "rock_smallC", "rock_smallD" };
        for (int i = 0; i < small.Length; i++)
        {
            GameObject model = LoadModel(KenneyModels + smallModels[i % smallModels.Length] + ".fbx");
            if (model == null) continue;
            PlaceSized(group.transform, model, "Rock_Small_" + i, small[i],
                UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0.35f, 0.65f),
                UnityEngine.Random.Range(0.5f, 0.9f), matSandstone, false);
        }
    }

    private static void PlaceRock(Transform parent, string modelName, Vector3 position, float yRotation, float height)
    {
        GameObject model = LoadModel(KenneyModels + modelName + ".fbx");
        if (model == null) return;
        PlaceSized(parent, model, "Rock_" + modelName, position, yRotation, height, 0f, matSandstone, true);
    }

    // ================================================================
    // 3. VEGETASI KERING
    // ================================================================
    private static void BuildVegetation(Transform parent)
    {
        GameObject group = new GameObject("Vegetation");
        group.transform.SetParent(parent, false);

        GameObject bush = LoadModel(KenneyModels + "plant_bush.fbx");
        GameObject bushSmall = LoadModel(KenneyModels + "plant_bushSmall.fbx");
        GameObject grass = LoadModel(KenneyModels + "grass.fbx");
        GameObject grassLarge = LoadModel(KenneyModels + "grass_large.fbx");
        GameObject cactusShort = LoadModel(KenneyModels + "cactus_short.fbx");
        GameObject cactusTall = LoadModel(KenneyModels + "cactus_tall.fbx");

        Vector3[] bushPositions = {
            new Vector3(-27f, 0f, -2f), new Vector3(-24f, 0f, 8f),
            new Vector3(-26f, 0f, -14f), new Vector3(-19f, 0f, -25f),
            new Vector3(-8f, 0f, -27f), new Vector3(6f, 0f, -27f),
            new Vector3(17f, 0f, -25f), new Vector3(19f, 0f, 9f),
            new Vector3(-8f, 0f, 26f), new Vector3(-20f, 0f, 24f),
            new Vector3(10f, 0f, 13f), new Vector3(-13f, 0f, 3f),
            new Vector3(8f, 0f, 23f), new Vector3(19f, 0f, -5f),
            new Vector3(-2f, 0f, -20f), new Vector3(12f, 0f, -9f) };

        for (int i = 0; i < bushPositions.Length; i++)
        {
            GameObject model = (i % 3 == 0) ? bushSmall : bush;
            if (model == null) continue;
            PlaceSized(group.transform, model, "DryBush_" + i, bushPositions[i],
                UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0.55f, 1.05f),
                UnityEngine.Random.Range(0.9f, 1.7f), matDryPlant, false);
        }

        for (int i = 0; i < 22; i++)
        {
            Vector3 pos = new Vector3(UnityEngine.Random.Range(-27f, 27f), 0f,
                UnityEngine.Random.Range(-27f, 27f));
            if ((pos - new Vector3(-18f, 0f, 17f)).magnitude < 7f) continue;
            if ((pos - new Vector3(2f, 0f, -12f)).magnitude < 5f) continue;
            if (pos.x > 18f) continue;
            GameObject model = (i % 2 == 0) ? grass : grassLarge;
            if (model == null) continue;
            PlaceSized(group.transform, model, "DryGrass_" + i, pos,
                UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0.3f, 0.55f),
                UnityEngine.Random.Range(0.55f, 0.95f), matDryPlant, false);
        }

        PlaceSized(group.transform, cactusTall, "Cactus_01", new Vector3(-25.5f, 0f, 4f),
            25f, 2.7f, 0f, matDryPlant, true);
        PlaceSized(group.transform, cactusShort, "Cactus_02", new Vector3(21f, 0f, -16f),
            -15f, 2.3f, 0f, matDryPlant, true);
        PlaceSized(group.transform, cactusTall, "Cactus_03", new Vector3(16.5f, 0f, 26f),
            60f, 2.4f, 0f, matDryPlant, true);
        PlaceSized(group.transform, cactusShort, "Cactus_04", new Vector3(-16f, 0f, -23f),
            10f, 1.9f, 0f, matDryPlant, true);
    }

    // ================================================================
    // 4. PROPS UMUM
    // ================================================================
    private static void BuildProps(Transform parent)
    {
        GameObject group = new GameObject("Props");
        group.transform.SetParent(parent, false);

        GameObject sign = LoadModel(KenneyModels + "sign.fbx");
        PlaceSized(group.transform, sign, "Sign_Exit", new Vector3(5.5f, 0f, 26.5f),
            -20f, 1.7f, 0f, matWoodDark, true);

        GameObject crate = LoadModel(QuaternEnv + "Props/Crate.fbx");
        GameObject package = LoadModel(QuaternEnv + "Props/Package_1.fbx");
        GameObject barrel = LoadModel(QuaternEnv + "Props/Barrel.fbx");

        PlaceSized(group.transform, crate, "Junk_Crate_01", new Vector3(17.6f, 0f, -7.6f),
            -25f, 0.65f, 0f, matWood, true);
        PlaceSized(group.transform, package, "Junk_Package_01", new Vector3(18.5f, 0f, -6.6f),
            30f, 0.55f, 0f, matWoodDark, false);
        PlaceSized(group.transform, barrel, "Junk_Barrel_01", new Vector3(16.6f, 0f, -8.6f),
            0f, 0.9f, 0f, matWoodDark, true);
        PlaceSized(group.transform, barrel, "Junk_Barrel_02", new Vector3(17.2f, 0f, -8.9f),
            0f, 0.85f, 0f, matWood, true);
    }

    // ================================================================
    // 5. RUMAH PINGGIR
    // ================================================================
    private static void BuildHouses(Transform parent)
    {
        GameObject group = new GameObject("Houses");
        group.transform.SetParent(parent, false);

        // Deret selatan - bagian tengah kosong sebagai jalan ke gerbang utara
        // Semua rumah menghadap ke utara (0°) agar terlihat natural dan merata
        // Pastikan tidak ada rumah yang kebalik arah (rotasi 180° menyentuh garis tengah)
        PlaceBuilding(group.transform, "House_3", "House_Sel_01", new Vector3(-22f, 0f, -26.5f), 0f, 5.0f);
        PlaceBuilding(group.transform, "House_1", "House_Sel_02", new Vector3(-16.5f, 0f, -26.5f), 0f, 4.6f);
        PlaceBuilding(group.transform, "House_4", "House_Sel_03", new Vector3(-11f, 0f, -26.8f), 180f, 4.8f);
        PlaceBuilding(group.transform, "House_2", "House_Sel_04", new Vector3(11f, 0f, -26.8f), 0f, 4.8f);
        PlaceBuilding(group.transform, "House_1", "House_Sel_05", new Vector3(16.5f, 0f, -26.5f), 0f, 4.6f);
        PlaceBuilding(group.transform, "House_3", "House_Sel_06", new Vector3(22f, 0f, -26.5f), 0f, 5.0f);

        // Deret barat menghadap timur
        // Semua rumah menghadap ke timur (90°) agar merata menghadap ke center
        PlaceBuilding(group.transform, "House_2", "House_Bar_01", new Vector3(-26.8f, 0f, -20f), 90f, 4.8f);
        PlaceBuilding(group.transform, "House_4", "House_Bar_02", new Vector3(-26.8f, 0f, -13.5f), 90f, 4.6f);
        PlaceBuilding(group.transform, "Stable", "Stable_Bar_01", new Vector3(-26.5f, 0f, -7f), 90f, 4.0f);
        PlaceBuilding(group.transform, "House_1", "House_Bar_03", new Vector3(-26.2f, 0f, -1f), 90f, 4.4f);

        // Flank utara: gerbang tetap terbuka (x ~= 0)
        PlaceBuilding(group.transform, "House_2", "House_Ut_01", new Vector3(-14f, 0f, 26.5f), 180f, 4.6f);
        PlaceBuilding(group.transform, "House_4", "House_Ut_02", new Vector3(11f, 0f, 26.5f), 180f, 4.6f);
        // Model Bell_Tower sangat panjang di sumbu z (~14 m), jadi ditempatkan di sisi barat
        // agar tidak keluar peta dan tidak menutup koridor gerbang utara (|x| <= 4.6).
        PlaceBuilding(group.transform, "Bell_Tower", "BellTower_01", new Vector3(-22f, 0f, 12f), 0f, 6.5f);
    }

    private static void PlaceBuilding(Transform parent, string modelName, string name, Vector3 position,
        float yRotation, float height)
    {
        GameObject model = LoadModel(QuaternEnv + "Buildings/" + modelName + ".fbx");
        if (model == null) return;
        PlaceSized(parent, model, name, position, yRotation, height, 0f, matHouseWall, true, 0f, matRoof);
    }

    // ================================================================
    // 6. POS TPS
    // ================================================================
    private static void RedesignTpsStation()
    {
        GameObject tps = GameObject.Find("TPS_Station");
        if (tps == null) { Debug.LogError("[Desert] TPS_Station tidak ditemukan."); return; }

        tps.transform.position = new Vector3(-18f, 0f, 17f);
        tps.transform.rotation = Quaternion.identity;

        string[] oldPieces = { "Pallet", "TPS_BackFence_Left", "TPS_BackFence_Right",
            "TPS_Deck", "TPS_Stall", "TPS_Cart", "TPS_BackFence_01", "TPS_BackFence_02",
            "TPS_BackFence_03", "TPS_Prop_Barrel_01", "TPS_Prop_Barrel_02", "TPS_Prop_Crate_01",
            "TPS_Prop_Bucket" };
        foreach (string piece in oldPieces) DestroyChild(tps.transform, piece);

        BuildTpsDeck(tps.transform);
        MoveChild(tps.transform, "Bin_Green", new Vector3(-20.4f, 0.17f, 15.4f), 0f);
        MoveChild(tps.transform, "Bin_Yellow", new Vector3(-18f, 0.17f, 15.4f), 0f);
        MoveChild(tps.transform, "Bin_Red", new Vector3(-15.6f, 0.17f, 15.4f), 0f);
        MoveChild(tps.transform, "Machine_Compost", new Vector3(-20.6f, 0.15f, 18.4f), 0f);
        MoveChild(tps.transform, "Machine_Press", new Vector3(-15.4f, 0.15f, 18.4f), 0f);

        GameObject stall = LoadModel(QuaternEnv + "Props/MarketStand_2.fbx");
        PlaceSized(tps.transform, stall, "TPS_Stall", new Vector3(-18f, 0f, 21.4f),
            180f, 2.5f, 0f, matWood, true);

        GameObject cart = LoadModel(QuaternEnv + "Props/Cart.fbx");
        PlaceSized(tps.transform, cart, "TPS_Cart", new Vector3(-9.4f, 0f, 18.2f),
            200f, 1.5f, 0f, matWood, true);

        GameObject fence = LoadModel(KenneyModels + "fence_planks.fbx");
        PlaceSized(tps.transform, fence, "TPS_BackFence_01", new Vector3(-21.6f, 0f, 23.6f),
            0f, 1.35f, 3.9f, matWood, true, 1.5f);
        PlaceSized(tps.transform, fence, "TPS_BackFence_02", new Vector3(-17.9f, 0f, 23.6f),
            0f, 1.35f, 3.9f, matWood, true, -1.2f);
        PlaceSized(tps.transform, fence, "TPS_BackFence_03", new Vector3(-14.2f, 0f, 23.6f),
            0f, 1.35f, 3.9f, matWood, true, 1f);

        GameObject barrel = LoadModel(QuaternEnv + "Props/Barrel.fbx");
        PlaceSized(tps.transform, barrel, "TPS_Prop_Barrel_01", new Vector3(-24.2f, 0f, 19.2f),
            0f, 0.9f, 0f, matWoodDark, true);
        PlaceSized(tps.transform, barrel, "TPS_Prop_Barrel_02", new Vector3(-23.8f, 0f, 16.6f),
            0f, 0.85f, 0f, matWoodDark, true);
        PlaceSized(tps.transform, barrel, "TPS_Prop_Bucket", new Vector3(-11.2f, 0f, 15.4f),
            0f, 0.55f, 0f, matWoodDark, false);

        GameObject crate = LoadModel(QuaternEnv + "Props/Crate.fbx");
        PlaceSized(tps.transform, crate, "TPS_Prop_Crate_01", new Vector3(-24.6f, 0f, 14.4f),
            20f, 0.7f, 0f, matWood, true);
        PlaceSized(tps.transform, crate, "TPS_Prop_Crate_02", new Vector3(-11.8f, 0f, 20.4f),
            -12f, 0.6f, 0f, matWood, true);

        GameObject npc = GameObject.Find("NPC_TetuaDesa");
        if (npc != null) { npc.transform.position = new Vector3(-13.6f, 0f, 19.2f); npc.transform.rotation = Quaternion.Euler(0f, 215f, 0f); }
    }

    private static void BuildTpsDeck(Transform parent)
    {
        GameObject deck = new GameObject("TPS_Deck");
        deck.transform.SetParent(parent, false);
        deck.transform.position = new Vector3(-18f, 0f, 17f);
        deck.transform.rotation = Quaternion.identity;

        const int plankCount = 7;
        const float pitch = 0.64f;
        for (int i = 0; i < plankCount; i++)
        {
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Deck_Plank_" + i;
            plank.transform.SetParent(deck.transform, false);
            plank.transform.localPosition = new Vector3(0f, 0.12f, -1.92f + i * pitch);
            plank.transform.localScale = new Vector3(7f, 0.06f, 0.58f);
            ApplyMaterial(plank, i % 2 == 0 ? matWood : matWoodDark);
            UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>());
        }
        for (int i = -1; i <= 1; i++)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Deck_Beam_" + (i + 1);
            beam.transform.SetParent(deck.transform, false);
            beam.transform.localPosition = new Vector3(i * 2.6f, 0.05f, 0f);
            beam.transform.localScale = new Vector3(0.16f, 0.1f, 4.3f);
            ApplyMaterial(beam, matWoodDark);
            UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());
        }
        BoxCollider collider = deck.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.075f, 0f);
        collider.size = new Vector3(7f, 0.15f, 4.3f);
    }

    private static void MoveChild(Transform parent, string name, Vector3 position, float yRotation)
    {
        Transform child = parent.Find(name);
        if (child == null) { Debug.LogWarning("[Desert] Child TPS tidak ditemukan: " + name); return; }
        child.position = position;
        child.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    // ================================================================
    // 7. TUMPUKAN SAMPAH
    // ================================================================
    private static void BuildTrashPiles()
    {
        GameObject container = GameObject.Find("TrashContainer");
        if (container == null) { container = new GameObject("TrashContainer"); }
        Transform pileRoot = container.transform.Find("TrashPiles");
        if (pileRoot != null) UnityEngine.Object.DestroyImmediate(pileRoot.gameObject);
        GameObject piles = new GameObject("TrashPiles");
        piles.transform.SetParent(container.transform, false);

        string[] organic = { "Assets/Prefabs/Trash/Organic/PF_Trash_Apel.prefab",
            "Assets/Prefabs/Trash/Organic/PF_Trash_DaunKering.prefab",
            "Assets/Prefabs/Trash/Organic/PF_Trash_KulitPisang.prefab",
            "Assets/Prefabs/Trash/Organic/PF_Trash_TulangIkan.prefab" };
        string[] inorganic = { "Assets/Prefabs/Trash/Inorganic/PF_Trash_BotolPlastik.prefab",
            "Assets/Prefabs/Trash/Inorganic/PF_Trash_KalengSoda.prefab",
            "Assets/Prefabs/Trash/Inorganic/PF_Trash_KardusBekas.prefab" };

        Vector3[] centers = { new Vector3(0f, 0f, -16f), new Vector3(-6f, 0f, 6f),
            new Vector3(14f, 0f, 2f), new Vector3(2f, 0f, 21f), new Vector3(-9f, 0f, 11f) };
        int[] counts = { 6, 5, 6, 5, 4 };

        int index = 0;
        for (int p = 0; p < centers.Length; p++)
            for (int i = 0; i < counts[p]; i++)
            {
                string path = UnityEngine.Random.value < 0.7f ? organic[UnityEngine.Random.Range(0, organic.Length)]
                    : inorganic[UnityEngine.Random.Range(0, inorganic.Length)];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                item.name = "PileTrash_" + (index++);
                item.transform.SetParent(piles.transform, false);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.25f;
                item.transform.position = centers[p] + new Vector3(offset.x, 0.03f, offset.y);
                item.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
    }

    // ================================================================
    // 8. SPAWNER & SPAWN AREA
    // ================================================================
    private static void UpdateSpawnerAndAreas()
    {
        GameObject spawnerObj = GameObject.Find("TrashSpawner");
        if (spawnerObj != null)
        {
            TrashSpawner spawner = spawnerObj.GetComponent<TrashSpawner>();
            if (spawner != null)
            {
                SerializedObject so = new SerializedObject(spawner);
                SetVector3(so, "tpsCenter", new Vector3(-18f, 0f, 17f));
                SetFloat(so, "tpsExclusionRadius", 6.5f);
                SetVector3(so, "tetuaCenter", new Vector3(-13.6f, 0f, 19.2f));
                SetFloat(so, "tetuaExclusionRadius", 3f);
                SetVector3(so, "playerSpawnCenter", new Vector3(2f, 0f, -12f));
                SetFloat(so, "playerSpawnExclusionRadius", 4f);
                SetInt(so, "initialSpawnCount", 8);
                SetInt(so, "maxActiveTrash", 30);
                SetInt(so, "minActiveTrash", 12);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        SetSpawnArea("Spawn_A_JalanUtamaSelatan", new Vector3(0f, 1f, -18f), new Vector3(18f, 3f, 12f), 35);
        SetSpawnArea("Spawn_B_BelakangRumahBarat", new Vector3(-8f, 1f, 5f), new Vector3(12f, 3f, 10f), 25);
        SetSpawnArea("Spawn_C_PagarTimur", new Vector3(16f, 1f, 2f), new Vector3(14f, 3f, 12f), 25);
        SetSpawnArea("Spawn_D_LapanganUtara", new Vector3(2f, 1f, 22f), new Vector3(14f, 3f, 8f), 15);
    }

    private static void SetSpawnArea(string name, Vector3 position, Vector3 size, int weight)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null) { Debug.LogWarning("[Desert] Spawn area tidak ditemukan: " + name); return; }
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        BoxCollider box = obj.GetComponent<BoxCollider>();
        if (box != null) { box.isTrigger = true; box.center = Vector3.zero; box.size = size; }
        TrashSpawnArea area = obj.GetComponent<TrashSpawnArea>();
        if (area != null) area.weight = weight;
    }

    private static void SetVector3(SerializedObject so, string name, Vector3 value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null) property.vector3Value = value;
    }

    private static void SetFloat(SerializedObject so, string name, float value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null) property.floatValue = value;
    }

    private static void SetInt(SerializedObject so, string name, int value)
    {
        SerializedProperty property = so.FindProperty(name);
        if (property != null) property.intValue = value;
    }

    // ================================================================
    // 9. PEMAIN & ATMOSFER
    // ================================================================
    private static void MovePlayerSpawn()
    {
        Vector3 position = new Vector3(2f, 0f, -12f);
        Quaternion rotation = Quaternion.Euler(0f, 325f, 0f);
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint != null) { spawnPoint.transform.position = position; spawnPoint.transform.rotation = rotation; }
        GameObject player = GameObject.Find("PF_Player_Kades");
        if (player != null) { player.transform.position = position; player.transform.rotation = rotation; }
        GameObject camera = GameObject.Find("PF_MainCamera");
        if (camera != null)
        {
            camera.transform.rotation = rotation;
            Camera cam = camera.GetComponent<Camera>();
            if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.98f, 0.82f, 0.55f, 1f); }
        }
    }

    private static void SetupAtmosphere()
    {
        GameObject lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            lightObj.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            Light sun = lightObj.GetComponent<Light>();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.88f, 0.70f, 1f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0055f;
        RenderSettings.fogColor = new Color(0.96f, 0.78f, 0.52f, 1f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.52f, 0.40f, 1f);
    }

    // ================================================================
    // HELPER: DESTROY
    // ================================================================
    private static void DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void BackupScene()
    {
        try
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string backupDir = Path.Combine(projectRoot, "Backups");
            Directory.CreateDirectory(backupDir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string source = Path.Combine(projectRoot, ScenePath.Replace('/', Path.DirectorySeparatorChar));
            string target = Path.Combine(backupDir, "Level1_Zona1_Pinggiran_" + stamp + ".unity");
            File.Copy(source, target, true);
            if (File.Exists(source + ".meta")) File.Copy(source + ".meta", target + ".meta", true);
            Debug.Log("[Desert] Backup scene dibuat: " + target);
        }
        catch (Exception e) { Debug.LogWarning("[Desert] Gagal backup: " + e.Message); }
    }

    // ================================================================
    // 10a. DISTANT LANDSCAPE (Hills sederhana dari Kenney Nature Kit)
    // ================================================================
    private static void BuildDistantLandscape(Transform parent)
    {
        // Gunakan model "platform_beach" dari Kenney Nature Kit yang sudah ada.
        GameObject hill = LoadModel(KenneyModels + "platform_beach.fbx");
        if (hill == null) { hill = LoadModel(KenneyModels + "ground_grass.fbx"); }
        if (hill == null) { Debug.LogWarning("[Desert] Distant landscape model tidak ditemukan, lewati."); return; }

        // Posisikan 4 "bukit" di ujung peta (±40 unit dari center) agar terlihat jauh.
        Vector3[] positions = {
            new Vector3(-40f, 0.5f, -40f),
            new Vector3( 40f, 0.5f, -40f),
            new Vector3(-40f, 0.5f,  40f),
            new Vector3( 40f, 0.5f,  40f)
        };
        foreach (var pos in positions)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(hill);
            inst.name = "DistantDune";
            inst.transform.SetParent(parent, false);
            inst.transform.position = pos;
            inst.transform.localScale = Vector3.one * 12f;
            SetLayer(inst, GroundLayer);
        }
    }

    // ================================================================
    // 10b. TAMBAHAN VEGETASI & PROP (bush & cactus random dari Kenney)
    // ================================================================
    private static void ScatterExtraPlants(Transform parent)
    {
        GameObject bush = LoadModel(KenneyModels + "plant_bush.fbx");
        GameObject cactusTall = LoadModel(KenneyModels + "cactus_tall.fbx");
        GameObject cactusShort = LoadModel(KenneyModels + "cactus_short.fbx");
        GameObject grass = LoadModel(KenneyModels + "grass.fbx");
        GameObject grassLarge = LoadModel(KenneyModels + "grass_large.fbx");

        if (bush == null && cactusTall == null && grass == null) return;

        for (int i = 0; i < 20; i++)
        {
            // Pilih model acak dari yang tersedia
            GameObject[] candidates = new GameObject[] { bush, cactusTall, cactusShort, grass, grassLarge };
            GameObject prefab = candidates[UnityEngine.Random.Range(0, candidates.Length)];
            if (prefab == null) continue;

            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(-30f, 30f),
                0f,
                UnityEngine.Random.Range(-30f, 30f));

            // Jangan letak di area jalan utama (±8 unit) atau area TPS
            if (Mathf.Abs(pos.x) < 8f && Mathf.Abs(pos.z) < 8f) continue;

            float sizeY = (prefab == bush) ? UnityEngine.Random.Range(0.55f, 1.05f) :
                          (prefab == cactusTall || prefab == cactusShort) ? UnityEngine.Random.Range(1.9f, 2.7f) :
                          UnityEngine.Random.Range(0.3f, 0.55f);

            PlaceSized(parent, prefab, $"ExtraPlant_{i}", pos,
                UnityEngine.Random.Range(0f, 360f), sizeY, 0.8f, matDryPlant, false);
        }
    }

    // ================================================================
    // 10c. DEKORASI PROP (crates, barrels, sign dari proyek)
    // ================================================================
    private static void BuildDecoProps(Transform parent)
    {
        GameObject crate = LoadModel(QuaternEnv + "Props/Crate.fbx");
        GameObject barrel = LoadModel(QuaternEnv + "Props/Barrel.fbx");
        GameObject sign = LoadModel(KenneyModels + "sign.fbx");

        GameObject[] prefabs = new GameObject[] { crate, barrel, sign };
        int idx = 0;
        for (int i = 0; i < 12; i++)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(-28f, 28f),
                0f,
                UnityEngine.Random.Range(-28f, 28f));

            // Jangan menumpuk di jalan (±8) atau TPS
            if (Mathf.Abs(pos.x) < 8f && Mathf.Abs(pos.z) < 8f) continue;

            GameObject p = prefabs[idx % prefabs.Length];
            idx++;
            if (p == null) continue;

            // Atur rotasi acak dan skala
            PlaceSized(parent, p, $"Deco_{i}", pos,
                UnityEngine.Random.Range(0f, 360f), 0.9f, 0.9f, matWood, true);
        }
    }

    // Panggil fungsi-fungsi ini dari dalam RedesignDesert()
    private static void InvokeBuildLandscapeAndDecor(Transform parent)
    {
        BuildDistantLandscape(parent);
        ScatterExtraPlants(parent);
        BuildDecoProps(parent);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
