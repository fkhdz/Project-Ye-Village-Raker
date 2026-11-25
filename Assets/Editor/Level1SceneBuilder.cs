using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Membuat scene dasar Zona 1 (Level1_Zona1_Pinggiran) untuk Tahap 2.
/// Semua objek memakai prefab/material yang sudah ada — tidak membuat aset baru dari nol.
/// Menu: YeVillage/Level1/Build Base Scene
/// </summary>
public static class Level1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string OldGameplayPath = "Assets/Scenes/SampleScene.unity";
    private const string SharedFolder = "Assets/Prefabs/Shared";
    private const string SandMaterialPath = "Assets/Materials/Ye_Village/M_Sand.mat";
    private const string TargetSceneName = "Level1_Zona1_Pinggiran";

    [MenuItem("YeVillage/Level1/Build Base Scene")]
    public static void BuildBaseScene()
    {
        // Jangan menimpa pekerjaan yang belum disimpan.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[Level1Scene] Ada scene yang belum disimpan ('" + open.name +
                    "'). Simpan dulu sebelum menjalankan builder.");
                return;
            }
        }

        string[] requiredPrefabs =
        {
            "PF_Player_Kades", "PF_MainCamera", "PF_Canvas_HUD",
            "PF_AudioManager", "PF_VFXManager", "PF_ScoreManager"
        };

        foreach (string prefabName in requiredPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedFolder + "/" + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("[Level1Scene] Prefab tidak ditemukan: " + prefabName +
                    ". Jalankan YeVillage/Level1/Build Shared Prefabs dulu.");
                return;
            }
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            AssetDatabase.DeleteAsset(ScenePath);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        ConfigureRenderSettings();
        CreateGround();
        CreateBoundaryWalls();
        CreateSunLight();

        GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(10f, 0f, -12f);
        spawnPoint.transform.rotation = Quaternion.Euler(0f, -32f, 0f);

        GameObject player = InstantiateSharedPrefab("PF_Player_Kades",
            spawnPoint.transform.position, spawnPoint.transform.rotation);
        GameObject cam = InstantiateSharedPrefab("PF_MainCamera", Vector3.zero, Quaternion.identity);
        InstantiateSharedPrefab("PF_ScoreManager", Vector3.zero, Quaternion.identity);
        GameObject hud = InstantiateSharedPrefab("PF_Canvas_HUD", Vector3.zero, Quaternion.identity);
        InstantiateSharedPrefab("PF_AudioManager", Vector3.zero, Quaternion.identity);
        InstantiateSharedPrefab("PF_VFXManager", Vector3.zero, Quaternion.identity);

        WireReferences(player, cam, hud);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("[Level1Scene] Gagal menyimpan scene: " + ScenePath);
            return;
        }

        UpdateBuildSettings();
        UpdateMainMenuTarget();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Level1Scene] Selesai. Scene dasar Zona 1 dibuat: " + ScenePath +
                  " | Build Settings: MainMenu=0, " + TargetSceneName + "=1, SampleScene=2" +
                  " | MainMenu.gameplaySceneName = " + TargetSceneName);
    }

    private static GameObject InstantiateSharedPrefab(string prefabName, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedFolder + "/" + prefabName + ".prefab");
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("[Level1Scene] Gagal meng-instantiate prefab: " + prefabName);
            return null;
        }

        instance.name = prefabName;
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    private static void WireReferences(GameObject player, GameObject cam, GameObject hud)
    {
        if (player == null || cam == null)
        {
            return;
        }

        ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc != null)
        {
            tpc.target = player.transform;
        }

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.cam = cam.transform;
        }

        if (hud != null)
        {
            UIManager ui = hud.GetComponent<UIManager>();
            TrashInventory inventory = player.GetComponent<TrashInventory>();
            if (ui != null && inventory != null)
            {
                ui.playerInventory = inventory;
            }

            if (ui != null)
            {
                SerializedObject serializedUI = new SerializedObject(ui);
                SerializedProperty pTarget = serializedUI.FindProperty("targetPoin");
                if (pTarget != null) pTarget.intValue = 250;
                SerializedProperty pHint = serializedUI.FindProperty("targetReachedHint");
                if (pHint != null) pHint.stringValue = "Target Zona 1 Tercapai (250 Poin)! Silakan ke Exit Utara.";
                serializedUI.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.009f;
        RenderSettings.fogColor = new Color(0.78f, 0.70f, 0.55f, 1f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.50f, 0.43f, 1f);
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.15f, 0f);
        ground.transform.localScale = new Vector3(60f, 0.3f, 60f);
        ground.layer = LayerMask.NameToLayer("Ground");

        Material sand = AssetDatabase.LoadAssetAtPath<Material>(SandMaterialPath);
        if (sand != null)
        {
            ground.GetComponent<Renderer>().sharedMaterial = sand;
        }
        else
        {
            Debug.LogWarning("[Level1Scene] Material M_Sand tidak ditemukan; memakai material default.");
        }
    }

    private static void CreateBoundaryWalls()
    {
        GameObject walls = new GameObject("BoundaryWalls");

        CreateWall(walls.transform, "Wall_South", new Vector3(0f, 1.5f, -31f), new Vector3(62f, 4f, 2f));
        CreateWall(walls.transform, "Wall_West", new Vector3(-31f, 1.5f, 0f), new Vector3(2f, 4f, 62f));
        CreateWall(walls.transform, "Wall_East", new Vector3(31f, 1.5f, 0f), new Vector3(2f, 4f, 62f));

        // Celah keluar di utara, x = -3 .. 3 (untuk pintu keluar Tahap 8).
        CreateWall(walls.transform, "Wall_North_Left", new Vector3(-16.5f, 1.5f, 31f), new Vector3(27f, 4f, 2f));
        CreateWall(walls.transform, "Wall_North_Right", new Vector3(16.5f, 1.5f, 31f), new Vector3(27f, 4f, 2f));
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static void CreateSunLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.86f, 0.66f, 1f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(OldGameplayPath, true)
        };
    }

    private static void UpdateMainMenuTarget()
    {
        Scene menuScene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Additive);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(menuScene);

        MainMenu menu = null;
        foreach (GameObject root in menuScene.GetRootGameObjects())
        {
            menu = root.GetComponent<MainMenu>();
            if (menu != null)
            {
                break;
            }
        }

        if (menu == null)
        {
            Debug.LogError("[Level1Scene] Komponen MainMenu tidak ditemukan di " + MainMenuPath);
        }
        else
        {
            SerializedObject serialized = new SerializedObject(menu);
            SerializedProperty property = serialized.FindProperty("gameplaySceneName");
            if (property == null)
            {
                Debug.LogError("[Level1Scene] Field gameplaySceneName tidak ditemukan pada MainMenu.");
            }
            else
            {
                property.stringValue = TargetSceneName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(menuScene);
            }
        }

        SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(menuScene, true);
    }
}
