using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 15: Perluasan peta seamless (~3x) + pembangunan Zona 2 Bantaran Sungai Desa.
/// Menu: YeVillage / 15 - Expand Map & Build Zona 2 River
/// Idempoten: aman dijalankan berulang kali tanpa duplikasi.
/// </summary>
public static class Phase15Zona2RiverBuilder
{
    private const string RootName = "Environment_Expanded_P15";
    private const string SpawnRootName = "SpawnPoints_Expanded";
    private const string MaterialFolder = "Assets/Materials/Zona2_Environment";
    private const string KenneyFolder = "Assets/ThirdParty/KenneyNatureKit/Models";
    private const string NatureFolder = "Assets/ThirdParty/Quaternius/Environment/Nature";
    private const string BuildingFolder = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Buildings";
    private const string PropFolder = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props";

    private const float GroundY = 0f;
    private const float RiverBedY = -1.35f;
    private const float WaterY = -0.35f;
    private const float DeckHeight = 0.45f;
    private const int GroundLayer = 8;

    private const float SouthLandEdgeZ = -2.0f;
    private const float NorthLandEdgeZ = 9.0f;
    private const float RiverBedSouthZ = -3.5f;
    private const float RiverBedNorthZ = 10.5f;
    private const float RiverHalfWidthX = 56f;
    private const float GroundHalfWidthX = 62f;

    private const float BridgeSouthZ = -2.5f;
    private const float BridgeRampSouthEndZ = 0.5f;
    private const float BridgeRampNorthStartZ = 6.5f;
    private const float BridgeNorthZ = 9.5f;
    private const float BridgeHalfWidth = 2.2f;

    private static System.Random rng;
    private static readonly List<string> missingAssets = new List<string>();
    private static readonly List<string> disabledObjects = new List<string>();

    private static int rockCount;
    private static int treeCount;
    private static int bushCount;
    private static int grassCount;
    private static int fenceCount;
    private static int logCount;
    private static int houseCount;
    private static int bridgePieceCount;
    private static int lilyCount;

    private static Material matDirt;
    private static Material matSand;
    private static Material matPath;
    private static Material matBankSand;
    private static Material matGrass;
    private static Material matGrassDeep;
    private static Material matRiverBed;
    private static Material matPlazaGrass;
    private static Material matPlazaStone;
    private static Material matWater;
    private static Material matWoodA;
    private static Material matWoodB;
    private static Material matWoodC;
    private static Material matFoam;
    private static Material matRock;
    private static Material matRockLight;
    private static Material matDryGrass;
    private static Material matFlowerA;
    private static Material matFlowerB;
    private static Material matEmbankment;
    private static Material matFallback;

    [MenuItem("YeVillage/15 - Expand Map & Build Zona 2 River")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[Phase15] Tidak ada scene aktif yang valid. Buka SampleScene terlebih dahulu.");
            return;
        }

