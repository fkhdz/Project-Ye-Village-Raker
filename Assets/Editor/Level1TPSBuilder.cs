using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tahap 4 — Pos TPS (3 Tong + 2 Mesin nonaktif) dan NPC Tetua Desa + Exit Marker.
/// Menempatkan prefab bersama (dari Tahap 1) ke posisi sesuai denah prompt.
/// Menu: YeVillage/Level1/Build TPS Station
///
/// PENTING: Scene Level1_Zona1_Pinggiran harus sudah terbuka dan aktif.
/// Prefab bersama harus sudah ada (jalankan Build Shared Prefabs dulu).
/// </summary>
public static class Level1TPSBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string SharedFolder = "Assets/Prefabs/Shared";

    [MenuItem("YeVillage/Level1/Build TPS Station")]
    public static void BuildTPSStation()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[Level1TPS] Scene aktif bukan Level1_Zona1_Pinggiran. " +
                           "Buka scene tersebut dulu.");
            return;
        }

        // Validasi prefab yang dibutuhkan
        string[] requiredPrefabs =
        {
            "PF_Pallet_TPS", "PF_Tong_Hijau", "PF_Tong_Kuning", "PF_Tong_Merah",
            "PF_Mesin_Kompos", "PF_Mesin_Press", "PF_TetuaDesa"
        };

        foreach (string prefabName in requiredPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SharedFolder + "/" + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("[Level1TPS] Prefab tidak ditemukan: " + prefabName +
                    ". Jalankan YeVillage/Level1/Build Shared Prefabs dulu.");
                return;
            }
        }

        // Pastikan parent utama Gameplay ada dan di (0,0,0)
        GameObject gameplayRoot = GameObject.Find("--- GAMEPLAY ---");
        if (gameplayRoot == null)
        {
            gameplayRoot = new GameObject("--- GAMEPLAY ---");
        }
        gameplayRoot.transform.position = Vector3.zero;
        gameplayRoot.transform.rotation = Quaternion.identity;
        gameplayRoot.transform.localScale = Vector3.one;

        // Hapus child TPS lama di dalam Gameplay jika ada (idempotent)
        Transform oldTPS = gameplayRoot.transform.Find("TPS_Station");
        if (oldTPS != null) Object.DestroyImmediate(oldTPS.gameObject);
        Transform oldTetua = gameplayRoot.transform.Find("NPC_TetuaDesa");
        if (oldTetua != null) Object.DestroyImmediate(oldTetua.gameObject);
        Transform oldExit = gameplayRoot.transform.Find("Exit_To_Zona2");
        if (oldExit != null) Object.DestroyImmediate(oldExit.gameObject);

        // 1. TPS Station
        BuildTPSArea(gameplayRoot.transform);

        // 2. NPC Tetua Desa
        BuildTetuaDesa(gameplayRoot.transform);

        // 3. Exit_To_Zona2
        BuildExitMarker(gameplayRoot.transform);

        // 4. Pastikan PlayerSpawnPoint ada di bawah --- GAMEPLAY ---
        ValidatePlayerSpawn(gameplayRoot.transform);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[Level1TPS] Selesai. Pos TPS, Tetua Desa, dan Exit Marker " +
                  "telah ditempatkan di scene " + ScenePath);
    }

    // ================================================================
    // POS TPS: PALLET + 3 TONG + 2 MESIN
    // ================================================================
    private static void BuildTPSArea(Transform parent)
    {
        // Parent group TPS
        GameObject tpsStation = new GameObject("TPS_Station");
        tpsStation.transform.SetParent(parent, false);
        tpsStation.transform.position = new Vector3(0f, 0f, 4f);

        // --- Pallet (platform kayu) ---
        // Posisi: pusat TPS (0, 0, 4), ukuran 8x4 m
        GameObject palletPrefab = LoadPrefab("PF_Pallet_TPS");
        GameObject pallet = InstantiatePrefab(palletPrefab, tpsStation.transform,
            new Vector3(0f, 0f, 4f), Quaternion.identity, "Pallet");
        // Pallet sudah punya scale 8x0.2x4 dari SharedPrefabBuilder

        // --- 3 Tong Sampah ---
        // Susunan kiri ke kanan: Hijau → Kuning → Merah
        // Berdiri di atas pallet (y = 0 karena pallet tipis)

        // Tong Hijau (Organik) — paling kiri
        GameObject tongHijauPrefab = LoadPrefab("PF_Tong_Hijau");
        GameObject tongHijau = InstantiatePrefab(tongHijauPrefab, tpsStation.transform,
            new Vector3(-2.5f, 0f, 4f), Quaternion.identity, "Bin_Green");
        ValidateTrashBin(tongHijau, "Organik");

        // Tong Kuning (Anorganik) — tengah
        GameObject tongKuningPrefab = LoadPrefab("PF_Tong_Kuning");
        GameObject tongKuning = InstantiatePrefab(tongKuningPrefab, tpsStation.transform,
            new Vector3(0f, 0f, 4f), Quaternion.identity, "Bin_Yellow");
        ValidateTrashBin(tongKuning, "Anorganik");

        // Tong Merah (B3) — paling kanan
        // Tetap ada walau Level 1 tidak men-spawn B3 (sesuai prompt)
        GameObject tongMerahPrefab = LoadPrefab("PF_Tong_Merah");
        GameObject tongMerah = InstantiatePrefab(tongMerahPrefab, tpsStation.transform,
            new Vector3(2.5f, 0f, 4f), Quaternion.identity, "Bin_Red");
        ValidateTrashBin(tongMerah, "B3");

        // --- 2 Mesin (nonaktif di awal Level 1) ---

        // Mesin Kompos — di belakang Tong Hijau
        GameObject mesinKomposPrefab = LoadPrefab("PF_Mesin_Kompos");
        GameObject mesinKompos = InstantiatePrefab(mesinKomposPrefab, tpsStation.transform,
            new Vector3(-2.5f, 0f, 6.5f), Quaternion.identity, "Machine_Compost");
        if (mesinKompos != null)
        {
            mesinKompos.SetActive(false); // Nonaktif di awal
        }

        // Mesin Press Plastik — di belakang Tong Kuning
        GameObject mesinPressPrefab = LoadPrefab("PF_Mesin_Press");
        GameObject mesinPress = InstantiatePrefab(mesinPressPrefab, tpsStation.transform,
            new Vector3(0f, 0f, 6.5f), Quaternion.identity, "Machine_Press");
        if (mesinPress != null)
        {
            mesinPress.SetActive(false); // Nonaktif di awal
        }

        // Hubungkan referensi visual mesin ke ShopManager
        // (akan dilakukan di WireTPSReferences)
        WireTPSReferences(tongHijau, tongKuning, tongMerah, mesinKompos, mesinPress);

        // Dekorasi Pagar Belakang Stasiun TPS (Z = 7.8)
        string kenneyFence = "Assets/ThirdParty/KenneyNatureKit/Models/fence_simple.fbx";
        GameObject fenceModel = AssetDatabase.LoadAssetAtPath<GameObject>(kenneyFence);
        if (fenceModel != null)
        {
            GameObject f1 = (GameObject)PrefabUtility.InstantiatePrefab(fenceModel);
            f1.name = "TPS_BackFence_Left";
            f1.transform.SetParent(tpsStation.transform, false);
            f1.transform.position = new Vector3(-2.2f, 0f, 7.8f);
            f1.transform.localScale = Vector3.one * 1.5f;

            GameObject f2 = (GameObject)PrefabUtility.InstantiatePrefab(fenceModel);
            f2.name = "TPS_BackFence_Right";
            f2.transform.SetParent(tpsStation.transform, false);
            f2.transform.position = new Vector3(2.2f, 0f, 7.8f);
            f2.transform.localScale = Vector3.one * 1.5f;
        }

        Debug.Log("[Level1TPS] TPS Station selesai: Pallet + 3 Tong + 2 Mesin (nonaktif) + Pagar Pembatas.");
    }

    // ================================================================
    // NPC TETUA DESA
    // ================================================================
    private static void BuildTetuaDesa(Transform parent)
    {
        GameObject tetuaPrefab = LoadPrefab("PF_TetuaDesa");
        GameObject tetua = InstantiatePrefab(tetuaPrefab, parent,
            new Vector3(-9f, 0f, 3f), Quaternion.Euler(0f, 60f, 0f), "NPC_TetuaDesa");

        if (tetua == null)
        {
            Debug.LogError("[Level1TPS] Gagal menempatkan NPC Tetua Desa.");
            return;
        }

        // Pastikan ada interaction trigger
        EnsureInteractionTrigger(tetua, 2.5f);

        // Wire referensi ShopManager ke player
        WireShopToPlayer(tetua);

        Debug.Log("[Level1TPS] NPC Tetua Desa ditempatkan di (-9, 0, 3) menghadap area pemain.");
    }

    // ================================================================
    // EXIT MARKER KE ZONA 2
    // ================================================================
    private static void BuildExitMarker(Transform parent)
    {
        // Empty GameObject dengan BoxCollider trigger sebagai penanda keluar
        GameObject exit = new GameObject("Exit_To_Zona2");
        exit.transform.SetParent(parent, false);
        exit.transform.position = new Vector3(0f, 1.5f, 29f);

        // BoxCollider trigger (cukup besar agar pemain melewati)
        BoxCollider col = exit.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(6f, 4f, 2f);

        // Tambah ZoneExitMarker script (Tahap 8)
        ZoneExitMarker marker = exit.AddComponent<ZoneExitMarker>();
        marker.targetSceneName = "SampleScene";
        marker.loadOnEnter = false; // Tekan [E] untuk lanjut
        marker.requireTargetScore = false;
        marker.requiredScore = 250;

        Debug.Log("[Level1TPS] Exit_To_Zona2 ditempatkan di (0, 1.5, 29) dengan ZoneExitMarker.");
    }

    // ================================================================
    // HELPER: WIRE REFERENSI
    // ================================================================

    /// <summary>
    /// Mencari ShopManager di scene dan menghubungkan referensi visual mesin + player.
    /// </summary>
    private static void WireTPSReferences(
        GameObject tongHijau, GameObject tongKuning, GameObject tongMerah,
        GameObject mesinKompos, GameObject mesinPress)
    {
        // Cari ShopManager di scene (ada di Tetua atau akan ada setelah BuildTetuaDesa)
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            Debug.LogWarning("[Level1TPS] ShopManager belum ada di scene. " +
                             "Referensi visual mesin akan dihubungkan saat Build TPS Station dijalankan " +
                             "setelah Tetua Desa dipasang.");
            return;
        }

        // Hubungkan visual mesin ke ShopManager
        SerializedObject serialized = new SerializedObject(shop);

        SetObjectReference(serialized, "mesinKomposVisual", mesinKompos);
        SetObjectReference(serialized, "mesinPressVisual", mesinPress);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Menghubungkan ShopManager ke Player (PlayerController + TrashInventory).
    /// </summary>
    private static void WireShopToPlayer(GameObject tetuaObj)
    {
        ShopManager shop = tetuaObj.GetComponent<ShopManager>();
        if (shop == null)
        {
            shop = tetuaObj.GetComponentInChildren<ShopManager>(true);
        }

        if (shop == null)
        {
            Debug.LogWarning("[Level1TPS] ShopManager tidak ditemukan pada Tetua Desa.");
            return;
        }

        // Cari Player di scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            // Fallback: cari prefab PF_Player_Kades di scene
            player = FindByName("PF_Player_Kades");
        }

        if (player == null)
        {
            Debug.LogWarning("[Level1TPS] Player tidak ditemukan di scene. " +
                             "Pastikan Tahap 2 (Build Base Scene) sudah dijalankan.");
            return;
        }

        SerializedObject serialized = new SerializedObject(shop);

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            SetObjectReference(serialized, "playerController", pc);
        }

        TrashInventory inv = player.GetComponent<TrashInventory>();
        if (inv != null)
        {
            SetObjectReference(serialized, "playerInventory", inv);
        }

        // Hubungkan visual mesin yang ada di scene
        GameObject mesinKompos = FindByName("Machine_Compost");
        GameObject mesinPress = FindByName("Machine_Press");
        if (mesinKompos != null) SetObjectReference(serialized, "mesinKomposVisual", mesinKompos);
        if (mesinPress != null) SetObjectReference(serialized, "mesinPressVisual", mesinPress);

        // Filter Toko Level 1 (Zona 1): Mesin dan Renovasi dinonaktifkan
        SerializedProperty pKompos = serialized.FindProperty("sellMesinKompos");
        if (pKompos != null) pKompos.boolValue = false;

        SerializedProperty pPress = serialized.FindProperty("sellMesinPress");
        if (pPress != null) pPress.boolValue = false;

        SerializedProperty pRenovasi = serialized.FindProperty("sellRenovasi");
        if (pRenovasi != null) pRenovasi.boolValue = false;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Juga pastikan UIManager di scene diset target 250 untuk Zona 1
        UIManager ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui != null)
        {
            SerializedObject serializedUI = new SerializedObject(ui);
            SerializedProperty pTarget = serializedUI.FindProperty("targetPoin");
            if (pTarget != null) pTarget.intValue = 250;
            SerializedProperty pHint = serializedUI.FindProperty("targetReachedHint");
            if (pHint != null) pHint.stringValue = "Target Zona 1 Tercapai (250 Poin)! Silakan ke Exit Utara.";
            serializedUI.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Level1TPS] UIManager diset targetPoin = 250 untuk Zona 1.");
        }

        Debug.Log("[Level1TPS] ShopManager dihubungkan ke Player (" + player.name + ") & filter zona 1 diterapkan.");
    }

    /// <summary>
    /// Memastikan TrashBin punya trigger collider dan acceptedTrashTag benar.
    /// </summary>
    private static void ValidateTrashBin(GameObject tongObj, string expectedTag)
    {
        if (tongObj == null) return;

        TrashBin bin = tongObj.GetComponent<TrashBin>();
        if (bin == null)
        {
            bin = tongObj.GetComponentInChildren<TrashBin>(true);
        }

        if (bin != null)
        {
            // Pastikan acceptedTrashTag benar
            SerializedObject serialized = new SerializedObject(bin);
            SerializedProperty tagProp = serialized.FindProperty("acceptedTrashTag");
            if (tagProp != null && tagProp.stringValue != expectedTag)
            {
                tagProp.stringValue = expectedTag;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Level1TPS] TrashBin " + tongObj.name +
                          ": acceptedTrashTag diset ke '" + expectedTag + "'.");
            }
        }
        else
        {
            Debug.LogWarning("[Level1TPS] TrashBin component tidak ditemukan pada " + tongObj.name);
        }

        // Pastikan ada trigger collider
        EnsureInteractionTrigger(tongObj, 0.7f);
    }

    /// <summary>
    /// Memastikan ada SphereCollider isTrigger pada objek untuk interaksi [E].
    /// </summary>
    private static void EnsureInteractionTrigger(GameObject obj, float radius)
    {
        if (obj == null) return;

        // Cek apakah sudah ada trigger collider
        Collider[] colliders = obj.GetComponents<Collider>();
        bool hasTrigger = false;
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }

        // Cek juga di children
        if (!hasTrigger)
        {
            Collider[] childColliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in childColliders)
            {
                if (col.isTrigger)
                {
                    hasTrigger = true;
                    break;
                }
            }
        }

        if (!hasTrigger)
        {
            // Tambah SphereCollider trigger
            SphereCollider trigger = obj.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            trigger.center = Vector3.up * 0.5f; // Sedikit di atas ground
            Debug.Log("[Level1TPS] Trigger collider ditambahkan ke " + obj.name +
                      " (radius " + radius + ").");
        }
    }

    private static void ValidatePlayerSpawn(Transform gameplayParent)
    {
        GameObject spawnPoint = FindByName("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.position = new Vector3(10f, 0f, -12f);
            spawnPoint.transform.rotation = Quaternion.Euler(0f, -58f, 0f);
        }
        spawnPoint.transform.SetParent(gameplayParent, true);
        spawnPoint.transform.position = new Vector3(10f, 0f, -12f);
        spawnPoint.transform.rotation = Quaternion.Euler(0f, -58f, 0f);
        Debug.Log("[Level1TPS] PlayerSpawnPoint ditempatkan di " + spawnPoint.transform.position + " menghadap Pos TPS.");
    }

    // ================================================================
    // UTILITY
    // ================================================================

    private static GameObject LoadPrefab(string prefabName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            SharedFolder + "/" + prefabName + ".prefab");
    }

    private static GameObject InstantiatePrefab(GameObject prefab, Transform parent,
        Vector3 position, Quaternion rotation, string name)
    {
        if (prefab == null)
        {
            Debug.LogError("[Level1TPS] Prefab null untuk: " + name);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("[Level1TPS] Gagal instantiate prefab: " + name);
            return null;
        }

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = rotation;

        return instance;
    }

    private static void SetObjectReference(SerializedObject obj, string propertyName, Object value)
    {
        SerializedProperty prop = obj.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
    }

    private static GameObject FindByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child.gameObject;
            }
        }
        return null;
    }

    private static System.Type FindType(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    private static void DestroyExisting(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}

// ExitGizmoDrawer dipindahkan ke Assets/Scripts/UI/ExitGizmoDrawer.cs
// (MonoBehaviour tidak boleh di folder Editor karena tidak bisa di-attach ke GameObject runtime)
