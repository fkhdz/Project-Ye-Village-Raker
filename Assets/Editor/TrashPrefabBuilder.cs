using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Utilitas editor sementara untuk Fase 5:
/// - Membuat 3 prefab sampah dari objek contoh di scene (tag + trigger collider + material ikut).
/// - Membuat 9 spawn point di area terbuka.
/// - Membuat TrashSpawnerManager dan mengisi array TrashSpawner.
/// Jalankan via menu: YeVillage > Build Trash Prefabs & Setup Spawner.
/// </summary>
public static class TrashPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Trash";

    private static readonly string[] SpawnPointNames =
    {
        "SP_Zona1_01", "SP_Zona1_02", "SP_Zona1_03",
        "SP_Zona2_01", "SP_Zona2_02", "SP_Zona2_03",
        "SP_Zona3_01", "SP_Zona3_02", "SP_Zona3_03"
    };

    private static readonly Vector3[] SpawnPointPositions =
    {
        new Vector3(-7f, 0.3f, 0f),
        new Vector3(7f, 0.3f, 0f),
        new Vector3(0f, 0.3f, 7f),
        new Vector3(5f, 0.3f, -5f),
        new Vector3(7f, 0.3f, 5f),
        new Vector3(-5f, 0.3f, -5f),
        new Vector3(0f, 0.3f, -9.2f),
        new Vector3(-7f, 0.3f, 5f),
        new Vector3(-7f, 0.3f, -5f)
    };

    [MenuItem("YeVillage/Build Trash Prefabs & Setup Spawner")]
    public static void BuildTrashPrefabsAndSpawner()
    {
        EnsureFolder();

        GameObject organikPrefab = CreatePrefabFromSample("Trash_Organik_A", "Trash_Organik");
        GameObject anorganikPrefab = CreatePrefabFromSample("Trash_Anorganik_A", "Trash_Anorganik");
        GameObject b3Prefab = CreatePrefabFromSample("Trash_B3_A", "Trash_B3");

        if (organikPrefab == null || anorganikPrefab == null || b3Prefab == null)
        {
            Debug.LogError("[TrashPrefabBuilder] Gagal membuat prefab. Pastikan objek contoh Trash_Organik_A, Trash_Anorganik_A, Trash_B3_A ada di scene.");
            return;
        }

        GameObject spawnRoot = GameObject.Find("SpawnPoints");
        if (spawnRoot == null)
        {
            spawnRoot = new GameObject("SpawnPoints");
        }

        List<Transform> spawnPoints = new List<Transform>();
        for (int i = 0; i < SpawnPointNames.Length; i++)
        {
            Transform point = spawnRoot.transform.Find(SpawnPointNames[i]);
            if (point == null)
            {
                GameObject pointObject = new GameObject(SpawnPointNames[i]);
                point = pointObject.transform;
                point.SetParent(spawnRoot.transform, false);
            }

            point.position = SpawnPointPositions[i];
            point.rotation = Quaternion.identity;
            spawnPoints.Add(point);
        }

        GameObject manager = GameObject.Find("TrashSpawnerManager");
        if (manager == null)
        {
            manager = new GameObject("TrashSpawnerManager");
        }

        TrashSpawner spawner = manager.GetComponent<TrashSpawner>();
        if (spawner == null)
        {
            spawner = manager.AddComponent<TrashSpawner>();
        }

        spawner.trashPrefabs = new[] { organikPrefab, anorganikPrefab, b3Prefab };
        spawner.spawnPoints = spawnPoints.ToArray();
        spawner.spawnInterval = 5f;
        spawner.maxTrashOnMap = 15;
        spawner.isSpawning = true;

        EditorSceneManager.MarkSceneDirty(manager.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[TrashPrefabBuilder] Selesai: 3 prefab + 9 spawn point + TrashSpawnerManager siap.");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Trash");
        }
    }

    private static GameObject CreatePrefabFromSample(string sampleName, string prefabName)
    {
        GameObject source = GameObject.Find(sampleName);
        if (source == null)
        {
            Debug.LogError("[TrashPrefabBuilder] Objek contoh tidak ditemukan: " + sampleName);
            return null;
        }

        GameObject temp = Object.Instantiate(source);
        temp.name = prefabName;
        temp.transform.SetParent(null);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;

        // Pastikan semua collider sampah tetap trigger agar bisa dideteksi TrashInventory.
        foreach (Collider col in temp.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        string path = PrefabFolder + "/" + prefabName + ".prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);

        Debug.Log("[TrashPrefabBuilder] Prefab dibuat: " + path + " (tag: " + prefabAsset.tag + ")");
        return prefabAsset;
    }
}
