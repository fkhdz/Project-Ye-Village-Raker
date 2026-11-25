using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Master Builder: Menjalankan seluruh pipeline setup Level 1 dalam satu klik!
/// Urutan eksekusi:
/// 1. Generate Trash Data & Prefab Variasi (Tahap 6)
/// 2. Buka Scene Level 1
/// 3. Bangun Environment Low-Poly: Pagar, Rumah, Props, Rumput (Tahap 3)
/// 4. Bangun Pos TPS: Pallet, 3 Tong, 2 Mesin, Tetua Desa, Exit Marker (Tahap 4 & 8)
/// 5. Bangun Spawner v2 & 4 Area Spawn (Tahap 7)
/// 6. Jalankan QA Checklist Otomatis (Tahap 10)
/// Menu: YeVillage/Level1/🚀 ONE-CLICK BUILD LEVEL 1 (Semua Tahap)
/// </summary>
public static class Level1MasterBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";

    [MenuItem("YeVillage/Level1/🚀 ONE-CLICK BUILD LEVEL 1 (Semua Tahap)", false, 0)]
    public static void BuildAllLevel1()
    {
        Debug.Log("=================================================================");
        Debug.Log("[MasterBuilder] Memulai One-Click Setup Level 1 (Tahap 3 - 10)...");
        Debug.Log("=================================================================");

        // 1. Generate Trash Data & Prefabs (Tahap 6)
        Debug.Log("[MasterBuilder] Langkah 1/5: Men-generate data & prefab variasi sampah...");
        TrashDataAndPrefabBuilder.BuildAllTrashDataAndPrefabs();

        // 2. Buka scene Level 1 jika belum aktif
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.Log("[MasterBuilder] Membuka scene: " + ScenePath);
            activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // 3. Bangun Environment (Tahap 3)
        Debug.Log("[MasterBuilder] Langkah 2/5: Membangun environment low-poly...");
        Level1EnvironmentBuilder.BuildEnvironment();

        // 4. Bangun Pos TPS, Tetua, & Exit Marker (Tahap 4 & 8)
        Debug.Log("[MasterBuilder] Langkah 3/5: Membangun Pos TPS, Tetua Desa, & Exit Marker...");
        Level1TPSBuilder.BuildTPSStation();

        // 5. Bangun Spawner v2 & 4 Area Spawn (Tahap 7)
        Debug.Log("[MasterBuilder] Langkah 4/5: Menyusun Spawner v2 & 4 Area Spawn...");
        Level1SpawnerSetup.SetupSpawnerAndAreas();

        // Simpan scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MasterBuilder] Langkah 5/5: Menjalankan audit QA Checklist...");
        Level1QAChecklist.RunChecklist();

        Debug.Log("=================================================================");
        Debug.Log("[MasterBuilder] SELESAI! Scene Level 1 sudah terisi lengkap dan siap dimainkan.");
        Debug.Log("=================================================================");
    }
}
