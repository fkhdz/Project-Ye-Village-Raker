using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Redesign Total Visual & Environment untuk Level 1 (Zona 1: Pinggiran Desa & Pos TPS).
/// Tema: Pinggiran dan Gersang (Arid / Outskirts Wasteland).
/// Elemen:
/// 1. Pagar kayu miring berkarakter dengan bagian rubuh & batu pengisi celah.
/// 2. Kompleks pemukiman pinggiran (Farmstead Barat, Pasar & Sumur Tua Timur, Pondok Penjaga Utara).
/// 3. Gugusan tebing batu pembatas alami yang megah mengitari luar peta (canyon rim).
/// 4. Bebatuan tanah lapang (batu besar, formasi tebing rendah, batu datar/lempengan tertanam di tanah).
/// 5. Pepohonan kering (autumn fall trees, batang kayu mati, tunggul lapuk, dan kaktus gersang).
/// 6. Props pinggiran desa (gerobak rusak, jerami, karung goni, sumur kering, peti kayu, api unggun padam, papan penunjuk arah).
/// 7. Jaringan jalur tanah berdebu alami menghubungkan Player Spawn, Pos TPS, Exit, dan Rumah.
/// 8. Tata cahaya matahari sore hangat (golden hour) dan kabut debu atmosferik (dust haze).
/// Menu: YeVillage/Level1/Build Environment
/// </summary>
public static class Level1EnvironmentBuilder
{
    // --- Path Aset ---
    private const string QuaternEnv = "Assets/ThirdParty/Quaternius/Environment/";
    private const string KenneyModels = "Assets/ThirdParty/KenneyNatureKit/Models/";
    private const string MatYeVillage = "Assets/Materials/Ye_Village/";
    private const string MatQuaternius = "Assets/Materials/Quaternius/";
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";

    // Layer & Tag
    private const string ObstacleLayer = "Obstacle";
    private const string GroundLayer = "Ground";
    private const string DefaultLayer = "Default";

    [MenuItem("YeVillage/Level1/Build Environment")]
    public static void BuildEnvironment()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // Hapus Environment lama jika ada (idempotent)
        DestroyExisting("--- ENVIRONMENT ---");

        // Parent utama
        GameObject envRoot = new GameObject("--- ENVIRONMENT ---");
        envRoot.transform.position = Vector3.zero;
        envRoot.transform.rotation = Quaternion.identity;
        envRoot.transform.localScale = Vector3.one;

        // 0. Ground & InvisibleWalls
        SetupGroundAndWalls(envRoot.transform);

        // 1. Pagar kayu miring keliling area dengan gerbang keluar
        BuildFences(envRoot.transform);

        // 2. Kompleks bangunan desa pinggiran (Houses)
        BuildHouses(envRoot.transform);

        // 3. Props lengkap (Tebing Pembatas, Batu Tanah Lapang, Pohon Kering, Props Desa, Jalur, Rumput Kering)
        BuildProps(envRoot.transform);

        // 4. Pencahayaan & Atmosfer Gersang (Warm Arid Sun & Dust Fog)
        SetupAtmosphereAndLighting();

