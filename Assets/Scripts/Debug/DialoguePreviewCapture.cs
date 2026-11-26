using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

public class DialoguePreviewCapture : MonoBehaviour
{
#if UNITY_EDITOR
    private const string PathKey = "YeVillage.DialoguePreview.Path";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        string path = UnityEditor.EditorPrefs.GetString(PathKey, string.Empty);
        if (string.IsNullOrEmpty(path)) return;

        UnityEditor.EditorPrefs.DeleteKey(PathKey);
        StoryProgress.openingRequested = true;

        GameObject captureObject = new GameObject("DialoguePreviewCapture");
        DontDestroyOnLoad(captureObject);
        DialoguePreviewCapture capture = captureObject.AddComponent<DialoguePreviewCapture>();
        capture.outputPath = path;
    }

    private string outputPath;

    private IEnumerator Start()
    {
        float timeout = 8f;
        while (DialogueManager.Instance == null || !DialogueManager.Instance.IsOpen)
        {
            timeout -= Time.unscaledDeltaTime;
            if (timeout <= 0f)
            {
                Debug.LogWarning("[DialoguePreviewCapture] Dialog tidak muncul dalam batas waktu.");
                Finish();
                yield break;
            }
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.75f);
        Capture(outputPath);

        yield return new WaitForSecondsRealtime(0.75f);
        AdvanceToLastLine();

        yield return new WaitForSecondsRealtime(0.5f);
        Capture(Path.Combine(Path.GetDirectoryName(outputPath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(outputPath) + "Last" + Path.GetExtension(outputPath)));

        yield return new WaitForSecondsRealtime(1f);
        Finish();
    }

    private static void Capture(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        ScreenCapture.CaptureScreenshot(fullPath);
        Debug.Log("[DialoguePreviewCapture] Screenshot disimpan: " + fullPath);
    }

    private static void AdvanceToLastLine()
    {
        DialogueManager manager = DialogueManager.Instance;
        if (manager == null) return;

        FieldInfo lineField = typeof(DialogueManager).GetField("currentLine", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo sequenceField = typeof(DialogueManager).GetField("currentSequence", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo advance = typeof(DialogueManager).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);
        if (lineField == null || sequenceField == null || advance == null) return;

        DialogueSequence sequence = sequenceField.GetValue(manager) as DialogueSequence;
        if (sequence == null || sequence.lines == null) return;

        int guard = 0;
        while (manager.IsOpen && (int)lineField.GetValue(manager) < sequence.lines.Length - 1 && guard < 30)
        {
            advance.Invoke(manager, null);
            guard++;
        }
    }

    private void Finish()
    {
        UnityEditor.EditorApplication.isPlaying = false;
    }
#endif
}
