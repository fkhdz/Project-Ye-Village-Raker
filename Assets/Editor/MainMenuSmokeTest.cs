using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Diagnostik sementara: menguji jalur tombol MULAI BERMAIN (MainMenu.PlayGame)
/// dari dalam play mode tanpa perlu klik mouse.
/// </summary>
public static class MainMenuSmokeTest
{
    [MenuItem("Tools/Quaternius/16. Test MainMenu Start")]
    public static void TestStart()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[SMOKE] Not in play mode");
            return;
        }

        MainMenu menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        EventSystemState();

        Debug.Log($"[SMOKE] before: active={SceneManager.GetActiveScene().name} loaded={SceneManager.loadedSceneCount} menu={(menu != null)} buttons={buttons.Length} timeScale={Time.timeScale}");

        if (menu == null)
        {
            Debug.LogError("[SMOKE] MainMenu tidak ditemukan di scene yang dimuat.");
            return;
        }

        menu.PlayGame();

        Debug.Log($"[SMOKE] after: active={SceneManager.GetActiveScene().name} loaded={SceneManager.loadedSceneCount} timeScale={Time.timeScale}");
    }

    private static void EventSystemState()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null)
        {
            Debug.LogError("[SMOKE] EventSystem.current = null (UI tidak akan menerima input).");
            return;
        }

        var modules = es.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
        Debug.Log($"[SMOKE] EventSystem '{es.name}' modules={modules.Length} firstSelected={(es.firstSelectedGameObject != null ? es.firstSelectedGameObject.name : "none")}");
        foreach (var m in modules)
        {
            Debug.Log($"[SMOKE]   module={m.GetType().Name} enabled={m.enabled}");
        }
    }
}
