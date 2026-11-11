using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Toko Tetua Desa (sesuai dokumen 06).
/// Dipasang pada GameObject NPC "TetuaDesa".
/// Membuka panel toko saat Kades di area trigger dan menekan [E].
/// Panel UI dibuat otomatis saat runtime agar tidak perlu setup Canvas manual.
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

    private bool isPlayerNear;
    private bool isShopOpen;
    private bool renovasiBought;

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
        // Panel dibuat runtime jika belum di-assign di Inspector.
        if (shopUIPanel == null)
        {
            BuildShopUI();
        }

        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isShopOpen)
        {
            // Tombol Escape sebagai jalan keluar darurat agar tidak softlock.
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
            Debug.Log("[Shop] Tekan [E] untuk membuka Toko Tetua.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
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
            Debug.Log("[Shop] Kantong sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 200.
        if (!TryPay(200))
        {
            return;
        }

        playerInventory.maxCapacity = 3;
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
            Debug.Log("[Shop] Sepatu Boots sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 250.
        if (!TryPay(250))
        {
            return;
        }

        playerController.moveSpeed = 7.5f;
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
            Debug.Log("[Shop] Sarung Tangan sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 300.
        if (!TryPay(300))
        {
            return;
        }

        playerInventory.hasThickGloves = true;

        // Efek slow B3 langsung dihapus jika saat ini membawa B3.
        playerInventory.EvaluateB3Debuff();
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
            Debug.Log("[Shop] Gerobak sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 400.
        if (!TryPay(400))
        {
            return;
        }

        playerInventory.maxCapacity = 7;

        if (gerobakVisual != null)
        {
            gerobakVisual.SetActive(true);
        }

        Debug.Log("[Shop] Gerobak dibeli. Kapasitas tas menjadi 7.");
        RefreshShopUI();
    }

    public void BuyMesinKompos()
    {
        if (ScoreManager.instance == null)
        {
            Debug.LogError("[Shop] ScoreManager tidak ditemukan.");
            return;
        }

        if (ScoreManager.instance.hasCompostMachine)
        {
            Debug.Log("[Shop] Mesin Kompos sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 500.
        if (!TryPay(500))
        {
            return;
        }

        ScoreManager.instance.hasCompostMachine = true;

        if (mesinKomposVisual != null)
        {
            mesinKomposVisual.SetActive(true);
        }

        Debug.Log("[Shop] Mesin Kompos dibeli. Organik bernilai 20 poin.");
        RefreshShopUI();
    }

    public void BuyMesinPress()
    {
        if (ScoreManager.instance == null)
        {
            Debug.LogError("[Shop] ScoreManager tidak ditemukan.");
            return;
        }

        if (ScoreManager.instance.hasPressMachine)
        {
            Debug.Log("[Shop] Mesin Press sudah dimiliki.");
            return;
        }

        // Cek poin dan potong harga 500.
        if (!TryPay(500))
        {
            return;
        }

        ScoreManager.instance.hasPressMachine = true;

        if (mesinPressVisual != null)
        {
            mesinPressVisual.SetActive(true);
        }

        Debug.Log("[Shop] Mesin Press dibeli. Anorganik bernilai 20 poin.");
        RefreshShopUI();
    }

    public void BuyRenovasiBalaiDesa()
    {
        if (renovasiBought)
        {
            Debug.Log("[Shop] Renovasi Balai Desa sudah dibeli.");
            return;
        }

        // Cek poin dan potong harga 1500.
        if (!TryPay(1500))
        {
            return;
        }

        renovasiBought = true;

        // Transisi bangunan/victory akan dipanggil GameManager pada Fase 7.
        Debug.Log("[Shop] Renovasi Balai Desa dibeli. Menunggu GameManager Fase 7.");
        RefreshShopUI();
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
            Debug.Log("Uang Tidak Cukup!");
            return false;
        }

        ScoreManager.instance.currentPoin -= price;
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
            Transform existingPanel = existingCanvas.transform.Find("ShopPanel");
            if (existingPanel != null)
            {
                shopUIPanel = existingPanel.gameObject;
                return;
            }
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

        for (int i = 0; i < ItemNames.Length; i++)
        {
            Button button = CreateButton(panel.transform, "Item_" + i,
                ItemNames[i] + " - " + ItemPrices[i] + " Poin", 22,
                new Vector2(0.5f, 1f), new Vector2(0f, -148f - (i * 62f)), new Vector2(640f, 52f));

            if (i == 6)
            {
                Image image = button.GetComponent<Image>();
                image.color = new Color(0.5f, 0.36f, 0.1f, 1f);
            }

            button.onClick.AddListener(buyActions[i]);
            itemButtons[i] = button;
            itemLabels[i] = button.GetComponentInChildren<Text>();
        }

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
