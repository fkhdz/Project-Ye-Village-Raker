using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pipeline Refactor & Clean-up Level 1 (Zona 1: Pinggiran Desa & Pos TPS).
/// Menjalankan Tahap 1 (Audit), Tahap 2 (Restrukturisasi Hierarki),
/// Tahap 3 (Presisi Denah & Transform), Tahap 3B (Prefab Variasi CC0 & Material),
/// Tahap 4 (Kerapian Komponen & Layer/Tag), Tahap 5 (Trash Spawner v2),
/// Tahap 6 (Folder & File), serta Tahap 7 (QA Checklist Otomatis).
/// Menu: YeVillage/Level1/🔧 JALANKAN REFACTOR & CLEAN-UP LENGKAP
/// </summary>
public static class Level1SceneRefactorer
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string BackupPath = "Assets/Scenes/Level1_Zona1_Pinggiran_BACKUP.unity";
    private const string EnvPrefabFolder = "Assets/Prefabs/Environment";
    private const string SharedPrefabFolder = "Assets/Prefabs/Shared";
    private const string TrashPrefabsFolder = "Assets/Prefabs/Trash";
    private const string SORoot = "Assets/ScriptableObjects/Trash";
    private const string AuditLogPath = "Assets/Editor/Level1_Audit_Report.txt";

    // Layer names
    private const string LayerGround = "Ground";
    private const string LayerObstacle = "Obstacle";

    // Tags
    private const string TagPlayer = "Player";
    private const string TagElder = "NPC_Elder";
    private const string TagBinGreen = "Bin_Green";
    private const string TagBinYellow = "Bin_Yellow";
    private const string TagBinRed = "Bin_Red";
    private const string TagTrashOrganic = "TrashOrganic";
    private const string TagTrashInorganic = "TrashInorganic";
    private const string TagTrashB3 = "TrashB3";

    [MenuItem("YeVillage/Level1/🔧 JALANKAN REFACTOR & CLEAN-UP LENGKAP", false, 1)]
    public static void ExecuteFullRefactorPipeline()
    {
        Debug.Log("==========================================================================");
        Debug.Log("[Level1Refactor] MEMULAI PIPELINE REFACTOR & CLEAN-UP LEVEL 1...");
        Debug.Log("==========================================================================");

        // ATURAN 1: Pastikan Scene Backup tersedia
        EnsureBackup();

        // Buka Scene Level 1
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // TAHAP 1: Audit Mendalam (Simpan Laporan a-g)
        string auditReport = RunAudit(scene);
        File.WriteAllText(AuditLogPath, auditReport, Encoding.UTF8);
        Debug.Log("[Level1Refactor] Tahap 1 Audit selesai. Laporan disimpan ke: " + AuditLogPath);

        // TAHAP 3B: Bangun Prefab Variasi Environment di Assets/Prefabs/Environment/
        BuildEnvironmentPrefabs();

        // Bangun Environment Lengkap (Ground, Fences 2-digit, Houses, Props, InvisibleWalls)
        Level1EnvironmentBuilder.BuildEnvironment();

        // Bangun Pos TPS, NPC Tetua Desa, Exit Marker, & PlayerSpawnPoint
        Level1TPSBuilder.BuildTPSStation();

        // Bangun Spawner v2 & 4 Area Spawn
        Level1SpawnerSetup.SetupSpawnerAndAreas();

        // TAHAP 2 & 3: Rekonstruksi Hierarki Presisi & Finalisasi Tata Letak
        RefactorHierarchyAndLayout(scene);

        // TAHAP 4: Rapikan Komponen, Tag, Layer, & GPU Instancing
        CleanComponentsAndMaterials(scene);

        // TAHAP 5: Konfigurasi Spawner & Area
        Level1SpawnerSetup.SetupSpawnerAndAreas();

        // Re-wire Semua Inspector References agar tidak ada Missing Reference
        WireAllInspectorReferences(scene);

        // Simpan Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // TAHAP 7: Jalankan QA Checklist
        Debug.Log("[Level1Refactor] Menjalankan QA Checklist verifikasi akhir...");
        Level1QAChecklist.RunChecklist();

        Debug.Log("==========================================================================");
        Debug.Log("[Level1Refactor] REFACTOR & CLEAN-UP LEVEL 1 SELESAI DENGAN SUKSES!");
        Debug.Log("==========================================================================");
    }

    // =========================================================================
    // ATURAN 1: BACKUP SCENE
    // =========================================================================
    private static void EnsureBackup()
    {
        if (!File.Exists(BackupPath))
        {
            AssetDatabase.CopyAsset(ScenePath, BackupPath);
            AssetDatabase.Refresh();
            Debug.Log("[Level1Refactor] Backup scene berhasil dibuat di: " + BackupPath);
        }
        else
        {
            Debug.Log("[Level1Refactor] File backup scene sudah ada dan dipertahankan: " + BackupPath);
        }
    }

    // =========================================================================
    // TAHAP 1: AUDIT MENDALAM (a - g)
    // =========================================================================
    private static string RunAudit(Scene scene)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# LAPORAN AUDIT SCENE LEVEL 1 (Zona 1: Pinggiran Desa & Pos TPS)");
        sb.AppendLine("Tanggal: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Scene: " + scene.path);
        sb.AppendLine("--------------------------------------------------------------------------------");

        GameObject[] rootObjects = scene.GetRootGameObjects();

        // a. Isi Hierarchy (Root objects, default names, overlap, OOB, empty)
        sb.AppendLine("\n[A. HIERARCHY & ROOT OBJECTS]");
        sb.AppendLine("Total root GameObjects: " + rootObjects.Length);
        List<string> unorganizedRoots = new List<string>();
        List<string> defaultNames = new List<string>();
        List<string> outOfBounds = new List<string>();
        List<string> emptyObjects = new List<string>();

        foreach (var root in rootObjects)
        {
            if (root.name != "--- ENVIRONMENT ---" &&
                root.name != "--- GAMEPLAY ---" &&
                root.name != "--- TRASH SYSTEM ---" &&
                root.name != "--- LIGHTING ---" &&
                root.name != "_TO_REVIEW")
            {
                unorganizedRoots.Add(root.name);
            }
        }

        foreach (var root in rootObjects)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                string n = t.gameObject.name;
                if (System.Text.RegularExpressions.Regex.IsMatch(n, @"^(Cube|Sphere|Cylinder|Capsule|GameObject|New Game Object|Plane)(\s*\(\d+\))?$"))
                {
                    defaultNames.Add(n + " at " + t.position);
                }

                Vector3 p = t.position;
                if (Mathf.Abs(p.x) > 31f || Mathf.Abs(p.z) > 31f || p.y < -2f || p.y > 15f)
                {
                    outOfBounds.Add(n + " pos: " + p);
                }

                Component[] comps = t.gameObject.GetComponents<Component>();
                if (comps.Length <= 1 && t.childCount == 0)
                {
                    emptyObjects.Add(n);
                }
            }
        }

        sb.AppendLine("- Objek tanpa grup di Root: " + (unorganizedRoots.Count > 0 ? string.Join(", ", unorganizedRoots) : "Bersih (0)"));
        sb.AppendLine("- Objek bernama default Unity: " + defaultNames.Count + " buah (" + (defaultNames.Count > 0 ? string.Join(", ", defaultNames.GetRange(0, Mathf.Min(5, defaultNames.Count))) : "None") + ")");
        sb.AppendLine("- Objek di luar batas map (X/Z [-30..30]): " + outOfBounds.Count + " buah");
        sb.AppendLine("- Objek kosong tanpa fungsi: " + emptyObjects.Count + " buah");

        // b. Transform bermasalah
        sb.AppendLine("\n[B. TRANSFORM BERMASALAH]");
        int strangeScaleCount = 0;
        int floatingCount = 0;
        foreach (var root in rootObjects)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                Vector3 s = t.localScale;
                if (s.x <= 0f || s.y <= 0f || s.z <= 0f || s.x > 50f || s.y > 50f || s.z > 50f)
                {
                    strangeScaleCount++;
                }
                if (t.name.Contains("House") || t.name.Contains("Fence") || t.name.Contains("Bin") || t.name.Contains("Pallet"))
                {
                    if (t.position.y > 0.5f || t.position.y < -0.1f)
                    {
                        floatingCount++;
                    }
                }
            }
        }
        sb.AppendLine("- Objek dengan scale aneh/non-positif: " + strangeScaleCount);
        sb.AppendLine("- Objek grounded melayang/terbenam: " + floatingCount);

        // c. Komponen bermasalah
        sb.AppendLine("\n[C. KOMPONEN BERMASALAH]");
        int multiColliderCount = 0;
        int missingScriptCount = 0;
        int cameraCount = 0;
        int audioListenerCount = 0;

        foreach (var root in rootObjects)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                Collider[] cols = t.GetComponents<Collider>();
                if (cols.Length > 1) multiColliderCount++;

                Component[] comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null) missingScriptCount++;
                }

                if (t.GetComponent<Camera>() != null) cameraCount++;
                if (t.GetComponent<AudioListener>() != null) audioListenerCount++;
            }
        }
        sb.AppendLine("- Objek dengan collider ganda: " + multiColliderCount);
        sb.AppendLine("- Missing Script detected: " + missingScriptCount);
        sb.AppendLine("- Total Camera di scene: " + cameraCount + " (Wajib 1)");
        sb.AppendLine("- Total AudioListener di scene: " + audioListenerCount + " (Wajib 1)");

        // d. Tag dan Layer
        sb.AppendLine("\n[D. TAG DAN LAYER]");
        GameObject player = GameObject.FindWithTag(TagPlayer);
        sb.AppendLine("- Tag Player: " + (player != null ? "Valid (" + player.name + ")" : "TIDAK DITEMUKAN / Perlu diperbaiki"));

        // e. Script & Spawner
        sb.AppendLine("\n[E. SCRIPT DAN SPAWNER]");
        TrashSpawner spawner = UnityEngine.Object.FindFirstObjectByType<TrashSpawner>();
        sb.AppendLine("- TrashSpawner: " + (spawner != null ? "Tersedia (" + spawner.gameObject.name + ")" : "Belum terpasang"));

        // f. Material & Prefab
        sb.AppendLine("\n[F. MATERIAL DAN PREFAB]");
        int missingMaterialCount = 0;
        foreach (var root in rootObjects)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial == null || r.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                {
                    missingMaterialCount++;
                }
            }
        }
        sb.AppendLine("- Missing / Pink Material: " + missingMaterialCount);

        // g. Denah Target
        sb.AppendLine("\n[G. KESESUAIAN DENAH TARGET]");
        GameObject tps = GameObject.Find("TPS_Station");
        GameObject tetua = GameObject.Find("NPC_TetuaDesa");
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        GameObject exitObj = GameObject.Find("Exit_To_Zona2");

        sb.AppendLine("- PlayerSpawnPoint: " + (spawnPoint != null ? spawnPoint.transform.position.ToString() + " (Target: (10, 0, -12))" : "Missing"));
        sb.AppendLine("- Pos TPS: " + (tps != null ? tps.transform.position.ToString() + " (Target: (0, 0, 4))" : "Missing"));
        sb.AppendLine("- NPC Tetua: " + (tetua != null ? tetua.transform.position.ToString() + " (Target: (-9, 0, 3))" : "Missing"));
        sb.AppendLine("- Exit Marker: " + (exitObj != null ? exitObj.transform.position.ToString() + " (Target: (0, 1.5, 29))" : "Missing"));

        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine("AUDIT SELESAI.");
        return sb.ToString();
    }

    // =========================================================================
    // TAHAP 3B: PREFAB VARIANTS ENVIRONMENT (Assets/Prefabs/Environment/)
    // =========================================================================
    private static void BuildEnvironmentPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(EnvPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");
        }

        string kenneyModels = "Assets/ThirdParty/KenneyNatureKit/Models/";
        string quatModels = "Assets/ThirdParty/Quaternius/Environment/";

        CreatePrefabFromModel(kenneyModels + "fence_simple.fbx", EnvPrefabFolder + "/PF_Fence_Wood.prefab", 1.5f, true);
        CreatePrefabFromModel(kenneyModels + "fence_planks.fbx", EnvPrefabFolder + "/PF_Fence_Tilted.prefab", 1.5f, true);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Buildings/House_1.fbx", EnvPrefabFolder + "/PF_House_A.prefab", 2.5f, true);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Buildings/House_2.fbx", EnvPrefabFolder + "/PF_House_B.prefab", 2.5f, true);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Buildings/House_3.fbx", EnvPrefabFolder + "/PF_House_C.prefab", 2.5f, true);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Buildings/House_4.fbx", EnvPrefabFolder + "/PF_House_D.prefab", 2.25f, true);
        CreatePrefabFromModel(kenneyModels + "tree_thin_fall.fbx", EnvPrefabFolder + "/PF_Tree_Thin.prefab", 1.4f, false);
        CreatePrefabFromModel(quatModels + "Nature/Rock_1.fbx", EnvPrefabFolder + "/PF_Rock_Cluster.prefab", 1.5f, true);
        CreatePrefabFromModel(quatModels + "Nature/Grass.fbx", EnvPrefabFolder + "/PF_Grass_Dry.prefab", 1.2f, false);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Props/Crate.fbx", EnvPrefabFolder + "/PF_Village_Crate.prefab", 1.5f, true);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Props/Barrel.fbx", EnvPrefabFolder + "/PF_Village_Barrel.prefab", 1.5f, true);
        CreatePrefabFromModel(quatModels + "Nature/WoodLog.fbx", EnvPrefabFolder + "/PF_WoodLog.prefab", 1.3f, false);
        CreatePrefabFromModel(quatModels + "Nature/TreeStump.fbx", EnvPrefabFolder + "/PF_TreeStump.prefab", 1.3f, false);
        CreatePrefabFromModel(quatModels + "MedievalVillage/Props/Cart.fbx", EnvPrefabFolder + "/PF_Village_Cart.prefab", 1.4f, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Level1Refactor] Prefab variants environment berhasil disusun di: " + EnvPrefabFolder);
    }

    private static GameObject CreatePrefabFromModel(string modelPath, string prefabPath, float scale, bool addBoxCollider)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) return null;

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(model);
        temp.transform.localScale = Vector3.one * scale;

        if (addBoxCollider)
        {
            BoxCollider col = temp.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = temp.AddComponent<BoxCollider>();
                Renderer r = temp.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    col.center = temp.transform.InverseTransformPoint(r.bounds.center);
                    col.size = temp.transform.InverseTransformVector(r.bounds.size);
                }
            }
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    // =========================================================================
    // TAHAP 2, 3: STRUKTUR HIERARKI & POSISI SESUAI DENAH TARGET
    // =========================================================================
    private static void RefactorHierarchyAndLayout(Scene scene)
    {
        // 1. Pastikan 4 Grup Utama Root + _TO_REVIEW ada dan di (0,0,0)
        GameObject envRoot = EnsureRootGroup("--- ENVIRONMENT ---");
        GameObject gameplayRoot = EnsureRootGroup("--- GAMEPLAY ---");
        GameObject trashRoot = EnsureRootGroup("--- TRASH SYSTEM ---");
        GameObject lightingRoot = EnsureRootGroup("--- LIGHTING ---");
        GameObject reviewRoot = EnsureRootGroup("_TO_REVIEW");
        reviewRoot.SetActive(false); // Sesuai aturan: _TO_REVIEW dinonaktifkan

        // 2. Rapikan LIGHTING
        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight != null && dirLight.transform.parent != lightingRoot.transform)
        {
            dirLight.transform.SetParent(lightingRoot.transform, true);
        }

        // 3. Rapikan ENVIRONMENT
        // Sub-grup wajib: Ground, Fences, Houses, Props, InvisibleWalls
        Transform tGround = EnsureSubGroup(envRoot.transform, "Ground");
        Transform tFences = EnsureSubGroup(envRoot.transform, "Fences");
        Transform tHouses = EnsureSubGroup(envRoot.transform, "Houses");
        Transform tProps = EnsureSubGroup(envRoot.transform, "Props");
        Transform tWalls = EnsureSubGroup(envRoot.transform, "InvisibleWalls");

        // Reparent Ground yang ada di root ke --- ENVIRONMENT ---/Ground
        GameObject existingGround = GameObject.Find("Ground");
        if (existingGround != null && existingGround.transform != tGround)
        {
            if (existingGround.transform.parent == null)
            {
                existingGround.transform.SetParent(tGround, true);
            }
        }

        // Reparent BoundaryWalls yang ada di root ke --- ENVIRONMENT ---/InvisibleWalls
        GameObject boundaryWalls = GameObject.Find("BoundaryWalls");
        if (boundaryWalls != null)
        {
            // Pindahkan semua child boundary walls ke InvisibleWalls
            Transform[] bChildren = boundaryWalls.GetComponentsInChildren<Transform>(true);
            foreach (var bc in bChildren)
            {
                if (bc != boundaryWalls.transform)
                {
                    bc.SetParent(tWalls, true);
                }
            }
            UnityEngine.Object.DestroyImmediate(boundaryWalls);
        }

        // Reorganize and rename existing Environment sub-elements if any
        ReorganizeExistingEnvironment(envRoot, tFences, tHouses, tProps);

        // 4. Rapikan GAMEPLAY
        // Posisi & Denah Target:
        // PlayerSpawnPoint: (10, 0, -12), rotasi menghadap TPS (0, 0, 4)
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("PlayerSpawnPoint");
        }
        spawnPoint.transform.SetParent(gameplayRoot.transform, true);
        spawnPoint.transform.position = new Vector3(10f, 0f, -12f);
        spawnPoint.transform.rotation = Quaternion.Euler(0f, -58f, 0f);

        // Bangun / Rapikan TPS Station
        Level1TPSBuilder.BuildTPSStation();

        // Cari kembali TPS_Station & Tetua yang baru dibuat dan pastikan di bawah --- GAMEPLAY ---
        GameObject tpsStation = GameObject.Find("TPS_Station");
        if (tpsStation != null)
        {
            tpsStation.transform.SetParent(gameplayRoot.transform, true);
            tpsStation.transform.position = new Vector3(0f, 0f, 4f);
        }

        GameObject tetua = GameObject.Find("NPC_TetuaDesa");
        if (tetua != null)
        {
            tetua.transform.SetParent(gameplayRoot.transform, true);
            tetua.transform.position = new Vector3(-9f, 0f, 3f);
            tetua.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            tetua.tag = TagElder;
        }

        GameObject exitObj = GameObject.Find("Exit_To_Zona2");
        if (exitObj != null)
        {
            exitObj.transform.SetParent(gameplayRoot.transform, true);
            exitObj.transform.position = new Vector3(0f, 1.5f, 29f);
        }

        // Susun Runtime Manager Prefabs (Player, Camera, Score, HUD, Audio, VFX) di bawah --- GAMEPLAY ---
        Transform tManagers = EnsureSubGroup(gameplayRoot.transform, "Managers");
        string[] mgrNames = { "PF_Player_Kades", "PF_MainCamera", "PF_ScoreManager", "PF_Canvas_HUD", "PF_AudioManager", "PF_VFXManager" };
        foreach (var mName in mgrNames)
        {
            GameObject mObj = GameObject.Find(mName);
            if (mObj != null)
            {
                mObj.transform.SetParent(tManagers, true);
                if (mName == "PF_Player_Kades")
                {
                    mObj.transform.position = spawnPoint.transform.position;
                    mObj.transform.rotation = spawnPoint.transform.rotation;
                    mObj.tag = TagPlayer;
                }
            }
        }

        // 5. Rapikan TRASH SYSTEM
        Level1SpawnerSetup.SetupSpawnerAndAreas();
        GameObject trashSys = GameObject.Find("--- TRASH SYSTEM ---");
        if (trashSys != null)
        {
            trashSys.transform.position = Vector3.zero;
        }

        // 6. Rapikan Fences dan Props dengan format nomor urut 2 digit (Fence_01, Fence_02..)
        RenumberChildren(tFences, "Fence_");
        RenumberChildren(tProps, "Prop_");

        Debug.Log("[Level1Refactor] Restrukturisasi hierarki & tata letak selesai.");
    }

    private static void ReorganizeExistingEnvironment(GameObject envRoot, Transform tFences, Transform tHouses, Transform tProps)
    {
        // Jika ada grup lama seperti 'Buildings', pindahkan isinya ke 'Houses'
        Transform oldBuildings = envRoot.transform.Find("Buildings");
        if (oldBuildings != null)
        {
            List<Transform> toMove = new List<Transform>();
            for (int i = 0; i < oldBuildings.childCount; i++) toMove.Add(oldBuildings.GetChild(i));
            foreach (var c in toMove) c.SetParent(tHouses, true);
            UnityEngine.Object.DestroyImmediate(oldBuildings.gameObject);
        }

        // Jika ada grup lama 'NaturalBoundaries', 'PerimeterForest', 'VillageProps', 'Paths', pindahkan ke 'Props'
        string[] oldPropGroups = { "NaturalBoundaries", "PerimeterForest", "VillageProps", "Paths" };
        foreach (var gName in oldPropGroups)
        {
            Transform oldG = envRoot.transform.Find(gName);
            if (oldG != null)
            {
                List<Transform> toMove = new List<Transform>();
                for (int i = 0; i < oldG.childCount; i++) toMove.Add(oldG.GetChild(i));
                foreach (var c in toMove) c.SetParent(tProps, true);
                UnityEngine.Object.DestroyImmediate(oldG.gameObject);
            }
        }
    }

    private static void RenumberChildren(Transform parent, string prefix)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            // Hanya ganti nama jika belum memiliki nama kustom khusus (seperti House_A, dsb.)
            if (c.name.StartsWith("Fence_") || c.name.StartsWith("Prop_") || c.name.Contains("(Clone)"))
            {
                c.name = string.Format("{0}{1:D2}", prefix, i + 1);
            }
        }
    }

    private static GameObject EnsureRootGroup(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
        }
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    private static Transform EnsureSubGroup(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    // =========================================================================
    // TAHAP 4: BERESKAN KOMPONEN, LAYER, TAG, & GPU INSTANCING
    // =========================================================================
    private static void CleanComponentsAndMaterials(Scene scene)
    {
        int obstacleLayer = LayerMask.NameToLayer(LayerObstacle);
        int groundLayer = LayerMask.NameToLayer(LayerGround);

        // Ground: Layer Ground, 1 BoxCollider
        GameObject groundObj = GameObject.Find("Ground");
        if (groundObj != null)
        {
            if (groundLayer >= 0) groundObj.layer = groundLayer;
            BoxCollider col = groundObj.GetComponent<BoxCollider>();
            if (col == null) col = groundObj.AddComponent<BoxCollider>();
            col.isTrigger = false;
        }

        // Pagar, Rumah, Props Besar: Layer Obstacle, Static
        GameObject envRoot = GameObject.Find("--- ENVIRONMENT ---");
        if (envRoot != null)
        {
            foreach (Transform t in envRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Ground") continue;

                // Terapkan layer Obstacle pada rumah, pagar, dan peti/batu
                if (obstacleLayer >= 0)
                {
                    if (t.name.Contains("Fence") || t.name.Contains("House") || t.name.Contains("Wall") || t.name.Contains("Crate"))
                    {
                        t.gameObject.layer = obstacleLayer;
                    }
                }

                // Hapus collider dari props kecil dekoratif (rumput, bunga, ranting) agar ringan
                if (t.name.Contains("Grass") || t.name.Contains("Flower") || t.name.Contains("Twig") || t.name.Contains("Path"))
                {
                    Collider c = t.GetComponent<Collider>();
                    if (c != null) UnityEngine.Object.DestroyImmediate(c);
                }

                // Tandai Static (Batching Static, Occludee Static)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }
        }

        // Tong Sampah: Trigger Collider 2m, Tag Bin_Green/Yellow/Red & TongHijau/Kuning/Merah
        SetupBinComponent("Bin_Green", TagBinGreen, "Organik");
        SetupBinComponent("Bin_Yellow", TagBinYellow, "Anorganik");
        SetupBinComponent("Bin_Red", TagBinRed, "B3");

        // Aktifkan GPU Instancing pada seluruh Material Ye_Village & Quaternius
        EnableGPUInstancingOnMaterials("Assets/Materials/Ye_Village");
        EnableGPUInstancingOnMaterials("Assets/Materials/Quaternius");

        Debug.Log("[Level1Refactor] Komponen, tag, layer, dan GPU Instancing berhasil dirapikan.");
    }

    private static void SetupBinComponent(string binName, string tagName, string acceptedTag)
    {
        GameObject bin = GameObject.Find(binName);
        if (bin == null) return;

        bin.tag = tagName;

        TrashBin tb = bin.GetComponent<TrashBin>();
        if (tb == null) tb = bin.AddComponent<TrashBin>();
        tb.acceptedTrashTag = acceptedTag;

        // Pastikan trigger radius 2.0m
        SphereCollider sc = bin.GetComponent<SphereCollider>();
        if (sc == null) sc = bin.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 2.0f;
        sc.center = Vector3.up * 0.6f;
    }

    private static void EnableGPUInstancingOnMaterials(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }
        }
    }

    // =========================================================================
    // INSPECTOR WIRING & MISSING REFERENCE FIX
    // =========================================================================
    private static void WireAllInspectorReferences(Scene scene)
    {
        GameObject player = GameObject.FindWithTag(TagPlayer);
        if (player == null) player = GameObject.Find("PF_Player_Kades");

        GameObject camObj = GameObject.Find("PF_MainCamera");
        Camera cam = camObj != null ? camObj.GetComponent<Camera>() : Camera.main;

        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && cam != null)
            {
                SerializedObject soPC = new SerializedObject(pc);
                SerializedProperty pCam = soPC.FindProperty("cam");
                if (pCam != null) pCam.objectReferenceValue = cam.transform;
                soPC.ApplyModifiedPropertiesWithoutUndo();
            }

            if (camObj != null)
            {
                ThirdPersonCamera tpc = camObj.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    SerializedObject soCam = new SerializedObject(tpc);
                    SerializedProperty pTarget = soCam.FindProperty("target");
                    if (pTarget != null) pTarget.objectReferenceValue = player.transform;
                    soCam.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Wire UIManager
            UIManager ui = UnityEngine.Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui != null)
            {
                SerializedObject soUI = new SerializedObject(ui);
                SerializedProperty pInv = soUI.FindProperty("playerInventory");
                if (pInv != null) pInv.objectReferenceValue = player.GetComponent<TrashInventory>();
                SerializedProperty pTarget = soUI.FindProperty("targetPoin");
                if (pTarget != null) pTarget.intValue = 250;
                SerializedProperty pHint = soUI.FindProperty("targetReachedHint");
                if (pHint != null) pHint.stringValue = "Target Zona 1 Tercapai (250 Poin)! Silakan ke Exit Utara.";
                soUI.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire ShopManager
            ShopManager shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
            if (shop != null)
            {
                SerializedObject soShop = new SerializedObject(shop);
                SerializedProperty pCtrl = soShop.FindProperty("playerController");
                if (pCtrl != null) pCtrl.objectReferenceValue = pc;
                SerializedProperty pInv = soShop.FindProperty("playerInventory");
                if (pInv != null) pInv.objectReferenceValue = player.GetComponent<TrashInventory>();

                GameObject mesinK = GameObject.Find("Machine_Compost");
                GameObject mesinP = GameObject.Find("Machine_Press");
                SerializedProperty pMK = soShop.FindProperty("mesinKomposVisual");
                if (pMK != null && mesinK != null) pMK.objectReferenceValue = mesinK;
                SerializedProperty pMP = soShop.FindProperty("mesinPressVisual");
                if (pMP != null && mesinP != null) pMP.objectReferenceValue = mesinP;

                // Nonaktifkan penjualan mesin & renovasi di Level 1
                SerializedProperty pSellK = soShop.FindProperty("sellMesinKompos");
                if (pSellK != null) pSellK.boolValue = false;
                SerializedProperty pSellP = soShop.FindProperty("sellMesinPress");
                if (pSellP != null) pSellP.boolValue = false;
                SerializedProperty pSellR = soShop.FindProperty("sellRenovasi");
                if (pSellR != null) pSellR.boolValue = false;

                soShop.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Debug.Log("[Level1Refactor] Seluruh referensi Inspector berhasil dihubungkan ulang tanpa missing reference.");
    }
}
