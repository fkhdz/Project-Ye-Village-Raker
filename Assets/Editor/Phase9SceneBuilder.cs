using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builder Fase 9: menyiapkan scene MainMenu (build index 0) dan memasang
/// PauseMenu pada Canvas_HUD di SampleScene (build index 1).
/// Menu: YeVillage > Setup Phase 9 Menus. Aman dijalankan berulang (idempotent).
/// </summary>
public static class Phase9SceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("YeVillage/Setup Phase 9 Menus")]
    public static void SetupPhase9()
    {
        // 1. Simpan scene yang sedang terbuka agar perubahan tidak hilang.
        EditorSceneManager.SaveOpenScenes();

        // 2. Pasang PauseMenu pada Canvas_HUD di scene gameplay.
        Scene gameplayScene = SceneManager.GetActiveScene();
        if (gameplayScene.path != GameplayScenePath)
        {
            gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        GameObject canvasHUD = GameObject.Find("Canvas_HUD");
        if (canvasHUD == null)
        {
            Debug.LogError("[Phase9Builder] GameObject 'Canvas_HUD' tidak ditemukan di " + GameplayScenePath + ".");
            return;
        }

        PauseMenu pauseMenu = canvasHUD.GetComponent<PauseMenu>();
        if (pauseMenu == null)
        {
            pauseMenu = canvasHUD.AddComponent<PauseMenu>();
            Debug.Log("[Phase9Builder] PauseMenu dipasang di Canvas_HUD.");
        }

        EditorUtility.SetDirty(pauseMenu);
        EditorSceneManager.MarkSceneDirty(gameplayScene);
        EditorSceneManager.SaveScene(gameplayScene);
        Debug.Log("[Phase9Builder] SampleScene disimpan dengan PauseMenu.");

        // 3. Buat atau buka scene MainMenu.
        bool mainMenuExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
        Scene menuScene;

        if (mainMenuExists)
        {
            menuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            EnsureMenuSceneObjects();
            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene);
            Debug.Log("[Phase9Builder] MainMenu scene diperbarui.");
        }
        else
        {
            menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureMenuSceneObjects();
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
            Debug.Log("[Phase9Builder] MainMenu scene dibuat di " + MainMenuScenePath + ".");
        }

        // 4. Atur Build Settings: MainMenu = 0, SampleScene = 1.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };
        Debug.Log("[Phase9Builder] Build Settings: MainMenu = 0, SampleScene = 1.");

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = GameObject.Find("Canvas_MainMenu");

        Debug.Log("[Phase9Builder] Setup Fase 9 selesai. Scene MainMenu aktif; tekan Play untuk mulai dari menu.");
    }

    [MenuItem("YeVillage/Test Play Game")]
    public static void TestPlayGame()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Phase9Builder] Test Play Game hanya bisa dijalankan saat Play mode.");
            return;
        }

        MainMenu mainMenu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            Debug.LogError("[Phase9Builder] MainMenu tidak ditemukan di scene.");
            return;
        }

        mainMenu.PlayGame();
        Debug.Log("[Phase9Builder] PlayGame dipicu.");
    }

    [MenuItem("YeVillage/Test Pause Menu")]
    public static void TestPauseMenu()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Phase9Builder] Test Pause Menu hanya bisa dijalankan saat Play mode.");
            return;
        }

        PauseMenu pauseMenu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pauseMenu == null)
        {
            Debug.LogError("[Phase9Builder] PauseMenu tidak ditemukan di scene.");
            return;
        }

        if (PauseMenu.isPaused)
        {
            pauseMenu.Resume();
            Debug.Log("[Phase9Builder] Resume dipicu.");
        }
        else
        {
            pauseMenu.Pause();
            Debug.Log("[Phase9Builder] Pause dipicu.");
        }
    }

    private static void EnsureMenuSceneObjects()
    {
        // Kamera menu dengan latar warna desa.
        Camera camera = Object.FindFirstObjectByType<Camera>();
        GameObject cameraObject;

        if (camera == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            camera = cameraObject.GetComponent<Camera>();
        }
        else
        {
            cameraObject = camera.gameObject;
            if (cameraObject.GetComponent<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }
        }

        cameraObject.transform.position = new Vector3(0f, 1f, -10f);
        cameraObject.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.16f, 0.11f);

        // Canvas + MainMenu (UI dibangun otomatis saat runtime).
        GameObject canvasObject = GameObject.Find("Canvas_MainMenu");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Canvas_MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (canvasObject.GetComponent<MainMenu>() == null)
        {
            canvasObject.AddComponent<MainMenu>();
        }

        // EventSystem agar tombol UI bisa diklik.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
