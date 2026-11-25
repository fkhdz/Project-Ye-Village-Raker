using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pengendali kondisi menang (Victory) sesuai dokumen 07.
/// Dipasang pada GameObject "GameManager" yang sama dengan ScoreManager.
/// Dipanggil oleh ShopManager saat pemain membeli "Renovasi Balai Desa" (1500 poin).
/// Modul 14: Auto-discovery referensi, runtime Victory Panel, dan urutan sinematik.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Bangunan Balai Desa")]
    [Tooltip("Model 3D lama (kusam/rusak). Akan disembunyikan saat victory.")]
    public GameObject balaiDesaRusak;

    [Tooltip("Model 3D baru (megah/bagus). Akan dimunculkan saat victory.")]
    public GameObject balaiDesaMegah;

    [Header("UI Victory")]
    [Tooltip("Panel layar kemenangan. Jika kosong, dibuat otomatis saat runtime.")]
    public GameObject victoryPanelUI;

    [Header("Trash Spawner")]
    [Tooltip("Referensi ke TrashSpawner untuk menghentikan pemunculan sampah.")]
    public TrashSpawner trashSpawner;

    private bool victoryTriggered;
    private Text victoryStatsText;

    /// <summary>
    /// True jika victory sudah dipicu. Dipakai PauseMenu (Fase 9) agar tidak ikut memproses ESC.
    /// </summary>
    public bool IsVictory => victoryTriggered;

    private void Awake()
    {
        // Pola singleton dasar: hanya satu GameManager yang aktif.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[GameManager] Instance duplikat terdeteksi. Menghancurkan komponen duplikat.");
            Destroy(this);
            return;
        }

        instance = this;
        ResolveSceneReferences();

        // Modul 14: pastikan Demo Presenter Helper selalu tersedia di scene gameplay.
        if (Object.FindFirstObjectByType<DemoPresenterHelper>() == null)
        {
            gameObject.AddComponent<DemoPresenterHelper>();
        }
    }

    private void Start()
    {
        ResolveSceneReferences();

        if (!victoryTriggered)
        {
            if (balaiDesaRusak != null)
            {
                balaiDesaRusak.SetActive(true);
            }

            if (balaiDesaMegah != null)
            {
                balaiDesaMegah.SetActive(false);
            }
        }

        if (victoryPanelUI != null)
        {
            victoryPanelUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Saat victory, ESC berfungsi keluar dari game (sesuai teks panel).
        if (victoryTriggered && Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    /// <summary>
    /// Keluar dari game. Di Unity Editor, menghentikan Play mode.
    /// Di build, memanggil Application.Quit().
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Keluar dari game.");

        // Pastikan waktu normal sebelum keluar.
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Mencari referensi scene secara otomatis, termasuk GameObject yang sedang inactive
    /// (BalaiDesa_Megah awal-awal nonaktif sehingga GameObject.Find biasa tidak menemukannya).
    /// </summary>
    private void ResolveSceneReferences()
    {
        if (balaiDesaRusak == null)
        {
            balaiDesaRusak = FindSceneObjectByName("BalaiDesa_Rusak");
        }

        if (balaiDesaMegah == null)
        {
            balaiDesaMegah = FindSceneObjectByName("BalaiDesa_Megah");
        }

        if (trashSpawner == null)
        {
            trashSpawner = Object.FindFirstObjectByType<TrashSpawner>();
        }
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = SceneManager.GetActiveScene();
        }

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Memicu transisi menang: tukar bangunan, hentikan spawner, mainkan VFX/audio,
    /// lalu tampilkan panel victory setelah jeda sinematik.
    /// Dipanggil oleh ShopManager.BuyRenovasiBalaiDesa() saat pembelian berhasil.
    /// </summary>
    public void TriggerVictory()
    {
        if (victoryTriggered)
        {
            Debug.Log("[GameManager] Victory sudah dipicu sebelumnya.");
            return;
        }

        victoryTriggered = true;
        ResolveSceneReferences();

        // Modul 14: pastikan waktu berjalan agar partikel renovasi bisa bergerak.
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;

        // Tutup toko jika sedang terbuka agar panel victory tidak tertutup panel toko.
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>();
        if (shop != null && shop.IsShopOpen)
        {
            shop.CloseShop();
        }

        // 1. Sembunyikan bangunan lama.
        if (balaiDesaRusak != null)
        {
            balaiDesaRusak.SetActive(false);
            Debug.Log("[GameManager] Balai Desa rusak disembunyikan.");
        }

        // 2. Munculkan bangunan baru.
        if (balaiDesaMegah != null)
        {
            balaiDesaMegah.SetActive(true);
            Debug.Log("[GameManager] Balai Desa megah dimunculkan.");
        }

        // 3. Hentikan pemunculan sampah baru.
        if (trashSpawner != null)
        {
            trashSpawner.StopSpawning();
            trashSpawner.ClearAllTrash();
            Debug.Log("[GameManager] Trash spawner dihentikan dan sampah sisa dibersihkan.");
        }

        // 3b. Modul 17: sinkronisasi ending — air sungai Zona 2 ikut jernih 100%.
        Zona2WaterPurificationController waterPurification = Object.FindFirstObjectByType<Zona2WaterPurificationController>();
        if (waterPurification != null)
        {
            waterPurification.ForceFullClean();
            Debug.Log("[GameManager] Air sungai Zona 2 dipaksa jernih (SDG 6).");
        }

        // 4. Musik kemenangan (Fase 8).
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryMusic();
        }

        // 5. Titik VFX renovasi: prioritaskan BalaiDesa_Megah, fallback ke Player.
        Vector3 vfxPos = transform.position + Vector3.up * 2f;
        if (balaiDesaMegah != null)
        {
            vfxPos = balaiDesaMegah.transform.position + Vector3.up * 2.5f;
        }
        else
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                vfxPos = player.transform.position + Vector3.up * 2f;
            }
        }

        // 6. Toast konfirmasi renovasi.
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowFeedback(
                "BALAI DESA BERHASIL DIRENOVASI!",
                new Color(1f, 0.84f, 0.25f),
                2.5f);
        }

        // 7. Jeda sinematik 1.8 detik sebelum panel menutup layar.
        StartCoroutine(VictorySequenceRoutine(vfxPos));

        Debug.Log("[GameManager] VICTORY! Permainan berakhir.");
    }

    private IEnumerator VictorySequenceRoutine(Vector3 vfxPos)
    {
        VFXManager.instance?.PlayRenovationVFX(vfxPos);
        yield return new WaitForSecondsRealtime(0.4f);
        VFXManager.instance?.PlayRenovationVFX(vfxPos + Vector3.up * 1.2f);
        yield return new WaitForSecondsRealtime(1.4f);

        if (victoryPanelUI == null)
        {
            victoryPanelUI = BuildDefaultVictoryPanel();
        }

        UpdateVictoryStats();

        if (victoryPanelUI != null)
        {
            victoryPanelUI.SetActive(true);
            Debug.Log("[GameManager] Victory panel ditampilkan.");
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void UpdateVictoryStats()
    {
        if (victoryStatsText == null && victoryPanelUI != null)
        {
            Text[] texts = victoryPanelUI.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == "VictoryStats")
                {
                    victoryStatsText = texts[i];
                    break;
                }
            }
        }

        if (victoryStatsText == null)
        {
            return;
        }

        int poin = ScoreManager.instance != null ? ScoreManager.instance.currentPoin : 0;
        victoryStatsText.text = "Sisa Poin Akhir: " + poin + " Poin";
    }

    /// <summary>
    /// Panel victory runtime murni UnityEngine.UI (tanpa TextMeshPro).
    /// </summary>
    private GameObject BuildDefaultVictoryPanel()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObj = new GameObject("Victory_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(canvasObj.transform, false);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.color = new Color(8f / 255f, 12f / 255f, 18f / 255f, 0.78f);
        backdropImage.raycastTarget = true;

        GameObject card = new GameObject("VictoryCard",
            typeof(RectTransform), typeof(Image), typeof(Outline));
        card.transform.SetParent(canvasObj.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(680f, 440f);

        Image cardImage = card.GetComponent<Image>();
        Color cardColor = HexColor("#101824");
        cardColor.a = 0.96f;
        cardImage.color = cardColor;

        Outline cardOutline = card.GetComponent<Outline>();
        cardOutline.effectColor = HexColor("#FFD54F");
        cardOutline.effectDistance = new Vector2(3f, -3f);

        CreateVictoryText(card.transform, "VictoryHeader", "RENOVASI BERHASIL!",
            38, FontStyle.Bold, HexColor("#FFD54F"), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 50f));

        CreateVictoryText(card.transform, "VictorySubtitle", "Balai Desa Kembali Megah & Desa Bersih!",
            24, FontStyle.Bold, HexColor("#81C784"), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(620f, 36f));

        CreateVictoryText(card.transform, "VictoryDescription",
            "Selamat, Pak Kades! Pengelolaan sampah Organik, Anorganik, dan B3 telah mewujudkan desa yang bersih dan berkelanjutan (SDGs 6, 11, 12).",
            20, FontStyle.Normal, HexColor("#E2E8F0"), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(600f, 100f), true);

        victoryStatsText = CreateVictoryText(card.transform, "VictoryStats",
            "Sisa Poin Akhir: 0 Poin",
            22, FontStyle.Bold, HexColor("#FFF59D"), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -234f), new Vector2(620f, 34f));

        Button restartButton = CreateVictoryButton(card.transform, "RestartButton", "MAIN LAGI",
            HexColor("#2E7D32"), new Vector2(-128f, 36f));
        restartButton.onClick.AddListener(RestartGame);

        Button menuButton = CreateVictoryButton(card.transform, "MainMenuButton", "MENU UTAMA",
            HexColor("#334155"), new Vector2(128f, 36f));
        menuButton.onClick.AddListener(ReturnToMainMenu);

        return canvasObj;
    }

    private Button CreateVictoryButton(Transform parent, string name, string label, Color bgColor,
        Vector2 anchoredPosition)
    {
        GameObject buttonObj = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(240f, 58f);

        Image image = buttonObj.GetComponent<Image>();
        image.color = bgColor;

        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        Text labelText = CreateVictoryText(buttonObj.transform, "Label", label,
            22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 46f));

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        return button;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;
        SceneManager.LoadScene(0);
    }

    private static Text CreateVictoryText(Transform parent, string name, string content,
        int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment,
        Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, bool wrap = false)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
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
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Shadow shadow = textObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        return text;
    }

    private static Color HexColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }

        return Color.white;
    }
}
