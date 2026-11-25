using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1ArtDirectionBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string MatYe = "Assets/Materials/Ye_Village/";
    private const string MatL1 = "Assets/Materials/Level1/";

    private static readonly string[] HouseNames =
    {
        "House_Sel_01", "House_Sel_02", "House_Sel_03", "House_Sel_04", "House_Sel_05", "House_Sel_06",
        "House_Bar_01", "House_Bar_02", "Stable_Bar_01", "House_Bar_03",
        "House_Ut_01", "House_Ut_02", "BellTower_01"
    };

    private static Material[] walls;
    private static Material[] roofs;
    private static Material foundation;
    private static Material trim;

    [MenuItem("YeVillage/Level1/Apply Art Direction (Low-Poly Stylized)")]
    public static void Apply()
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
                Debug.LogError("[ArtDirection] Scene '" + open.name + "' belum disimpan. Simpan dulu.");
                return;
            }
        }

        BackupScene();
        LoadMaterials();
        CleanupStrayRoots();
        int changed = 0;
        for (int i = 0; i < HouseNames.Length; i++)
        {
            GameObject house = GameObject.Find(HouseNames[i]);
            if (house == null)
            {
                Debug.LogWarning("[ArtDirection] " + HouseNames[i] + " tidak ditemukan.");
                continue;
            }
            if (Recolor(house, i)) changed++;
        }

        ValidateScale();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArtDirection] Selesai. " + changed + " bangunan diberi variasi dinding/atap dari material existing. Palette, skala, dan gameplay dipertahankan.");
    }

    private static void LoadMaterials()
    {
        walls = new[]
        {
            Load(MatYe + "M_Wood.mat"),
            Load(MatL1 + "M_Clay.mat"),
            Load(MatL1 + "M_Sandstone.mat"),
            Load(MatL1 + "M_Wood_Dark.mat"),
            Load(MatL1 + "M_Sandstone_Light.mat"),
            Load(MatYe + "M_Sand.mat")
        };
        roofs = new[]
        {
            Load(MatL1 + "M_Clay.mat"),
            Load(MatL1 + "M_Wood_Dark.mat"),
            Load(MatL1 + "M_Sandstone.mat"),
            Load(MatL1 + "M_Clay.mat"),
            Load(MatL1 + "M_Wood_Dark.mat"),
            Load(MatYe + "M_Rock.mat")
        };
        foundation = Load(MatL1 + "M_Sandstone.mat");
        trim = Load(MatL1 + "M_Wood_Dark.mat");
    }

    private static Material Load(string path)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) Debug.LogWarning("[ArtDirection] Material tidak ditemukan: " + path);
        return mat;
    }

    private static bool Recolor(GameObject house, int index)
    {
        MeshFilter mf = house.GetComponentInChildren<MeshFilter>(true);
        Renderer renderer = house.GetComponentInChildren<Renderer>(true);
        if (mf == null || mf.sharedMesh == null || renderer == null) return false;

        Mesh mesh = mf.sharedMesh;
        int slots = Mathf.Max(mesh.subMeshCount, renderer.sharedMaterials.Length);
        if (slots <= 1)
        {
            Material single = walls[index % walls.Length];
            Material[] one = { single };
            renderer.sharedMaterials = one;
            return true;
        }

        float[] avgY = new float[slots];
        for (int s = 0; s < slots; s++)
        {
            if (s >= mesh.subMeshCount) { avgY[s] = 0f; continue; }
            int[] tris = mesh.GetTriangles(s);
            double sum = 0;
            for (int t = 0; t < tris.Length; t++)
            {
                if (tris[t] >= 0 && tris[t] < mesh.vertexCount) sum += mesh.vertices[tris[t]].y;
            }
            avgY[s] = tris.Length > 0 ? (float)(sum / tris.Length) : 0f;
        }

        int roofSlot = 0;
        int baseSlot = 0;
        for (int s = 1; s < slots; s++)
        {
            if (avgY[s] > avgY[roofSlot]) roofSlot = s;
            if (avgY[s] < avgY[baseSlot]) baseSlot = s;
        }

        Material wall = walls[index % walls.Length];
        Material roof = roofs[index % roofs.Length];
        Material[] mats = new Material[slots];
        for (int s = 0; s < slots; s++)
        {
            if (s == roofSlot) mats[s] = roof;
            else if (s == baseSlot && baseSlot != roofSlot) mats[s] = foundation;
            else mats[s] = (s % 3 == 1 && trim != null) ? trim : wall;
        }
        renderer.sharedMaterials = mats;
        return true;
    }

    private static void CleanupStrayRoots()
    {
        string[] stray =
        {
            "Rock_Small_1", "House_Sel_03", "Stable_Bar_01", "Rock_rock_largeE", "Rock_rock_largeA"
        };
        int removed = 0;
        foreach (string name in stray)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject go in roots)
            {
                if (go.name != name) continue;
                Component[] comps = go.GetComponents<Component>();
                bool decorOnly = true;
                foreach (Component c in comps)
                {
                    if (c is Transform || c is MeshFilter || c is MeshRenderer || c is BoxCollider) continue;
                    decorOnly = false;
                    break;
                }
                if (!decorOnly) continue;
                UnityEngine.Object.DestroyImmediate(go);
                removed++;
            }
        }
        if (removed > 0) Debug.Log("[ArtDirection] " + removed + " objek dekorasi duplicate di root scene dibersihkan.");
    }

    private static void ValidateScale()
    {
        GameObject player = GameObject.FindWithTag("Player");
        float playerHeight = 1.8f;
        if (player != null)
        {
            foreach (SkinnedMeshRenderer smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                playerHeight = Mathf.Max(playerHeight, smr.bounds.max.y);
            }
        }

        GameObject house = GameObject.Find("House_Sel_01");
        float houseHeight = 0f;
        if (house != null)
        {
            foreach (Renderer r in house.GetComponentsInChildren<Renderer>(true))
            {
                houseHeight = Mathf.Max(houseHeight, r.bounds.size.y);
            }
        }

        GameObject bin = GameObject.Find("Bin_Green");
        float binHeight = 0f;
        if (bin != null)
        {
            foreach (Renderer r in bin.GetComponentsInChildren<Renderer>(true))
            {
                binHeight = Mathf.Max(binHeight, r.bounds.max.y);
            }
        }

        bool ok = houseHeight > 0f && houseHeight < playerHeight * 4f && binHeight > playerHeight * 0.4f && binHeight < playerHeight;
        Debug.Log("[ArtDirection] Skala: pemain " + playerHeight.ToString("F2") + "m, rumah " + houseHeight.ToString("F2") +
                  "m, tong " + binHeight.ToString("F2") + "m -> " + (ok ? "masuk akal" : "PERLU DITINJAU"));
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
            Debug.Log("[ArtDirection] Backup: " + target);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ArtDirection] Gagal backup: " + e.Message);
        }
    }
}
