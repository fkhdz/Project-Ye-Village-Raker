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

    [Header("Pengaturan Target Poin & Goal (Per Zona)")]
    [Tooltip("Target poin yang ingin dicapai pada zona ini (default 1500 untuk map akhir, 250 untuk Level 1).")]
    public int targetPoin = 1500;
    [Tooltip("Tampilkan format 'Poin: X / Target' jika true, atau hanya 'Poin: X' jika false.")]
    public bool showTotalGoal = true;
    [Tooltip("Pesan hint saat target poin tercapai. Kosongkan untuk pesan default sesuai target.")]
    public string targetReachedHint = "";

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

    // --- Modul 18A: HUD Antrean Tas (BUG-05) ---
    private GameObject queueRow;
    private RectTransform queueRowRect;
    private Text queueTitleText;
    private Text queueReadyMarker;
    private readonly List<Image> queueSlotBackgrounds = new List<Image>();
    private readonly List<Outline> queueSlotOutlines = new List<Outline>();
    private readonly List<Text> queueSlotLabels = new List<Text>();
    private readonly List<TrashInventory.TrashQueueSlotInfo> queueBuffer =
        new List<TrashInventory.TrashQueueSlotInfo>();
    private int builtQueueCapacity = -1;
    private bool inventoryEventBound;

    // Tinggi panel HUD diperbesar agar deretan 7 slot tidak menimpa banner B3.
    private const float HudPanelHeight = 256f;
    private const float QueueSlotSize = 40f;
    private const float QueueReadySlotSize = 44f;
    private const float QueueSlotSpacing = 5f;
    private bool queueDirty = true;

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

        // Modul 18A (BUG-05): HUD antrean hanya diperbarui saat isi tas berubah.
        if (playerInventory != null && !inventoryEventBound)
        {
            playerInventory.OnInventoryChanged += HandleInventoryChanged;
            inventoryEventBound = true;
        }

        queueDirty = true;
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
            {
                if (showTotalGoal)
                    hudScoreText.text = "Poin: " + poin + " / " + targetPoin;
                else
                    hudScoreText.text = "Poin: " + poin;
            }

            if (progressBarFill != null)
            {
                float ratio = targetPoin > 0 ? Mathf.Clamp01((float)poin / targetPoin) : 1f;
                progressBarFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                progressBarFill.color = poin >= targetPoin
                    ? HexColor("#FFD54F")
                    : HexColor("#66BB6A");
            }

            if (progressHintText != null)
            {
                string hint = !string.IsNullOrEmpty(targetReachedHint)
                    ? targetReachedHint
                    : (targetPoin >= 1500 ? "Siap Renovasi Balai Desa!" : "Target Poin Zona Tercapai!");
                progressHintText.text = hint;
                progressHintText.gameObject.SetActive(poin >= targetPoin);
            }
        }

        if (playerInventory != null)
        {
            if (hudCapacityText != null)
                hudCapacityText.text = "Tas: " + playerInventory.carriedTrash.Count + " / " + playerInventory.maxCapacity;

            // Kapasitas berubah (beli Kantong/Gerobak) tanpa event -> picu rebuild.
            if (playerInventory.maxCapacity != builtQueueCapacity)
                queueDirty = true;
        }

        // Modul 18A (BUG-05): 0 alokasi per frame, hanya saat ada perubahan.
        if (queueDirty)
        {
            queueDirty = false;
            RefreshQueueRow();
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
        hudRect.sizeDelta = new Vector2(390f, HudPanelHeight);

        Image hudBg = hudPanel.GetComponent<Image>();
        hudBg.color = new Color(0.06f, 0.09f, 0.13f, 0.84f);

        Outline hudOutline = hudPanel.GetComponent<Outline>();
        hudOutline.effectColor = new Color(1f, 1f, 1f, 0.18f);
        hudOutline.effectDistance = new Vector2(2f, -2f);

        // Poin text
        string initialScore = showTotalGoal ? ("Poin: 0 / " + targetPoin) : "Poin: 0";
        hudScoreText = CreateUIText(hudPanel.transform, "HUD_Score", initialScore,
            new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(352f, 32f), 22, TextAnchor.MiddleLeft);
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

        // Sampah teratas (siap buang) - Modul 18A: kini menampilkan nama + kategori
        hudTopTrashText = CreateUIText(hudPanel.transform, "HUD_TopTrash", "Siap Buang: -",
            new Vector2(0f, 1f), new Vector2(20f, -132f), new Vector2(350f, 26f), 20, TextAnchor.MiddleLeft);
        hudTopTrashText.color = HexColor("#B0BEC5");
        hudTopTrashText.fontStyle = FontStyle.Bold;

        // Modul 18A (BUG-05): judul baris antrean tas
        queueTitleText = CreateUIText(hudPanel.transform, "HUD_QueueTitle", "Antrean Tas (Urutan Buang):",
            new Vector2(0f, 1f), new Vector2(20f, -160f), new Vector2(350f, 18f), 14, TextAnchor.MiddleLeft);
        queueTitleText.color = HexColor("#90A4AE");
        queueTitleText.fontStyle = FontStyle.Bold;

        BuildQueueRow();
        BuildQueueReadyMarker();

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

    private void BuildQueueRow()
    {
        if (hudPanel == null) return;

        queueRow = new GameObject("HUD_QueueRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        queueRow.transform.SetParent(hudPanel.transform, false);
        queueRowRect = queueRow.GetComponent<RectTransform>();
        queueRowRect.anchorMin = new Vector2(0f, 1f);
        queueRowRect.anchorMax = new Vector2(0f, 1f);
        queueRowRect.pivot = new Vector2(0f, 1f);
        queueRowRect.anchoredPosition = new Vector2(20f, -182f);
        queueRowRect.sizeDelta = new Vector2(350f, 48f);

        HorizontalLayoutGroup layout = queueRow.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = QueueSlotSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        builtQueueCapacity = -1;
        BuildQueueSlots(1);
    }

    private void BuildQueueReadyMarker()
    {
        if (hudPanel == null) return;

        queueReadyMarker = CreateUIText(hudPanel.transform, "HUD_QueueReadyMarker", "\u25B2",
            new Vector2(0f, 1f), new Vector2(20f, -232f), new Vector2(48f, 14f), 13, TextAnchor.MiddleCenter);
        queueReadyMarker.color = HexColor("#FFF59D");
        queueReadyMarker.fontStyle = FontStyle.Bold;
        queueReadyMarker.gameObject.SetActive(false);
    }

    /// <summary>
    /// Modul 18A: bangun ulang deretan slot sesuai kapasitas (1/3/7).
    /// Hanya dipanggil saat kapasitas berubah, bukan setiap frame.
    /// </summary>
    private void BuildQueueSlots(int capacity)
    {
        if (queueRowRect == null) return;

        for (int i = queueRowRect.childCount - 1; i >= 0; i--)
        {
            Transform child = queueRowRect.GetChild(i);
            // Lepas langsung agar tidak terhitung sebagai slot selama frame ini.
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        queueSlotBackgrounds.Clear();
        queueSlotOutlines.Clear();
        queueSlotLabels.Clear();

        for (int i = 0; i < capacity; i++)
        {
            GameObject slot = new GameObject("QueueSlot_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Outline), typeof(LayoutElement));
            slot.transform.SetParent(queueRowRect, false);

            Image bg = slot.GetComponent<Image>();
            bg.color = new Color(0x2C / 255f, 0x31 / 255f, 0x3A / 255f, 0.65f);

            Outline outline = slot.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
            outline.effectDistance = new Vector2(1f, -1f);

            LayoutElement layoutElement = slot.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = QueueSlotSize;
            layoutElement.preferredHeight = QueueSlotSize;

            Text label = CreateUIText(slot.transform, "QueueSlotLabel", "",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(QueueSlotSize, QueueSlotSize), 20, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            queueSlotBackgrounds.Add(bg);
            queueSlotOutlines.Add(outline);
            queueSlotLabels.Add(label);
        }

        builtQueueCapacity = capacity;
    }

    private static string CategoryLetter(string category)
    {
        switch (category)
        {
            case "Organik": return "O";
            case "Anorganik": return "A";
            case "B3": return "B3";
            default: return "?";
        }
    }

    /// <summary>
    /// Modul 18A: segarkan isi slot antrean (dipicu event / perubahan kapasitas).
    /// Tidak dipanggil dari Update() setiap frame agar 0 alokasi.
    /// </summary>
    private void RefreshQueueRow()
    {
        if (queueRowRect == null || playerInventory == null) return;

        int capacity = Mathf.Max(1, playerInventory.maxCapacity);
        if (capacity != builtQueueCapacity)
        {
            BuildQueueSlots(capacity);
        }

        playerInventory.GetQueueSnapshot(queueBuffer);

        for (int i = 0; i < queueSlotBackgrounds.Count; i++)
        {
            bool filled = i < queueBuffer.Count;
            bool isReady = filled && i == 0;

            Image bg = queueSlotBackgrounds[i];
            Outline outline = queueSlotOutlines[i];
            Text label = queueSlotLabels[i];
            LayoutElement layoutElement = bg.GetComponent<LayoutElement>();

            if (filled)
            {
                string category = queueBuffer[i].category;
                bg.color = GetTrashColor(category);
                label.text = CategoryLetter(category);
                label.color = category == "Anorganik" ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
            }
            else
            {
                bg.color = new Color(0x2C / 255f, 0x31 / 255f, 0x3A / 255f, 0.65f);
                label.text = "";
            }

            outline.effectColor = isReady ? HexColor("#FFF59D") : new Color(1f, 1f, 1f, 0.15f);
            outline.effectDistance = isReady ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            if (layoutElement != null)
            {
                float size = isReady ? QueueReadySlotSize : QueueSlotSize;
                layoutElement.preferredWidth = size;
                layoutElement.preferredHeight = size;
            }
        }

        if (queueReadyMarker != null)
        {
            queueReadyMarker.gameObject.SetActive(queueBuffer.Count > 0);
        }

        if (hudTopTrashText != null)
        {
            if (queueBuffer.Count > 0)
            {
                TrashInventory.TrashQueueSlotInfo top = queueBuffer[0];
                hudTopTrashText.text = "Siap Buang: " + top.displayName + " (" + top.category + ")";
                hudTopTrashText.color = GetTrashColor(top.category);
            }
            else
            {
                hudTopTrashText.text = "Siap Buang: -";
                hudTopTrashText.color = HexColor("#B0BEC5");
            }
        }
    }

    private void HandleInventoryChanged()
    {
        queueDirty = true;
    }

    private void OnDestroy()
    {
        if (playerInventory != null && inventoryEventBound)
        {
            playerInventory.OnInventoryChanged -= HandleInventoryChanged;
            inventoryEventBound = false;
        }
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
