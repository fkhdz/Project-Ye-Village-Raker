using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Toko Tetua Desa (sesuai dokumen 06).
/// Dipasang pada GameObject NPC "TetuaDesa".
/// Membuka panel toko saat Kades di area trigger dan menekan [E].
/// Panel UI dibuat otomatis saat runtime agar tidak perlu setup Canvas manual.
/// Modul 13: Interaction Prompt, Toast Feedback, dan World Label 3D.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Referensi UI & Player")]
    public GameObject shopUIPanel;
    public PlayerController playerController;
    public TrashInventory playerInventory;

    [Header("Visual Upgrade")]
    public GameObject gerobakVisual;
    public GameObject mesinKomposVisual;
    public GameObject mesinPressVisual;

    [Header("Filter Penjualan Item (Per Zona)")]
    [Tooltip("Jika false, tombol item ini tidak akan muncul di toko.")]
    public bool sellKantong = true;
    public bool sellBoots = true;
    public bool sellSarungTangan = true;
    public bool sellGerobak = true;
    public bool sellMesinKompos = true;
    public bool sellMesinPress = true;
    public bool sellRenovasi = true;

    private bool isPlayerNear;
    private bool isShopOpen;
    private bool renovasiBought;

    public bool IsItemAllowed(int index)
    {
        switch (index)
        {
            case 0: return sellKantong;
            case 1: return sellBoots;
            case 2: return sellSarungTangan;
            case 3: return sellGerobak;
            case 4: return sellMesinKompos;
            case 5: return sellMesinPress;
            case 6: return sellRenovasi;
            default: return true;
        }
    }

    /// <summary>
    /// True saat panel toko sedang terbuka.
    /// Dipakai PauseMenu agar tombol ESC tidak bentrok menutup toko.
    /// </summary>
    public bool IsShopOpen => isShopOpen;

    private Text pointsText;
    private readonly Button[] itemButtons = new Button[7];
    private readonly Text[] itemLabels = new Text[7];

    private static readonly string[] ItemNames =
    {
        "Kantong (Kapasitas 3)",
        "Sepatu Boots (Kecepatan 7.5)",
        "Sarung Tangan (Kebal Slow B3)",
        "Gerobak (Kapasitas 7)",
        "Mesin Kompos (+10 Organik)",
        "Mesin Press (+10 Anorganik)",
        "Renovasi Balai Desa"
    };

    private static readonly int[] ItemPrices = { 200, 250, 300, 400, 500, 500, 1500 };

    private void Start()
    {
        if (shopUIPanel == null)
        {
            BuildShopUI();
        }

        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }

        // Fitur 4: World Label 3D di atas Tetua Desa
        Color labelColor;
        ColorUtility.TryParseHtmlString("#1565C0", out labelColor);
        WorldLabelBillboard.CreateLabel(transform, "TOKO TETUA DESA [E]", labelColor, new Vector3(0f, 2.4f, 0f));
    }

    private void Update()
    {
        if (isShopOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
            }
            return;
        }

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            OpenShop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            UIManager.instance?.SetInteractionPrompt("TetuaShop", "[E] Buka Toko Tetua Desa");
            Debug.Log("[Shop] Tekan [E] untuk membuka Toko Tetua.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            UIManager.instance?.ClearInteractionPrompt("TetuaShop");
        }
    }

    public void OpenShop()
    {
        if (isShopOpen)
        {
            return;
        }

        if (shopUIPanel == null)
        {
            BuildShopUI();
        }

        if (shopUIPanel == null)
        {
            Debug.LogError("[Shop] Panel toko tidak ditemukan dan gagal dibuat.");
            return;
        }

        shopUIPanel.SetActive(true);
        isShopOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIManager.instance?.ClearInteractionPrompt("TetuaShop");
        RefreshShopUI();
    }

    /// <summary>
    /// Wajib dipanggil tombol Tutup/X agar permainan tidak freeze selamanya.
    /// </summary>
    public void CloseShop()
    {
        if (!isShopOpen)
        {
            return;
        }

        isShopOpen = false;

        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void BuyKantong()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (playerInventory.maxCapacity >= 3)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Kantong sudah dimiliki.");
            return;
        }

        if (!TryPay(200))
        {
            return;
        }

        playerInventory.maxCapacity = 3;
        PlayUpgradeVFX(playerController != null ? playerController.transform : null);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Kantong (Kapasitas 3)!", new Color(0.4f, 0.85f, 0.4f));
        Debug.Log("[Shop] Kapasitas tas menjadi 3.");
        RefreshShopUI();
    }

    public void BuyBoots()
    {
        if (playerController == null)
        {
            return;
        }

        if (playerController.moveSpeed >= 7.5f)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Sepatu Boots sudah dimiliki.");
            return;
        }

        if (!TryPay(250))
        {
            return;
        }

        playerController.moveSpeed = 7.5f;
        PlayUpgradeVFX(playerController.transform);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Sepatu Boots!", new Color(0.4f, 0.85f, 0.4f));
        Debug.Log("[Shop] Kecepatan jalan menjadi 7.5.");
        RefreshShopUI();
    }

    public void BuySarungTangan()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (playerInventory.hasThickGloves)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Sarung Tangan sudah dimiliki.");
            return;
        }

        if (!TryPay(300))
        {
            return;
        }

        playerInventory.hasThickGloves = true;
        playerInventory.EvaluateB3Debuff();
        PlayUpgradeVFX(playerController != null ? playerController.transform : null);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Sarung Tangan!", new Color(0.4f, 0.85f, 0.4f));
        Debug.Log("[Shop] Sarung Tangan dibeli. Debuff B3 hilang.");
        RefreshShopUI();
    }

    public void BuyGerobak()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (playerInventory.maxCapacity >= 7)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Gerobak sudah dimiliki.");
            return;
        }

        if (!TryPay(400))
        {
            return;
        }

        playerInventory.maxCapacity = 7;

        if (gerobakVisual != null)
        {
            gerobakVisual.SetActive(true);
        }

        PlayUpgradeVFX(gerobakVisual != null ? gerobakVisual.transform : null);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Gerobak (Kapasitas 7)!", new Color(0.4f, 0.85f, 0.4f));
        Debug.Log("[Shop] Gerobak dibeli. Kapasitas tas menjadi 7.");
        RefreshShopUI();
    }

    public void BuyMesinKompos()
    {
        if (!sellMesinKompos)
        {
            UIManager.instance?.ShowFeedback("Mesin Kompos belum tersedia di zona ini.", new Color(1f, 0.5f, 0.5f));
            return;
        }

        if (ScoreManager.instance == null)
        {
            Debug.LogError("[Shop] ScoreManager tidak ditemukan.");
            return;
        }

        if (ScoreManager.instance.hasCompostMachine)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Mesin Kompos sudah dimiliki.");
            return;
        }

        if (!TryPay(500))
        {
            return;
        }

        ScoreManager.instance.hasCompostMachine = true;

        if (mesinKomposVisual != null)
        {
            mesinKomposVisual.SetActive(true);
        }

        PlayUpgradeVFX(mesinKomposVisual != null ? mesinKomposVisual.transform : null);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Mesin Kompos!", new Color(1f, 0.84f, 0.31f));
        Debug.Log("[Shop] Mesin Kompos dibeli. Organik bernilai 20 poin.");
        RefreshShopUI();
    }

    public void BuyMesinPress()
    {
        if (!sellMesinPress)
        {
            UIManager.instance?.ShowFeedback("Mesin Press belum tersedia di zona ini.", new Color(1f, 0.5f, 0.5f));
            return;
        }

        if (ScoreManager.instance == null)
        {
            Debug.LogError("[Shop] ScoreManager tidak ditemukan.");
            return;
        }

        if (ScoreManager.instance.hasPressMachine)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Mesin Press sudah dimiliki.");
            return;
        }

        if (!TryPay(500))
        {
            return;
        }

        ScoreManager.instance.hasPressMachine = true;

        if (mesinPressVisual != null)
        {
            mesinPressVisual.SetActive(true);
        }

        PlayUpgradeVFX(mesinPressVisual != null ? mesinPressVisual.transform : null);
        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Mesin Press!", new Color(1f, 0.84f, 0.31f));
        Debug.Log("[Shop] Mesin Press dibeli. Anorganik bernilai 20 poin.");
        RefreshShopUI();
    }

    public void BuyRenovasiBalaiDesa()
    {
        if (!sellRenovasi)
        {
            UIManager.instance?.ShowFeedback("Renovasi Balai Desa belum tersedia di zona ini.", new Color(1f, 0.5f, 0.5f));
            return;
        }

        if (renovasiBought)
        {
            UIManager.instance?.ShowFeedback("Upgrade ini sudah dimiliki.", new Color(1f, 0.95f, 0.46f));
            Debug.Log("[Shop] Renovasi Balai Desa sudah dibeli.");
            return;
        }

        if (!TryPay(1500))
        {
            return;
        }

        renovasiBought = true;

        UIManager.instance?.ShowFeedback("Upgrade Berhasil: Renovasi Balai Desa!", new Color(1f, 0.84f, 0.31f));

        if (GameManager.instance != null)
        {
            GameManager.instance.TriggerVictory();
        }
        else
        {
            Debug.LogError("[Shop] GameManager.instance tidak ditemukan. Victory tidak bisa dipicu.");
        }

        RefreshShopUI();
    }

    /// <summary>
    /// Efek kilauan saat upgrade berhasil (Fase 10).
    /// Jika anchor kosong, efek muncul di posisi Tetua.
    /// </summary>
    private void PlayUpgradeVFX(Transform anchor)
    {
        if (VFXManager.instance == null)
        {
            return;
        }

        Vector3 position = anchor != null ? anchor.position : transform.position;
        VFXManager.instance.PlayUpgradeVFX(position + Vector3.up * 1.2f);
    }

    private bool TryPay(int price)
    {
        if (ScoreManager.instance == null)
        {
            Debug.LogError("[Shop] ScoreManager tidak ditemukan.");
            return false;
        }

        if (ScoreManager.instance.currentPoin < price)
        {
            UIManager.instance?.ShowFeedback("Poin kurang! Butuh " + price + " Poin.", new Color(1f, 0.32f, 0.32f));
            Debug.Log("Uang Tidak Cukup!");
            return false;
        }

        ScoreManager.instance.currentPoin -= price;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxBuy);
        }

        Debug.Log("[Shop] Pembelian berhasil. Sisa poin: " + ScoreManager.instance.currentPoin);
        return true;
    }

    private void RefreshShopUI()
    {
        if (pointsText != null && ScoreManager.instance != null)
        {
            pointsText.text = "Poin: " + ScoreManager.instance.currentPoin;
        }

        bool compostBought = ScoreManager.instance != null && ScoreManager.instance.hasCompostMachine;
        bool pressBought = ScoreManager.instance != null && ScoreManager.instance.hasPressMachine;

        SetItemState(0, playerInventory != null && playerInventory.maxCapacity >= 3);
        SetItemState(1, playerController != null && playerController.moveSpeed >= 7.5f);
        SetItemState(2, playerInventory != null && playerInventory.hasThickGloves);
        SetItemState(3, playerInventory != null && playerInventory.maxCapacity >= 7);
        SetItemState(4, compostBought);
        SetItemState(5, pressBought);
        SetItemState(6, renovasiBought);
    }

    private void SetItemState(int index, bool soldOut)
    {
        if (index < 0 || index >= itemButtons.Length)
        {
            return;
        }

        if (itemButtons[index] != null)
        {
            itemButtons[index].interactable = !soldOut;
        }

        if (itemLabels[index] != null)
        {
            itemLabels[index].text = soldOut
                ? ItemNames[index] + " - Sudah dibeli"
                : ItemNames[index] + " - " + ItemPrices[index] + " Poin";
        }
    }

    private void BuildShopUI()
    {
        GameObject existingCanvas = GameObject.Find("Shop_Canvas");
        if (existingCanvas != null)
        {
            Destroy(existingCanvas);
        }

        GameObject canvasObj = new GameObject("Shop_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 620f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.07f, 0.94f);

        Text title = CreateText(panel.transform, "Title", "Toko Tetua Desa", 36, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(700f, 50f));
        title.color = new Color(1f, 0.9f, 0.6f);
        title.fontStyle = FontStyle.Bold;

        pointsText = CreateText(panel.transform, "Points", "Poin: 0", 24, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(32f, -88f), new Vector2(320f, 36f));
        pointsText.color = new Color(1f, 0.85f, 0.3f);

        Text hint = CreateText(panel.transform, "Hint", "Klik upgrade untuk membeli (permanen)", 18, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(-32f, -88f), new Vector2(380f, 36f));
        hint.color = new Color(0.8f, 0.8f, 0.8f);

        Button closeButton = CreateButton(panel.transform, "CloseButton", "X", 22,
            new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(44f, 44f));
        closeButton.onClick.AddListener(CloseShop);

        UnityAction[] buyActions =
        {
            BuyKantong,
            BuyBoots,
            BuySarungTangan,
            BuyGerobak,
            BuyMesinKompos,
            BuyMesinPress,
            BuyRenovasiBalaiDesa
        };

        int visibleCount = 0;
        for (int i = 0; i < ItemNames.Length; i++)
        {
            if (!IsItemAllowed(i))
            {
                itemButtons[i] = null;
                itemLabels[i] = null;
                continue;
            }

            Button button = CreateButton(panel.transform, "Item_" + i,
                ItemNames[i] + " - " + ItemPrices[i] + " Poin", 22,
                new Vector2(0.5f, 1f), new Vector2(0f, -148f - (visibleCount * 62f)), new Vector2(640f, 52f));

            if (i == 6)
            {
                Image image = button.GetComponent<Image>();
                image.color = new Color(0.5f, 0.36f, 0.1f, 1f);
            }

            button.onClick.AddListener(buyActions[i]);
            itemButtons[i] = button;
            itemLabels[i] = button.GetComponentInChildren<Text>();
            visibleCount++;
        }

        // Sesuaikan tinggi panel secara proporsional dengan jumlah item yang aktif
        float calculatedHeight = Mathf.Clamp(170f + (visibleCount * 64f), 280f, 620f);
        panelRect.sizeDelta = new Vector2(760f, calculatedHeight);

        shopUIPanel = panel;
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
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
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
