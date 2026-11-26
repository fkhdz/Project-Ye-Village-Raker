using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DialoguePreviewCaptureMenu
{
    private const string PathKey = "YeVillage.DialoguePreview.Path";
    private const string ReturnSceneKey = "YeVillage.DialoguePreview.ReturnScene";
    private const string Level1Path = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string OutputPath = "Assets/Screenshots/DialogueThemePreview.png";

    static DialoguePreviewCaptureMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("YeVillage/Story/Capture Dialogue Preview")]
    public static void CapturePreview()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[DialoguePreview] Hentikan play mode dulu.");
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[DialoguePreview] Ada scene belum disimpan: " + open.name);
                return;
            }
        }

        if (!File.Exists(Level1Path))
        {
            Debug.LogError("[DialoguePreview] Scene Level 1 tidak ditemukan.");
            return;
        }

        EditorPrefs.SetString(ReturnSceneKey, SceneManager.GetActiveScene().path);
        EditorPrefs.SetString(PathKey, OutputPath);
        EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;

        string returnScene = EditorPrefs.GetString(ReturnSceneKey, string.Empty);
        if (string.IsNullOrEmpty(returnScene)) return;

        EditorPrefs.DeleteKey(ReturnSceneKey);
        if (File.Exists(returnScene))
        {
            EditorSceneManager.OpenScene(returnScene, OpenSceneMode.Single);
        }
    }
}
