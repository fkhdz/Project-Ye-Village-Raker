using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD pemain: menampilkan poin dan kapasitas tas (sesuai dokumen 07).
/// Dipasang pada Canvas utama atau GameObject kosong dengan Canvas runtime.
/// Menggantikan ScoreHUD pratinjau Fase 6.
/// Modul 13: Upgrade HUD, Interaction Prompt, dan Toast Feedback.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Referensi UI")]
    public Text scoreText;
    public Text capacityText;

    [Header("Referensi Player")]
    public TrashInventory playerInventory;

    // --- Fitur 1: HUD Panel Kiri-Atas ---
    private GameObject hudPanel;
    private Text hudScoreText;
    private Image progressBarFill;
    private Text hudCapacityText;
    private Text hudTopTrashText;
    private Text progressHintText;
    private GameObject b3WarningPanel;
    private Text b3WarningText;
    private PlayerController playerController;

    // --- Fitur 2: Interaction Prompt ---
    private GameObject promptPanel;
    private Text promptText;
    private Dictionary<string, string> activePrompts = new Dictionary<string, string>();
    private string lastPromptKey = "";

    // --- Fitur 3: Toast Feedback ---
    private GameObject toastPanel;
    private Text toastText;
    private CanvasGroup toastCanvasGroup;
    private Outline toastOutline;
    private Coroutine toastCoroutine;

    // --- Canvas runtime ---
    private Canvas runtimeCanvas;

    // --- Referensi ShopManager ---
    private ShopManager shopManagerRef;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<TrashInventory>();
                playerController = player.GetComponent<PlayerController>();
            }
        }
        else
        {
            playerController = playerInventory.GetComponent<PlayerController>();
        }

        shopManagerRef = Object.FindFirstObjectByType<ShopManager>();

        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (capacityText != null) capacityText.gameObject.SetActive(false);

        BuildRuntimeCanvas();
        BuildHUDPanel();
        BuildPromptPanel();
        BuildToastPanel();
    }

    private void Update()
    {
        UpdateHUD();
        UpdatePromptVisibility();
    }

    // ================================================================
    //  FITUR 1: HUD PANEL KIRI-ATAS
    // ================================================================

    private void UpdateHUD()
    {
        if (ScoreManager.instance != null)
        {
            int poin = ScoreManager.instance.currentPoin;
            if (hudScoreText != null)
                hudScoreText.text = "Poin: " + poin + " / 1500";

            if (progressBarFill != null)
            {
                float ratio = Mathf.Clamp01(poin / 1500f);
                progressBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                progressBarFill.color = poin >= 1500
                    ? HexColor("#FFC107")
                    : HexColor("#4CAF50");
            }

            if (progressHintText != null)
            {
                progressHintText.gameObject.SetActive(poin >= 1500);
            }
        }

        if (playerInventory != null)
        {
            if (hudCapacityText != null)
                hudCapacityText.text = "Tas: " + playerInventory.carriedTrash.Count + " / " + playerInventory.maxCapacity;

            if (hudTopTrashText != null)
            {
                if (playerInventory.carriedTrash.Count > 0)
                {
                    string top = playerInventory.carriedTrash[playerInventory.carriedTrash.Count - 1];
                    hudTopTrashText.text = "Siap Buang: [" + top + "]";
                    hudTopTrashText.color = GetTrashColor(top);
                }
                else
                {
                    hudTopTrashText.text = "Siap Buang: -";
                    hudTopTrashText.color = HexColor("#B0BEC5");
                }
            }
        }

        if (b3WarningPanel != null)
        {
            bool showWarning = playerController != null && playerController.isSlowed;
            b3WarningPanel.SetActive(showWarning);
        }
    }

    // ================================================================
    //  FITUR 2: INTERACTION PROMPT
    // ================================================================

    public void SetInteractionPrompt(string sourceId, string message)
    {
        activePrompts[sourceId] = message;
        lastPromptKey = sourceId;
        RefreshPrompt();
    }

    public void ClearInteractionPrompt(string sourceId)
    {
        activePrompts.Remove(sourceId);
        if (lastPromptKey == sourceId)
            lastPromptKey = "";
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptPanel == null || promptText == null) return;

        if (activePrompts.Count == 0)
        {
            promptPanel.SetActive(false);
            return;
        }

        string msg = "";
        if (!string.IsNullOrEmpty(lastPromptKey) && activePrompts.ContainsKey(lastPromptKey))
            msg = activePrompts[lastPromptKey];
        else
        {
            foreach (var kv in activePrompts) { msg = kv.Value; lastPromptKey = kv.Key; break; }
        }

        promptText.text = msg;
        promptPanel.SetActive(true);
    }

    private void UpdatePromptVisibility()
    {
        if (promptPanel == null) return;

        bool shouldHide = PauseMenu.isPaused
            || (GameManager.instance != null && GameManager.instance.IsVictory)
            || (shopManagerRef != null && shopManagerRef.IsShopOpen);

        if (shouldHide && promptPanel.activeSelf)
            promptPanel.SetActive(false);
        else if (!shouldHide && activePrompts.Count > 0 && !promptPanel.activeSelf)
            promptPanel.SetActive(true);
    }

    // ================================================================
    //  FITUR 3: TOAST FEEDBACK
    // ================================================================

    public void ShowFeedback(string message, Color color, float duration = 1.8f)
    {
        if (toastText == null || toastCanvasGroup == null) return;

        if (toastCoroutine != null)
            StopCoroutine(toastCoroutine);

        toastText.text = message;
        toastText.color = color;

        // Modul 14: panel toast minimum 520x54, melebar otomatis untuk pesan panjang.
        RectTransform toastRect = toastPanel.GetComponent<RectTransform>();
        if (toastRect != null)
        {
            float width = Mathf.Max(520f, toastText.preferredWidth + 56f);
            toastRect.sizeDelta = new Vector2(width, 54f);
        }

        if (toastOutline != null)
            toastOutline.effectColor = color;

        toastCanvasGroup.alpha = 1f;
        toastPanel.SetActive(true);

        toastCoroutine = StartCoroutine(FadeOutToast(duration));
    }

    private IEnumerator FadeOutToast(float duration)
    {
        float holdTime = duration * 0.6f;
        float fadeTime = duration * 0.4f;

        // Gunakan waktu realtime agar toast tetap memudar saat game di-pause / victory.
        yield return new WaitForSecondsRealtime(holdTime);

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            toastCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }

        toastCanvasGroup.alpha = 0f;
        toastPanel.SetActive(false);
        toastCoroutine = null;
    }

    // ================================================================
    //  BUILD UI RUNTIME
    // ================================================================

    private void BuildRuntimeCanvas()
    {
        GameObject existing = GameObject.Find("UIManager_Canvas");
        if (existing != null)
        {
            runtimeCanvas = existing.GetComponent<Canvas>();
            CanvasScaler existingScaler = existing.GetComponent<CanvasScaler>();
            if (existingScaler == null)
                existingScaler = existing.AddComponent<CanvasScaler>();
            ConfigureCanvasScaler(existingScaler);
            return;
        }

        GameObject canvasObj = new GameObject("UIManager_Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        ConfigureCanvasScaler(canvasObj.GetComponent<CanvasScaler>());

        runtimeCanvas = canvas;
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void BuildHUDPanel()
    {
        if (runtimeCanvas == null) return;

        hudPanel = new GameObject("HUD_Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        hudPanel.transform.SetParent(runtimeCanvas.transform, false);
        RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(28f, -28f);
        hudRect.sizeDelta = new Vector2(390f, 175f);

        Image hudBg = hudPanel.GetComponent<Image>();
        hudBg.color = new Color(0.06f, 0.09f, 0.13f, 0.84f);

        Outline hudOutline = hudPanel.GetComponent<Outline>();
        hudOutline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        hudOutline.effectDistance = new Vector2(2f, -2f);

        // Poin text
        hudScoreText = CreateUIText(hudPanel.transform, "HUD_Score", "Poin: 0 / 1500",
            new Vector2(0f, 1f), new Vector2(20f, -12f), new Vector2(350f, 40f), 28, TextAnchor.MiddleLeft);
        hudScoreText.fontStyle = FontStyle.Bold;
        hudScoreText.color = HexColor("#FFD54F");
        Shadow scoreShadow = hudScoreText.GetComponent<Shadow>();
        if (scoreShadow != null)
            scoreShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // Progress bar background
        GameObject progressBg = new GameObject("ProgressBG", typeof(RectTransform), typeof(Image));
        progressBg.transform.SetParent(hudPanel.transform, false);
        RectTransform pbgRect = progressBg.GetComponent<RectTransform>();
        pbgRect.anchorMin = new Vector2(0f, 1f);
        pbgRect.anchorMax = new Vector2(0f, 1f);
        pbgRect.pivot = new Vector2(0f, 1f);
        pbgRect.anchoredPosition = new Vector2(20f, -58f);
        pbgRect.sizeDelta = new Vector2(350f, 12f);
        progressBg.GetComponent<Image>().color = HexColor("#1E293B");

        // Progress bar fill
        GameObject progressFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        progressFill.transform.SetParent(progressBg.transform, false);
        RectTransform pfRect = progressFill.GetComponent<RectTransform>();
        pfRect.anchorMin = Vector2.zero;
        pfRect.anchorMax = new Vector2(0f, 1f);
        pfRect.offsetMin = Vector2.zero;
        pfRect.offsetMax = Vector2.zero;
        progressBarFill = progressFill.GetComponent<Image>();
        progressBarFill.color = HexColor("#4CAF50");

        // Progress hint
        progressHintText = CreateUIText(hudPanel.transform, "HUD_ProgressHint", "\u2605 Siap Renovasi Balai Desa!",
            new Vector2(0f, 1f), new Vector2(20f, -74f), new Vector2(350f, 22f), 16, TextAnchor.MiddleLeft);
        progressHintText.color = HexColor("#FFD54F");
        progressHintText.fontStyle = FontStyle.Bold;
        progressHintText.gameObject.SetActive(false);

        // Kapasitas tas
        hudCapacityText = CreateUIText(hudPanel.transform, "HUD_Capacity", "Tas: 0 / 1",
            new Vector2(0f, 1f), new Vector2(20f, -100f), new Vector2(350f, 30f), 22, TextAnchor.MiddleLeft);
        hudCapacityText.color = Color.white;
        hudCapacityText.fontStyle = FontStyle.Bold;

        // Sampah teratas
        hudTopTrashText = CreateUIText(hudPanel.transform, "HUD_TopTrash", "Siap Buang: -",
            new Vector2(0f, 1f), new Vector2(20f, -132f), new Vector2(350f, 30f), 21, TextAnchor.MiddleLeft);
        hudTopTrashText.color = HexColor("#B0BEC5");
        hudTopTrashText.fontStyle = FontStyle.Bold;

        // B3 Warning panel
        b3WarningPanel = new GameObject("B3Warning", typeof(RectTransform), typeof(Image));
        b3WarningPanel.transform.SetParent(hudPanel.transform, false);
        RectTransform warnRect = b3WarningPanel.GetComponent<RectTransform>();
        warnRect.anchorMin = new Vector2(0f, 0f);
        warnRect.anchorMax = new Vector2(1f, 0f);
        warnRect.pivot = new Vector2(0.5f, 1f);
        warnRect.anchoredPosition = new Vector2(0f, -4f);
        warnRect.sizeDelta = new Vector2(0f, 32f);
        b3WarningPanel.GetComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.85f);

        Text warnText = CreateUIText(b3WarningPanel.transform, "B3WarnText",
            "\u26A0 BAWA B3: Gerak Lambat (-40%)! Beli Sarung Tangan di Toko",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(370f, 28f), 13, TextAnchor.MiddleCenter);
        warnText.color = Color.white;
        warnText.fontStyle = FontStyle.Bold;
        b3WarningText = warnText;
        b3WarningPanel.SetActive(false);
    }

    private void BuildPromptPanel()
    {
        if (runtimeCanvas == null) return;

        promptPanel = new GameObject("Prompt_Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        promptPanel.transform.SetParent(runtimeCanvas.transform, false);
        RectTransform prRect = promptPanel.GetComponent<RectTransform>();
        prRect.anchorMin = new Vector2(0.5f, 0f);
        prRect.anchorMax = new Vector2(0.5f, 0f);
        prRect.pivot = new Vector2(0.5f, 0f);
        prRect.anchoredPosition = new Vector2(0f, 194f);
        prRect.sizeDelta = new Vector2(480f, 52f);

        Image prBg = promptPanel.GetComponent<Image>();
        prBg.color = new Color(10f / 255f, 15f / 255f, 22f / 255f, 0.88f);

        Outline prOutline = promptPanel.GetComponent<Outline>();
        prOutline.effectColor = new Color(1f, 0.84f, 0.25f, 0.55f);
        prOutline.effectDistance = new Vector2(1.5f, -1.5f);

        promptText = CreateUIText(promptPanel.transform, "PromptText", "",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(452f, 44f), 24, TextAnchor.MiddleCenter);
        promptText.color = HexColor("#FFF59D");
        promptText.fontStyle = FontStyle.Bold;

        promptPanel.SetActive(false);
    }

    private void BuildToastPanel()
    {
        if (runtimeCanvas == null) return;

        toastPanel = new GameObject("Toast_Panel", typeof(RectTransform), typeof(CanvasGroup));
        toastPanel.transform.SetParent(runtimeCanvas.transform, false);
        RectTransform tRect = toastPanel.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -42f);
        tRect.sizeDelta = new Vector2(520f, 54f);

        toastCanvasGroup = toastPanel.GetComponent<CanvasGroup>();
        toastCanvasGroup.alpha = 0f;
        toastCanvasGroup.blocksRaycasts = false;
        toastCanvasGroup.interactable = false;

        GameObject bgObj = new GameObject("ToastBG", typeof(RectTransform), typeof(Image), typeof(Outline));
        bgObj.transform.SetParent(toastPanel.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.90f);

        toastOutline = bgObj.GetComponent<Outline>();
        toastOutline.effectColor = new Color(1f, 0.84f, 0.25f, 0.9f);
        toastOutline.effectDistance = new Vector2(2f, -2f);

        toastText = CreateUIText(bgObj.transform, "ToastText", "",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(464f, 30f), 24, TextAnchor.MiddleCenter);
        toastText.color = Color.white;
        toastText.fontStyle = FontStyle.Bold;
        toastText.verticalOverflow = VerticalWrapMode.Overflow;

        toastPanel.SetActive(false);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private static Text CreateUIText(Transform parent, string name, string content,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        Shadow shadow = obj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);

        return text;
    }

    public static Color GetTrashColor(string tag)
    {
        switch (tag)
        {
            case "Organik": return HexColor("#81C784");
            case "Anorganik": return HexColor("#FFF176");
            case "B3": return HexColor("#FF5252");
            default: return Color.white;
        }
    }

    private static Color HexColor(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hex, out c))
            return c;
        return Color.white;
    }
}
