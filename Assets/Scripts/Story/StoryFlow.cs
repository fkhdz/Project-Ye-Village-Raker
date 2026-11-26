using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryFlow : MonoBehaviour
{
    public static StoryFlow Instance { get; private set; }

    public DialogueManager dialogueManager;
    public string levelSceneName = "Level1_Zona1_Pinggiran";

    private GameObject tutorialRoot;
    private float tutorialOpenTime;

    public bool IsLevelExitAllowed
    {
        get { return StoryProgress.zone2GateUnlocked && StoryProgress.zone1FollowUpDone; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (dialogueManager == null) dialogueManager = DialogueManager.Instance;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == levelSceneName)
        {
            if (StoryProgress.openingRequested && !StoryProgress.openingDone)
            {
                StoryProgress.openingRequested = false;
                dialogueManager?.Play("Opening", OnOpeningComplete);
            }
            else if (StoryProgress.openingDone && !StoryProgress.tutorialDone)
            {
                StartTutorial();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!StoryProgress.tutorialPanelOpen) return;
        if (Time.unscaledTime - tutorialOpenTime < 0.35f) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) FinishTutorial();
    }

    private void OnOpeningComplete()
    {
        StoryProgress.openingDone = true;
        StartTutorial();
    }

    private void StartTutorial()
    {
        if (StoryProgress.tutorialDone || StoryProgress.tutorialPanelOpen) return;
        if (dialogueManager != null && dialogueManager.IsOpen) return;

        EnsureTutorialUI();
        StoryProgress.tutorialPanelOpen = true;
        tutorialOpenTime = Time.unscaledTime;
        tutorialRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void FinishTutorial()
    {
        if (!StoryProgress.tutorialPanelOpen) return;

        StoryProgress.tutorialPanelOpen = false;
        tutorialRoot.SetActive(false);
        StoryProgress.tutorialDone = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ShowFeedback("Kembali ke Tetua Desa di Pos TPS untuk menerima tugas.", new Color(1f, 0.86f, 0.46f), 5f);
    }

    public bool TryElderInteraction()
    {
        if (!StoryProgress.tutorialDone || StoryProgress.zone1FollowUpDone) return false;
        if (dialogueManager == null || dialogueManager.IsOpen) return false;

        StoryProgress.zone1FollowUpDone = true;
        UIManager.instance?.ClearInteractionPrompt("TetuaShop");
        dialogueManager.Play("Zona1", () => ShowFeedback("Kerjakan pemilahan sampah, lalu buka Gate Zona 2.",
            new Color(0.65f, 1f, 0.72f), 4f));
        return true;
    }

    public void TryZoneIntro(string sequenceId)
    {
        if (dialogueManager == null || dialogueManager.IsOpen) return;

        if (sequenceId == "Zona2")
        {
            if (!StoryProgress.zone2GateUnlocked || StoryProgress.zone2IntroDone) return;
            StoryProgress.zone2IntroDone = true;
            dialogueManager.Play("Zona2", () => ShowFeedback("Zona 2: waspadai sampah B3 tanpa sarung tangan.",
                new Color(1f, 0.78f, 0.52f), 4f));
            return;
        }

        if (sequenceId == "Zona3")
        {
            if (!StoryProgress.zone3GateUnlocked || StoryProgress.zone3IntroDone) return;
            StoryProgress.zone3IntroDone = true;
            dialogueManager.Play("Zona3", () => ShowFeedback("Zona 3: area Balai Desa. Bersihkan sampah yang menumpuk.",
                new Color(0.65f, 1f, 0.72f), 4f));
        }
    }

    public void UnlockGate(string gateId)
    {
        if (gateId == "Zone2") StoryProgress.zone2GateUnlocked = true;
        if (gateId == "Zone3") StoryProgress.zone3GateUnlocked = true;
    }

    private static void ShowFeedback(string message, Color color, float duration)
    {
        UIManager.instance?.ShowFeedback(message, color, duration);
    }

    private void EnsureTutorialUI()
    {
        if (tutorialRoot != null) return;

        GameObject canvasObject = new GameObject("Tutorial_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 280;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        tutorialRoot = canvasObject;

        GameObject dim = CreateRect("Dim", tutorialRoot.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0.01f, 0.03f, 0.05f, 0.78f);
        dimImage.raycastTarget = true;

        GameObject box = CreateRect("TutorialBox", tutorialRoot.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 520f));
        Image boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.05f, 0.1f, 0.13f, 0.98f);
        boxImage.raycastTarget = true;

        TMP_FontAsset font = dialogueManager != null ? dialogueManager.fontAsset : null;
        TextMeshProUGUI title = CreateText("Title", box.transform, 42f, TextAlignmentOptions.Center,
            new Color(1f, 0.85f, 0.45f), FontStyles.Bold, new Vector2(0f, 180f), new Vector2(960f, 70f), font);
        title.text = "PETUNJUK ZONA 1";

        TextMeshProUGUI body = CreateText("Body", box.transform, 34f, TextAlignmentOptions.Center,
            new Color(0.93f, 0.96f, 0.92f), FontStyles.Normal, new Vector2(0f, 0f), new Vector2(980f, 280f), font);
        body.text = "W A S D  = bergerak\nE  = ambil / buang sampah\nGerakkan mouse = mengatur kamera\n\nSelesaikan tutorial, lalu temui Tetua Desa.";
        body.textWrappingMode = TextWrappingModes.Normal;

        TextMeshProUGUI footer = CreateText("Footer", box.transform, 26f, TextAlignmentOptions.Center,
            new Color(1f, 0.8f, 0.42f), FontStyles.Bold, new Vector2(0f, -195f), new Vector2(900f, 50f), font);
        footer.text = "[Spasi / Klik] Mengerti";

        tutorialRoot.SetActive(false);
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment,
        Color color, FontStyles style, Vector2 anchoredPosition, Vector2 rectSize, TMP_FontAsset font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = rectSize;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }
}
