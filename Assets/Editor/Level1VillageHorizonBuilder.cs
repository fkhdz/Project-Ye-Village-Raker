using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class Level1VillageHorizonBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string Kenney = "Assets/ThirdParty/KenneyNatureKit/Models/";
    private const string Village = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/";
    private const string MatYe = "Assets/Materials/Ye_Village/";
    private const string MatL1 = "Assets/Materials/Level1/";
    private const string PolishRoot = "VillageHorizon_Polish";
    private const string HorizonRoot = "HorizonBackdrop";
    private const int ObstacleLayer = 9;

    private static Material matRoad;
    private static Material matYard;
    private static Material matTrack;
    private static Material matSand;
    private static Material matRock;
    private static Material matRockLight;
    private static Material matDry;
    private static Material matWood;
    private static Material matWoodDark;
    private static Material matHazeNear;
    private static Material matHazeMid;
    private static Material matHazeFar;
    private static Material matSkyline;
    private static Material matFloor;

    [MenuItem("YeVillage/Level1/Polish Village & Infinite Horizon")]
    public static void Polish()
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
                Debug.LogError("[VillagePolish] Scene '" + open.name + "' belum disimpan. Simpan dulu.");
                return;
            }
        }

        BackupScene();
        EnsureFolder(MatL1.TrimEnd('/'));
        CreateMaterials();
        UnityEngine.Random.InitState(20261125);

        GameObject env = GameObject.Find("--- ENVIRONMENT ---");
        if (env == null)
        {
            env = new GameObject("--- ENVIRONMENT ---");
        }

        Cleanup(env.transform);
        GameObject polish = new GameObject(PolishRoot);
        polish.transform.SetParent(env.transform, false);
        GameObject horizon = new GameObject(HorizonRoot);
        horizon.transform.SetParent(env.transform, false);

        FixHouseFacing();
        BuildRoads(polish.transform);
        BuildYards(polish.transform);
        BuildApproachToTps(polish.transform);
        DressTps(polish.transform);
        BuildHorizon(horizon.transform);
        TuneAtmosphere();

        MarkStatic(polish);
        MarkStatic(horizon);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int objects = polish.GetComponentsInChildren<Transform>(true).Length + horizon.GetComponentsInChildren<Transform>(true).Length - 2;
        Debug.Log("[VillagePolish] Selesai. " + objects + " objek dekorasi dibuat. Batas alami berlapis (bukit dekat–menengah–jauh) dan batas fisik tersembunyi sudah diselaraskan; gameplay, tong, NPC, spawner, dan jalur Exit tidak diubah.");
    }

    private static void CreateMaterials()
    {
        matRoad = Tinted("M_Road_Dust.mat", new Color(0.90f, 0.79f, 0.57f));
        matYard = Tinted("M_Yard_Dirt.mat", new Color(0.69f, 0.55f, 0.37f));
        matTrack = Tinted("M_Road_Track.mat", new Color(0.72f, 0.60f, 0.40f));
        matHazeNear = Tinted("M_Haze_Near.mat", new Color(0.84f, 0.69f, 0.49f));
        matHazeMid = Tinted("M_Haze_Mid.mat", new Color(0.90f, 0.78f, 0.59f));
        matHazeFar = Tinted("M_Haze_Far.mat", new Color(0.95f, 0.86f, 0.69f));
        matSkyline = Tinted("M_Skyline.mat", new Color(0.98f, 0.90f, 0.75f));
        matFloor = Tinted("M_Horizon_Floor.mat", new Color(0.88f, 0.76f, 0.56f));
        matSand = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Sand.mat");
        matRock = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Sandstone.mat");
        matRockLight = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Sandstone_Light.mat");
        matDry = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_DryPlant.mat");
        matWood = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Wood.mat");
        matWoodDark = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Wood_Dark.mat");
        if (matRock == null) matRock = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Rock.mat");
        if (matRockLight == null) matRockLight = matRock;
        if (matDry == null) matDry = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_LeafYellow.mat");
        if (matWoodDark == null) matWoodDark = matWood;
    }

    private static Material Tinted(string fileName, Color color)
    {
        string path = MatL1 + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Material source = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Sand.mat");
            mat = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.ImportAsset(path);
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void Cleanup(Transform env)
    {
        Transform old = env.Find(PolishRoot);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        Transform oldHorizon = env.Find(HorizonRoot);
        if (oldHorizon != null) UnityEngine.Object.DestroyImmediate(oldHorizon.gameObject);

        for (int i = env.childCount - 1; i >= 0; i--)
        {
            Transform child = env.GetChild(i);
            string n = child.name;
            if (n.StartsWith("DistantDune") || n.StartsWith("ExtraPlant_") || n.StartsWith("Deco_"))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        GameObject tps = GameObject.Find("TPS_Station");
        if (tps != null)
        {
            string[] keep = { "Bin_Green", "Bin_Yellow", "Bin_Red", "Machine_Compost", "Machine_Press",
                "TPS_Deck", "TPS_Stall", "TPS_Cart", "TPS_BackFence_01", "TPS_BackFence_02", "TPS_BackFence_03",
                "TPS_Prop_Barrel_01", "TPS_Prop_Barrel_02", "TPS_Prop_Bucket", "TPS_Prop_Crate_01", "TPS_Prop_Crate_02" };
            for (int i = tps.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = tps.transform.GetChild(i);
                bool allowed = false;
                int seen = 0;
                foreach (string k in keep)
                {
                    if (child.name == k)
                    {
                        seen++;
                        for (int j = 0; j < i; j++)
                        {
                            if (tps.transform.GetChild(j).name == k) seen++;
                        }
                        allowed = seen == 1;
                        break;
                    }
                }
                if (!allowed) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void FixHouseFacing()
    {
        GameObject house = GameObject.Find("House_Sel_03");
        if (house != null)
        {
            float x = house.transform.eulerAngles.x;
            house.transform.rotation = Quaternion.Euler(x, 0f, 0f);
        }
    }

    private static void BuildRoads(Transform parent)
    {
        GameObject group = new GameObject("VillageRoads");
        group.transform.SetParent(parent, false);

        Road(group.transform, new Vector3(2f, 0f, -29f), new Vector3(2f, 0f, -20f), 5.6f);
        Road(group.transform, new Vector3(2f, 0f, -20f), new Vector3(2f, 0f, -10f), 5.3f);
        Road(group.transform, new Vector3(2f, 0f, -10f), new Vector3(2f, 0f, -2f), 4.9f);
        Road(group.transform, new Vector3(2f, 0f, -2f), new Vector3(-4f, 0f, 3f), 4.5f);
        Road(group.transform, new Vector3(-4f, 0f, 3f), new Vector3(-10f, 0f, 9f), 4.1f);
        Road(group.transform, new Vector3(-10f, 0f, 9f), new Vector3(-16f, 0f, 14f), 3.7f);
        Road(group.transform, new Vector3(-16f, 0f, 14f), new Vector3(-18f, 0f, 15.5f), 3.3f);
        Road(group.transform, new Vector3(0f, 0f, 11f), new Vector3(0f, 0f, 29f), 4.2f);
        Road(group.transform, new Vector3(-18f, 0f, 16f), new Vector3(-26f, 0f, 11f), 3f);

        Vector3[] trackPairs = {
            new Vector3(2f, 0f, -24f), new Vector3(2f, 0f, -15f), new Vector3(2f, 0f, -6f),
            new Vector3(-1f, 0f, 1f), new Vector3(-7f, 0f, 7f), new Vector3(-13f, 0f, 12f)
        };
        foreach (Vector3 p in trackPairs)
        {
            Cube(group.transform, "CartTrack_L", p + new Vector3(-1.05f, 0.025f, 0f), new Vector3(0.14f, 0.012f, 5.4f), matTrack, 0f, false);
            Cube(group.transform, "CartTrack_R", p + new Vector3(1.05f, 0.025f, 0f), new Vector3(0.14f, 0.012f, 5.4f), matTrack, 0f, false);
        }

        GameObject pebbleA = Load(Kenney + "rock_smallA.fbx");
        GameObject pebbleB = Load(Kenney + "rock_smallB.fbx");
        for (int i = 0; i < 26; i++)
        {
            float t = i / 25f;
            Vector3 p = new Vector3(2f - t * 20f, 0f, -28f + t * 42f);
            p += new Vector3(i % 2 == 0 ? -3.1f : 3.1f, 0f, UnityEngine.Random.Range(-1.2f, 1.2f));
            GameObject model = i % 2 == 0 ? pebbleA : pebbleB;
            Place(group.transform, model, "RoadPebble_" + i, p, UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0.16f, 0.34f), UnityEngine.Random.Range(0.3f, 0.6f), matRock, false);
        }
    }

    private static void Road(Transform parent, Vector3 a, Vector3 b, float width)
    {
        Vector3 d = b - a;
        float length = d.magnitude;
        float angle = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        Vector3 center = (a + b) * 0.5f;
        Cube(parent, "Road_" + Mathf.RoundToInt(center.x) + "_" + Mathf.RoundToInt(center.z),
            center + new Vector3(0f, 0.015f, 0f), new Vector3(width, 0.03f, length), matRoad, angle, false);
    }

    private static void BuildYards(Transform parent)
    {
        GameObject group = new GameObject("VillageYards");
        group.transform.SetParent(parent, false);

        Vector3[] southHouses = {
            new Vector3(-22f, 0f, -26.5f), new Vector3(-16.5f, 0f, -26.5f), new Vector3(-11f, 0f, -26.8f),
            new Vector3(11f, 0f, -26.8f), new Vector3(16.5f, 0f, -26.5f), new Vector3(22f, 0f, -26.5f)
        };
        Vector3[] westHouses = {
            new Vector3(-26.8f, 0f, -20f), new Vector3(-26.8f, 0f, -13.5f),
            new Vector3(-26.5f, 0f, -7f), new Vector3(-26.2f, 0f, -1f)
        };

        int index = 0;
        foreach (Vector3 h in southHouses)
        {
            Yard(group.transform, "Yard_S_" + index, h + new Vector3(0f, 0f, 4.6f), 0f, index);
            index++;
        }
        foreach (Vector3 h in westHouses)
        {
            Yard(group.transform, "Yard_W_" + index, h + new Vector3(4.6f, 0f, 0f), 90f, index);
            index++;
        }
    }

    private static void Yard(Transform parent, string name, Vector3 center, float yaw, int variant)
    {
        GameObject yard = new GameObject(name);
        yard.transform.SetParent(parent, false);
        yard.transform.position = center;
        yard.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Cube(yard.transform, "Yard_Dirt", new Vector3(0f, 0.02f, 0f), new Vector3(7f, 0.04f, 5f), matYard, 0f, false);

        GameObject fence = Load(Kenney + "fence_planks.fbx");
        Place(yard.transform, fence, "Yard_Fence_L", new Vector3(-3.5f, 0f, -0.2f), 90f, 0.95f, 2.4f, matWood, true, 2f);
        Place(yard.transform, fence, "Yard_Fence_R", new Vector3(3.5f, 0f, -0.2f), 90f, 0.95f, 2.4f, matWood, true, -1.5f);
        Place(yard.transform, fence, "Yard_Fence_Back_A", new Vector3(-2.2f, 0f, 2.5f), 0f, 0.9f, 2.5f, matWood, true, 1f);
        Place(yard.transform, fence, "Yard_Fence_Back_B", new Vector3(2.2f, 0f, 2.5f), 0f, 0.85f, 2.5f, matWood, true, -2f);

        GameObject crate = Load(Village + "Props/Crate.fbx");
        GameObject barrel = Load(Village + "Props/Barrel.fbx");
        GameObject hay = Load(Village + "Props/Hay.fbx");
        GameObject bench = Load(Village + "Props/Bench_1.fbx");
        GameObject well = Load(Village + "Props/Well.fbx");
        GameObject pot = Load(Kenney + "pot_large.fbx");
        GameObject palm = Load(Kenney + "tree_palmShort.fbx");
        GameObject dryTree = Load(Kenney + "tree_thin_fall.fbx");

        switch (variant % 5)
        {
            case 0:
                Place(yard.transform, well, "Yard_Well", new Vector3(-1.5f, 0f, 0.8f), 20f, 1.35f, 0f, matRock, true);
                Place(yard.transform, pot, "Yard_Pot", new Vector3(1.9f, 0f, 1.5f), 0f, 0.6f, 0f, matRockLight, false);
                break;
            case 1:
                Place(yard.transform, hay, "Yard_Hay", new Vector3(-1.7f, 0f, 0.5f), 15f, 0.9f, 0f, matDry, false);
                Place(yard.transform, crate, "Yard_Crate", new Vector3(1.6f, 0f, 0.9f), -20f, 0.7f, 0f, matWood, true);
                Clothesline(yard.transform, new Vector3(0f, 0f, 1.2f));
                break;
            case 2:
                for (int r = 0; r < 2; r++)
                {
                    GameObject row = Load(Kenney + "crops_dirtRow.fbx");
                    GameObject crop = Load(Kenney + "crops_wheatStageA.fbx");
                    Vector3 rp = new Vector3(-1.8f + r * 1.8f, 0f, 0.4f);
                    Place(yard.transform, row, "Yard_CropBed_" + r, rp, 0f, 0.2f, 1.2f, matYard, false);
                    Place(yard.transform, crop, "Yard_DryCrop_" + r, rp, 0f, 0.55f, 0.9f, matDry, false);
                }
                break;
            case 3:
                Place(yard.transform, bench, "Yard_Bench", new Vector3(0f, 0f, 1.8f), 0f, 0.75f, 0f, matWoodDark, true);
                Place(yard.transform, barrel, "Yard_Barrel", new Vector3(2f, 0f, 0.2f), 0f, 0.85f, 0f, matWoodDark, true);
                break;
            default:
                Place(yard.transform, palm, "Yard_Palm", new Vector3(-1.9f, 0f, 0.8f), 25f, 3.1f, 0f, matDry, true);
                Place(yard.transform, crate, "Yard_Crate", new Vector3(1.7f, 0f, 0.6f), 35f, 0.65f, 0f, matWood, true);
                Clothesline(yard.transform, new Vector3(0f, 0f, 1.4f));
                break;
        }

        if (variant % 3 == 0)
        {
            Place(yard.transform, dryTree, "Yard_DeadTree", new Vector3(2.9f, 0f, -1.8f), UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(2.6f, 3.6f), 0f, matDry, true);
        }
    }

    private static void Clothesline(Transform parent, Vector3 center)
    {
        GameObject a = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        a.name = "Clothesline_Post_A";
        a.transform.SetParent(parent, false);
        a.transform.localPosition = center + new Vector3(-1.6f, 0.9f, 0f);
        a.transform.localScale = new Vector3(0.06f, 0.9f, 0.06f);
        UnityEngine.Object.DestroyImmediate(a.GetComponent<Collider>());
        Apply(a, matWoodDark, false);

        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        b.name = "Clothesline_Post_B";
        b.transform.SetParent(parent, false);
        b.transform.localPosition = center + new Vector3(1.6f, 0.9f, 0f);
        b.transform.localScale = new Vector3(0.06f, 0.9f, 0.06f);
        UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
        Apply(b, matWoodDark, false);

        Cube(parent, "Clothesline_Rope", center + new Vector3(0f, 1.78f, 0f), new Vector3(3.2f, 0.025f, 0.025f), matDry, 0f, false);
        for (int i = 0; i < 3; i++)
        {
            Cube(parent, "Clothesline_Cloth_" + i, center + new Vector3(-0.9f + i * 0.9f, 1.45f, 0f),
                new Vector3(0.5f, 0.55f, 0.03f), i == 1 ? matWood : matDry, 0f, false);
        }
    }

    private static void BuildApproachToTps(Transform parent)
    {
        GameObject group = new GameObject("ApproachToTPS");
        group.transform.SetParent(parent, false);

        GameObject fence = Load(Kenney + "fence_planks.fbx");
        GameObject bush = Load(Kenney + "plant_bush.fbx");
        GameObject dryTree = Load(Kenney + "tree_thin_fall.fbx");
        GameObject smallTree = Load(Kenney + "tree_small_fall.fbx");
        GameObject crate = Load(Village + "Props/Crate.fbx");
        GameObject barrel = Load(Village + "Props/Barrel.fbx");
        GameObject sign = Load(Kenney + "sign.fbx");

        Vector3[] fenceSpots = {
            new Vector3(-6f, 0f, 5f), new Vector3(-9f, 0f, 8f), new Vector3(-12f, 0f, 11f),
            new Vector3(-15f, 0f, 14f), new Vector3(-21f, 0f, 12f), new Vector3(-24f, 0f, 14f)
        };
        for (int i = 0; i < fenceSpots.Length; i++)
        {
            Place(group.transform, fence, "Approach_Fence_" + i, fenceSpots[i], i % 2 == 0 ? 35f : -20f,
                UnityEngine.Random.Range(0.75f, 1.05f), 3f, matWood, true, UnityEngine.Random.Range(-3f, 3f));
        }

        Vector3[] trees = {
            new Vector3(-5f, 0f, 6f), new Vector3(-11f, 0f, 5f), new Vector3(-14f, 0f, 10f),
            new Vector3(-18f, 0f, 9f), new Vector3(-22f, 0f, 8f), new Vector3(-8f, 0f, 13f)
        };
        for (int i = 0; i < trees.Length; i++)
        {
            GameObject model = i % 2 == 0 ? dryTree : smallTree;
            Place(group.transform, model, "Approach_DeadTree_" + i, trees[i], UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(2.8f, 4.2f), 0f, matDry, true);
        }

        for (int i = 0; i < 8; i++)
        {
            Vector3 p = new Vector3(UnityEngine.Random.Range(-18f, -7f), 0f, UnityEngine.Random.Range(7f, 15f));
            Place(group.transform, bush, "Approach_DryBush_" + i, p, UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0.5f, 0.9f), UnityEngine.Random.Range(0.8f, 1.4f), matDry, false);
        }

        Place(group.transform, crate, "Approach_Crate_01", new Vector3(-8f, 0f, 10.5f), 25f, 0.7f, 0f, matWood, true);
        Place(group.transform, barrel, "Approach_Barrel_01", new Vector3(-12.5f, 0f, 13f), 0f, 0.85f, 0f, matWoodDark, true);
        Place(group.transform, crate, "Approach_Crate_02", new Vector3(-15.5f, 0f, 11.5f), -15f, 0.65f, 0f, matWood, true);

        GameObject signObj = Place(group.transform, sign, "Sign_TPS", new Vector3(-11.5f, 0f, 10.2f), 205f, 1.8f, 0f, matWoodDark, true);
        if (signObj != null)
        {
            WorldLabelBillboard.CreateLabel(signObj.transform, "TPS SUKAMAJU", new Color(0.36f, 0.28f, 0.16f), new Vector3(0f, 2.3f, 0f));
        }

        for (int i = 0; i < 5; i++)
        {
            Vector3 p = new Vector3(-9f - i * 1.6f, 0.06f, 8f + i * 1.5f);
            Cube(group.transform, "Heap_Sack_" + i, p, new Vector3(0.65f, 0.32f, 0.45f),
                i % 2 == 0 ? matWood : matTrack, UnityEngine.Random.Range(0f, 20f), false);
        }
    }

    private static void DressTps(Transform parent)
    {
        GameObject group = new GameObject("TPSDressing");
        group.transform.SetParent(parent, false);

        Color[] colors = { new Color(0.22f, 0.55f, 0.27f), new Color(0.85f, 0.68f, 0.16f), new Color(0.72f, 0.20f, 0.16f) };
        string[] labels = { "ORGANIK", "ANORGANIK", "B3" };
        string[] bins = { "Bin_Green", "Bin_Yellow", "Bin_Red" };
        float[] xs = { -20.4f, -18f, -15.6f };

        for (int i = 0; i < 3; i++)
        {
            Material postMat = Tinted("M_TPS_Post_" + i + ".mat", colors[i]);
            Cube(group.transform, "TPS_Post_" + i, new Vector3(xs[i], 0.95f, 17.7f), new Vector3(0.16f, 1.9f, 0.16f), postMat, 0f, true);
            Cube(group.transform, "TPS_PostCap_" + i, new Vector3(xs[i], 1.95f, 17.7f), new Vector3(0.55f, 0.12f, 0.2f), postMat, 0f, false);

            GameObject bin = GameObject.Find(bins[i]);
            if (bin != null)
            {
                WorldLabelBillboard.CreateLabel(bin.transform, labels[i], colors[i], new Vector3(0f, 2.35f, 0f));
            }
        }

        float[] px = { -21.2f, -14.8f };
        float[] pz = { 15.2f, 18.8f };
        foreach (float x in px)
        {
            foreach (float z in pz)
            {
                Cube(group.transform, "TPS_CanopyPost", new Vector3(x, 1.25f, z), new Vector3(0.14f, 2.5f, 0.14f), matWoodDark, 0f, true);
            }
        }
        Cube(group.transform, "TPS_CanopyRoof", new Vector3(-18f, 2.55f, 17f), new Vector3(7.3f, 0.1f, 4f), matWoodDark, 0f, false);

        GameObject elder = GameObject.Find("NPC_TetuaDesa");
        if (elder != null)
        {
            WorldLabelBillboard.CreateLabel(elder.transform, "TETUA DESA", new Color(0.38f, 0.29f, 0.18f), new Vector3(0f, 2.7f, 0f));
        }
    }

    private static void BuildHorizon(Transform parent)
    {
        Cube(parent, "OuterGround_Infinite", new Vector3(0f, -1.65f, 0f), new Vector3(900f, 0.5f, 900f), matFloor, 0f, false);

        GameObject dunes = Load(Kenney + "platform_beach.fbx");
        GameObject cliffA = Load(Kenney + "cliff_large_rock.fbx");
        GameObject cliffB = Load(Kenney + "cliff_cornerLarge_rock.fbx");
        GameObject cliffC = Load(Kenney + "cliff_blockSlope_rock.fbx");
        GameObject cliffHalf = Load(Kenney + "cliff_half_rock.fbx");
        GameObject rockA = Load(Kenney + "rock_largeA.fbx");
        GameObject rockC = Load(Kenney + "rock_largeC.fbx");
        GameObject rockE = Load(Kenney + "rock_largeE.fbx");
        GameObject tallA = Load(Kenney + "rock_tallA.fbx");
        GameObject tallB = Load(Kenney + "rock_tallB.fbx");

        BuildNearApron(parent, dunes, cliffA, cliffB, cliffHalf, rockA, rockC, tallA, tallB);

        float[] radii = { 39f, 61f, 88f, 122f, 168f, 232f };
        int[] counts = { 19, 24, 28, 31, 34, 36 };
        float[] heights = { 6.5f, 10f, 17f, 27f, 45f, 72f };
        Material[] mats = { matHazeNear, matHazeNear, matHazeMid, matHazeFar, matSkyline, matSkyline };

        for (int ring = 0; ring < radii.Length; ring++)
        {
            GameObject r = new GameObject("HorizonRing_" + (ring + 1));
            r.transform.SetParent(parent, false);
            float step = 360f / counts[ring];
            for (int i = 0; i < counts[ring]; i++)
            {
                float angle = i * step + UnityEngine.Random.Range(-step * 0.45f, step * 0.45f);
                if (ring <= 1 && Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < 18f) continue;
                if (ring == 3 && i % 13 == 5) continue;

                float radius = radii[ring] * UnityEngine.Random.Range(0.91f, 1.09f);
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(rad) * radius, -0.4f, Mathf.Sin(rad) * radius);
                float h = heights[ring] * UnityEngine.Random.Range(0.62f, 1.32f);
                float yaw = UnityEngine.Random.Range(0f, 360f);

                Material mat = mats[ring];
                if (ring == 0 && UnityEngine.Random.value < 0.35f && matRockLight != null) mat = matRockLight;

                bool dune = ring <= 2 && UnityEngine.Random.value < (ring == 0 ? 0.52f : ring == 1 ? 0.34f : 0.18f);
                if (dune && dunes != null)
                {
                    GameObject inst = InstantiateRaw(dunes);
                    inst.name = "Dune_R" + (ring + 1) + "_" + i;
                    inst.transform.SetParent(r.transform, false);
                    inst.transform.position = p;
                    inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    inst.transform.localScale = Vector3.one * (h * UnityEngine.Random.Range(0.5f, 0.8f));
                    Apply(inst, ring == 0 && matSand != null ? matSand : mat, false);
                }
                else
                {
                    GameObject[] models = { cliffA, cliffB, cliffC, cliffHalf, rockA, rockC, rockE, tallA, tallB };
                    GameObject model = models[UnityEngine.Random.Range(0, models.Length)];
                    Place(r.transform, model, "Ridge_R" + (ring + 1) + "_" + i, p, yaw, h, 0f, mat, false);
                    if (ring <= 1 && UnityEngine.Random.value < 0.32f)
                    {
                        GameObject cap = models[UnityEngine.Random.Range(0, models.Length)];
                        Vector3 q = p + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), h * 0.32f,
                            UnityEngine.Random.Range(-2.5f, 2.5f));
                        Place(r.transform, cap, "RidgeCap_R" + (ring + 1) + "_" + i, q,
                            yaw + UnityEngine.Random.Range(-40f, 40f), h * UnityEngine.Random.Range(0.35f, 0.65f),
                            0f, mat, false);
                    }
                }
            }
        }

        AlignInvisibleWalls();
    }

    private static void BuildNearApron(Transform parent, GameObject dunes, GameObject cliffA, GameObject cliffB,
        GameObject cliffHalf, GameObject rockA, GameObject rockC, GameObject tallA, GameObject tallB)
    {
        GameObject apron = new GameObject("Horizon_NearApron");
        apron.transform.SetParent(parent, false);
        GameObject[] rocks = { cliffA, cliffB, cliffHalf, rockA, rockC, tallA, tallB };
        const int segments = 34;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (360f / segments) + UnityEngine.Random.Range(-5.5f, 5.5f);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < 19f) continue;

            float radius = UnityEngine.Random.Range(30.5f, 38f);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(rad) * radius, -0.25f, Mathf.Sin(rad) * radius);
            float h = UnityEngine.Random.Range(3.5f, 8.5f);
            bool dune = dunes != null && UnityEngine.Random.value < 0.45f;
            Material mat = dune ? matSand : (UnityEngine.Random.value < 0.5f && matRockLight != null ? matRockLight : matHazeNear);

            GameObject primary = dune ? dunes : rocks[UnityEngine.Random.Range(0, rocks.Length)];
            Place(apron.transform, primary, "Apron_" + i, p, UnityEngine.Random.Range(0f, 360f), h,
                dune ? 10f : 7f, mat, false);
            if (UnityEngine.Random.value < 0.55f)
            {
                GameObject cap = rocks[UnityEngine.Random.Range(0, rocks.Length)];
                Vector3 q = p + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), h * 0.3f,
                    UnityEngine.Random.Range(-2.5f, 2.5f));
                Place(apron.transform, cap, "ApronCap_" + i, q, UnityEngine.Random.Range(0f, 360f),
                    h * UnityEngine.Random.Range(0.35f, 0.7f), 6f, matRockLight != null ? matRockLight : matHazeNear, false);
            }
        }
    }

    private static void AlignInvisibleWalls()
    {
        GameObject env = GameObject.Find("--- ENVIRONMENT ---");
        Transform walls = env != null ? env.transform.Find("InvisibleWalls") : null;
        if (walls == null)
        {
            GameObject found = GameObject.Find("InvisibleWalls");
            if (found != null) walls = found.transform;
        }
        if (walls == null) return;

        SetWall(walls, "Wall_West", new Vector3(-32.5f, 2.5f, 0f), new Vector3(1f, 8f, 68f));
        SetWall(walls, "Wall_East", new Vector3(32.5f, 2.5f, 0f), new Vector3(1f, 8f, 68f));
        SetWall(walls, "Wall_South", new Vector3(0f, 2.5f, -32.5f), new Vector3(68f, 8f, 1f));
        SetWall(walls, "Wall_North_Left", new Vector3(-17f, 2.5f, 32.5f), new Vector3(27f, 8f, 1f));
        SetWall(walls, "Wall_North_Right", new Vector3(17f, 2.5f, 32.5f), new Vector3(27f, 8f, 1f));
        Debug.Log("[VillagePolish] Batas fisik dipindahkan ke balik bukit dekat; celah jalur Exit Zona 2 tetap terbuka.");
    }

    private static void SetWall(Transform parent, string name, Vector3 pos, Vector3 size)
    {
        Transform t = parent.Find(name);
        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        go.transform.position = pos;
        BoxCollider c = go.GetComponent<BoxCollider>();
        if (c == null) c = go.AddComponent<BoxCollider>();
        c.size = size;
        c.isTrigger = false;
        SetLayer(go, ObstacleLayer);
    }

    private static void TuneAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0044f;
        RenderSettings.fogColor = new Color(0.97f, 0.84f, 0.63f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.67f, 0.57f, 0.44f);

        GameObject camera = GameObject.Find("PF_MainCamera");
        if (camera != null)
        {
            Camera cam = camera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.97f, 0.84f, 0.63f);
            }
        }
    }

    private static GameObject Load(string path)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogWarning("[VillagePolish] Model tidak ditemukan: " + path);
        return go;
    }

    private static GameObject InstantiateRaw(GameObject model)
    {
        GameObject inst = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (inst == null) inst = UnityEngine.Object.Instantiate(model);
        return inst;
    }

    private static GameObject Place(Transform parent, GameObject model, string name, Vector3 position,
        float yRotation, float height, float width, Material material, bool collider, float tiltZ = 0f)
    {
        if (model == null) return null;
        GameObject inst = InstantiateRaw(model);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        Bounds b = BoundsOf(inst);
        float scale = 1f;
        if (b.size.y > 0.00001f && height > 0f) scale = height / b.size.y;
        if (width > 0f)
        {
            float horizontal = Mathf.Max(b.size.x, b.size.z);
            if (horizontal > 0.00001f) scale = Mathf.Min(scale, width / horizontal);
        }
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale)) scale = 1f;

        inst.transform.localScale = Vector3.one * scale;
        inst.transform.position = position;
        inst.transform.rotation = Quaternion.Euler(0f, yRotation, tiltZ);
        Apply(inst, material, false);

        if (collider)
        {
            SetLayer(inst, ObstacleLayer);
            EnsureCollider(inst);
        }
        return inst;
    }

    private static Bounds BoundsOf(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        Bounds b = BoundsOf(go);
        if (b.size == Vector3.zero) return;
        BoxCollider c = go.AddComponent<BoxCollider>();
        c.center = go.transform.InverseTransformPoint(b.center);
        c.size = new Vector3(Mathf.Abs(go.transform.InverseTransformVector(b.size).x),
            Mathf.Abs(go.transform.InverseTransformVector(b.size).y),
            Mathf.Abs(go.transform.InverseTransformVector(b.size).z));
    }

    private static void SetLayer(GameObject go, int layer)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    private static void Apply(GameObject go, Material material, bool shadows)
    {
        if (material == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
            if (!shadows)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size,
        Material material, float yRotation, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        go.transform.localScale = size;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        else SetLayer(go, ObstacleLayer);
        Apply(go, material, false);
        return go;
    }

    private static void MarkStatic(GameObject root)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }
    }

    private static void BackupScene()
    {
        try
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string dir = Path.Combine(root, "Backups");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string source = Path.Combine(root, ScenePath.Replace('/', Path.DirectorySeparatorChar));
            string target = Path.Combine(dir, "Level1_Zona1_Pinggiran_" + stamp + ".unity");
            File.Copy(source, target, true);
            if (File.Exists(source + ".meta")) File.Copy(source + ".meta", target + ".meta", true);
            Debug.Log("[VillagePolish] Backup: " + target);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VillagePolish] Gagal backup: " + e.Message);
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
