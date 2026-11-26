using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu jeda gameplay (sesuai dokumen 09).
/// Dipasang pada Canvas_HUD di scene gameplay (build index 1).
/// UI dibuat otomatis saat runtime agar listener tombol selalu valid.
/// Catatan Build Settings: MainMenu = index 0, SampleScene (gameplay) = index 1.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("Panel Pause")]
    public GameObject pauseMenuPanel;

    private void Start()
    {
        // Pastikan state dari sesi sebelumnya tidak terbawa.
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel == null)
        {
            BuildPauseUI();
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (StoryProgress.IsGameplayInputBlocked) return;

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        // Saat victory, ESC dipakai GameManager untuk keluar dari game.
        if (GameManager.instance != null && GameManager.instance.IsVictory)
        {
            return;
        }

        if (isPaused)
        {
            Resume();
            return;
        }

        // Saat toko terbuka, ESC dipakai ShopManager untuk menutup toko.
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop != null && shop.IsShopOpen)
        {
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (pauseMenuPanel == null)
        {
            BuildPauseUI();
        }

        if (pauseMenuPanel == null)
        {
            Debug.LogError("[PauseMenu] Panel pause tidak ditemukan dan gagal dibuat.");
            return;
        }

        pauseMenuPanel.SetActive(true);
        pauseMenuPanel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        // timeScale wajib dikembalikan sebelum reload agar game tidak diam.
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(0);
    }

    private void BuildPauseUI()
    {
        EnsureEventSystem();

        GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        GameObject box = new GameObject("PauseBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(panel.transform, false);

        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(760f, 560f);

        Image boxImage = box.GetComponent<Image>();
        boxImage.color = new Color(0.06f, 0.13f, 0.09f, 0.98f);

        Text title = CreateText(box.transform, "PauseTitle", "GAME DIJEDA", 54,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(700f, 80f));
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, 0.88f, 0.45f);

        Button resumeButton = CreateButton(box.transform, "ResumeButton", "LANJUTKAN", 28,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(420f, 68f));
        resumeButton.onClick.AddListener(Resume);

        Button restartButton = CreateButton(box.transform, "RestartButton", "ULANGI PERMAINAN", 24,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -68f), new Vector2(420f, 62f));
        restartButton.onClick.AddListener(RestartGame);

        Button menuButton = CreateButton(box.transform, "MenuButton", "MENU UTAMA", 24,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -148f), new Vector2(420f, 62f));
        menuButton.onClick.AddListener(LoadMainMenu);

        Text hint = CreateText(box.transform, "PauseHint", "Tekan ESC untuk melanjutkan", 20,
            TextAnchor.MiddleCenter, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(600f, 36f));
        hint.color = new Color(0.84f, 0.9f, 0.84f);

        pauseMenuPanel = panel;
        panel.SetActive(false);
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