        // Tandai semua child sebagai Static untuk batching optimal
        MarkStatic(envRoot);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[Level1Env] SUKSES! Redesign visual environment Level 1 telah selesai diterapkan di " + ScenePath +
                  "\nTotal grup environment: " + envRoot.transform.childCount);
    }

    // ================================================================
    // 0. GROUND & INVISIBLE WALLS
    // ================================================================
    private static void SetupGroundAndWalls(Transform parent)
    {
        // Ground utama
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);
            ApplyMaterial(ground, MatYeVillage + "M_Sand.mat");
        }
        ground.transform.SetParent(parent, true);
        int gLayer = LayerMask.NameToLayer(GroundLayer);
        if (gLayer >= 0) ground.layer = gLayer;

        // Pastikan Collider Ground aktif dan tepat
        BoxCollider groundCol = ground.GetComponent<BoxCollider>();
        if (groundCol == null) groundCol = ground.AddComponent<BoxCollider>();
        groundCol.size = Vector3.one;
        groundCol.center = Vector3.zero;

        // InvisibleWalls
        Transform wallsGroup = parent.Find("InvisibleWalls");
        if (wallsGroup == null)
        {
            GameObject wgObj = new GameObject("InvisibleWalls");
            wgObj.transform.SetParent(parent, false);
            wgObj.transform.localPosition = Vector3.zero;
            wgObj.transform.localRotation = Quaternion.identity;
            wgObj.transform.localScale = Vector3.one;
            wallsGroup = wgObj.transform;
        }

        GameObject oldBoundary = GameObject.Find("BoundaryWalls");
        if (oldBoundary != null)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < oldBoundary.transform.childCount; i++)
            {
                children.Add(oldBoundary.transform.GetChild(i));
            }
            foreach (var c in children)
            {
                c.SetParent(wallsGroup, true);
            }
            Object.DestroyImmediate(oldBoundary);
        }
        else if (wallsGroup.childCount == 0)
        {
            // Buat 5 dinding pembatas tak terlihat (X: -30..30, Z: -30..30, dengan celah di utara)
            CreateWall(wallsGroup, "Wall_West", new Vector3(-30.5f, 5f, 0f), new Vector3(1f, 10f, 60f));
            CreateWall(wallsGroup, "Wall_East", new Vector3(30.5f, 5f, 0f), new Vector3(1f, 10f, 60f));
            CreateWall(wallsGroup, "Wall_South", new Vector3(0f, 5f, -30.5f), new Vector3(60f, 10f, 1f));
            CreateWall(wallsGroup, "Wall_North_Left", new Vector3(-17f, 5f, 30.5f), new Vector3(26.5f, 10f, 1f));
            CreateWall(wallsGroup, "Wall_North_Right", new Vector3(17f, 5f, 30.5f), new Vector3(26.5f, 10f, 1f));
        }
    }

    private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;
        SetLayer(wall, ObstacleLayer);
    }

    // ================================================================
    // 1. PAGAR KAYU MIRING KELILING AREA (Pagar Miring & Lapuk)
    // ================================================================
    private static void BuildFences(Transform parent)
    {
        GameObject fenceGroup = new GameObject("Fences");
        fenceGroup.transform.SetParent(parent, false);
        fenceGroup.transform.localPosition = Vector3.zero;
        fenceGroup.transform.localRotation = Quaternion.identity;
        fenceGroup.transform.localScale = Vector3.one;

        string fenceSimple = KenneyModels + "fence_simple.fbx";
        string fencePlanks = KenneyModels + "fence_planks.fbx";
        string fencePlanksDbl = KenneyModels + "fence_planksDouble.fbx";
        string fenceGate = KenneyModels + "fence_gate.fbx";
        string qFence = QuaternEnv + "MedievalVillage/Props/Fence.fbx";

        GameObject fencePrefab = LoadModelWithFallback(fenceSimple, fencePlanks, qFence);
        GameObject fencePlanksPrefab = LoadModel(fencePlanks);
        GameObject fenceDblPrefab = LoadModel(fencePlanksDbl);
        GameObject fenceGatePrefab = LoadModel(fenceGate);

        float fenceSpacing = 2.0f;
        float fenceY = 0f;

        // --- Sisi Selatan (Z = -29.5) ---
        for (float x = -29f; x <= 29f; x += fenceSpacing)
        {
            float tilt = GetRandomTilt();
            PlaceFencePiece(fenceGroup.transform, fencePrefab, fencePlanksPrefab, fenceDblPrefab,
                new Vector3(x, fenceY, -29.5f), Quaternion.Euler(0f, 0f, tilt));
        }

        // --- Sisi Barat (X = -29.5) ---
        for (float z = -29f; z <= 29f; z += fenceSpacing)
        {
            float tilt = GetRandomTilt();
            PlaceFencePiece(fenceGroup.transform, fencePrefab, fencePlanksPrefab, fenceDblPrefab,
                new Vector3(-29.5f, fenceY, z), Quaternion.Euler(0f, 90f, tilt));
        }

        // --- Sisi Timur (X = +29.5) ---
        for (float z = -29f; z <= 29f; z += fenceSpacing)
        {
            float tilt = GetRandomTilt();
            PlaceFencePiece(fenceGroup.transform, fencePrefab, fencePlanksPrefab, fenceDblPrefab,
                new Vector3(29.5f, fenceY, z), Quaternion.Euler(0f, -90f, tilt));
        }

        // --- Sisi Utara (Z = +29.5) dengan Celah Gerbang Exit (X = -3.5 .. 3.5) ---
        for (float x = -29f; x <= 29f; x += fenceSpacing)
        {
            if (x >= -3.5f && x <= 3.5f) continue;

            float tilt = GetRandomTilt();
            PlaceFencePiece(fenceGroup.transform, fencePrefab, fencePlanksPrefab, fenceDblPrefab,
                new Vector3(x, fenceY, 29.5f), Quaternion.Euler(0f, 180f, tilt));
        }

        // Gerbang keluar di celah utara (X=0, Z=29.5)
        if (fenceGatePrefab != null)
        {
            GameObject gateObj = PlaceModel(fenceGroup.transform, fenceGatePrefab,
                new Vector3(0f, fenceY, 29.5f), Quaternion.Euler(0f, 180f, 0f),
                Vector3.one * 1.4f, "Fence_Gate_Exit", true);
        }

        // Format nama nomor urut dua digit (Fence_01, Fence_02, ...)
        int count = 1;
        for (int i = 0; i < fenceGroup.transform.childCount; i++)
        {
            Transform fc = fenceGroup.transform.GetChild(i);
            if (fc.name != "Fence_Gate_Exit")
            {
                fc.name = string.Format("Fence_{0:D2}", count++);
            }
        }
    }

    private static float GetRandomTilt()
    {
        // 40% kemungkinan pagar miring antara -15 s/d 15 derajat
        if (Random.value < 0.40f)
        {
            return Random.Range(-14f, 14f);
        }
        return 0f;
    }

    private static void PlaceFencePiece(Transform parent, GameObject primary, GameObject planks, GameObject planksDbl,
        Vector3 pos, Quaternion rot)
    {
        float r = Random.value;
        GameObject prefab = primary;
        if (r < 0.25f && planks != null) prefab = planks;
        else if (r < 0.45f && planksDbl != null) prefab = planksDbl;

        if (prefab == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Fence_Temp";
            cube.transform.SetParent(parent, false);
            cube.transform.position = pos;
            cube.transform.rotation = rot;
            cube.transform.localScale = new Vector3(2f, 1.2f, 0.15f);
            SetLayer(cube, ObstacleLayer);
            ApplyMaterial(cube, MatYeVillage + "M_Wood.mat");
            return;
        }

        PlaceModel(parent, prefab, pos, rot, Vector3.one * 1.5f, "Fence_Piece", true);
    }

    // ================================================================
    // 2. BANGUNAN DESA PINGGIRAN (Houses)
    // ================================================================
    private static void BuildHouses(Transform parent)
    {
        GameObject houseGroup = new GameObject("Houses");
        houseGroup.transform.SetParent(parent, false);
        houseGroup.transform.localPosition = Vector3.zero;
        houseGroup.transform.localRotation = Quaternion.identity;
        houseGroup.transform.localScale = Vector3.one;

        GameObject house1 = LoadModel(QuaternEnv + "MedievalVillage/Buildings/House_1.fbx");
        GameObject house2 = LoadModel(QuaternEnv + "MedievalVillage/Buildings/House_2.fbx");
        GameObject house3 = LoadModel(QuaternEnv + "MedievalVillage/Buildings/House_3.fbx");
        GameObject house4 = LoadModel(QuaternEnv + "MedievalVillage/Buildings/House_4.fbx");
        GameObject stable = LoadModel(QuaternEnv + "MedievalVillage/Buildings/Stable.fbx");
        GameObject marketStand1 = LoadModel(QuaternEnv + "MedievalVillage/Props/MarketStand_1.fbx");
        GameObject marketStand2 = LoadModel(QuaternEnv + "MedievalVillage/Props/MarketStand_2.fbx");

        float hScale = 2.4f;

        // 1. Rumah Utama Peternakan Terbengkalai (Barat Daya)
        PlaceHouseWithCollider(houseGroup.transform, house1,
            new Vector3(-20f, 0f, -20f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * hScale, "House_01_Farmhouse");

        // 2. Rumah Pinggiran Barat Tengah
        PlaceHouseWithCollider(houseGroup.transform, house2,
            new Vector3(-25f, 0f, 10f), Quaternion.Euler(0f, -15f, 0f), Vector3.one * hScale, "House_02_Dwelling");

        // 3. Rumah Kayu Besar / Pos Dagang Tua (Timur)
        PlaceHouseWithCollider(houseGroup.transform, house3,
            new Vector3(23f, 0f, 6f), Quaternion.Euler(0f, -115f, 0f), Vector3.one * (hScale * 1.05f), "House_03_TradingPost");

        // 4. Pondok Penjaga Gerbang Utara (Utara-Barat dekat Exit)
        if (house4 != null)
        {
            PlaceHouseWithCollider(houseGroup.transform, house4,
                new Vector3(-16f, 0f, 23f), Quaternion.Euler(0f, 55f, 0f), Vector3.one * (hScale * 0.9f), "House_04_CaretakerCabin");
        }

        // 5. Kandang Ternak Lapuk Tua (Stable Barat)
        if (stable != null)
        {
            PlaceHouseWithCollider(houseGroup.transform, stable,
                new Vector3(-25f, 0f, -8f), Quaternion.Euler(0f, 75f, 0f), Vector3.one * 2.2f, "House_05_Stable");
        }

        // 6 & 7. Lapak Pasar Kosong Terbengkalai (Timur Dekat Jalur)
        if (marketStand1 != null)
        {
            PlaceHouseWithCollider(houseGroup.transform, marketStand1,
                new Vector3(17f, 0f, -5f), Quaternion.Euler(0f, -40f, 0f), Vector3.one * 1.7f, "House_06_MarketStand_1");
        }
        if (marketStand2 != null)
        {
            PlaceHouseWithCollider(houseGroup.transform, marketStand2,
                new Vector3(17f, 0f, -1.5f), Quaternion.Euler(0f, -30f, 0f), Vector3.one * 1.7f, "House_07_MarketStand_2");
        }
    }

    private static void PlaceHouseWithCollider(Transform parent, GameObject model,
        Vector3 pos, Quaternion rot, Vector3 scale, string name)
    {
        if (model == null) return;

        GameObject instance = PlaceModel(parent, model, pos, rot, scale, name, true);
        if (instance != null)
        {
            BoxCollider col = instance.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = instance.AddComponent<BoxCollider>();
                Renderer rend = instance.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    col.center = instance.transform.InverseTransformPoint(b.center);
                    Vector3 size = instance.transform.InverseTransformVector(b.size);
                    col.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                }
            }
        }
    }

    // ================================================================
    // 3. PROPS & ENVIRONMENT DETAILING (Tebing, Batu, Pohon, Props, Jalur)
    // ================================================================
    private static void BuildProps(Transform parent)
    {
        GameObject propGroup = new GameObject("Props");
        propGroup.transform.SetParent(parent, false);
        propGroup.transform.localPosition = Vector3.zero;
        propGroup.transform.localRotation = Quaternion.identity;
        propGroup.transform.localScale = Vector3.one;

        // 1. Batas Alami & Tebing Pegunungan Gersang (Perimeter Mountain Rim)
        BuildNaturalBoundaries(propGroup.transform);

        // 2. Gerbang Celah Bebatuan Utara (Northern Gorge Exit Gateway)
        BuildExitGorge(propGroup.transform);

        // 3. Formasi & Batu Tanah Lapang (Open Field Boulders & Flat Stone Slabs)
        BuildOpenFieldRocks(propGroup.transform);

        // 4. Hutan Kering, Batang Kayu Mati & Kaktus (Dry Arid Vegetation)
        BuildAridVegetation(propGroup.transform);

        // 5. Props Tematik Pinggiran Desa (Carts, Wells, Hay, Barrels, Crates)
        BuildVillagePropsInternal(propGroup.transform);

        // 6. Jaringan Jalur Tanah Berdebu (Dusty Soil Paths)
        BuildPaths(propGroup.transform);

        // 7. Rerumputan Kering & Semak Padang Pasir (Dry Grass Tufts)
        BuildDryGrass(propGroup.transform);
    }

    // ================================================================
    // 3.1 BATAS ALAMI & TEBING LUAR (Perimeter Mountain Rim)
    // ================================================================
    private static void BuildNaturalBoundaries(Transform parent)
    {
        GameObject boundaryGroup = new GameObject("NaturalBoundaries");
        boundaryGroup.transform.SetParent(parent, false);
        boundaryGroup.transform.localPosition = Vector3.zero;
        boundaryGroup.transform.localRotation = Quaternion.identity;
        boundaryGroup.transform.localScale = Vector3.one;

        GameObject cliffSlope = LoadModel(KenneyModels + "cliff_blockSlope_stone.fbx");
        GameObject cliffHalf = LoadModel(KenneyModels + "cliff_half_stone.fbx");
        GameObject cliffLarge = LoadModel(KenneyModels + "cliff_large_stone.fbx");
        GameObject cliffCorner = LoadModel(KenneyModels + "cliff_cornerLarge_stone.fbx");
        GameObject rockLargeA = LoadModel(KenneyModels + "rock_largeA.fbx");
        GameObject rockLargeB = LoadModel(KenneyModels + "rock_largeB.fbx");
        GameObject rockLargeC = LoadModel(KenneyModels + "rock_largeC.fbx");
        GameObject rockTallA = LoadModel(KenneyModels + "rock_tallA.fbx");
        GameObject rockTallB = LoadModel(KenneyModels + "rock_tallB.fbx");

        List<GameObject> cliffModels = new List<GameObject>();
        if (cliffLarge != null) cliffModels.Add(cliffLarge);
        if (cliffSlope != null) cliffModels.Add(cliffSlope);
        if (cliffCorner != null) cliffModels.Add(cliffCorner);
        if (cliffHalf != null) cliffModels.Add(cliffHalf);
        if (rockLargeA != null) cliffModels.Add(rockLargeA);
        if (rockLargeB != null) cliffModels.Add(rockLargeB);
        if (rockLargeC != null) cliffModels.Add(rockLargeC);
        if (rockTallA != null) cliffModels.Add(rockTallA);
        if (rockTallB != null) cliffModels.Add(rockTallB);

        if (cliffModels.Count == 0) return;

        // Susunan tebing batu perimeter luar yang rapat dan membingkai seluruh desa
        Vector3[] perimeterCliffPositions =
        {
            // Perimeter Barat Luar (X = -32 .. -36)
            new Vector3(-33f, 0f, -28f), new Vector3(-34f, 0f, -20f),
            new Vector3(-35f, 0f, -12f), new Vector3(-34f, 0f, -4f),
            new Vector3(-35f, 0f, 4f),   new Vector3(-34f, 0f, 12f),
            new Vector3(-35f, 0f, 20f),  new Vector3(-33f, 0f, 28f),

            // Perimeter Timur Luar (X = +32 .. +36)
            new Vector3(33f, 0f, -28f), new Vector3(34f, 0f, -20f),
            new Vector3(35f, 0f, -12f), new Vector3(34f, 0f, -4f),
            new Vector3(35f, 0f, 4f),   new Vector3(34f, 0f, 12f),
            new Vector3(35f, 0f, 20f),  new Vector3(33f, 0f, 28f),

            // Perimeter Selatan Luar (Z = -32 .. -36)
            new Vector3(-24f, 0f, -33f), new Vector3(-16f, 0f, -34f),
            new Vector3(-8f, 0f, -35f),  new Vector3(0f, 0f, -34f),
            new Vector3(8f, 0f, -35f),   new Vector3(16f, 0f, -34f),
            new Vector3(24f, 0f, -33f),

            // Perimeter Utara Luar Sayap Kiri & Kanan (Z = +32 .. +36)
            new Vector3(-26f, 0f, 33f), new Vector3(-18f, 0f, 34f), new Vector3(-10f, 0f, 33f),
            new Vector3(10f, 0f, 33f),  new Vector3(18f, 0f, 34f),  new Vector3(26f, 0f, 33f)
        };

        int idx = 1;
        foreach (Vector3 pos in perimeterCliffPositions)
        {
            GameObject model = cliffModels[(idx - 1) % cliffModels.Count];
            float rotY = Random.Range(0f, 360f);
            float scale = Random.Range(3.8f, 5.2f);
            PlaceModel(boundaryGroup.transform, model, pos, Quaternion.Euler(0f, rotY, 0f),
                Vector3.one * scale, string.Format("Cliff_{0:D2}", idx++), true);
        }
    }

    // ================================================================
    // 3.2 GERBANG CELAH BEBATUAN UTARA (Northern Gorge Exit Gateway)
    // ================================================================
    private static void BuildExitGorge(Transform parent)
    {
        GameObject gorgeGroup = new GameObject("ExitGorge");
        gorgeGroup.transform.SetParent(parent, false);
        gorgeGroup.transform.localPosition = Vector3.zero;
        gorgeGroup.transform.localRotation = Quaternion.identity;
        gorgeGroup.transform.localScale = Vector3.one;

        GameObject cliffLarge = LoadModel(KenneyModels + "cliff_large_stone.fbx");
        GameObject rockTallA = LoadModel(KenneyModels + "rock_tallA.fbx");
        GameObject rockTallB = LoadModel(KenneyModels + "rock_tallB.fbx");
        GameObject signModel = LoadModel(KenneyModels + "sign.fbx");
        GameObject logStack = LoadModel(KenneyModels + "log_stack.fbx");
        GameObject barrel = LoadModel(QuaternEnv + "MedievalVillage/Props/Barrel.fbx");

        // Pilar tebing kiri gerbang exit
        if (cliffLarge != null)
        {
            PlaceModel(gorgeGroup.transform, cliffLarge, new Vector3(-5.5f, 0f, 29.5f),
                Quaternion.Euler(0f, 25f, 0f), Vector3.one * 3.8f, "Gorge_Pillar_West", true);
        }
        if (rockTallA != null)
        {
            PlaceModel(gorgeGroup.transform, rockTallA, new Vector3(-4.0f, 0f, 28.5f),
                Quaternion.Euler(0f, -40f, 0f), Vector3.one * 2.8f, "Gorge_Rock_West", true);
        }

        // Pilar tebing kanan gerbang exit
        if (cliffLarge != null)
        {
            PlaceModel(gorgeGroup.transform, cliffLarge, new Vector3(5.5f, 0f, 29.5f),
                Quaternion.Euler(0f, -25f, 0f), Vector3.one * 3.8f, "Gorge_Pillar_East", true);
        }
        if (rockTallB != null)
        {
            PlaceModel(gorgeGroup.transform, rockTallB, new Vector3(4.0f, 0f, 28.5f),
                Quaternion.Euler(0f, 35f, 0f), Vector3.one * 2.8f, "Gorge_Rock_East", true);
        }

        // Papan penunjuk arah menuju Zona 2
        if (signModel != null)
        {
            PlaceModel(gorgeGroup.transform, signModel, new Vector3(2.8f, 0f, 27.5f),
                Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.5f, "Sign_Zona2_Gateway", true);
        }

        // Aksen tumpukan kayu & tong pembatas jalan di exit
        if (logStack != null)
        {
            PlaceModel(gorgeGroup.transform, logStack, new Vector3(-3.2f, 0f, 27.8f),
                Quaternion.Euler(0f, 15f, 0f), Vector3.one * 1.4f, "LogStack_Exit", true);
        }
        if (barrel != null)
        {
            PlaceModel(gorgeGroup.transform, barrel, new Vector3(3.5f, 0f, 26.5f),
                Quaternion.Euler(0f, 0f, 0f), Vector3.one * 1.3f, "Barrel_Exit", true);
        }
    }

    // ================================================================
    // 3.3 FORMASI BEBATUAN TANAH LAPANG (Open Field Boulders & Slabs)
    // ================================================================
    private static void BuildOpenFieldRocks(Transform parent)
    {
        GameObject rocksGroup = new GameObject("Rocks");
        rocksGroup.transform.SetParent(parent, false);
        rocksGroup.transform.localPosition = Vector3.zero;
        rocksGroup.transform.localRotation = Quaternion.identity;
        rocksGroup.transform.localScale = Vector3.one;

        // Model Bebatuan Kenney
        GameObject rockLargeA = LoadModel(KenneyModels + "rock_largeA.fbx");
        GameObject rockLargeB = LoadModel(KenneyModels + "rock_largeB.fbx");
        GameObject rockLargeC = LoadModel(KenneyModels + "rock_largeC.fbx");
        GameObject rockLargeD = LoadModel(KenneyModels + "rock_largeD.fbx");
        GameObject rockTallA = LoadModel(KenneyModels + "rock_tallA.fbx");
        GameObject rockTallB = LoadModel(KenneyModels + "rock_tallB.fbx");
        GameObject rockTallC = LoadModel(KenneyModels + "rock_tallC.fbx");
        GameObject rockSmallA = LoadModel(KenneyModels + "rock_smallA.fbx");
        GameObject rockSmallB = LoadModel(KenneyModels + "rock_smallB.fbx");
        GameObject rockSmallFlatA = LoadModel(KenneyModels + "rock_smallFlatA.fbx");
        GameObject rockSmallFlatB = LoadModel(KenneyModels + "rock_smallFlatB.fbx");
        GameObject rockSmallFlatC = LoadModel(KenneyModels + "rock_smallFlatC.fbx");

        // Gugusan Batu 1: Barat Daya (Batas Area Peternakan & Tanah Terbuka)
        PlaceRockCluster(rocksGroup.transform, rockLargeA, rockTallB, rockSmallA,
            new Vector3(-12.5f, 0f, -14.5f), 2.2f, "Cluster_SW");

        // Gugusan Batu 2: Selatan Dekat Spawn Pemain (Aksen Alam)
        PlaceRockCluster(rocksGroup.transform, rockLargeB, rockTallA, rockSmallB,
            new Vector3(15.5f, 0f, -16.5f), 2.0f, "Cluster_SE");

        // Gugusan Batu 3: Barat Tengah (Pemisah Alami Area Sampah Barat)
        PlaceRockCluster(rocksGroup.transform, rockLargeC, rockTallC, rockSmallA,
            new Vector3(-13.5f, 0f, 0f), 2.1f, "Cluster_MW");

        // Gugusan Batu 4: Timur Tengah (Dekat Jalur Pos Dagang)
        PlaceRockCluster(rocksGroup.transform, rockLargeD, rockTallA, rockSmallB,
            new Vector3(14.0f, 0f, -9.0f), 2.0f, "Cluster_ME");

        // Gugusan Batu 5: Barat Laut (Dekat Area Sampah Utara)
        PlaceRockCluster(rocksGroup.transform, rockLargeA, rockTallC, rockSmallA,
            new Vector3(-17.0f, 0f, 15.0f), 2.3f, "Cluster_NW");

        // Gugusan Batu 6: Timur Laut (Tanah Berbatu Terbuka)
        PlaceRockCluster(rocksGroup.transform, rockLargeB, rockTallB, rockSmallB,
            new Vector3(16.0f, 0f, 18.0f), 2.2f, "Cluster_NE");

        // Bebatuan Datar / Lempengan Batu Alam Tertanam di Tanah (Menambah tekstur tanah lapang gersang)
        Vector3[] flatRockPositions =
        {
            new Vector3(4f, 0.02f, -10f),
            new Vector3(7f, 0.02f, -5f),
            new Vector3(-5f, 0.02f, -8f),
            new Vector3(-8f, 0.02f, -15f),
            new Vector3(10f, 0.02f, 12f),
            new Vector3(-6f, 0.02f, 14f),
            new Vector3(3f, 0.02f, 16f),
            new Vector3(-14f, 0.02f, 8f),
            new Vector3(12f, 0.02f, -2f),
            new Vector3(8f, 0.02f, -18f)
        };

        int fIdx = 1;
        foreach (var fpos in flatRockPositions)
        {
            GameObject flatModel = (fIdx % 3 == 0) ? (rockSmallFlatC ?? rockSmallA) :
                                   (fIdx % 2 == 0) ? (rockSmallFlatB ?? rockSmallB) :
                                   (rockSmallFlatA ?? rockSmallA);

            if (flatModel != null)
            {
                PlaceModel(rocksGroup.transform, flatModel, fpos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Vector3.one * Random.Range(1.8f, 2.8f), string.Format("FlatStone_{0:D2}", fIdx++), false);
            }
        }

        // Format nama nomor urut dua digit (Rock_01, Rock_02, ...)
        int rCount = 1;
        for (int i = 0; i < rocksGroup.transform.childCount; i++)
        {
            Transform rc = rocksGroup.transform.GetChild(i);
            if (!rc.name.StartsWith("FlatStone_"))
            {
                rc.name = string.Format("Rock_{0:D2}", rCount++);
            }
        }
    }

    private static void PlaceRockCluster(Transform parent, GameObject large, GameObject tall, GameObject small,
        Vector3 center, float scale, string label)
    {
        if (large != null)
        {
            PlaceModel(parent, large, center, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Vector3.one * scale, label + "_Main", true);
        }
        if (tall != null)
        {
            Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * new Vector3(1.4f * scale * 0.5f, 0f, 0f);
            PlaceModel(parent, tall, center + offset, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Vector3.one * (scale * 0.75f), label + "_Pillar", true);
        }
        if (small != null)
        {
            Vector3 offset2 = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * new Vector3(1.8f * scale * 0.5f, 0f, 0f);
            PlaceModel(parent, small, center + offset2, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                Vector3.one * (scale * 0.8f), label + "_Small", true);
        }
    }

    // ================================================================
    // 3.4 HUTAN KERING, BATANG LAPUK & KAKTUS (Arid Vegetation)
    // ================================================================
    private static void BuildAridVegetation(Transform parent)
    {
        GameObject treeGroup = new GameObject("Trees");
        treeGroup.transform.SetParent(parent, false);
        treeGroup.transform.localPosition = Vector3.zero;
        treeGroup.transform.localRotation = Quaternion.identity;
        treeGroup.transform.localScale = Vector3.one;

        // Pohon Gugur / Kering Kenney
        GameObject treeDefaultFall = LoadModel(KenneyModels + "tree_default_fall.fbx");
        GameObject treeOakFall = LoadModel(KenneyModels + "tree_oak_fall.fbx");
        GameObject treeTallFall = LoadModel(KenneyModels + "tree_tall_fall.fbx");
        GameObject treeThinFall = LoadModel(KenneyModels + "tree_thin_fall.fbx");
        GameObject treeConeFall = LoadModel(KenneyModels + "tree_cone_fall.fbx");
        GameObject treePlateauFall = LoadModel(KenneyModels + "tree_plateau_fall.fbx");

        // Tunggul & Batang Lapuk
        GameObject stumpOld = LoadModel(KenneyModels + "stump_old.fbx");
        GameObject stumpOldTall = LoadModel(KenneyModels + "stump_oldTall.fbx");
        GameObject stumpRound = LoadModel(KenneyModels + "stump_round.fbx");
        GameObject woodLog = LoadModel(KenneyModels + "log.fbx") ?? LoadModel(QuaternEnv + "Nature/WoodLog.fbx");
        GameObject woodLogs = LoadModel(KenneyModels + "log_large.fbx") ?? LoadModel(KenneyModels + "log_stack.fbx");

        // Kaktus Padang Gersang
        GameObject cactusTall = LoadModel(KenneyModels + "cactus_tall.fbx");
        GameObject cactusShort = LoadModel(KenneyModels + "cactus_short.fbx");

        List<GameObject> fallTrees = new List<GameObject>();
        if (treeDefaultFall != null) fallTrees.Add(treeDefaultFall);
        if (treeOakFall != null) fallTrees.Add(treeOakFall);
        if (treeTallFall != null) fallTrees.Add(treeTallFall);
        if (treeThinFall != null) fallTrees.Add(treeThinFall);
        if (treeConeFall != null) fallTrees.Add(treeConeFall);
        if (treePlateauFall != null) fallTrees.Add(treePlateauFall);

        // Titik sebaran pohon kering perimeter dan halaman desa
        Vector3[] treePositions =
        {
            // Sudut Barat Daya (Dekat Rumah A & Pagar)
            new Vector3(-24f, 0f, -25f), new Vector3(-28f, 0f, -17f), new Vector3(-17f, 0f, -26f),
            // Sudut Tenggara (Dekat Spawn Pemain)
            new Vector3(25f, 0f, -24f), new Vector3(28f, 0f, -18f), new Vector3(18f, 0f, -26f),
            // Sisi Barat
            new Vector3(-28f, 0f, 2f),  new Vector3(-28f, 0f, 18f),
            // Sisi Timur (Dekat Pos Dagang & Sumur)
            new Vector3(28f, 0f, 0f),   new Vector3(26f, 0f, 14f),  new Vector3(28f, 0f, 22f),
            // Sudut Barat Laut & Utara
            new Vector3(-24f, 0f, 27f), new Vector3(-12f, 0f, 27f),
            // Sudut Timur Laut
            new Vector3(24f, 0f, 27f),  new Vector3(12f, 0f, 27f),
            // Pohon Kering Landmark Lapangan (Dekat Tetua Desa)
            new Vector3(-12f, 0f, 5.5f)
        };

        int tIdx = 1;
        foreach (Vector3 pos in treePositions)
        {
            if (fallTrees.Count > 0)
            {
                GameObject treeModel = fallTrees[(tIdx - 1) % fallTrees.Count];
                float rotY = Random.Range(0f, 360f);
                float scale = Random.Range(1.4f, 2.1f);
                PlaceModel(treeGroup.transform, treeModel, pos, Quaternion.Euler(0f, rotY, 0f),
                    Vector3.one * scale, string.Format("Tree_{0:D2}", tIdx++), true);
            }
        }

        // Sebaran Tunggul Pohon Mati & Batang Kayu Roboh
        Vector3[] stumpPositions =
        {
            new Vector3(-8.5f, 0f, -15f),
            new Vector3(13.5f, 0f, -4f),
            new Vector3(-6.5f, 0f, 12f),
            new Vector3(7.5f, 0f, 15f),
            new Vector3(-22f, 0f, 20f),
            new Vector3(11.5f, 0f, -14f)
        };

        foreach (var spos in stumpPositions)
        {
            GameObject sModel = (tIdx % 2 == 0) ? (stumpOldTall ?? stumpOld) : (stumpOld ?? stumpRound);
            if (sModel != null)
            {
                PlaceModel(treeGroup.transform, sModel, spos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Vector3.one * Random.Range(1.8f, 2.5f), string.Format("Tree_{0:D2}", tIdx++), true);
            }
        }

        // Batang Kayu Mati Tergeletak (Dead Logs)
        if (woodLog != null || woodLogs != null)
        {
            GameObject logModel = woodLogs ?? woodLog;
            PlaceModel(treeGroup.transform, logModel, new Vector3(5f, 0f, -24f), Quaternion.Euler(0f, 35f, 0f),
                Vector3.one * 1.6f, string.Format("Tree_{0:D2}", tIdx++), true);
            PlaceModel(treeGroup.transform, logModel, new Vector3(-11f, 0f, 18f), Quaternion.Euler(0f, -65f, 0f),
                Vector3.one * 1.6f, string.Format("Tree_{0:D2}", tIdx++), true);
        }

        // Kaktus Padang Gersang (Arid Cacti)
        Vector3[] cactusPositions =
        {
            new Vector3(-26f, 0f, -14f),
            new Vector3(-14f, 0f, -22f),
            new Vector3(22f, 0f, -20f),
            new Vector3(25f, 0f, -8f),
            new Vector3(-27f, 0f, 14f),
            new Vector3(26f, 0f, 18f),
            new Vector3(-8f, 0f, 24f),
            new Vector3(8f, 0f, 24f)
        };

        foreach (var cpos in cactusPositions)
        {
            GameObject cModel = (tIdx % 2 == 0) ? (cactusTall ?? cactusShort) : (cactusShort ?? cactusTall);
            if (cModel != null)
            {
                PlaceModel(treeGroup.transform, cModel, cpos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    Vector3.one * Random.Range(1.6f, 2.3f), string.Format("Tree_{0:D2}", tIdx++), true);
            }
        }
    }

    // ================================================================
    // 3.5 PROPS TEMATIK PINGGIRAN DESA (Village Props)
    // ================================================================
    private static void BuildVillagePropsInternal(Transform parent)
    {
        GameObject propGroup = new GameObject("VillageProps");
        propGroup.transform.SetParent(parent, false);
        propGroup.transform.localPosition = Vector3.zero;
        propGroup.transform.localRotation = Quaternion.identity;
        propGroup.transform.localScale = Vector3.one;

        // Model Quaternius
        GameObject qCrate = LoadModel(QuaternEnv + "MedievalVillage/Props/Crate.fbx");
        GameObject qHay = LoadModel(QuaternEnv + "MedievalVillage/Props/Hay.fbx");
        GameObject qBarrel = LoadModel(QuaternEnv + "MedievalVillage/Props/Barrel.fbx");
        GameObject qCart = LoadModel(QuaternEnv + "MedievalVillage/Props/Cart.fbx");
        GameObject qWell = LoadModel(QuaternEnv + "MedievalVillage/Props/Well.fbx");
        GameObject qBench = LoadModel(QuaternEnv + "MedievalVillage/Props/Bench_1.fbx");
        GameObject qBag = LoadModel(QuaternEnv + "MedievalVillage/Props/Bag.fbx");
        GameObject qBags = LoadModel(QuaternEnv + "MedievalVillage/Props/Bags.fbx");

        // Model Kenney
        GameObject kLogStack = LoadModel(KenneyModels + "log_stack.fbx");
        GameObject kSign = LoadModel(KenneyModels + "sign.fbx");
        GameObject kPotLarge = LoadModel(KenneyModels + "pot_large.fbx");
        GameObject kCampfire = LoadModel(KenneyModels + "campfire_stones.fbx");

        float baseScale = 1.6f;
        int pIdx = 1;

        // --- Props Area Spawn Pemain (Selatan: X=10, Z=-12) ---
        PlacePropNumbered(propGroup.transform, kSign, new Vector3(8.2f, 0f, -11.5f), 30f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBarrel, new Vector3(12.5f, 0f, -13.5f), 10f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qCrate, new Vector3(13.2f, 0f, -12.2f), -25f, baseScale, ref pIdx);

        // --- Props Kompleks Peternakan Barat (Dekat Rumah 1 & Kandang) ---
        PlacePropNumbered(propGroup.transform, qCrate, new Vector3(-16.5f, 0f, -18.5f), 35f, baseScale * 1.2f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBarrel, new Vector3(-18.0f, 0f, -17.5f), 0f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qHay, new Vector3(-21.5f, 0f, -23.5f), -20f, baseScale * 1.3f, ref pIdx);
        PlacePropNumbered(propGroup.transform, kLogStack, new Vector3(-23.0f, 0f, -17.0f), 85f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qCart, new Vector3(-23.5f, 0f, -4.0f), -35f, baseScale * 1.4f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qHay, new Vector3(-26.5f, 0f, -5.5f), 40f, baseScale * 1.4f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBarrel, new Vector3(-25.0f, 0f, 13.0f), 15f, baseScale * 1.1f, ref pIdx);
        PlacePropNumbered(propGroup.transform, kPotLarge, new Vector3(-21.0f, 0f, 11.5f), 0f, baseScale * 1.2f, ref pIdx);

        // --- Props Kompleks Pos Dagang Timur (Dekat Lapak & Sumur Tua) ---
        PlacePropNumbered(propGroup.transform, qCrate, new Vector3(15.5f, 0f, -6.5f), -20f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBarrel, new Vector3(18.5f, 0f, -7.0f), 45f, baseScale * 1.1f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBags, new Vector3(15.0f, 0f, -3.0f), 15f, baseScale * 1.2f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBarrel, new Vector3(25.5f, 0f, 8.5f), -15f, baseScale * 1.1f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qHay, new Vector3(26.0f, 0f, 3.0f), 45f, baseScale * 1.2f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBench, new Vector3(19.5f, 0f, 8.5f), -90f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qWell, new Vector3(19.0f, 0f, 13.5f), 15f, baseScale * 1.35f, ref pIdx);

        // --- Props Pos TPS & Tetua Desa ---
        // (Tetap di luar radius larangan 4m dari Pos TPS dan 3m dari Tetua Desa)
        PlacePropNumbered(propGroup.transform, qBags, new Vector3(4.8f, 0f, 4.0f), -15f, baseScale * 1.3f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBag, new Vector3(4.5f, 0f, 5.2f), 30f, baseScale * 1.1f, ref pIdx);
        PlacePropNumbered(propGroup.transform, qBench, new Vector3(-10.8f, 0f, 4.8f), 60f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, kSign, new Vector3(-5.5f, 0f, 1.5f), -45f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, kCampfire, new Vector3(-8.5f, 0f, 0.5f), 0f, baseScale * 1.1f, ref pIdx);

        // --- Props Pondok Penjaga Utara ---
        PlacePropNumbered(propGroup.transform, kLogStack, new Vector3(-11.0f, 0f, 25.0f), 10f, baseScale, ref pIdx);
        PlacePropNumbered(propGroup.transform, qCrate, new Vector3(-18.5f, 0f, 25.5f), 45f, baseScale, ref pIdx);
    }

    private static void PlacePropNumbered(Transform parent, GameObject model,
        Vector3 pos, float yRot, float scale, ref int index)
    {
        if (model == null) return;
        PlaceModel(parent, model, pos, Quaternion.Euler(0f, yRot, 0f),
            Vector3.one * scale, string.Format("Prop_{0:D2}", index++), true);
    }

    // ================================================================
    // 3.6 JARINGAN JALUR TANAH BERDEBU (Dusty Soil Paths)
    // ================================================================
    private static void BuildPaths(Transform parent)
    {
        GameObject pathGroup = new GameObject("Paths");
        pathGroup.transform.SetParent(parent, false);
        pathGroup.transform.localPosition = Vector3.zero;
        pathGroup.transform.localRotation = Quaternion.identity;
        pathGroup.transform.localScale = Vector3.one;

        string pathStraight = KenneyModels + "ground_pathStraight.fbx";
        string pathBend = KenneyModels + "ground_pathBend.fbx";
        string pathRocks = KenneyModels + "ground_pathRocks.fbx";

        GameObject pathModel = LoadModel(pathStraight);
        GameObject bendModel = LoadModel(pathBend) ?? pathModel;
        GameObject rocksModel = LoadModel(pathRocks) ?? pathModel;

        if (pathModel == null) return;

        float pathScale = 2.6f;
        int pIdx = 1;

        // Jalur 1: Dari Spawn Pemain (10, 0, -12) menuju Pos TPS (0, 0, 3)
        for (float t = 0f; t <= 1f; t += 0.12f)
        {
            Vector3 pos = Vector3.Lerp(new Vector3(9f, 0.015f, -11f), new Vector3(1.2f, 0.015f, 2.5f), t);
            GameObject chosen = (t > 0.4f && t < 0.6f && rocksModel != null) ? rocksModel : pathModel;
            PlaceModel(pathGroup.transform, chosen, pos, Quaternion.Euler(0f, -38f, 0f),
                Vector3.one * pathScale, string.Format("Path_{0:D2}", pIdx++), false);
        }

        // Jalur 2: Dari Pos TPS (0, 0, 6.5) lurus menuju Exit Utara (0, 0, 28)
        for (float z = 7.5f; z <= 27.5f; z += 2.5f)
        {
            Vector3 pos = new Vector3(0f, 0.015f, z);
            GameObject chosen = (Mathf.Abs(z - 17.5f) < 1.5f && rocksModel != null) ? rocksModel : pathModel;
            PlaceModel(pathGroup.transform, chosen, pos, Quaternion.Euler(0f, 0f, 0f),
                Vector3.one * pathScale, string.Format("Path_{0:D2}", pIdx++), false);
        }

        // Jalur 3: Cabang ke Pemukiman Peternakan Barat
        for (float t = 0f; t <= 1f; t += 0.20f)
        {
            Vector3 pos = Vector3.Lerp(new Vector3(-1f, 0.015f, -2f), new Vector3(-15f, 0.015f, -14f), t);
            PlaceModel(pathGroup.transform, pathModel, pos, Quaternion.Euler(0f, 45f, 0f),
                Vector3.one * (pathScale * 0.9f), string.Format("Path_{0:D2}", pIdx++), false);
        }

        // Jalur 4: Cabang ke Pos Dagang Timur
        for (float t = 0f; t <= 1f; t += 0.22f)
        {
            Vector3 pos = Vector3.Lerp(new Vector3(2f, 0.015f, 1f), new Vector3(15f, 0.015f, -3f), t);
            PlaceModel(pathGroup.transform, pathModel, pos, Quaternion.Euler(0f, -70f, 0f),
                Vector3.one * (pathScale * 0.9f), string.Format("Path_{0:D2}", pIdx++), false);
        }
    }

    // ================================================================
    // 3.7 RERUMPUTAN KERING & SEMAK GERSANG (Dry Grass Tufts)
    // ================================================================
    private static void BuildDryGrass(Transform parent)
    {
        GameObject grassGroup = new GameObject("DryGrass");
        grassGroup.transform.SetParent(parent, false);
        grassGroup.transform.localPosition = Vector3.zero;
        grassGroup.transform.localRotation = Quaternion.identity;
        grassGroup.transform.localScale = Vector3.one;

        GameObject kGrass = LoadModel(KenneyModels + "grass.fbx");
        GameObject kGrassLarge = LoadModel(KenneyModels + "grass_large.fbx");
        GameObject qGrass = LoadModel(QuaternEnv + "Nature/Grass.fbx");
        GameObject qBush = LoadModel(QuaternEnv + "Nature/Bush_1.fbx");

        List<GameObject> grassList = new List<GameObject>();
        if (kGrass != null) grassList.Add(kGrass);
        if (kGrassLarge != null) grassList.Add(kGrassLarge);
        if (qGrass != null) grassList.Add(qGrass);
        if (qBush != null) grassList.Add(qBush);

        if (grassList.Count == 0) return;

        // Sebaran rumput kering di sekitar bebatuan, akar pohon, dan tepi pagar
        Vector3[] grassPositions =
        {
            new Vector3(-22f, 0f, -28f), new Vector3(-14f, 0f, -27f), new Vector3(20f, 0f, -28f),
            new Vector3(25f, 0f, -27f), new Vector3(-28f, 0f, -18f), new Vector3(-28f, 0f, 10f),
            new Vector3(-28f, 0f, 22f), new Vector3(28f, 0f, -18f), new Vector3(28f, 0f, 10f),
            new Vector3(28f, 0f, 22f), new Vector3(-20f, 0f, 28f), new Vector3(20f, 0f, 28f),
            new Vector3(-11f, 0f, -13f), new Vector3(14f, 0f, -15f), new Vector3(-13f, 0f, 2f),
            new Vector3(13f, 0f, -7f),  new Vector3(-15f, 0f, 16f),  new Vector3(17f, 0f, 16f),
            new Vector3(-10f, 0f, 6.5f), new Vector3(17f, 0f, 11f),  new Vector3(-23f, 0f, -10f)
        };

        int gIdx = 1;
        foreach (Vector3 pos in grassPositions)
        {
            GameObject model = grassList[(gIdx - 1) % grassList.Count];
            float rotY = Random.Range(0f, 360f);
            float scale = Random.Range(2.0f, 3.2f);
            PlaceModel(grassGroup.transform, model, pos, Quaternion.Euler(0f, rotY, 0f),
                Vector3.one * scale, string.Format("Grass_{0:D2}", gIdx++), false);
        }
    }

    // ================================================================
    // 4. PENCAHAYAAN & ATMOSFER GERSANG (Atmospheric Lighting & Fog)
    // ================================================================
    private static void SetupAtmosphereAndLighting()
    {
        // 1. Directional Light (Matahari Sore Hangat / Golden Hour Gersang)
        GameObject lightObj = GameObject.Find("Directional Light");
        Light sun = null;
        if (lightObj != null)
        {
            sun = lightObj.GetComponent<Light>();
        }
        else
        {
            GameObject lightRoot = GameObject.Find("--- LIGHTING ---");
            if (lightRoot == null) lightRoot = new GameObject("--- LIGHTING ---");
            lightObj = new GameObject("Directional Light");
            lightObj.transform.SetParent(lightRoot.transform, false);
            sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        if (sun != null)
        {
            lightObj.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            sun.color = new Color(1f, 0.90f, 0.74f, 1f); // Warm desert sunlight
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }

        // 2. Kabut Debu Atmosferik (Dust Haze)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0085f;
        RenderSettings.fogColor = new Color(0.80f, 0.71f, 0.56f, 1f); // Warm dusty desert air

        // 3. Cahaya Ambien Padang Pasir
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.51f, 0.43f, 1f);
    }

    // ================================================================
    // HELPER METHODS
    // ================================================================

    private static GameObject PlaceModel(Transform parent, GameObject model,
        Vector3 pos, Quaternion rot, Vector3 scale, string name, bool addCollider)
    {
        if (model == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (instance == null) instance = Object.Instantiate(model);

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = pos;
        instance.transform.rotation = rot;
        instance.transform.localScale = scale;

        if (addCollider)
        {
            SetLayer(instance, ObstacleLayer);
            EnsureCollider(instance);
        }

        return instance;
    }

    private static void EnsureCollider(GameObject obj)
    {
        if (obj.GetComponentInChildren<Collider>() != null) return;

        BoxCollider col = obj.AddComponent<BoxCollider>();
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Bounds bounds = rend.bounds;
            col.center = obj.transform.InverseTransformPoint(bounds.center);
            Vector3 size = obj.transform.InverseTransformVector(bounds.size);
            col.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
    }

    private static GameObject LoadModel(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject LoadModelWithFallback(params string[] paths)
    {
        foreach (string path in paths)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null) return model;
        }
        return null;
    }

    private static void ApplyMaterial(GameObject obj, string materialPath)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null) return;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
    }

    private static void SetLayer(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) layer = 0;

        obj.layer = layer;
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }

    private static void MarkStatic(GameObject root)
    {
        GameObjectUtility.SetStaticEditorFlags(root,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }

    private static void DestroyExisting(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
