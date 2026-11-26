using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoryProgress
{
    public static bool openingRequested = false;
    public static bool openingDone = false;
    public static bool tutorialDone = false;
    public static bool tutorialPanelOpen = false;
    public static bool zone1FollowUpDone = false;
    public static bool zone2GateUnlocked = false;
    public static bool zone2IntroDone = false;
    public static bool zone3GateUnlocked = false;
    public static bool zone3IntroDone = false;

    public static bool IsGameplayInputBlocked
    {
        get
        {
            if (tutorialPanelOpen) return true;
            DialogueManager dialogue = DialogueManager.Instance;
            return dialogue != null && dialogue.IsOpen;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name == "MainMenu") Reset();
    }

    public static void Reset()
    {
        openingRequested = false;
        openingDone = false;
        tutorialDone = false;
        tutorialPanelOpen = false;
        zone1FollowUpDone = false;
        zone2GateUnlocked = false;
        zone2IntroDone = false;
        zone3GateUnlocked = false;
        zone3IntroDone = false;
    }
}