        ResetCounters();
        rng = new System.Random(15022026);

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialFolder);
        CreateMaterials();

        RemoveOldContainer(RootName);
        RemoveOldContainer(SpawnRootName);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Phase15 Environment Root");

        Transform terrain = NewChild(root.transform, "Terrain_Ground");
        Transform decals = NewChild(root.transform, "Terrain_Decals");
        Transform river = NewChild(root.transform, "RiverChannel");
        Transform bridge = NewChild(root.transform, "Zona2_WoodenBridge");
        Transform decor = NewChild(root.transform, "Zona2_Decor");
        Transform tps = NewChild(root.transform, "Zona1_TPS_Area");
        Transform plaza = NewChild(root.transform, "Zona3_BalaiDesa_Area");
        Transform boundary = NewChild(root.transform, "MapBoundary_Walls");
        Transform perimeter = NewChild(root.transform, "Perimeter_Forest");

        BuildTerrain(terrain);
        BuildDecals(decals);
        BuildRiver(river);
        BuildBridge(bridge);
        BuildZona2Decor(decor);
        BuildZona1Area(tps);
        BuildZona3Area(plaza);
        BuildBoundaries(boundary, perimeter);
        SetupAtmosphere();

        GameObject spawnRoot = BuildSpawnPoints();
        WireTrashSpawner(spawnRoot);

        RelocateCoreObjects();
        DisableConflictingLegacyObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        LogSummary(spawnRoot);
    }

    // ------------------------------------------------------------------ setup

    private static void ResetCounters()
    {
        missingAssets.Clear();
        disabledObjects.Clear();
        rockCount = 0;
        treeCount = 0;
        bushCount = 0;
        grassCount = 0;
        fenceCount = 0;
        logCount = 0;
        houseCount = 0;
        bridgePieceCount = 0;
        lilyCount = 0;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path);
        string leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent.Replace('\\', '/'));
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void RemoveOldContainer(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == name)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }
    }

    // -------------------------------------------------------------- materials

    private static void CreateMaterials()
    {
        matDirt = CreateLitMaterial("Mat_Zona2_DirtBare", "#9C7A54", 0.08f);
        matSand = CreateLitMaterial("Mat_Zona2_DrySand", "#B8976C", 0.06f);
        matPath = CreateLitMaterial("Mat_Zona2_PathOchre", "#C49E68", 0.1f);
        matBankSand = CreateLitMaterial("Mat_Zona2_WetBankSand", "#A88D64", 0.14f);
        matGrass = CreateLitMaterial("Mat_Zona2_GrassOlive", "#748F46", 0.1f);
        matGrassDeep = CreateLitMaterial("Mat_Zona2_GrassDeep", "#65823D", 0.1f);
        matRiverBed = CreateLitMaterial("Mat_Zona2_RiverBedMud", "#6A5A46", 0.24f);
        matPlazaGrass = CreateLitMaterial("Mat_Zona2_PlazaGrass", "#6E8E43", 0.1f);
        matPlazaStone = CreateLitMaterial("Mat_Zona2_PlazaStone", "#B8AD98", 0.16f);
        matWoodA = CreateLitMaterial("Mat_Zona2_WoodDeck", "#8D5E36", 0.12f);
        matWoodB = CreateLitMaterial("Mat_Zona2_WoodDark", "#7E522E", 0.12f);
        matWoodC = CreateLitMaterial("Mat_Zona2_WoodLight", "#9A6A40", 0.12f);
        matRock = CreateLitMaterial("Mat_Zona2_RiverRock", "#8A8578", 0.1f);
        matRockLight = CreateLitMaterial("Mat_Zona2_RiverRockLight", "#A39C8C", 0.1f);
        matDryGrass = CreateLitMaterial("Mat_Zona2_DryGrass", "#A9915C", 0.08f);
        matFlowerA = CreateLitMaterial("Mat_Zona2_FlowerWarm", "#D9A24B", 0.08f);
        matFlowerB = CreateLitMaterial("Mat_Zona2_FlowerSoft", "#C97B8A", 0.08f);
        matEmbankment = CreateLitMaterial("Mat_Zona2_EmbankmentEarth", "#7A6A50", 0.18f);
        matFallback = CreateLitMaterial("Mat_Zona2_PropFallback", "#B0A08A", 0.12f);

        matWater = CreateWaterMaterial();
        matFoam = CreateFoamMaterial();
    }

    private static Material CreateLitMaterial(string name, string hex, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.name = name;
        Color color = ParseColor(hex);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateWaterMaterial()
    {
        Material mat = CreateLitMaterial("Mat_Zona2_RiverWater", "#5E7B73", 0.55f);
        mat.SetOverrideTag("RenderType", "Transparent");

        Color waterColor = ParseColor("#5E7B73");
        waterColor.a = 0.84f;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", waterColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", waterColor);

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;

        Texture2D flowTexture = CreateFlowTexture();
        if (flowTexture != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", flowTexture);
            mat.SetTextureScale("_BaseMap", new Vector2(9f, 3f));
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateFoamMaterial()
    {
        Material mat = CreateLitMaterial("Mat_Zona2_Foam", "#FFFFFF", 0.6f);
        Color c = new Color(1f, 1f, 1f, 0.5f);
        mat.SetOverrideTag("RenderType", "Transparent");

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Texture2D CreateFlowTexture()
    {
        const string path = MaterialFolder + "/Tex_Zona2_RiverFlow.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float wave = Mathf.Sin((x * 0.35f) + (y * 0.18f)) * 0.5f + 0.5f;
                float fine = Mathf.Sin((x * 0.9f) - (y * 0.25f)) * 0.5f + 0.5f;
                float value = Mathf.Lerp(0.88f, 1f, wave * 0.7f + fine * 0.3f);
                pixels[(y * size) + x] = new Color(value, value, value, 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------------------------------------------------------- terrain

    private static void BuildTerrain(Transform parent)
    {
        Transform backdrop = NewChild(parent, "Backdrop_Ground");

        MakeBox("Backdrop_South", backdrop,
            new Vector3(0f, -0.5f, -118f), new Vector3(300f, 1f, 108f),
            Quaternion.identity, matGrassDeep, false, GroundLayer);

        MakeBox("Backdrop_North", backdrop,
            new Vector3(0f, -0.5f, 126f), new Vector3(300f, 1f, 108f),
            Quaternion.identity, matGrassDeep, false, GroundLayer);

        MakeBox("Backdrop_West", backdrop,
            new Vector3(-116f, -0.5f, 4f), new Vector3(108f, 1f, 136f),
            Quaternion.identity, matGrassDeep, false, GroundLayer);

        MakeBox("Backdrop_East", backdrop,
            new Vector3(116f, -0.5f, 4f), new Vector3(108f, 1f, 136f),
            Quaternion.identity, matGrassDeep, false, GroundLayer);

        MakeBox("Ground_South_Zone1_Zone2", parent,
            new Vector3(0f, -0.5f, -33f), new Vector3(124f, 1f, 62f),
            Quaternion.identity, matGrass, true, GroundLayer);

        MakeBox("Ground_North_Zone2_Zone3", parent,
            new Vector3(0f, -0.5f, 40.5f), new Vector3(124f, 1f, 63f),
            Quaternion.identity, matGrassDeep, true, GroundLayer);

        MakeBox("RiverBed", parent,
            new Vector3(0f, RiverBedY - 0.5f, 3.5f),
            new Vector3(GroundHalfWidthX * 2f, 1f, RiverBedNorthZ - RiverBedSouthZ),
            Quaternion.identity, matRiverBed, true, GroundLayer);

        MakeSlopedBox("Embankment_South", parent,
            -GroundHalfWidthX, SouthLandEdgeZ, GroundY,
            GroundHalfWidthX, RiverBedSouthZ, RiverBedY,
            0.5f, matEmbankment, true);

        MakeSlopedBox("Embankment_North", parent,
            -GroundHalfWidthX, NorthLandEdgeZ, GroundY,
            GroundHalfWidthX, RiverBedNorthZ, RiverBedY,
            0.5f, matEmbankment, true);
    }

    private static void BuildDecals(Transform parent)
    {
        // Zona 1: inti tanah tandus + blob tepi agar siluet tidak kotak.
        MakeBlobField("Patch_Dirt_Zone1", parent,
            new Vector3(0f, 0f, -34f), 60f, 30f, matDirt, 1, 0.008f, 18);

        // Zona 1: pasir kering di sekitar area TPS selatan.
        MakeBlobField("Patch_DrySand_Zone1", parent,
            new Vector3(0f, 0f, -44f), 46f, 11f, matSand, 1, 0.026f, 12);

        // Bantaran sungai: pasir lembap tipis di kedua tepi.
        MakeBlobField("Patch_Sand_SouthBank", parent,
            new Vector3(0f, 0f, -4.6f), 54f, 4.6f, matBankSand, 1, 0.011f, 16);
        MakeBlobField("Patch_Sand_NorthBank", parent,
            new Vector3(0f, 0f, 11.6f), 54f, 4.6f, matBankSand, 1, 0.011f, 16);

        // Zona 1: tanah tandus melebar menuju tepi sungai selatan,
        // supaya peralihan ke pasir bantaran terjadi organik (tanpa kotak hijau).
        MakeBlobField("Patch_Dirt_SouthBank", parent,
            new Vector3(0f, 0f, -11f), 40f, 5.5f, matDirt, 0, 0.0105f, 22);

        // Transisi rumput hangat menuju Zona 3 (utara).
        MakeBlobField("Patch_Grass_North", parent,
            new Vector3(0f, 0f, 18.5f), 50f, 5f, matGrass, 0, 0.009f, 18);

        BuildSmoothPath(parent, new Vector3(-9f, 0f, -34f), new Vector3(0f, 0f, -2.4f), 3.6f, 16);
        BuildSmoothPath(parent, new Vector3(0f, 0f, 9.4f), new Vector3(0f, 0f, 36f), 4.2f, 14);
    }

    /// <summary>
    /// Patch tanah organik: satu alas utama (opsional) + blob kecil bertumpuk di tepinya
    /// sehingga siluet area tidak terlihat seperti persegi panjang prosedural.
    /// </summary>
    private static void MakeBlobField(string name, Transform parent, Vector3 center,
        float halfX, float halfZ, Material mat, int basePatches, float baseY, int blobs)
    {
        for (int i = 0; i < basePatches; i++)
        {
            MakeBox(name + "_Base_" + i, parent,
                new Vector3(center.x, baseY, center.z),
                new Vector3(halfX * 2f, 0.003f, halfZ * 2f),
                Quaternion.identity, mat, false, GroundLayer);
        }

        for (int i = 0; i < blobs; i++)
        {
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radius = RandomRange(0.72f, 1.12f);
            float x = center.x + (Mathf.Cos(angle) * halfX * radius);
            float z = center.z + (Mathf.Sin(angle) * halfZ * radius);
            float size = RandomRange(3.5f, 9f);

            MakeBox(name + "_Blob_" + i, parent,
                new Vector3(x, baseY + 0.002f + (i * 0.0008f), z),
                new Vector3(size, 0.003f, size * RandomRange(0.6f, 1.3f)),
                Quaternion.Euler(0f, RandomRange(0f, 360f), 0f), mat, false, GroundLayer);
        }
    }

    private static void BuildSmoothPath(Transform parent, Vector3 start, Vector3 end, float bend, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float t2 = Mathf.Min(1f, (i + 2) / (float)segments);

            Vector3 p0 = PathPoint(start, end, bend, t0);
            Vector3 p1 = PathPoint(start, end, bend, t1);
            Vector3 p2 = PathPoint(start, end, bend, t2);

            Vector3 dir = (t2 - t1) > 0.0001f ? (p2 - p1) : (p1 - p0);
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float length = Vector3.Distance(p0, p1) * 1.18f;

            Vector3 center = (p0 + p1) * 0.5f;
            center.y = 0.03f;

            MakeBox("Path_Segment_" + i, parent, center,
                new Vector3(2.7f, 0.02f, Mathf.Max(1.2f, length)),
                Quaternion.Euler(0f, angle, 0f), matPath, false, GroundLayer);
        }
    }

    private static Vector3 PathPoint(Vector3 start, Vector3 end, float bend, float t)
    {
        Vector3 p = Vector3.Lerp(start, end, t);
        p.x += Mathf.Sin(t * Mathf.PI) * bend;
        return p;
    }

    // ----------------------------------------------------------------- river
    private static void BuildRiver(Transform river)
    {
        GameObject water = MakeBox("Zona2_RiverWater", river,
            new Vector3(0f, WaterY - 0.03f, 3.5f),
            new Vector3(RiverHalfWidthX * 2f, 0.06f, 12.6f),
            Quaternion.identity, matWater, false, 0);

        MeshRenderer waterRenderer = water.GetComponent<MeshRenderer>();
        if (waterRenderer != null)
        {
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
            waterRenderer.receiveShadows = true;
        }

        Zona2WaterVisual visual = water.AddComponent<Zona2WaterVisual>();

        Transform foamRoot = NewChild(river, "River_FoamStrips");
        List<Transform> foams = new List<Transform>();

        Vector3[] foamSpots =
        {
            new Vector3(-3.1f, WaterY + 0.02f, 0.5f),
            new Vector3(3.1f, WaterY + 0.02f, 0.8f),
            new Vector3(-3.1f, WaterY + 0.02f, 6.3f),
            new Vector3(3.1f, WaterY + 0.02f, 6.5f),
            new Vector3(-7.8f, WaterY + 0.02f, 2.6f),
            new Vector3(8.6f, WaterY + 0.02f, 5.0f),
            new Vector3(-15.8f, WaterY + 0.02f, 4.4f),
            new Vector3(17.5f, WaterY + 0.02f, 1.8f),
            new Vector3(-22.5f, WaterY + 0.02f, 7.2f),
            new Vector3(24.5f, WaterY + 0.02f, 3.4f)
        };

        for (int i = 0; i < foamSpots.Length; i++)
        {
            GameObject foam = MakeBox("Foam_Strip_" + i, foamRoot, foamSpots[i],
                new Vector3(RandomRange(0.8f, 1.6f), 0.01f, RandomRange(0.07f, 0.14f)),
                Quaternion.Euler(0f, RandomRange(-5f, 5f), 0f), matFoam, false, 0);

            MeshRenderer foamRenderer = foam.GetComponent<MeshRenderer>();
            if (foamRenderer != null)
            {
                foamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            foams.Add(foam.transform);

            // Riak kedua yang lebih pendek di dekatnya agar terlihat seperti busa menyebar.
            GameObject ripple = MakeBox("Foam_Ripple_" + i, foamRoot,
                foamSpots[i] + new Vector3(RandomRange(-0.6f, 0.6f), 0.001f, RandomRange(0.18f, 0.35f)),
                new Vector3(RandomRange(0.5f, 1.0f), 0.01f, RandomRange(0.05f, 0.1f)),
                Quaternion.Euler(0f, RandomRange(-8f, 8f), 0f), matFoam, false, 0);

            MeshRenderer rippleRenderer = ripple.GetComponent<MeshRenderer>();
            if (rippleRenderer != null)
            {
                rippleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            foams.Add(ripple.transform);
        }

        if (visual != null)
        {
            visual.SetFoamStrips(foams.ToArray());
        }

        BuildRiverMouths(river);
        BuildRiverBlockers(river);
    }

    /// <summary>
    /// Menutup ujung aliran sungai di kiri/kanan peta dengan bebatuan besar + rimbun pohon,
    /// supaya air tidak terlihat berhenti mendadak di tepi dunia.
    /// </summary>
    private static void BuildRiverMouths(Transform parent)
    {
        Transform mouthRoot = NewChild(parent, "River_Mouths");

        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? -1f : 1f;

            for (int i = 0; i < 9; i++)
            {
                float x = sign * RandomRange(RiverHalfWidthX - 0.5f, RiverHalfWidthX + 4.5f);
                float z = RandomRange(-3.2f, 10.2f);

                SpawnModel(PickKenney("rock_largeA", "rock_largeC", "rock_largeE", "cliff_block_rock"),
                    mouthRoot, new Vector3(x, 0.1f, z), RandomRange(0f, 360f),
                    RandomRange(2.4f, 4.6f), -1, matRock, 0.15f);
                rockCount++;
            }

            for (int i = 0; i < 4; i++)
            {
                float x = sign * RandomRange(RiverHalfWidthX + 2f, RiverHalfWidthX + 6f);
                float z = RandomRange(-4f, 11f);

                SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "Willow_2"),
                    mouthRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                    RandomRange(8f, 11f), 1, null, 0.02f);
                treeCount++;
            }
        }
    }

    private static void BuildRiverBlockers(Transform parent)
    {
        Transform blockerRoot = NewChild(parent, "RiverWaterBlockers");

        float gap = 2.6f;
        float span = RiverHalfWidthX - gap;
        float centerX = (RiverHalfWidthX + gap) * 0.5f;

        MakeColliderOnly("Blocker_South_West", blockerRoot,
            new Vector3(-centerX, 1.4f, -1.95f), new Vector3(span, 3f, 0.4f));

        MakeColliderOnly("Blocker_South_East", blockerRoot,
            new Vector3(centerX, 1.4f, -1.95f), new Vector3(span, 3f, 0.4f));

        MakeColliderOnly("Blocker_North_West", blockerRoot,
            new Vector3(-centerX, 1.4f, 8.95f), new Vector3(span, 3f, 0.4f));

        MakeColliderOnly("Blocker_North_East", blockerRoot,
            new Vector3(centerX, 1.4f, 8.95f), new Vector3(span, 3f, 0.4f));
    }

    private static void MakeColliderOnly(string name, Transform parent, Vector3 center, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = size;
    }

    // ---------------------------------------------------------------- bridge

    private static void BuildBridge(Transform parent)
    {
        BuildDeckSegment(parent, "Bridge_Ramp_South",
            BridgeSouthZ, 0f, BridgeRampSouthEndZ, DeckHeight, matWoodA);

        BuildDeckSegment(parent, "Bridge_Deck_Center",
            BridgeRampSouthEndZ, DeckHeight, BridgeRampNorthStartZ, DeckHeight, matWoodB);

        BuildDeckSegment(parent, "Bridge_Ramp_North",
            BridgeRampNorthStartZ, DeckHeight, BridgeNorthZ, 0f, matWoodA);

        BuildPlanks(parent);
        BuildPiers(parent);
        BuildRailings(parent);

        MakeColliderOnly("Bridge_RailCollider_West", parent,
            new Vector3(-2.3f, 0.7f, 3.5f), new Vector3(0.14f, 1.9f, 12f));

        MakeColliderOnly("Bridge_RailCollider_East", parent,
            new Vector3(2.3f, 0.7f, 3.5f), new Vector3(0.14f, 1.9f, 12f));
    }

    private static void BuildDeckSegment(Transform parent, string name,
        float zStart, float yStart, float zEnd, float yEnd, Material mat)
    {
        float thickness = 0.28f;
        Vector3 start = new Vector3(0f, yStart, zStart);
        Vector3 end = new Vector3(0f, yEnd, zEnd);
        Vector3 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.z) * Mathf.Rad2Deg;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 normal = new Vector3(0f, Mathf.Cos(rad), -Mathf.Sin(rad));
        Vector3 center = (start + end) * 0.5f - normal * (thickness * 0.5f);

        MakeBox(name, parent, center,
            new Vector3(BridgeHalfWidth * 2f, thickness, length),
            Quaternion.Euler(-angle, 0f, 0f), mat, true, GroundLayer);

        bridgePieceCount++;
    }

    private static void BuildPlanks(Transform parent)
    {
        Transform plankRoot = NewChild(parent, "Bridge_Planks");
        int plankCount = 26;
        float span = BridgeNorthZ - BridgeSouthZ;

        for (int i = 0; i < plankCount; i++)
        {
            float t = (i + 0.5f) / plankCount;
            float z = BridgeSouthZ + (span * t);
            float y = DeckY(z);
            float angle = DeckSlopeAngle(z);
            Material mat = i % 3 == 0 ? matWoodC : (i % 3 == 1 ? matWoodA : matWoodB);

            GameObject plank = MakeBox("Plank_" + i, plankRoot,
                new Vector3(0f, y + 0.03f, z),
                new Vector3(BridgeHalfWidth * 2f - 0.06f, 0.07f, (span / plankCount) * 0.86f),
                Quaternion.Euler(-angle, RandomRange(-1.4f, 1.4f), 0f), mat, false, GroundLayer);

            bridgePieceCount++;
            if (plank == null)
            {
                Debug.LogWarning("[Phase15] Gagal membuat papan jembatan " + i);
            }
        }
    }

    private static void BuildPiers(Transform parent)
    {
        Transform pierRoot = NewChild(parent, "Bridge_Piers");
        float[] zPositions = { 0.9f, 3.5f, 6.1f };
        float[] xPositions = { -1.9f, 1.9f };

        for (int z = 0; z < zPositions.Length; z++)
        {
            for (int x = 0; x < xPositions.Length; x++)
            {
                MakeBox("Pier_" + z + "_" + x, pierRoot,
                    new Vector3(xPositions[x], -0.55f, zPositions[z]),
                    new Vector3(0.3f, 1.75f, 0.3f),
                    Quaternion.identity, matWoodB, false, GroundLayer);

                bridgePieceCount++;
            }
        }

        MakeBox("Bridge_CrossBeam_A", pierRoot,
            new Vector3(0f, -0.1f, 3.5f), new Vector3(4.3f, 0.22f, 0.24f),
            Quaternion.identity, matWoodB, false, GroundLayer);

        MakeBox("Bridge_CrossBeam_B", pierRoot,
            new Vector3(0f, 0.1f, 6.1f), new Vector3(4.3f, 0.22f, 0.24f),
            Quaternion.identity, matWoodB, false, GroundLayer);
    }

    private static void BuildRailings(Transform parent)
    {
        Transform railRoot = NewChild(parent, "Bridge_Railings");

        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? -BridgeHalfWidth : BridgeHalfWidth;

            for (float z = BridgeSouthZ + 0.3f; z <= BridgeNorthZ - 0.2f; z += 1.55f)
            {
                float deck = DeckY(z);
                MakeBox("RailPost_" + side + "_" + Mathf.RoundToInt(z * 10f), railRoot,
                    new Vector3(x, deck + 0.55f, z),
                    new Vector3(0.13f, 1.1f, 0.13f),
                    Quaternion.identity, matWoodC, false, GroundLayer);

                bridgePieceCount++;
            }

            BuildRailBar(railRoot, "RailTop", side, 1.08f);
            BuildRailBar(railRoot, "RailMid", side, 0.58f);
        }
    }

    private static void BuildRailBar(Transform parent, string name, int side, float height)
    {
        float x = side == 0 ? -BridgeHalfWidth : BridgeHalfWidth;
        float[][] segments =
        {
            new[] { BridgeSouthZ, 0f, BridgeRampSouthEndZ, DeckHeight },
            new[] { BridgeRampSouthEndZ, DeckHeight, BridgeRampNorthStartZ, DeckHeight },
            new[] { BridgeRampNorthStartZ, DeckHeight, BridgeNorthZ, 0f }
        };

        for (int i = 0; i < segments.Length; i++)
        {
            float zStart = segments[i][0];
            float yStart = segments[i][1] + height;
            float zEnd = segments[i][2];
            float yEnd = segments[i][3] + height;

            Vector3 start = new Vector3(x, yStart, zStart);
            Vector3 end = new Vector3(x, yEnd, zEnd);
            Vector3 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.z) * Mathf.Rad2Deg;

            MakeBox(name + "_" + side + "_" + i, parent,
                (start + end) * 0.5f,
                new Vector3(0.1f, 0.11f, length),
                Quaternion.Euler(-angle, 0f, 0f), matWoodC, false, GroundLayer);

            bridgePieceCount++;
        }
    }

    private static float DeckY(float z)
    {
        if (z <= BridgeRampSouthEndZ)
        {
            return Mathf.Lerp(0f, DeckHeight, Mathf.InverseLerp(BridgeSouthZ, BridgeRampSouthEndZ, z));
        }

        if (z <= BridgeRampNorthStartZ)
        {
            return DeckHeight;
        }

        return Mathf.Lerp(DeckHeight, 0f, Mathf.InverseLerp(BridgeRampNorthStartZ, BridgeNorthZ, z));
    }

    private static float DeckSlopeAngle(float z)
    {
        if (z <= BridgeRampSouthEndZ)
        {
            return Mathf.Atan2(DeckHeight, BridgeRampSouthEndZ - BridgeSouthZ) * Mathf.Rad2Deg;
        }

        if (z <= BridgeRampNorthStartZ)
        {
            return 0f;
        }

        return -Mathf.Atan2(DeckHeight, BridgeNorthZ - BridgeRampNorthStartZ) * Mathf.Rad2Deg;
    }

    // -------------------------------------------------------------- zona 2 deco

    private static void BuildZona2Decor(Transform parent)
    {
        BuildRiverRocks(parent);
        BuildBankVegetation(parent);
        BuildBankTrees(parent);
        BuildFallenLogs(parent);
        BuildBankFences(parent);
        BuildLilypads(parent);
        BuildVillageBackdrop(parent);
    }

    private static void BuildRiverRocks(Transform parent)
    {
        Transform rockRoot = NewChild(parent, "River_Rocks");

        for (int cluster = 0; cluster < 9; cluster++)
        {
            float side = cluster % 2 == 0 ? -1f : 1f;
            float baseX = side * RandomRange(5f, 28f);
            float baseZ = cluster < 4
                ? RandomRange(-6.5f, -3f)
                : RandomRange(10.2f, 13.5f);

            int stones = 3 + rng.Next(0, 3);

            for (int i = 0; i < stones; i++)
            {
                float x = baseX + RandomRange(-2.4f, 2.4f);
                float z = baseZ + RandomRange(-1.6f, 1.6f);
                float size = i == 0 ? RandomRange(0.9f, 1.5f) : RandomRange(0.35f, 0.75f);
                Material mat = rng.NextDouble() > 0.5 ? matRock : matRockLight;

                if (IsBridgeCorridor(x, z))
                {
                    continue;
                }

                SpawnModel(PickKenney("rock_largeA", "rock_largeB", "rock_largeC", "rock_largeD"),
                    rockRoot, new Vector3(x, 0.15f, z), RandomRange(0f, 360f), size, 1, mat, 0.02f);
                rockCount++;
            }
        }

        for (int i = 0; i < 14; i++)
        {
            float x = RandomRange(-34f, 34f);
            float z = RandomRange(-2.2f, 9.2f);
            float size = RandomRange(1.1f, 2.4f);

            if (Mathf.Abs(x) < 6.5f)
            {
                continue;
            }

            SpawnModel(PickKenney("rock_largeE", "rock_largeF", "rock_largeA"),
                rockRoot, new Vector3(x, RiverBedY + 0.15f, z), RandomRange(0f, 360f),
                size, 1, rng.NextDouble() > 0.5 ? matRock : matRockLight, 0.05f);
            rockCount++;
        }

        for (int i = 0; i < 18; i++)
        {
            Vector2 p = SampleBankPoint(-30f, 30f, -9f, -2.6f, 9.4f, 14.5f);
            float size = RandomRange(0.3f, 0.6f);

            SpawnModel(PickKenney("rock_smallA", "rock_smallB", "rock_smallC", "rock_smallD"),
                rockRoot, new Vector3(p.x, 0.15f, p.y), RandomRange(0f, 360f), size, 1, matRock, 0.02f);
            rockCount++;
        }

        MakeRockCluster(rockRoot, -3.6f, -2.6f);
        MakeRockCluster(rockRoot, 3.6f, -2.6f);
        MakeRockCluster(rockRoot, -3.6f, 9.6f);
        MakeRockCluster(rockRoot, 3.6f, 9.6f);
    }

    private static bool IsBridgeCorridor(float x, float z)
    {
        return Mathf.Abs(x) < 6f && z > -4f && z < 11f;
    }

    private static void MakeRockCluster(Transform parent, float x, float z)
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnModel(PickKenney("rock_largeA", "rock_largeB", "rock_smallA"),
                parent, new Vector3(x + RandomRange(-0.5f, 0.5f), 0.15f, z + RandomRange(-0.4f, 0.4f)),
                RandomRange(0f, 360f), RandomRange(0.55f, 1.05f), 1, matRockLight, 0.03f);
            rockCount++;
        }
    }

    private static void BuildBankVegetation(Transform parent)
    {
        Transform grassRoot = NewChild(parent, "Bank_Vegetation");

        for (int i = 0; i < 26; i++)
        {
            Vector2 p = SampleBankPoint(-34f, 34f, -9.5f, -2.4f, 9.3f, 15.5f);

            SpawnModel(PickKenney("grass", "grass_large", "grass_leafs", "grass_leafsLarge"),
                grassRoot, new Vector3(p.x, 0.02f, p.y), RandomRange(0f, 360f),
                RandomRange(0.45f, 0.95f), 1, matDryGrass, 0.02f);
            grassCount++;
        }

        for (int i = 0; i < 22; i++)
        {
            Vector2 p = SampleBankPoint(-34f, 34f, -12f, -2.6f, 9.4f, 19f);

            SpawnModel(PickQuatNature("Grass", "Grass_2", "Grass_Short"),
                grassRoot, new Vector3(p.x, 0.02f, p.y), RandomRange(0f, 360f),
                RandomRange(0.35f, 0.6f), 1, null, 0.02f);
            grassCount++;
        }

        for (int i = 0; i < 16; i++)
        {
            Vector2 p = SampleBankPoint(-32f, 32f, -13f, -3f, 9.5f, 20f);

            SpawnModel(PickQuatNature("Bush_1", "Bush_2", "BushBerries_1"),
                grassRoot, new Vector3(p.x, 0.05f, p.y), RandomRange(0f, 360f),
                RandomRange(0.75f, 1.25f), 1, null, 0.03f);
            bushCount++;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector2 p = SampleBankPoint(-30f, 30f, -12f, -3f, 9.6f, 18f);
            Material mat = i % 3 == 0 ? matFlowerA : (i % 3 == 1 ? matFlowerB : null);

            SpawnModel(PickQuatNature("Plant_1", "Plant_2", "Plant_3"),
                grassRoot, new Vector3(p.x, 0.03f, p.y), RandomRange(0f, 360f),
                RandomRange(0.5f, 0.8f), 1, mat, 0.02f);
            grassCount++;
        }
    }

    /// <summary>
    /// Ambil titik acak di salah satu bantaran sungai (selatan atau utara),
    /// dijamin tidak menabrak koridor jembatan.
    /// </summary>
    private static Vector2 SampleBankPoint(float xMin, float xMax,
        float southMin, float southMax, float northMin, float northMax)
    {
        for (int attempt = 0; attempt < 24; attempt++)
        {
            float x = RandomRange(xMin, xMax);
            float z = rng.NextDouble() > 0.5
                ? RandomRange(southMin, southMax)
                : RandomRange(northMin, northMax);

            if (!IsBridgeCorridor(x, z))
            {
                return new Vector2(x, z);
            }
        }

        return new Vector2(xMax + 8f, southMin - 3f);
    }

    private static void BuildBankTrees(Transform parent)
    {
        Transform treeRoot = NewChild(parent, "Bank_Trees");
        float[] treeX = { -34f, -28f, -22f, -14f, 14f, 22f, 28f, 34f, -30f, 30f, -18f, 18f };

        for (int i = 0; i < treeX.Length; i++)
        {
            float x = treeX[i] + RandomRange(-1.6f, 1.6f);
            float z = rng.NextDouble() > 0.5 ? RandomRange(-15f, -10.5f) : RandomRange(16f, 21f);

            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "CommonTree_3",
                    "Willow_1", "Willow_2", "Willow_3"),
                treeRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(6f, 9f), 1, null, 0.02f);
            treeCount++;
        }
    }

    private static void BuildFallenLogs(Transform parent)
    {
        Transform logRoot = NewChild(parent, "Fallen_Logs");
        Vector3[] spots =
        {
            new Vector3(-13f, 0.1f, -6.5f),
            new Vector3(16f, 0.1f, 12.2f),
            new Vector3(-24f, 0.1f, 13.4f),
            new Vector3(9.5f, 0.1f, -10.5f)
        };

        for (int i = 0; i < spots.Length; i++)
        {
            SpawnModel(PickQuatNature("WoodLog"), logRoot, spots[i],
                RandomRange(0f, 360f), RandomRange(2.4f, 3.4f), 0, null, 0.05f);
            logCount++;
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 pos = new Vector3(RandomRange(-28f, 28f), 0.1f,
                rng.NextDouble() > 0.5 ? RandomRange(-13f, -4f) : RandomRange(10f, 16f));

            SpawnModel(PickQuatNature("TreeStump"), logRoot, pos,
                RandomRange(0f, 360f), RandomRange(0.6f, 0.95f), 1, null, 0.03f);
            logCount++;
        }
    }

    private static void BuildBankFences(Transform parent)
    {
        Transform fenceRoot = NewChild(parent, "Bank_Fences");

        BuildFenceLine(fenceRoot, -30f, -14f, -4f, 90f);
        BuildFenceLine(fenceRoot, 30f, -14f, -4f, 90f);
        BuildFenceLine(fenceRoot, -30f, 10f, 21f, 90f);
        BuildFenceLine(fenceRoot, 30f, 10f, 21f, 90f);
    }

    private static void BuildFenceLine(Transform parent, float x, float zStart, float zEnd, float rotY)
    {
        float length = Mathf.Abs(zEnd - zStart);
        int segments = Mathf.Max(1, Mathf.RoundToInt(length / 2f));
        float step = length / segments;

        for (int i = 0; i < segments; i++)
        {
            float z = zStart + (step * (i + 0.5f));
            GameObject fence = SpawnModel(PickKenney("fence_planks", "fence_simple", "fence_simpleLow"),
                parent, new Vector3(x + RandomRange(-0.12f, 0.12f), 0.05f, z),
                rotY + RandomRange(-4f, 4f), step * 1.02f, -1, matWoodC, 0.03f);

            if (fence != null)
            {
                fence.transform.localRotation *= Quaternion.Euler(RandomRange(-2.5f, 2.5f), 0f, RandomRange(-2.5f, 2.5f));
            }

            fenceCount++;
        }
    }

    private static void BuildLilypads(Transform parent)
    {
        Transform lilyRoot = NewChild(parent, "River_Lilypads");
        Vector3[] spots =
        {
            new Vector3(-18f, WaterY + 0.03f, 7.8f),
            new Vector3(-16.5f, WaterY + 0.03f, 6.4f),
            new Vector3(19f, WaterY + 0.03f, 1.4f),
            new Vector3(21f, WaterY + 0.03f, 2.6f),
            new Vector3(-26f, WaterY + 0.03f, 1.2f),
            new Vector3(12.5f, WaterY + 0.03f, 7.4f)
        };

        for (int i = 0; i < spots.Length; i++)
        {
            SpawnModel(PickQuatNature("Lilypad"), lilyRoot, spots[i],
                RandomRange(0f, 360f), RandomRange(0.9f, 1.5f), 0, null, 0f);
            lilyCount++;
        }
    }

    private static void BuildVillageBackdrop(Transform parent)
    {
        Transform villageRoot = NewChild(parent, "Village_Backdrop");
        float[] houseZ = { -8f, 0.5f, 8.5f, 17f };

        for (int i = 0; i < houseZ.Length; i++)
        {
            SpawnModel(PickQuatBuilding("House_1", "House_2", "House_3", "House_4"),
                villageRoot, new Vector3(-44f + RandomRange(-2f, 2f), 0f, houseZ[i]),
                RandomRange(-6f, 6f), RandomRange(6f, 8.5f), 1, null, 0.02f);
            houseCount++;

            SpawnModel(PickQuatBuilding("House_1", "House_2", "House_3", "House_4"),
                villageRoot, new Vector3(44f + RandomRange(-2f, 2f), 0f, houseZ[i] + 1.5f),
                RandomRange(-6f, 6f), RandomRange(6f, 8.5f), 1, null, 0.02f);
            houseCount++;
        }

        SpawnModel(PickQuatBuilding("Inn", "House_4"), villageRoot,
            new Vector3(-46f, 0f, 24f), 8f, 9f, 1, null, 0.02f);
        houseCount++;

        SpawnModel(PickQuatBuilding("Blacksmith", "Stable"), villageRoot,
            new Vector3(46f, 0f, 25f), -8f, 7.5f, 1, null, 0.02f);
        houseCount++;
    }

    // ---------------------------------------------------------------- zona 1

    private static void BuildZona1Area(Transform parent)
    {
        Transform tpsRoot = NewChild(parent, "TPS_WoodPlatform");

        MakeBox("TPS_Platform_Deck", tpsRoot,
            new Vector3(-9.3f, 0f, -29.2f), new Vector3(11f, 0.1f, 6.2f),
            Quaternion.identity, matWoodA, true, GroundLayer);

        for (int i = 0; i < 6; i++)
        {
            MakeBox("TPS_Deck_Plank_" + i, tpsRoot,
                new Vector3(-9.3f + ((i - 2.5f) * 1.75f), 0.065f, -29.2f),
                new Vector3(1.62f, 0.03f, 6.05f),
                Quaternion.Euler(0f, RandomRange(-0.8f, 0.8f), 0f),
                i % 2 == 0 ? matWoodB : matWoodC, false, GroundLayer);
        }

        BuildFenceLine(tpsRoot, -15.4f, -33f, -25.5f, 90f);
        BuildFenceLine(tpsRoot, -3.4f, -33f, -25.5f, 90f);

        Transform tpsProps = NewChild(parent, "TPS_Props");

        SpawnModel(PickQuatProp("Crate"), tpsProps,
            new Vector3(-16.4f, 0f, -27.5f), RandomRange(0f, 360f), 1.1f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Barrel"), tpsProps,
            new Vector3(-2.6f, 0f, -31.4f), RandomRange(0f, 360f), 1.1f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Bags", "Package_1"), tpsProps,
            new Vector3(-15.8f, 0f, -31.2f), RandomRange(0f, 360f), 1.2f, 1, null, 0.02f);

        SpawnModel(PickKenney("sign"), tpsProps,
            new Vector3(-2.2f, 0.05f, -33.5f), 20f, 2.2f, 1, matWoodC, 0.03f);

        Transform tetuaRoot = NewChild(parent, "Tetua_Shop_Area");

        MakeBox("Tetua_Canopy_Post_A", tetuaRoot,
            new Vector3(6.4f, 1.2f, -30.6f), new Vector3(0.16f, 2.4f, 0.16f),
            Quaternion.identity, matWoodB, false, GroundLayer);

        MakeBox("Tetua_Canopy_Post_B", tetuaRoot,
            new Vector3(11.6f, 1.2f, -30.6f), new Vector3(0.16f, 2.4f, 0.16f),
            Quaternion.identity, matWoodB, false, GroundLayer);

        MakeBox("Tetua_Canopy_Roof", tetuaRoot,
            new Vector3(9f, 2.45f, -30.9f), new Vector3(5.9f, 0.14f, 3.2f),
            Quaternion.Euler(-6f, 0f, 0f), matWoodC, false, GroundLayer);

        SpawnModel(PickQuatProp("MarketStand_1"), tetuaRoot,
            new Vector3(10.6f, 0f, -31.6f), -90f, 2.4f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Crate"), tetuaRoot,
            new Vector3(7.1f, 0f, -31.4f), RandomRange(0f, 360f), 0.9f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Barrel"), tetuaRoot,
            new Vector3(11.9f, 0f, -31.3f), RandomRange(0f, 360f), 1f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Bags"), tetuaRoot,
            new Vector3(6.6f, 0f, -30.1f), RandomRange(0f, 360f), 0.9f, 1, null, 0.02f);

        for (int i = 0; i < 8; i++)
        {
            float x = RandomRange(-32f, 32f);
            float z = RandomRange(-48f, -17f);

            SpawnModel(PickKenney("grass", "grass_leafs", "plant_bushSmall"),
                parent, new Vector3(x, 0.02f, z), RandomRange(0f, 360f),
                RandomRange(0.35f, 0.7f), 1, matDryGrass, 0.02f);
            grassCount++;
        }
    }

    // ---------------------------------------------------------------- zona 3

    private static void BuildZona3Area(Transform parent)
    {
        Transform plazaRoot = NewChild(parent, "Plaza_Paving");

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                float x = -11.2f + (col * 2.8f);
                float z = 36.2f + (row * 2.6f);

                if (Mathf.Abs(x) > 10.5f)
                {
                    continue;
                }

                MakeBox("Plaza_Tile_" + row + "_" + col, plazaRoot,
                    new Vector3(x, 0.03f, z), new Vector3(2.79f, 0.02f, 2.59f),
                    Quaternion.Euler(0f, RandomRange(-1.2f, 1.2f), 0f),
                    matPlazaStone, false, GroundLayer);
            }
        }

        MakeBlobField("Plaza_Edge_Gravel", plazaRoot,
            new Vector3(0f, 0f, 44f), 12.6f, 9.8f, matPlazaStone, 1, 0.018f, 10);

        Transform benchRoot = NewChild(parent, "Plaza_Benches");
        Vector3[] benchSpots =
        {
            new Vector3(-13f, 0f, 38f),
            new Vector3(13f, 0f, 38f),
            new Vector3(-13f, 0f, 46f),
            new Vector3(13f, 0f, 46f),
            new Vector3(-9f, 0f, 33f),
            new Vector3(9f, 0f, 33f)
        };

        for (int i = 0; i < benchSpots.Length; i++)
        {
            SpawnModel(PickQuatProp("Bench_1", "Bench_2"), benchRoot, benchSpots[i],
                i % 2 == 0 ? 90f : -90f, 1.1f, 1, null, 0.02f);
        }

        Transform gardenRoot = NewChild(parent, "Plaza_Garden");

        Vector3[] gardenSpots =
        {
            new Vector3(-15.5f, 0f, 34f),
            new Vector3(15.5f, 0f, 34f),
            new Vector3(-15.5f, 0f, 52f),
            new Vector3(15.5f, 0f, 52f)
        };

        for (int g = 0; g < gardenSpots.Length; g++)
        {
            for (int i = 0; i < 7; i++)
            {
                Vector3 pos = gardenSpots[g] + new Vector3(
                    RandomRange(-2.2f, 2.2f), 0.02f, RandomRange(-2.2f, 2.2f));

                Material mat = i % 3 == 0 ? matFlowerA : (i % 3 == 1 ? matFlowerB : matDryGrass);

                SpawnModel(PickQuatNature("Plant_1", "Plant_2", "Plant_3", "Bush_1"),
                    gardenRoot, pos, RandomRange(0f, 360f),
                    RandomRange(0.45f, 0.8f), 1, mat, 0.02f);
                grassCount++;
            }
        }

        SpawnModel(PickQuatProp("Well"), gardenRoot,
            new Vector3(-20f, 0f, 44f), 20f, 3.2f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Hay"), gardenRoot,
            new Vector3(20.5f, 0f, 44f), -25f, 2.2f, 1, null, 0.02f);

        SpawnModel(PickQuatProp("Cart"), gardenRoot,
            new Vector3(19f, 0f, 30.5f), 40f, 2.6f, 1, null, 0.02f);

        BuildFenceLine(gardenRoot, -19f, 56f, 56.6f, 0f);
        BuildFenceLine(gardenRoot, 19f, 56f, 56.6f, 0f);

        Transform houseRoot = NewChild(parent, "Plaza_Houses");
        float[] zRows = { 36f, 44f, 52f };

        for (int i = 0; i < zRows.Length; i++)
        {
            SpawnModel(PickQuatBuilding("House_2", "House_3", "House_1"), houseRoot,
                new Vector3(-22f + RandomRange(-1.5f, 1.5f), 0f, zRows[i]),
                90f + RandomRange(-5f, 5f), RandomRange(6.5f, 8.5f), 1, null, 0.02f);
            houseCount++;

            SpawnModel(PickQuatBuilding("House_3", "House_4", "House_1"), houseRoot,
                new Vector3(22f + RandomRange(-1.5f, 1.5f), 0f, zRows[i] + 1.2f),
                -90f + RandomRange(-5f, 5f), RandomRange(6.5f, 8.5f), 1, null, 0.02f);
            houseCount++;
        }

        Transform treeRoot = NewChild(parent, "Plaza_Trees");
        float[] treeX = { -27f, -16f, 16f, 27f, -20f, 20f };

        for (int i = 0; i < treeX.Length; i++)
        {
            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "Willow_1", "Willow_2"),
                treeRoot, new Vector3(treeX[i], 0f, RandomRange(30f, 56f)),
                RandomRange(0f, 360f), RandomRange(6.5f, 9f), 1, null, 0.02f);
            treeCount++;
        }
    }

    // ------------------------------------------------------------- boundaries

    private static void BuildBoundaries(Transform walls, Transform perimeter)
    {
        MakeColliderOnly("Boundary_West", walls, new Vector3(-36.5f, 5f, 4f), new Vector3(1f, 12f, 118f));
        MakeColliderOnly("Boundary_East", walls, new Vector3(36.5f, 5f, 4f), new Vector3(1f, 12f, 118f));
        MakeColliderOnly("Boundary_South", walls, new Vector3(0f, 5f, -50.5f), new Vector3(75f, 12f, 1f));
        MakeColliderOnly("Boundary_North", walls, new Vector3(0f, 5f, 58.5f), new Vector3(75f, 12f, 1f));

        Transform fenceRoot = NewChild(perimeter, "Perimeter_Fences");
        for (float z = -48f; z <= 56f; z += 6f)
        {
            // Lewati segmen pagar yang memotong aliran sungai (Z -2..+9).
            if (z > -5f && z < 12f)
            {
                continue;
            }

            SpawnModel(PickKenney("fence_planks", "fence_simple"), fenceRoot,
                new Vector3(-37.5f, 0.05f, z), 90f, 5.6f, -1, matWoodB, 0.03f);
            fenceCount++;

            SpawnModel(PickKenney("fence_planks", "fence_simple"), fenceRoot,
                new Vector3(37.5f, 0.05f, z), 90f, 5.6f, -1, matWoodB, 0.03f);
            fenceCount++;
        }

        Transform forestRoot = NewChild(perimeter, "Perimeter_Forest");

        for (int i = 0; i < 26; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * RandomRange(42f, 58f);
            float z = RandomRange(-52f, 58f);

            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "CommonTree_3",
                    "Willow_1", "Willow_2"),
                forestRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(7f, 11f), 1, null, 0.02f);
            treeCount++;
        }

        for (int i = 0; i < 10; i++)
        {
            float x = RandomRange(-34f, 34f);
            float z = i % 2 == 0 ? -54f : 62f;

            SpawnModel(PickQuatNature("CommonTree_2", "CommonTree_3", "Willow_3"),
                forestRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(7f, 10f), 1, null, 0.02f);
            treeCount++;
        }

        for (int i = 0; i < 12; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * RandomRange(38f, 44f);
            float z = RandomRange(-46f, 54f);

            SpawnModel(PickKenney("rock_largeA", "rock_largeC", "rock_largeE"),
                forestRoot, new Vector3(x, 0.1f, z), RandomRange(0f, 360f),
                RandomRange(1.4f, 2.6f), 1, matRock, 0.05f);
            rockCount++;
        }

        // Sabuk hutan jauh: mengisi cakrawala agar pemain tidak melihat hamparan kosong.
        Transform farRoot = NewChild(perimeter, "Far_Forest_Ring");

        for (int i = 0; i < 74; i++)
        {
            float angle = (i / 74f) * Mathf.PI * 2f;
            float radius = RandomRange(66f, 96f);
            float x = Mathf.Cos(angle) * radius * 1.15f;
            float z = 4f + (Mathf.Sin(angle) * radius);

            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "CommonTree_3",
                    "Willow_1", "Willow_2", "Willow_3"),
                farRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(9f, 15f), 1, null, 0.02f);
            treeCount++;
        }

        for (int i = 0; i < 30; i++)
        {
            float angle = (i / 30f) * Mathf.PI * 2f;
            float radius = RandomRange(58f, 74f);
            float x = Mathf.Cos(angle) * radius * 1.2f;
            float z = 4f + (Mathf.Sin(angle) * radius);

            SpawnModel(PickKenney("rock_largeB", "rock_largeD", "rock_largeF"),
                farRoot, new Vector3(x, 0.15f, z), RandomRange(0f, 360f),
                RandomRange(2f, 4f), 1, matRockLight, 0.08f);
            rockCount++;
        }

        // Dinding hutan tepi utara & selatan: menutup horizon tepat di luar pagar desa,
        // sehingga pandangan lurus ke depan tidak berakhir di hamparan hijau kosong.
        for (int i = 0; i < 44; i++)
        {
            float x = RandomRange(-64f, 64f);
            float z = RandomRange(-74f, -58f);

            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "CommonTree_3", "Willow_2"),
                farRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(10f, 16f), 1, null, 0.02f);
            treeCount++;
        }

        for (int i = 0; i < 44; i++)
        {
            float x = RandomRange(-64f, 64f);
            float z = RandomRange(68f, 86f);

            SpawnModel(PickQuatNature("CommonTree_1", "CommonTree_2", "CommonTree_3", "Willow_2"),
                farRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(10f, 16f), 1, null, 0.02f);
            treeCount++;
        }

        for (int i = 0; i < 22; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * RandomRange(52f, 72f);
            float z = RandomRange(-62f, 72f);

            SpawnModel(PickQuatNature("CommonTree_2", "CommonTree_3", "Willow_1", "Willow_3"),
                farRoot, new Vector3(x, 0f, z), RandomRange(0f, 360f),
                RandomRange(9f, 15f), 1, null, 0.02f);
            treeCount++;
        }
    }

    // ------------------------------------------------------------- atmosphere

    private static void SetupAtmosphere()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Light sun = null;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            sun.color = ParseColor("#FFE7C2");
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            EditorUtility.SetDirty(sun);
        }
        else
        {
            Debug.LogWarning("[Phase15] Directional Light tidak ditemukan, atmosfer matahari dilewati.");
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ParseColor("#A8CCE8");
        RenderSettings.ambientEquatorColor = ParseColor("#DECBA8");
        RenderSettings.ambientGroundColor = ParseColor("#6E5D49");
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = ParseColor("#D9E2D8");
        RenderSettings.fogStartDistance = 52f;
        RenderSettings.fogEndDistance = 132f;
    }

    // ------------------------------------------------------------ spawn points

    private static GameObject BuildSpawnPoints()
    {
        GameObject spawnRoot = new GameObject(SpawnRootName);
        Transform z1 = NewChild(spawnRoot.transform, "Zona1_TPS");
        Transform z2 = NewChild(spawnRoot.transform, "Zona2_Riverbank");
        Transform z3 = NewChild(spawnRoot.transform, "Zona3_BalaiDesa");

        int index = 1;
        int created = 0;

        while (created < 12)
        {
            float x = RandomRange(-28f, 28f);
            float z = RandomRange(-46f, -17f);

            if (!IsBlockedZona1(x, z))
            {
                CreateSpawnPoint(z1, "SP15_Z1_" + index.ToString("00"), x, z);
                created++;
                index++;
            }
        }

        index = 1;
        created = 0;
        while (created < 12)
        {
            float x = RandomRange(-30f, 30f);
            float z = RandomRange(-14f, -3.2f);

            if (!IsBlockedBridge(x, z))
            {
                CreateSpawnPoint(z2, "SP15_Z2_S_" + index.ToString("00"), x, z);
                created++;
                index++;
            }
        }

        index = 1;
        created = 0;
        while (created < 12)
        {
            float x = RandomRange(-30f, 30f);
            float z = RandomRange(10.2f, 20f);

            if (!IsBlockedBridge(x, z))
            {
                CreateSpawnPoint(z2, "SP15_Z2_N_" + index.ToString("00"), x, z);
                created++;
                index++;
            }
        }

        index = 1;
        created = 0;
        while (created < 14)
        {
            float x = RandomRange(-24f, 24f);
            float z = RandomRange(25f, 52f);

            if (!IsBlockedZona3(x, z))
            {
                CreateSpawnPoint(z3, "SP15_Z3_" + index.ToString("00"), x, z);
                created++;
                index++;
            }
        }

        return spawnRoot;
    }

    private static void CreateSpawnPoint(Transform parent, string name, float x, float z)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = new Vector3(x, 0.15f, z);
    }

    private static bool IsBlockedZona1(float x, float z)
    {
        if (x > -15.5f && x < -3.2f && z > -33.2f && z < -25.2f)
        {
            return true;
        }

        if (x > 5.8f && x < 12.4f && z > -32.4f && z < -25.2f)
        {
            return true;
        }

        return Mathf.Abs(x) < 3.2f && z < -35.5f;
    }

    private static bool IsBlockedBridge(float x, float z)
    {
        return Mathf.Abs(x) < 3.4f && z > -4.5f && z < 11.5f;
    }

    private static bool IsBlockedZona3(float x, float z)
    {
        if (Mathf.Abs(x) < 8.5f && z > 39f && z < 53f)
        {
            return true;
        }

        if (Mathf.Abs(Mathf.Abs(x) - 22f) < 7.5f && z > 31f && z < 56f)
        {
            return true;
        }

        return Mathf.Abs(x) < 13f && z > 33f && z < 53f;
    }

    private static void WireTrashSpawner(GameObject spawnRoot)
    {
        TrashSpawner spawner = Object.FindFirstObjectByType<TrashSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[Phase15] TrashSpawner tidak ditemukan, titik spawn tidak di-wire.");
            return;
        }

        Transform[] children = spawnRoot.GetComponentsInChildren<Transform>(true);
        List<Transform> points = new List<Transform>();

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null || children[i] == spawnRoot.transform)
            {
                continue;
            }

            if (children[i].parent == spawnRoot.transform)
            {
                continue;
            }

            points.Add(children[i]);
        }

        spawner.spawnPoints = points.ToArray();
        spawner.maxTrashOnMap = 30;
        EditorUtility.SetDirty(spawner);
    }

    // ------------------------------------------------------------ relocations

    private static void RelocateCoreObjects()
    {
        GameObject player = null;
        try
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            player = null;
        }

        if (player == null)
        {
            PlayerController controller = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                player = controller.gameObject;
            }
        }

        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0.1f, -38f);
            player.transform.rotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("[Phase15] Player/Kades tidak ditemukan.");
        }

        RelocateTrashBins();
        RelocateMachine("MesinKompos", new Vector3(-13.8f, 0.08f, -29f));
        RelocateMachine("MesinPress", new Vector3(-9f, 0.08f, -31.8f));

        GameObject tetua = FindObjectByName("TetuaDesa");
        if (tetua != null)
        {
            tetua.transform.position = new Vector3(9f, 0f, -28f);
            tetua.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            Debug.LogWarning("[Phase15] TetuaDesa tidak ditemukan.");
        }

        RelocateBuilding("BalaiDesa_Rusak", true);
        RelocateBuilding("BalaiDesa_Megah", false);

        RelocateCamera();
        RelocateTrashSamples();
    }

    private static void RelocateCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                camera = cameras[0];
            }
        }

        if (camera == null)
        {
            Debug.LogWarning("[Phase15] Main Camera tidak ditemukan.");
            return;
        }

        camera.transform.position = new Vector3(0f, 3.5f, -43.6f);
        camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 320f);
    }

    private static void RelocateTrashBins()
    {
        TrashBin[] bins = Object.FindObjectsByType<TrashBin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int moved = 0;

        for (int i = 0; i < bins.Length; i++)
        {
            if (bins[i] == null)
            {
                continue;
            }

            Vector3 target;

            switch (bins[i].acceptedTrashTag)
            {
                case "Organik":
                    target = new Vector3(-11.5f, 0.08f, -29f);
                    break;
                case "Anorganik":
                    target = new Vector3(-9f, 0.08f, -29f);
                    break;
                case "B3":
                    target = new Vector3(-6.5f, 0.08f, -29f);
                    break;
                default:
                    continue;
            }

            bins[i].transform.position = target;
            bins[i].transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            moved++;
        }

        if (moved != 3)
        {
            Debug.LogWarning("[Phase15] Tong sampah terdeteksi " + moved + "/3. Cek tag acceptedTrashTag.");
        }
    }

    private static void RelocateMachine(string keyword, Vector3 position)
    {
        GameObject machine = FindObjectByName(keyword);
        if (machine == null)
        {
            machine = FindObjectByPrefix(keyword);
        }

        if (machine == null)
        {
            Debug.LogWarning("[Phase15] Objek mesin '" + keyword + "' tidak ditemukan.");
            return;
        }

        bool wasActive = machine.activeSelf;
        machine.transform.position = position;
        machine.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        machine.SetActive(wasActive);
    }

    private static GameObject FindObjectByPrefix(string prefix)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepByPrefix(roots[i].transform, prefix);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDeepByPrefix(Transform parent, string prefix)
    {
        if (parent.name.StartsWith(prefix))
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepByPrefix(parent.GetChild(i), prefix);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void RelocateBuilding(string name, bool expectedActive)
    {
        GameObject building = FindObjectByName(name);
        if (building == null)
        {
            Debug.LogWarning("[Phase15] Bangunan '" + name + "' tidak ditemukan.");
            return;
        }

        building.transform.position = new Vector3(0f, 0f, 46f);
        building.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Model BalaiDesa diimpor sangat kecil (~3.5m). Skalakan ke ukuran
        // balai desa yang wajar (~8m) agar terbaca sebagai bangunan utama Zona 3.
        Bounds worldBounds;
        if (TryGetWorldBounds(building, out worldBounds) && worldBounds.size.y > 0.01f)
        {
            float factor = 8.2f / worldBounds.size.y;
            building.transform.localScale *= Mathf.Clamp(factor, 0.5f, 6f);
        }

        if (expectedActive)
        {
            building.SetActive(true);
        }
        else
        {
            building.SetActive(false);
        }
    }

    private static void RelocateTrashSamples()
    {
        GameObject samples = FindObjectByName("Trash_Samples");
        if (samples == null)
        {
            return;
        }

        Vector3[] positions =
        {
            new Vector3(-14f, 0.2f, -36f),
            new Vector3(-11f, 0.2f, -37.5f),
            new Vector3(-6f, 0.2f, -36.5f),
            new Vector3(-3.5f, 0.2f, -41f),
            new Vector3(4f, 0.2f, -37f)
        };

        for (int i = 0; i < samples.transform.childCount && i < positions.Length; i++)
        {
            samples.transform.GetChild(i).position = positions[i];
        }
    }

    private static GameObject FindObjectByName(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeep(roots[i].transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void DisableConflictingLegacyObjects()
    {
        string[] legacyNames =
        {
            "Ground",
            "Pond_Water",
            "YeVillage_Nature",
            "Village_Decor",
            "SpawnPoints"
        };

        for (int i = 0; i < legacyNames.Length; i++)
        {
            GameObject legacy = FindObjectByName(legacyNames[i]);
            if (legacy == null)
            {
                continue;
            }

            if (legacy.activeSelf)
            {
                legacy.SetActive(false);
                disabledObjects.Add(legacyNames[i]);
            }
        }
    }

    // --------------------------------------------------------------- spawning

    private static GameObject SpawnModel(string assetPath, Transform parent, Vector3 position,
        float rotY, float targetSize, int axis, Material overrideMaterial, float sink)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            if (!missingAssets.Contains(assetPath))
            {
                missingAssets.Add(assetPath);
            }

            return null;
        }

        GameObject go = (GameObject)Object.Instantiate(asset, parent);
        go.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * go.transform.localRotation;

        FitModel(go, targetSize, axis);
        PlaceOnGround(go, position, sink);

        if (overrideMaterial != null)
        {
            ApplyMaterial(go, overrideMaterial);
        }
        else
        {
            EnsureUrpShader(go, assetPath);
        }

        return go;
    }

    private static void FitModel(GameObject go, float targetSize, int axis)
    {
        if (targetSize <= 0f)
        {
            return;
        }

        Bounds worldBounds;
        if (!TryGetWorldBounds(go, out worldBounds))
        {
            return;
        }

        float current;

        if (axis < 0)
        {
            current = Mathf.Max(worldBounds.size.x, Mathf.Max(worldBounds.size.y, worldBounds.size.z));
        }
        else
        {
            current = axis == 0 ? worldBounds.size.x : (axis == 1 ? worldBounds.size.y : worldBounds.size.z);
        }

        if (current <= 0.0001f)
        {
            return;
        }

        float factor = targetSize / current;
        go.transform.localScale *= factor;
    }

    private static void PlaceOnGround(GameObject go, Vector3 position, float sink)
    {
        go.transform.localPosition = position;

        Bounds worldBounds;
        if (!TryGetWorldBounds(go, out worldBounds))
        {
            return;
        }

        float deltaY = position.y - sink - worldBounds.min.y;
        go.transform.localPosition = position + new Vector3(0f, deltaY, 0f);
    }

    /// <summary>
    /// Bounds dihitung manual dari vertex mesh dalam ruang dunia (bukan Renderer.bounds),
    /// karena Renderer.bounds masih basi pada frame saat objek baru di-Instantiate.
    /// Transform matriks dipakai penuh agar rotasi 270° bawaan FBX ikut terhitung benar.
    /// </summary>
    private static bool TryGetWorldBounds(GameObject go, out Bounds worldBounds)
    {
        worldBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] == null || filters[i].sharedMesh == null)
            {
                continue;
            }

            AccumulateBounds(filters[i].sharedMesh.bounds,
                filters[i].transform.localToWorldMatrix, ref worldBounds, ref hasBounds);
        }

        SkinnedMeshRenderer[] skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i] == null || skinned[i].sharedMesh == null)
            {
                continue;
            }

            AccumulateBounds(skinned[i].sharedMesh.bounds,
                skinned[i].transform.localToWorldMatrix, ref worldBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void AccumulateBounds(Bounds meshBounds, Matrix4x4 matrix,
        ref Bounds worldBounds, ref bool hasBounds)
    {
        Vector3 center = meshBounds.center;
        Vector3 extents = meshBounds.extents;

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = new Vector3(
                center.x + ((corner & 1) == 0 ? -extents.x : extents.x),
                center.y + ((corner & 2) == 0 ? -extents.y : extents.y),
                center.z + ((corner & 4) == 0 ? -extents.z : extents.z));

            Vector3 world = matrix.MultiplyPoint3x4(point);

            if (!hasBounds)
            {
                worldBounds = new Bounds(world, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(world);
            }
        }
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] materials = renderers[i].sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }

            renderers[i].sharedMaterials = materials;
        }
    }

    private static void EnsureUrpShader(GameObject go, string assetPath)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material[] materials = renderers[i].sharedMaterials;
            bool changed = false;

            for (int m = 0; m < materials.Length; m++)
            {
                Material mat = materials[m];
                if (mat == null)
                {
                    materials[m] = matFallback;
                    changed = true;
                    continue;
                }

                string shaderName = mat.shader != null ? mat.shader.name : string.Empty;
                bool isUrp = shaderName.StartsWith("Universal Render Pipeline") ||
                             shaderName.StartsWith("Shader Graphs") ||
                             shaderName.StartsWith("Hidden/Universal");

                if (!isUrp)
                {
                    materials[m] = matFallback;
                    changed = true;

                    if (!missingAssets.Contains(assetPath + " (shader non-URP)"))
                    {
                        missingAssets.Add(assetPath + " (shader non-URP)");
                    }
                }
            }

            if (changed)
            {
                renderers[i].sharedMaterials = materials;
            }
        }
    }

    // -------------------------------------------------------------- primitives

    private static GameObject MakeBox(string name, Transform parent, Vector3 center, Vector3 size,
        Quaternion rotation, Material material, bool withCollider, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localRotation = rotation;
        go.transform.localScale = size;
        go.layer = layer;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        if (!withCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        return go;
    }

    private static void MakeSlopedBox(string name, Transform parent,
        float xStart, float zStart, float yStart,
        float xEnd, float zEnd, float yEnd,
        float thickness, Material material, bool withCollider)
    {
        if (zEnd < zStart)
        {
            float tempZ = zStart;
            zStart = zEnd;
            zEnd = tempZ;

            float tempY = yStart;
            yStart = yEnd;
            yEnd = tempY;
        }

        Vector3 start = new Vector3(xStart, yStart, zStart);
        Vector3 end = new Vector3(xStart, yEnd, zEnd);
        Vector3 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.z) * Mathf.Rad2Deg;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 normal = new Vector3(0f, Mathf.Cos(rad), -Mathf.Sin(rad));
        Vector3 center = (start + end) * 0.5f - normal * (thickness * 0.5f);
        center.x = 0f;

        MakeBox(name, parent, center,
            new Vector3(Mathf.Abs(xEnd - xStart), thickness, length),
            Quaternion.Euler(-angle, 0f, 0f), material, withCollider, GroundLayer);
    }

    // ---------------------------------------------------------------- helpers

    private static float RandomRange(float min, float max)
    {
        return min + ((float)rng.NextDouble() * (max - min));
    }

    private static Color ParseColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }

        return Color.white;
    }

    private static string PickKenney(params string[] names)
    {
        return PickModel(KenneyFolder, names);
    }

    private static string PickQuatNature(params string[] names)
    {
        return PickModel(NatureFolder, names);
    }

    private static string PickQuatBuilding(params string[] names)
    {
        return PickModel(BuildingFolder, names);
    }

    private static string PickQuatProp(params string[] names)
    {
        return PickModel(PropFolder, names);
    }

    private static string PickModel(string folder, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            string path = folder + "/" + names[i] + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return path;
            }
        }

        if (names.Length > 0)
        {
            string missing = folder + "/" + names[0] + ".fbx";
            if (!missingAssets.Contains(missing))
            {
                missingAssets.Add(missing);
            }
        }

        return null;
    }

    // -------------------------------------------------------------- reporting

    private static void LogSummary(GameObject spawnRoot)
    {
        int z1 = 0;
        int z2 = 0;
        int z3 = 0;

        if (spawnRoot != null)
        {
            Transform[] all = spawnRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == spawnRoot.transform)
                {
                    continue;
                }

                if (all[i].parent == spawnRoot.transform)
                {
                    continue;
                }

                if (all[i].name.StartsWith("SP15_Z1"))
                {
                    z1++;
                }
                else if (all[i].name.StartsWith("SP15_Z2"))
                {
                    z2++;
                }
                else if (all[i].name.StartsWith("SP15_Z3"))
                {
                    z3++;
                }
            }
        }

        Debug.Log("[Phase15] ===== EXPAND MAP & BUILD ZONA 2 =====");
        Debug.Log("[Phase15] Map playable       : 76m x 116m (bingkai 110m x 145m)");
        Debug.Log("[Phase15] Zona 1 (TPS)       : Z -52..-16");
        Debug.Log("[Phase15] Zona 2 (Sungai)    : Z -16..+22 (sungai X -56..+56)");
        Debug.Log("[Phase15] Zona 3 (BalaiDesa) : Z +22..+60");
        Debug.Log("[Phase15] Batu sungai        : " + rockCount);
        Debug.Log("[Phase15] Pohon              : " + treeCount);
        Debug.Log("[Phase15] Semak              : " + bushCount);
        Debug.Log("[Phase15] Rumput/ilalang     : " + grassCount);
        Debug.Log("[Phase15] Pagar              : " + fenceCount);
        Debug.Log("[Phase15] Batang tumbang     : " + logCount);
        Debug.Log("[Phase15] Teratai            : " + lilyCount);
        Debug.Log("[Phase15] Rumah latar        : " + houseCount);
        Debug.Log("[Phase15] Piece jembatan     : " + bridgePieceCount);
        Debug.Log("[Phase15] Spawn point        : Z1=" + z1 + " | Z2=" + z2 + " | Z3=" + z3 +
                  " (total " + (z1 + z2 + z3) + ")");
        Debug.Log("[Phase15] Player             : (0, 0.1, -38) hadap +Z");
        Debug.Log("[Phase15] TetuaDesa          : (9, 0, -28) hadap -X");
        Debug.Log("[Phase15] BalaiDesa          : (0, 0, 46) hadap -Z");
        Debug.Log("[Phase15] Objek lama nonaktif: " +
                  (disabledObjects.Count == 0 ? "-" : string.Join(", ", disabledObjects.ToArray())));

        if (missingAssets.Count > 0)
        {
            Debug.LogWarning("[Phase15] Aset bermasalah (" + missingAssets.Count + "): " +
                string.Join(", ", missingAssets.ToArray()));
        }
    }
}
