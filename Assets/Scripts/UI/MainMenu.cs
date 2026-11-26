using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu utama (sesuai dokumen 09). Dipasang pada Canvas scene MainMenu (build index 0).
/// UI dibuat otomatis saat runtime agar tidak perlu setup manual di Inspector.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Panel Petunjuk")]
    public GameObject tutorialPanel;

    [Header("Scene Gameplay")]
    [Tooltip("Nama scene gameplay yang dimuat saat tombol MULAI BERMAIN ditekan.")]
    public string gameplaySceneName = "Level1_Zona1_Pinggiran";

    private void Start()
    {
        // Pastikan state dari sesi sebelumnya tidak terbawa.
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (tutorialPanel == null)
        {
            BuildMenuUI();
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StoryProgress.Reset();
        StoryProgress.openingRequested = true;

        // Muat berdasarkan nama scene, JANGAN berdasarkan buildIndex scene aktif.
        // Saat diuji di Editor, scene aktif bisa SampleScene (index 1) sehingga
        // "buildIndex + 1" menjadi 2 (di luar Build Settings) dan tombol tampak
        // tidak merespon. Nama scene bekerja di Editor maupun build.
        string targetScene = ResolveGameplaySceneName();

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[MainMenu] Scene gameplay belum terdaftar di Build Settings.");
            return;
        }

        Debug.Log("[MainMenu] Memuat scene gameplay: " + targetScene);
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    /// <summary>
    /// Cari nama scene gameplay: utamakan nama eksplisit, lalu fallback ke scene
    /// tepat setelah MainMenu pada urutan Build Settings.
    /// </summary>
    private string ResolveGameplaySceneName()
    {
        if (!string.IsNullOrEmpty(gameplaySceneName) &&
            Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            return gameplaySceneName;
        }

        int mainMenuIndex = -1;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrEmpty(path) &&
                path.EndsWith("/MainMenu.unity", System.StringComparison.OrdinalIgnoreCase))
            {
                mainMenuIndex = i;
                break;
            }
        }

        if (mainMenuIndex >= 0)
        {
            int nextIndex = mainMenuIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(nextIndex);
                if (!string.IsNullOrEmpty(path))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(path);
                }
            }
        }

        return null;
    }

    public void OpenTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Game Keluar");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenuUI()
    {
        EnsureEventSystem();

        Text title = CreateText(transform, "Title", "YE VILLAGE RAKER", 84,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(1300f, 130f));
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.88f, 0.45f);

        Text subtitle = CreateText(transform, "Subtitle", "Kades Terbuang: Penebusan Desa", 30,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(900f, 50f));
        subtitle.color = new Color(0.94f, 0.97f, 0.92f);

        Button playButton = CreateButton(transform, "PlayButton", "MULAI BERMAIN", 30,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(460f, 72f));
        playButton.onClick.AddListener(PlayGame);

        Button tutorialButton = CreateButton(transform, "TutorialButton", "PETUNJUK", 26,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(460f, 64f));
        tutorialButton.onClick.AddListener(OpenTutorial);

        Button quitButton = CreateButton(transform, "QuitButton", "KELUAR", 26,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -192f), new Vector2(460f, 64f));
        quitButton.onClick.AddListener(QuitGame);

        Text footer = CreateText(transform, "Footer", "Tugas Besar Komputer Grafis - Pemilahan Sampah", 18,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(900f, 30f));
        footer.color = new Color(0.85f, 0.9f, 0.85f);

        tutorialPanel = BuildTutorialPanel();
    }

    private GameObject BuildTutorialPanel()
    {
        GameObject panel = new GameObject("TutorialPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.86f);

        GameObject box = new GameObject("TutorialBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);

        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(980f, 680f);

        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.06f, 0.13f, 0.09f, 0.98f);

        Text title = CreateText(box.transform, "TutorialTitle", "PETUNJUK PEMILAHAN", 42,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(900f, 60f));
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.88f, 0.45f);

        Text body = CreateText(box.transform, "TutorialBody",
            "Cara Bermain:\n\n" +
            "1. Dekati sampah, lalu tekan [E] untuk memungut.\n" +
            "2. Buang sampah ke tong yang sesuai:\n" +
            "     HIJAU = Organik (sisa makanan, daun)\n" +
            "     KUNING = Anorganik (plastik, kardus)\n" +
            "     MERAH = B3 (baterai, limbah kimia)\n" +
            "3. Kumpulkan 1500 poin, lalu beli Renovasi Balai Desa\n" +
            "     di Toko Tetua untuk memenangkan permainan.\n\n" +
            "Salah tong = -5 poin. Tidak ada game over.",
            24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.50f), new Vector2(0f, -10f), new Vector2(900f, 430f));
        body.color = new Color(0.92f, 0.96f, 0.9f);

        Button closeButton = CreateButton(box.transform, "CloseButton", "TUTUP", 24,
            new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(260f, 58f));
        closeButton.onClick.AddListener(CloseTutorial);

        return panel;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize,
        TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        }

        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, int fontSize,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.16f, 0.42f, 0.26f, 1f);

        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = CreateText(buttonObj.transform, "Label", label, fontSize, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        return button;
    }
}
