using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1LayoutCompositionBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string Kenney = "Assets/ThirdParty/KenneyNatureKit/Models/";
    private const string MatYe = "Assets/Materials/Ye_Village/";
    private const string MatL1 = "Assets/Materials/Level1/";
    private const string PolishRoot = "LayoutComposition_Polish";
    private const int ObstacleLayer = 9;

    private static Material matWood;
    private static Material matWoodDark;
    private static Material matDry;
    private static Material matStone;
    private static Material matSand;

    [MenuItem("YeVillage/Level1/Compose Layout & Spawn Landmarks")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[Layout] Scene '" + open.name + "' belum disimpan. Simpan dulu.");
                return;
            }
        }

        BackupScene();
        matWood = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Wood.mat");
        matWoodDark = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Wood_Dark.mat");
        matDry = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_DryPlant.mat");
        matStone = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Rock.mat");
        matSand = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Sand.mat");

        GameObject env = GameObject.Find("--- ENVIRONMENT ---");
        if (env == null) { Debug.LogError("[Layout] ENVIRONMENT tidak ditemukan."); return; }

        Transform old = env.transform.Find(PolishRoot);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        GameObject polish = new GameObject(PolishRoot);
        polish.transform.SetParent(env.transform, false);
        UnityEngine.Random.InitState(77125);

        StaggerHouses();
        int grounded = GroundQuaterniusDecor();
        BuildSpawnLandmarks(polish.transform);
        BuildRoadEdges(polish.transform);
        EnrichSelectedYards();
        ValidateSpawnClearance();

        foreach (Transform t in polish.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Layout] Selesai. " + grounded + " aset Quaternius dinapakkan ke tanah; landmark spawn, tepian jalan, dan detail pekarangan ditambahkan. Spawn, Exit, TPS, dan spawner tidak diubah.");
    }

    private static void StaggerHouses()
    {
        string[] houses =
        {
            "House_Sel_01", "House_Sel_02", "House_Sel_03", "House_Sel_04", "House_Sel_05", "House_Sel_06",
            "House_Bar_01", "House_Bar_02", "Stable_Bar_01", "House_Bar_03", "House_Ut_01", "House_Ut_02"
        };
        Vector3[] positions =
        {
            new Vector3(-22.5f, 0f, -27.2f), new Vector3(-16.2f, 0f, -25.9f), new Vector3(-11.4f, 0f, -27.4f),
            new Vector3(10.8f, 0f, -26.2f), new Vector3(16.8f, 0f, -27.1f), new Vector3(22.3f, 0f, -26.1f),
            new Vector3(-27.1f, 0f, -19.6f), new Vector3(-26.4f, 0f, -13.9f), new Vector3(-27.0f, 0f, -6.6f),
            new Vector3(-26.1f, 0f, -0.6f), new Vector3(-14.6f, 0f, 26.9f), new Vector3(11.6f, 0f, 25.8f)
        };
        float[] yaws = { 8f, -6f, 0f, 12f, -9f, 5f, 96f, 84f, 92f, 88f, 186f, 174f };

        for (int i = 0; i < houses.Length; i++)
        {
            GameObject go = GameObject.Find(houses[i]);
            if (go == null) continue;
            go.transform.position = positions[i];
            go.transform.rotation = Quaternion.Euler(go.transform.eulerAngles.x, yaws[i], 0f);
        }

        string[] yards =
        {
            "Yard_S_0", "Yard_S_1", "Yard_S_2", "Yard_S_3", "Yard_S_4", "Yard_S_5",
            "Yard_W_6", "Yard_W_7", "Yard_W_8", "Yard_W_9"
        };
        Vector3[] yardPositions =
        {
            new Vector3(-22.5f, 0f, -22.6f), new Vector3(-16.2f, 0f, -21.3f), new Vector3(-11.4f, 0f, -22.8f),
            new Vector3(10.8f, 0f, -21.6f), new Vector3(16.8f, 0f, -22.5f), new Vector3(22.3f, 0f, -21.5f),
            new Vector3(-22.5f, 0f, -19.6f), new Vector3(-21.8f, 0f, -13.9f), new Vector3(-22.4f, 0f, -6.6f),
            new Vector3(-21.5f, 0f, -0.6f)
        };
        for (int i = 0; i < yards.Length; i++)
        {
            GameObject go = GameObject.Find(yards[i]);
            if (go != null) go.transform.position = yardPositions[i];
        }
    }

    private static int GroundQuaterniusDecor()
    {
        int count = 0;
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "--- GAMEPLAY ---" || root.name == "--- TRASH SYSTEM ---" || root.name == "--- LIGHTING ---") continue;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject go = t.gameObject;
                string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                bool quaternius = !string.IsNullOrEmpty(prefab) && prefab.StartsWith("Assets/ThirdParty/Quaternius/");
                bool namedDecor = go.name.StartsWith("TPS_") || go.name.StartsWith("Junk_") || go.name.StartsWith("Yard_");
                if (!quaternius && !namedDecor) continue;
                Bounds b = WorldBounds(go);
                if (b.size.y <= 0.001f || b.min.y > -0.05f) continue;

                float yRot = go.transform.eulerAngles.y;
                go.transform.rotation = Quaternion.Euler(-90f, yRot, 0f);
                b = WorldBounds(go);
                Vector3 p = go.transform.position;
                p.y -= b.min.y;
                go.transform.position = p;

                BoxCollider existing = go.GetComponent<BoxCollider>();
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                    b = WorldBounds(go);
                    BoxCollider c = go.AddComponent<BoxCollider>();
                    c.center = go.transform.InverseTransformPoint(b.center);
                    c.size = new Vector3(
                        Mathf.Abs(go.transform.InverseTransformVector(b.size).x),
                        Mathf.Abs(go.transform.InverseTransformVector(b.size).y),
                        Mathf.Abs(go.transform.InverseTransformVector(b.size).z));
                }
                count++;
            }
        }
        return count;
    }

    private static void BuildSpawnLandmarks(Transform parent)
    {
        GameObject gate = new GameObject("Spawn_Landmark_Gate");
        gate.transform.SetParent(parent, false);
        gate.transform.position = new Vector3(2f, 0f, -7.6f);

        Cube(gate.transform, "Gate_Post_L", new Vector3(0f, 0f, 0f) + new Vector3(-2.85f, 1.4f, 0f),
            new Vector3(0.22f, 2.8f, 0.22f), matWoodDark, 0f);
        Cube(gate.transform, "Gate_Post_R", new Vector3(0f, 0f, 0f) + new Vector3(2.85f, 1.4f, 0f),
            new Vector3(0.22f, 2.8f, 0.22f), matWoodDark, 0f);
        Cube(gate.transform, "Gate_Beam", new Vector3(0f, 2.72f, 0f), new Vector3(6.1f, 0.22f, 0.24f), matWoodDark, 0f);
        Cube(gate.transform, "Gate_Brace_L", new Vector3(-2.42f, 2.35f, 0f), new Vector3(0.12f, 0.9f, 0.12f), matWood, -38f);
        Cube(gate.transform, "Gate_Brace_R", new Vector3(2.42f, 2.35f, 0f), new Vector3(0.12f, 0.9f, 0.12f), matWood, 38f);
        Cube(gate.transform, "Gate_Sign", new Vector3(0f, 2.36f, 0f), new Vector3(1.8f, 0.55f, 0.08f), matWood, 0f);

        GameObject sign = Load(Kenney + "sign.fbx");
        Place(parent, sign, "Spawn_Landmark_Sign", new Vector3(5.4f, 0f, -9.2f), 205f, 1.9f, 0f, matWoodDark);

        GameObject tree = Load(Kenney + "tree_thin_fall.fbx");
        Place(parent, tree, "Spawn_Landmark_DryTree", new Vector3(-4.8f, 0f, -5.5f), 24f, 5.4f, 0f, matDry);

        GameObject rock = Load(Kenney + "rock_smallC.fbx");
        Place(parent, rock, "Spawn_Landmark_Rock", new Vector3(-5.6f, 0f, -8.4f), 62f, 0.55f, 0f, matStone);
        Place(parent, rock, "Spawn_Landmark_Rock_2", new Vector3(6.1f, 0f, -5.2f), 17f, 0.42f, 0f, matStone);
    }

    private static void BuildRoadEdges(Transform parent)
    {
        GameObject group = new GameObject("RoadEdges");
        group.transform.SetParent(parent, false);

        GameObject grass = Load(Kenney + "grass_large.fbx");
        GameObject rock = Load(Kenney + "rock_smallA.fbx");
        GameObject path = Load(Kenney + "ground_pathRocks.fbx");

        Vector3[] edge =
        {
            new Vector3(-0.9f, 0f, -25f), new Vector3(4.9f, 0f, -22f),
            new Vector3(-0.9f, 0f, -18f), new Vector3(5.0f, 0f, -12f),
            new Vector3(-1.1f, 0f, -4f), new Vector3(4.7f, 0f, 0f),
            new Vector3(-2.6f, 0f, 5f), new Vector3(0.2f, 0f, 8f),
            new Vector3(-6.4f, 0f, 9f), new Vector3(-12.5f, 0f, 6f),
            new Vector3(-17.2f, 0f, 12f), new Vector3(-20.5f, 0f, 18f),
            new Vector3(-2.8f, 0f, 16f), new Vector3(2.9f, 0f, 22f)
        };
        for (int i = 0; i < edge.Length; i++)
        {
            GameObject model = i % 3 == 0 ? rock : grass;
            Place(group.transform, model, "RoadEdge_" + i, edge[i], UnityEngine.Random.Range(0f, 360f),
                UnityEngine.Random.Range(0.35f, 0.75f), 0f, i % 3 == 0 ? matStone : matDry);
        }

        Vector3[] stones =
        {
            new Vector3(0.4f, 0f, -20f), new Vector3(3.5f, 0f, -6f),
            new Vector3(-1.8f, 0f, 2f), new Vector3(-8.5f, 0f, 7f),
            new Vector3(-15f, 0f, 12.5f), new Vector3(0.6f, 0f, 20f)
        };
        for (int i = 0; i < stones.Length; i++)
        {
            Place(group.transform, path, "RoadPathDetail_" + i, stones[i], UnityEngine.Random.Range(-12f, 12f),
                0.02f, UnityEngine.Random.Range(1.6f, 2.4f), matSand);
        }
    }

    private static void EnrichSelectedYards()
    {
        GameObject logs = Load(Kenney + "log_stack.fbx");
        GameObject pot = Load(Kenney + "pot_small.fbx");
        GameObject fence = Load(Kenney + "fence_simpleDiagonal.fbx");
        GameObject sack = Load(Kenney + "ground_pathRocks.fbx");

        AddToYard("Yard_S_01", logs, "Yard_Firewood", new Vector3(2.1f, 0f, 1.9f), 22f, 0.8f, matWoodDark);
        AddToYard("Yard_S_02", pot, "Yard_WaterPot", new Vector3(-2.2f, 0f, 1.8f), 0f, 0.7f, matStone);
        AddToYard("Yard_S_04", logs, "Yard_Firewood", new Vector3(-2.2f, 0f, 1.6f), -18f, 0.75f, matWoodDark);
        AddToYard("Yard_S_05", sack, "Yard_DustCorner", new Vector3(2.7f, 0f, 2f), 15f, 0.18f, matSand);
        AddToYard("Yard_W_02", fence, "Yard_BrokenFence", new Vector3(3.2f, 0f, -2.1f), 35f, 1.0f, matWoodDark);
        AddToYard("Yard_W_04", pot, "Yard_WaterPot", new Vector3(2.3f, 0f, 1.7f), 0f, 0.65f, matStone);
    }

    private static void AddToYard(string yardName, GameObject model, string name, Vector3 localPos, float yaw, float height, Material mat)
    {
        GameObject yard = GameObject.Find(yardName);
        if (yard == null || model == null) return;
        if (yard.transform.Find(name) != null) return;
        Place(yard.transform, model, name, localPos, yaw, height, 0f, mat);
    }

    private static void ValidateSpawnClearance()
    {
        GameObject spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn == null) return;
        Vector3 p = spawn.transform.position;
        float min = 999f;
        string closest = "";
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "--- GAMEPLAY ---" || root.name == "--- TRASH SYSTEM ---" || root.name == "--- LIGHTING ---") continue;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.StartsWith("Road") || n.StartsWith("CartTrack") || n.StartsWith("Ground") || n.StartsWith("WorldLabel_")) continue;
                if (t.GetComponent<Renderer>() == null && t.GetComponentInChildren<Renderer>() == null) continue;
                float d = Vector3.Distance(t.position, p);
                if (d < min) { min = d; closest = n; }
            }
        }
        Debug.Log("[Layout] Spawn clearance: objek terdekat '" + closest + "' pada " + min.ToString("F2") + "m.");
    }

    private static GameObject Load(string path)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogWarning("[Layout] Model tidak ditemukan: " + path);
        return go;
    }

    private static GameObject InstantiateRaw(GameObject model)
    {
        GameObject inst = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (inst == null) inst = UnityEngine.Object.Instantiate(model);
        return inst;
    }

    private static GameObject Place(Transform parent, GameObject model, string name, Vector3 position,
        float yRotation, float height, float width, Material material)
    {
        if (model == null) return null;
        GameObject inst = InstantiateRaw(model);
        inst.name = name;
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        Bounds b = WorldBounds(inst);
        float scale = 1f;
        if (b.size.y > 0.00001f && height > 0f) scale = height / b.size.y;
        if (width > 0f)
        {
            float h = Mathf.Max(b.size.x, b.size.z);
            if (h > 0.00001f) scale = Mathf.Min(scale, width / h);
        }
        inst.transform.localScale = Vector3.one * scale;
        inst.transform.position = position;
        inst.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in rs)
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        return inst;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, float yRotation)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
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
            Debug.Log("[Layout] Backup: " + target);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Layout] Gagal backup: " + e.Message);
        }
    }
}
