using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Setup Editor untuk Spawner v2 dan 4 Area Spawn di Level 1.
/// Membangun hierarki --- TRASH SYSTEM --- sesuai dokumen spesifikasi.
/// Menu: YeVillage/Level1/Setup Spawner & Areas
/// </summary>
public static class Level1SpawnerSetup
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string SORoot = "Assets/ScriptableObjects/Trash";

    [MenuItem("YeVillage/Level1/Setup Spawner & Areas")]
    public static void SetupSpawnerAndAreas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[Level1SpawnerSetup] Scene aktif bukan Level1_Zona1_Pinggiran. " +
                           "Buka scene tersebut dulu.");
            return;
        }

        // Hapus TRASH SYSTEM lama jika ada (idempotent)
        GameObject existingRoot = GameObject.Find("--- TRASH SYSTEM ---");
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        GameObject trashSystemRoot = new GameObject("--- TRASH SYSTEM ---");

        // 1. TrashContainer
        GameObject containerObj = new GameObject("TrashContainer");
        containerObj.transform.SetParent(trashSystemRoot.transform, false);

        // 2. SpawnAreas Group
        GameObject areasGroup = new GameObject("SpawnAreas");
        areasGroup.transform.SetParent(trashSystemRoot.transform, false);

        // 4 Area sesuai spesifikasi Prompt 5.3:
        // A. Jalan Utama Selatan: (0, -19), 40x14, weight 30
        TrashSpawnArea areaA = CreateSpawnArea(areasGroup.transform,
            "Spawn_A_JalanUtamaSelatan", new Vector3(0f, 1f, -19f), new Vector3(40f, 3f, 14f), 30);

        // B. Belakang Rumah Barat: (-21, 5), 14x18, weight 25
        TrashSpawnArea areaB = CreateSpawnArea(areasGroup.transform,
            "Spawn_B_BelakangRumahBarat", new Vector3(-21f, 1f, 5f), new Vector3(14f, 3f, 18f), 25);

        // C. Pagar Timur: (20, 2), 12x20, weight 25
        TrashSpawnArea areaC = CreateSpawnArea(areasGroup.transform,
            "Spawn_C_PagarTimur", new Vector3(20f, 1f, 2f), new Vector3(12f, 3f, 20f), 25);

        // D. Lapangan Utara: (0, 21), 24x10, weight 20
        TrashSpawnArea areaD = CreateSpawnArea(areasGroup.transform,
            "Spawn_D_LapanganUtara", new Vector3(0f, 1f, 21f), new Vector3(24f, 3f, 10f), 20);

        // 3. GameObject TrashSpawner
        GameObject spawnerObj = new GameObject("TrashSpawner");
        spawnerObj.transform.SetParent(trashSystemRoot.transform, false);
        TrashSpawner spawner = spawnerObj.AddComponent<TrashSpawner>();

        // Konfigurasi spawner v2
        spawner.mode = TrashSpawner.SpawnerMode.AreaVolume;
        spawner.spawnAreas = new[] { areaA, areaB, areaC, areaD };
        spawner.trashContainer = containerObj.transform;

        // Parameter default Level 1 sesuai GDD
        spawner.initialSpawnCount = 15;
        spawner.maxActiveTrash = 25;
        spawner.minActiveTrash = 8;
        spawner.spawnInterval = 6f;
        spawner.organicWeight = 50;
        spawner.inorganicWeight = 50;
        spawner.b3Weight = 0; // Wajib 0 di Level 1
        spawner.minDistanceBetweenTrash = 1.5f;
        spawner.maxPlacementAttempts = 20;

        spawner.tpsCenter = new Vector3(0f, 0f, 4f);
        spawner.tpsExclusionRadius = 4f;
        spawner.tetuaCenter = new Vector3(-9f, 0f, 3f);
        spawner.tetuaExclusionRadius = 3f;
        spawner.playerSpawnCenter = new Vector3(10f, 0f, -12f);
        spawner.playerSpawnExclusionRadius = 3f;
        spawner.spawnStarterTrash = true;

        // Layers
        int groundLayerIdx = LayerMask.NameToLayer("Ground");
        spawner.groundLayer = groundLayerIdx >= 0 ? (1 << groundLayerIdx) : (1 << 0);

        int obstacleLayerIdx = LayerMask.NameToLayer("Obstacle");
        spawner.blockingLayers = obstacleLayerIdx >= 0 ? (1 << obstacleLayerIdx) : 0;

        // Load ScriptableObject TrashData ke pool
        spawner.organicPool = LoadTrashDataFromFolder(SORoot + "/Organic");
        spawner.inorganicPool = LoadTrashDataFromFolder(SORoot + "/Inorganic");
        spawner.b3Pool = LoadTrashDataFromFolder(SORoot + "/B3");

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[Level1SpawnerSetup] Selesai! Spawner v2 dan 4 Area Spawn berhasil dipasang di " + ScenePath +
                  "\n- Organik pool: " + spawner.organicPool.Count + " item" +
                  "\n- Anorganik pool: " + spawner.inorganicPool.Count + " item" +
                  "\n- B3 pool: " + spawner.b3Pool.Count + " item (Weight: 0 di Level 1)");
    }

    private static TrashSpawnArea CreateSpawnArea(Transform parent, string name, Vector3 position, Vector3 size, int weight)
    {
        GameObject areaObj = new GameObject(name);
        areaObj.transform.SetParent(parent, false);
        areaObj.transform.position = position;

        BoxCollider col = areaObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = Vector3.zero;
        col.size = size;

        TrashSpawnArea areaComp = areaObj.AddComponent<TrashSpawnArea>();
        areaComp.weight = weight;

        return areaComp;
    }

    private static List<TrashData> LoadTrashDataFromFolder(string folderPath)
    {
        List<TrashData> list = new List<TrashData>();
        string[] guids = AssetDatabase.FindAssets("t:TrashData", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TrashData data = AssetDatabase.LoadAssetAtPath<TrashData>(path);
            if (data != null)
            {
                list.Add(data);
            }
        }

        return list;
    }
}
