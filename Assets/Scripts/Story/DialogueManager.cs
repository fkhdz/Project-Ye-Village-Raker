using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public DialogueSequence openingSequence;
    public DialogueSequence zone1Sequence;
    public DialogueSequence zone2Sequence;
    public DialogueSequence zone3Sequence;
    public Sprite portraitKepalaDesa;
    public Sprite portraitTetuaDesa;
    public Sprite bubbleSprite;
    public TMP_FontAsset fontAsset;

    public bool IsOpen { get; private set; }

    private const float BubbleWidth = 1560f;
    private const float BubbleFallbackHeight = 453f;
    private const string DialogueFontResourcePath = "Dialogue/Alegreya-Bold SDF";

    private static readonly Color SpeakerColor = new Color(0.97f, 0.90f, 0.72f);
    private static readonly Color BodyColor = new Color(0.96f, 0.94f, 0.83f);
    private static readonly Color HintColor = new Color(0.95f, 0.90f, 0.78f, 0.62f);
    private static readonly Color TextOutlineColor = new Color(0.045f, 0.06f, 0.10f, 1f);

    private GameObject root;
    private Image portraitKadesImage;
    private Image portraitTetuaImage;
    private Image bubbleImage;
    private TextMeshProUGUI speakerText;
    private TextMeshProUGUI bodyText;
    private GameObject continueIndicator;

    private DialogueSequence currentSequence;
    private int currentLine;
    private Action onComplete;
    private int lastAdvanceFrame = -1;
    private float openTime;

    private PlayerController playerController;
    private ThirdPersonCamera thirdPersonCamera;
    private Rigidbody playerBody;
    private Vector3 savedVelocity;
    private bool savedPlayerEnabled;
    private bool savedCameraEnabled;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private bool controlsCaptured;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUI();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Time.frameCount == lastAdvanceFrame) return;
        if (Time.unscaledTime - openTime < 0.2f) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            lastAdvanceFrame = Time.frameCount;
            Advance();
        }
    }

    private void OnDestroy()
    {
        if (IsOpen) RestoreControls();
        if (Instance == this) Instance = null;
    }

    public void Play(string sequenceId, Action completed)
    {
        if (IsOpen)
        {
            Debug.LogWarning("[DialogueManager] Sequence '" + sequenceId + "' diabaikan karena dialog aktif.");
            return;
        }

        DialogueSequence sequence = Resolve(sequenceId);
        if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
        {
            Debug.LogError("[DialogueManager] Sequence tidak ditemukan: " + sequenceId);
            completed?.Invoke();
            return;
        }

        EnsureUI();
        currentSequence = sequence;
        currentLine = 0;
        onComplete = completed;
        lastAdvanceFrame = Time.frameCount;
        openTime = Time.unscaledTime;
        IsOpen = true;
        root.SetActive(true);
        CaptureControls();
        RenderLine();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (root != null) root.SetActive(false);
        RestoreControls();

        Action callback = onComplete;
        onComplete = null;
        currentSequence = null;
        callback?.Invoke();
    }

    private DialogueSequence Resolve(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId)) return null;
        if (openingSequence != null && openingSequence.sequenceId == sequenceId) return openingSequence;
        if (zone1Sequence != null && zone1Sequence.sequenceId == sequenceId) return zone1Sequence;
        if (zone2Sequence != null && zone2Sequence.sequenceId == sequenceId) return zone2Sequence;
        if (zone3Sequence != null && zone3Sequence.sequenceId == sequenceId) return zone3Sequence;
        return Resources.Load<DialogueSequence>("Dialogue/" + sequenceId);
    }

    private void Advance()
    {
        if (currentSequence == null) return;

        if (currentLine < currentSequence.lines.Length - 1)
        {
            currentLine++;
            RenderLine();
            return;
        }

        Close();
    }

    private void RenderLine()
    {
        if (currentSequence == null) return;

        DialogueLine line = currentSequence.lines[currentLine];
        bool kades = line.speaker == DialogueSpeaker.KepalaDesa;
        speakerText.text = kades ? "Kepala Desa" : "Tetua Desa";
        bodyText.text = line.text;

        SetPortraitFocus(portraitKadesImage, kades, -430f);
        SetPortraitFocus(portraitTetuaImage, !kades, 430f);

        if (continueIndicator != null)
        {
            continueIndicator.SetActive(currentLine == currentSequence.lines.Length - 1);
        }
    }

    private void SetPortraitFocus(Image image, bool active, float baseX)
    {
        if (image == null) return;
        image.color = active ? Color.white : new Color(0.55f, 0.57f, 0.62f, 0.95f);
        RectTransform rect = image.rectTransform;
        rect.sizeDelta = active ? new Vector2(520f, 880f) : new Vector2(490f, 830f);
        rect.anchoredPosition = new Vector2(baseX, active ? -60f : -105f);
    }

    private void CaptureControls()
    {
        if (controlsCaptured) return;
        controlsCaptured = true;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerBody = player.GetComponent<Rigidbody>();
            if (playerController != null)
            {
                savedPlayerEnabled = playerController.enabled;
                playerController.enabled = false;
            }
            if (playerBody != null)
            {
                savedVelocity = playerBody.linearVelocity;
                playerBody.linearVelocity = Vector3.zero;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            thirdPersonCamera = mainCamera.GetComponent<ThirdPersonCamera>();
            if (thirdPersonCamera != null)
            {
                savedCameraEnabled = thirdPersonCamera.enabled;
                thirdPersonCamera.enabled = false;
            }
        }

        savedCursorLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestoreControls()
    {
        if (!controlsCaptured) return;
        controlsCaptured = false;

        if (playerController != null) playerController.enabled = savedPlayerEnabled;
        if (playerBody != null) playerBody.linearVelocity = savedVelocity;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = savedCameraEnabled;
        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;

        playerController = null;
        playerBody = null;
        thirdPersonCamera = null;
    }

    private void EnsureUI()
    {
        if (root != null) return;

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Dialogue_Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasObject;

        GameObject dim = CreateRect("Dim", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0.02f, 0.04f, 0.07f, 0.62f);
        dimImage.raycastTarget = true;

        portraitKadesImage = CreatePortrait("Portrait_KepalaDesa", root.transform);
        portraitKadesImage.sprite = portraitKepalaDesa;
        portraitTetuaImage = CreatePortrait("Portrait_TetuaDesa", root.transform);
        portraitTetuaImage.sprite = portraitTetuaDesa;

        float bubbleHeight = BubbleFallbackHeight;
        if (bubbleSprite != null && bubbleSprite.rect.width > 0f)
        {
            bubbleHeight = BubbleWidth * bubbleSprite.rect.height / bubbleSprite.rect.width;
        }

        GameObject panel = CreateRect("BubbleChat", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(BubbleWidth, bubbleHeight));
        panel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        bubbleImage = panel.AddComponent<Image>();
        bubbleImage.sprite = bubbleSprite;
        bubbleImage.color = bubbleSprite == null ? new Color(0.06f, 0.08f, 0.14f, 0.96f) : Color.white;
        bubbleImage.type = Image.Type.Simple;
        bubbleImage.raycastTarget = true;

        float spriteWidth = bubbleSprite != null && bubbleSprite.rect.width > 0f ? bubbleSprite.rect.width : 1789f;
        float spriteHeight = bubbleSprite != null && bubbleSprite.rect.height > 0f ? bubbleSprite.rect.height : 520f;
        float widthScale = BubbleWidth / spriteWidth;
        float heightScale = bubbleHeight / spriteHeight;

        speakerText = CreateText("SpeakerName", panel.transform, 36f, TextAlignmentOptions.Center,
            SpeakerColor, FontStyles.Normal, Vector2.zero, new Vector2(340f, 54f));
        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(0f, 1f);
        speakerRect.pivot = new Vector2(0.5f, 0.5f);
        speakerRect.anchoredPosition = new Vector2(281f * widthScale, -53f * heightScale);
        speakerText.textWrappingMode = TextWrappingModes.NoWrap;
        speakerText.overflowMode = TextOverflowModes.Overflow;
        speakerText.outlineWidth = 0.1f;
        speakerText.outlineColor = TextOutlineColor;
        speakerText.text = "Kepala Desa";

        bodyText = CreateText("DialogueText", panel.transform, 46f, TextAlignmentOptions.TopLeft,
            BodyColor, FontStyles.Normal, Vector2.zero, new Vector2(1414f, 285f));
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(0f, 1f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.anchoredPosition = new Vector2(66f * widthScale, -126f * heightScale);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.lineSpacing = -2f;
        bodyText.outlineWidth = 0.1f;
        bodyText.outlineColor = TextOutlineColor;

        continueIndicator = CreateRect("ContinueIndicator", panel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-120f, 34f), new Vector2(240f, 32f));
        continueIndicator.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
        TextMeshProUGUI indicatorText = CreateText("Label", continueIndicator.transform, 22f,
            TextAlignmentOptions.Right, HintColor, FontStyles.Normal, Vector2.zero, new Vector2(240f, 32f));
        indicatorText.outlineWidth = 0.08f;
        indicatorText.outlineColor = TextOutlineColor;
        indicatorText.text = "Klik / Spasi";

        root.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

    private Image CreatePortrait(string name, Transform parent)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, -60f), new Vector2(490f, 830f));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        Image image = go.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontAsset == null)
        {
            fontAsset = Resources.Load<TMP_FontAsset>(DialogueFontResourcePath);
        }

        return fontAsset;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment,
        Color color, FontStyles style, Vector2 anchoredPosition, Vector2 rectSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = rectSize;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveFont();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }
}
