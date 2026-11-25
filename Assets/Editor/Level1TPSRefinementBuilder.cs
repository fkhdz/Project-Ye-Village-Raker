using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1TPSRefinementBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string Kenney = "Assets/ThirdParty/KenneyNatureKit/Models/";
    private const string Village = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/";
    private const string MatYe = "Assets/Materials/Ye_Village/";
    private const string MatL1 = "Assets/Materials/Level1/";
    private const string RootName = "TPS_Refinement";

    private static Material matWood;
    private static Material matWoodDark;
    private static Material matGround;
    private static Material matDry;
    private static Material matTrack;

    [MenuItem("YeVillage/Level1/Refine TPS Area")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[TPS] Scene '" + open.name + "' belum disimpan. Simpan dulu.");
                return;
            }
        }

        BackupScene();
        matWood = AssetDatabase.LoadAssetAtPath<Material>(MatYe + "M_Wood.mat");
        matWoodDark = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Wood_Dark.mat");
        matGround = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_CrackedEarth.mat");
        matDry = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_DryPlant.mat");
        matTrack = AssetDatabase.LoadAssetAtPath<Material>(MatL1 + "M_Road_Track.mat");

        GameObject env = GameObject.Find("--- ENVIRONMENT ---");
        if (env == null) { Debug.LogError("[TPS] ENVIRONMENT tidak ditemukan."); return; }

        Transform old = env.transform.Find(RootName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        RemoveConflictingPolish();
        GroundStationProps();

        GameObject root = new GameObject(RootName);
        root.transform.SetParent(env.transform, false);
        UnityEngine.Random.InitState(40417);

        BuildDryGround(root.transform);
        BuildShade(root.transform);
        BuildFencesAndBoard(root.transform);
        BuildSacksAndClutter(root.transform);
        ValidateClearance();

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[TPS] Selesai. Area TPS dirapikan: tanah tandus, atap peneduh kecil, pagar rustic, papan info, karung & sampah dekoratif. Tong, NPC, mesin, prompt, dan sampah gameplay tidak diubah.");
    }

    private static void RemoveConflictingPolish()
    {
        string[] names =
        {
            "TPS_Post_0", "TPS_Post_1", "TPS_Post_2",
            "TPS_PostCap_0", "TPS_PostCap_1", "TPS_PostCap_2",
            "TPS_CanopyPost", "TPS_CanopyRoof",
            "Approach_Fence_3", "Heap_Sack_3", "Heap_Sack_4"
        };
        int removed = 0;
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            Transform[] all = go.GetComponentsInChildren<Transform>(true);
            for (int i = all.Length - 1; i >= 0; i--)
            {
                GameObject child = all[i].gameObject;
                bool match = false;
                foreach (string n in names)
                {
                    if (child.name == n) { match = true; break; }
                }
                if (!match) continue;
                string prefabPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child);
                if (!string.IsNullOrEmpty(prefabPath) && prefabPath.StartsWith("Assets/ThirdParty/")) continue;
                UnityEngine.Object.DestroyImmediate(child);
                removed++;
            }
        }
        if (removed > 0) Debug.Log("[TPS] " + removed + " dekorasi lama yang menghalangi tong/mesin/NPC dibersihkan.");
    }

    private static void GroundStationProps()
    {
        string[] names = { "TPS_Cart", "TPS_Stall", "TPS_Prop_Crate_01", "TPS_Prop_Crate_02",
            "TPS_Prop_Barrel_01", "TPS_Prop_Barrel_02", "TPS_Prop_Bucket" };
        int fixedCount = 0;
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go == null) continue;
            Bounds b = WorldBounds(go);
            if (b.size.y <= 0.001f || b.min.y > -0.05f) continue;
            float y = go.transform.eulerAngles.y;
            go.transform.rotation = Quaternion.Euler(-90f, y, 0f);
            b = WorldBounds(go);
            Vector3 p = go.transform.position;
            p.y -= b.min.y;
            go.transform.position = p;
            fixedCount++;
        }
        if (fixedCount > 0) Debug.Log("[TPS] " + fixedCount + " properti TPS dinapakkan ke tanah.");
    }

    private static void BuildDryGround(Transform parent)
    {
        GameObject patch = Cube(parent, "TPS_DryGround", new Vector3(-18f, 0.008f, 17.4f),
            new Vector3(14f, 0.016f, 9.4f), matGround);
        patch.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private static void BuildShade(Transform parent)
    {
        Cube(parent, "TPS_ShadeRoof", new Vector3(-18f, 2.55f, 15.8f), new Vector3(8.6f, 0.1f, 3.1f), matWoodDark);
        float[] xs = { -21.9f, -14.1f };
        float[] zs = { 14.3f, 17.1f };
        foreach (float x in xs)
        {
            foreach (float z in zs)
            {
                Cube(parent, "TPS_ShadePost", new Vector3(x, 1.25f, z), new Vector3(0.14f, 2.5f, 0.14f), matWoodDark);
            }
        }
        Cube(parent, "TPS_ShadeBrace_L", new Vector3(-21.35f, 2.25f, 15.8f), new Vector3(0.1f, 0.8f, 0.1f), matWood);
        Cube(parent, "TPS_ShadeBrace_R", new Vector3(-14.65f, 2.25f, 15.8f), new Vector3(0.1f, 0.8f, 0.1f), matWood);
    }

    private static void BuildFencesAndBoard(Transform parent)
    {
        GameObject fence = Load(Kenney + "fence_planks.fbx");
        Place(parent, fence, "TPS_Fence_Broken_01", new Vector3(-23.4f, 0f, 16.2f), 8f, 1.25f, 2.6f, matWoodDark, 7f);
        Place(parent, fence, "TPS_Fence_Broken_02", new Vector3(-23.8f, 0f, 19.6f), -14f, 1.1f, 2.4f, matWoodDark, -6f);
        Place(parent, fence, "TPS_Fence_Broken_03", new Vector3(-11.9f, 0f, 17.2f), 95f, 1.2f, 2.5f, matWoodDark, 5f);

        GameObject sign = Load(Kenney + "sign.fbx");
        GameObject board = Place(parent, sign, "TPS_InfoBoard", new Vector3(-22.7f, 0f, 21.2f), 205f, 1.9f, 0f, matWoodDark, 0f);
        if (board != null)
        {
            WorldLabelBillboard.CreateLabel(board.transform, "TPS SUKAMAJU", new Color(0.36f, 0.28f, 0.16f), new Vector3(0f, 2.35f, 0f));
        }
    }

    private static void BuildSacksAndClutter(Transform parent)
    {
        Cube(parent, "TPS_Sack_01", new Vector3(-23.5f, 0.12f, 17.9f), new Vector3(0.65f, 0.24f, 0.45f), matWood, 14f);
        Cube(parent, "TPS_Sack_02", new Vector3(-23.9f, 0.1f, 18.6f), new Vector3(0.58f, 0.2f, 0.42f), matTrack, -12f);
        Cube(parent, "TPS_Sack_03", new Vector3(-23.3f, 0.09f, 19.2f), new Vector3(0.6f, 0.18f, 0.4f), matWood, 24f);

        GameObject crate = Load(Village + "Props/Crate.fbx");
        GameObject barrel = Load(Village + "Props/Barrel.fbx");
        GameObject logStack = Load(Kenney + "log_stack.fbx");
        Place(parent, crate, "TPS_Deco_Crate", new Vector3(-22.9f, 0f, 20.4f), 28f, 0.7f, 0f, matWoodDark, 0f);
        Place(parent, barrel, "TPS_Deco_Barrel", new Vector3(-11.6f, 0f, 21.4f), -18f, 0.85f, 0f, matWoodDark, 0f);
        Place(parent, logStack, "TPS_Deco_Log", new Vector3(-23.2f, 0f, 21.6f), 35f, 0.7f, 1.4f, matWoodDark, 0f);

        GameObject rock = Load(Kenney + "rock_smallA.fbx");
        Place(parent, rock, "TPS_Clutter_01", new Vector3(-23.9f, 0f, 21.8f), 44f, 0.3f, 0f, matDry, 0f);
        Place(parent, rock, "TPS_Clutter_02", new Vector3(-10.9f, 0f, 20.6f), 18f, 0.26f, 0f, matDry, 0f);
    }

    private static void ValidateClearance()
    {
        Vector3[] bins = { new Vector3(-20.4f, 0f, 15.4f), new Vector3(-18f, 0f, 15.4f), new Vector3(-15.6f, 0f, 15.4f) };
        Vector3[] special = { new Vector3(-13.6f, 0f, 19.2f), new Vector3(-20.6f, 0f, 18.4f), new Vector3(-15.4f, 0f, 18.4f) };
        string[] labels = { "Tetua", "Mesin Kompos", "Mesin Press" };
        GameObject root = GameObject.Find(RootName);
        if (root == null) return;

        Transform[] decor = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < bins.Length; i++)
        {
            Vector3 front = bins[i] + new Vector3(0f, 0f, -2f);
            float min = 999f;
            foreach (Transform t in decor)
            {
                if (t == root.transform) continue;
                min = Mathf.Min(min, Vector3.Distance(front, t.position));
            }
            Debug.Log("[TPS] Akses depan tong " + i + ": ruang kosong " + min.ToString("F2") + "m.");
        }
        for (int i = 0; i < special.Length; i++)
        {
            float min = 999f;
            foreach (Transform t in decor)
            {
                if (t == root.transform) continue;
                min = Mathf.Min(min, Vector3.Distance(special[i], t.position));
            }
            Debug.Log("[TPS] " + labels[i] + ": dekorasi terdekat " + min.ToString("F2") + "m.");
        }
    }

    private static GameObject Load(string path)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogWarning("[TPS] Model tidak ditemukan: " + path);
        return go;
    }

    private static GameObject Place(Transform parent, GameObject model, string name, Vector3 position,
        float yaw, float height, float width, Material material, float tiltZ)
    {
        if (model == null) return null;
        GameObject inst = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (inst == null) inst = UnityEngine.Object.Instantiate(model);
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
        inst.transform.rotation = Quaternion.Euler(0f, yaw, tiltZ);
        Apply(inst, material);
        return inst;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, float yaw = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        Apply(go, material);
        return go;
    }

    private static void Apply(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
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
            Debug.Log("[TPS] Backup: " + target);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TPS] Gagal backup: " + e.Message);
        }
    }
}
