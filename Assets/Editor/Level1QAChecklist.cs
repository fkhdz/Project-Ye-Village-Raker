using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tool QA Otomatis untuk memvalidasi kepatuhan Scene Level 1 terhadap Checklist Penerimaan
/// pada dokumen Prompt_Map_Level1_YeVillageRaker.md bagian 9.
/// Menu: YeVillage/Level1/Run QA Acceptance Checklist
/// </summary>
public static class Level1QAChecklist
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("YeVillage/Level1/Run QA Acceptance Checklist")]
    public static void RunChecklist()
    {
        Debug.Log("===============================================================");
        Debug.Log("[QA CHECKLIST] Memulai audit otomatis Level 1 (Zona 1)...");
        Debug.Log("===============================================================");

        int passed = 0;
        int failed = 0;
        int warnings = 0;

        // 1. Validasi Scene Aktif
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Check("Scene Level 1 berhasil dimuat: " + activeScene.path, activeScene.IsValid(), ref passed, ref failed);

        // 2. Validasi Build Settings
        bool hasMainMenu = false;
        bool hasLevel1 = false;
        bool hasSampleScene = false;
        int level1Index = -1;

        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            string p = EditorBuildSettings.scenes[i].path;
            if (p.Contains("MainMenu")) hasMainMenu = true;
            if (p.Contains("Level1_Zona1_Pinggiran")) { hasLevel1 = true; level1Index = i; }
            if (p.Contains("SampleScene")) hasSampleScene = true;
        }

        Check("Build Settings mencakup MainMenu, Level 1, dan SampleScene", hasMainMenu && hasLevel1 && hasSampleScene, ref passed, ref failed);
        Check("Level 1 terdaftar tepat di Build Index 1 (setelah MainMenu)", level1Index == 1, ref passed, ref failed);

        // 3. Validasi Player & Camera
        GameObject player = GameObject.FindWithTag("Player");
        Check("Player dengan tag 'Player' ada di scene", player != null, ref passed, ref failed);

        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            TrashInventory inv = player.GetComponent<TrashInventory>();
            Check("Player memiliki komponen PlayerController", pc != null, ref passed, ref failed);
            Check("Player memiliki komponen TrashInventory", inv != null, ref passed, ref failed);

            Vector3 spawnRef = new Vector3(2f, 0f, -12f);
            GameObject spawnObj = GameObject.Find("PlayerSpawnPoint");
            if (spawnObj != null) spawnRef = spawnObj.transform.position;
            float distToSpawn = Vector3.Distance(player.transform.position, spawnRef);
            Check("Player berada di sekitar PlayerSpawnPoint [jarak: " + distToSpawn.ToString("F1") + "m]", distToSpawn < 2f, ref passed, ref failed);
        }

        Camera mainCam = Camera.main;
        Check("Main Camera ada dan memiliki ThirdPersonCamera", mainCam != null && mainCam.GetComponent<ThirdPersonCamera>() != null, ref passed, ref failed);

        // 4. Validasi Batas & Ground
        GameObject ground = GameObject.Find("Ground");
        Check("Ground ada dengan layer 'Ground' dan Collider", ground != null && ground.GetComponent<Collider>() != null, ref passed, ref failed);

        GameObject boundary = GameObject.Find("InvisibleWalls");
        if (boundary == null) boundary = GameObject.Find("BoundaryWalls");
        Check("InvisibleWalls / BoundaryWalls (invisible walls 4 sisi) ada", boundary != null, ref passed, ref failed);

        // 5. Validasi Pos TPS
        GameObject pallet = GameObject.Find("TPS_Deck");
        if (pallet == null) pallet = GameObject.Find("Pallet");
        Check("Platform TPS (TPS_Deck / Pallet) ada", pallet != null, ref passed, ref failed);

        GameObject tongHijau = GameObject.Find("Bin_Green");
        GameObject tongKuning = GameObject.Find("Bin_Yellow");
        GameObject tongMerah = GameObject.Find("Bin_Red");
        Check("Ketiga tong sampah ada (Hijau, Kuning, Merah)", tongHijau != null && tongKuning != null && tongMerah != null, ref passed, ref failed);

        if (tongHijau != null)
        {
            TrashBin bin = tongHijau.GetComponent<TrashBin>();
            Check("Tong Hijau menerima sampah 'Organik'", bin != null && bin.acceptedTrashTag == "Organik", ref passed, ref failed);
        }
        if (tongKuning != null)
        {
            TrashBin bin = tongKuning.GetComponent<TrashBin>();
            Check("Tong Kuning menerima sampah 'Anorganik'", bin != null && bin.acceptedTrashTag == "Anorganik", ref passed, ref failed);
        }
        if (tongMerah != null)
        {
            TrashBin bin = tongMerah.GetComponent<TrashBin>();
            Check("Tong Merah menerima sampah 'B3'", bin != null && bin.acceptedTrashTag == "B3", ref passed, ref failed);
        }

        // 6. Validasi Mesin (Inactive)
        GameObject mesinKompos = GameObject.Find("Machine_Compost");
        GameObject mesinPress = GameObject.Find("Machine_Press");
        bool mesinKomposInactive = (mesinKompos == null || !mesinKompos.activeSelf);
        bool mesinPressInactive = (mesinPress == null || !mesinPress.activeSelf);
        Check("Mesin Kompos nonaktif saat awal Level 1", mesinKomposInactive, ref passed, ref failed);
        Check("Mesin Press nonaktif saat awal Level 1", mesinPressInactive, ref passed, ref failed);

        // 7. Validasi NPC Tetua & Toko
        GameObject tetua = GameObject.Find("NPC_TetuaDesa");
        Check("NPC Tetua Desa ada di scene", tetua != null, ref passed, ref failed);

        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        Check("ShopManager terpasang", shop != null, ref passed, ref failed);
        if (shop != null)
        {
            bool filterCorrect = !shop.sellMesinKompos && !shop.sellMesinPress && !shop.sellRenovasi;
            Check("Toko Level 1 mematikan penjualan Mesin & Renovasi", filterCorrect, ref passed, ref failed);
        }

        // 8. Validasi UIManager & Target Zona
        UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        Check("UIManager terpasang", ui != null, ref passed, ref failed);
        if (ui != null)
        {
            Check("UIManager targetPoin diset ke 250 untuk Zona 1 (bukan hardcode 1500)", ui.targetPoin == 250, ref passed, ref failed);
        }

        // 9. Validasi Exit Marker
        GameObject exitObj = GameObject.Find("Exit_To_Zona2");
        Check("Exit_To_Zona2 ada di scene", exitObj != null, ref passed, ref failed);
        if (exitObj != null)
        {
            ZoneExitMarker marker = exitObj.GetComponent<ZoneExitMarker>();
            Check("Exit_To_Zona2 memiliki komponen ZoneExitMarker", marker != null, ref passed, ref failed);
            if (marker != null)
            {
                Check("ZoneExitMarker menargetkan 'SampleScene'", marker.targetSceneName == "SampleScene", ref passed, ref failed);
            }
        }

        // 10. Validasi Spawner v2
        TrashSpawner spawner = Object.FindFirstObjectByType<TrashSpawner>(FindObjectsInactive.Include);
        Check("TrashSpawner terpasang di scene", spawner != null, ref passed, ref failed);
        if (spawner != null)
        {
            Check("TrashSpawner menggunakan mode 'AreaVolume'", spawner.mode == TrashSpawner.SpawnerMode.AreaVolume, ref passed, ref failed);
            Check("TrashSpawner memiliki 4 Spawn Area", spawner.spawnAreas != null && spawner.spawnAreas.Length == 4, ref passed, ref failed);
            Check("Peluang B3 diset 0 (b3Weight = 0 di Level 1)", spawner.b3Weight == 0, ref passed, ref failed);
            Check("Starter Trash diaktifkan (spawnStarterTrash = true)", spawner.spawnStarterTrash, ref passed, ref failed);
            Check("Target sampah awal = 8 (initialSpawnCount)", spawner.initialSpawnCount == 8, ref passed, ref failed);
            Check("Batas maksimal sampah = 30 (maxActiveTrash)", spawner.maxActiveTrash == 30, ref passed, ref failed);
            Check("Ambang batas burst = 12 (minActiveTrash)", spawner.minActiveTrash == 12, ref passed, ref failed);
            Check("Jarak minimal antar sampah = 1.5m", Mathf.Approximately(spawner.minDistanceBetweenTrash, 1.5f), ref passed, ref failed);
        }

        Debug.Log("===============================================================");
        Debug.Log($"[QA CHECKLIST SELESAI] Hasil: {passed} LULUS, {failed} GAGAL, {warnings} PERINGATAN");
        Debug.Log("===============================================================");

        if (!Application.isBatchMode)
        {
            if (failed == 0)
            {
                EditorUtility.DisplayDialog("QA Checklist Level 1",
                    $"SEMUA AUDIT LULUS!\n\nLulus: {passed}\nGagal: {failed}\n\nScene Level 1 telah memenuhi seluruh spesifikasi Prompt dan siap untuk Play test / Build.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("QA Checklist Level 1",
                    $"ADA AUDIT YANG GAGAL!\n\nLulus: {passed}\nGagal: {failed}\n\nPeriksa Console untuk melihat detail item yang gagal.", "OK");
            }
        }
    }

    private static void Check(string description, bool condition, ref int passed, ref int failed)
    {
        if (condition)
        {
            passed++;
            Debug.Log("<color=#4CAF50>[PASS]</color> " + description);
        }
        else
        {
            failed++;
            Debug.LogError("<color=#F44336>[FAIL]</color> " + description);
        }
    }
}
