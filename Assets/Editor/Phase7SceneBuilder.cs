using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utilitas editor untuk Fase 7: setup Canvas HUD, Victory Panel, dan pasang GameManager.
/// Jalankan via menu: YeVillage > Setup Phase 7 Scene.
/// </summary>
public static class Phase7SceneBuilder
{
    [MenuItem("YeVillage/Test Victory")]
    public static void TestVictory()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Phase7Builder] Test Victory hanya bisa dijalankan saat Play mode.");
            return;
        }

        if (GameManager.instance == null)
        {
            Debug.LogError("[Phase7Builder] GameManager.instance tidak ditemukan.");
            return;
        }

        GameManager.instance.TriggerVictory();
        Debug.Log("[Phase7Builder] Test Victory dipicu.");
    }

    [MenuItem("YeVillage/Setup Phase 7 Scene")]
    public static void SetupPhase7()
    {        GameObject gameManagerObj = GameObject.Find("GameManager");
        if (gameManagerObj == null)
        {
            Debug.LogError("[Phase7Builder] GameObject 'GameManager' tidak ditemukan.");
            return;
        }

        GameObject spawnerObj = GameObject.Find("TrashSpawnerManager");
        GameObject balaiRusak = FindIncludingInactive("BalaiDesa_Rusak");
        GameObject balaiMegah = FindIncludingInactive("BalaiDesa_Megah");

        // 1. Pasang GameManager script di GameObject GameManager (jika belum ada)
        GameManager gm = gameManagerObj.GetComponent<GameManager>();
        if (gm == null)
        {
            gm = gameManagerObj.AddComponent<GameManager>();
            Debug.Log("[Phase7Builder] GameManager script dipasang.");
        }

        // 2. Buat Canvas HUD jika belum ada
        GameObject canvasHUD = GameObject.Find("Canvas_HUD");
        if (canvasHUD == null)
        {
            canvasHUD = new GameObject("Canvas_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasHUD.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = canvasHUD.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Debug.Log("[Phase7Builder] Canvas_HUD dibuat.");
        }

        // 3. Buat Panel HUD (background transparan)
        GameObject hudPanel = canvasHUD.transform.Find("HUDPanel")?.gameObject;
        if (hudPanel == null)
        {
            hudPanel = new GameObject("HUDPanel", typeof(RectTransform));
            hudPanel.transform.SetParent(canvasHUD.transform, false);
            RectTransform hudRect = hudPanel.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
        }

        // 4. Buat teks Poin (top-left)
        Text scoreText = CreateText(hudPanel.transform, "ScoreText", "Poin: 0", 32, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(300f, 50f));

        // 5. Buat teks Kapasitas (top-left, di bawah Poin)
        Text capacityText = CreateText(hudPanel.transform, "CapacityText", "Tas: 0 / 1", 28, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(32f, -90f), new Vector2(300f, 40f));

        // 6. Pasang UIManager di Canvas HUD
        UIManager uiManager = canvasHUD.GetComponent<UIManager>();
        if (uiManager == null)
        {
            uiManager = canvasHUD.AddComponent<UIManager>();
        }

        GameObject kades = GameObject.Find("Kades");
        TrashInventory playerInventory = kades?.GetComponent<TrashInventory>();

        uiManager.scoreText = scoreText;
        uiManager.capacityText = capacityText;
        uiManager.playerInventory = playerInventory;

        Debug.Log("[Phase7Builder] UIManager dipasang dan diisi referensi.");

        // 7. Buat Victory Panel (nonaktif default)
        GameObject victoryPanel = canvasHUD.transform.Find("VictoryPanel")?.gameObject;
        if (victoryPanel == null)
        {
            victoryPanel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
            victoryPanel.transform.SetParent(canvasHUD.transform, false);

            RectTransform victoryRect = victoryPanel.GetComponent<RectTransform>();
            victoryRect.anchorMin = Vector2.zero;
            victoryRect.anchorMax = Vector2.one;
            victoryRect.offsetMin = Vector2.zero;
            victoryRect.offsetMax = Vector2.zero;

            Image victoryImage = victoryPanel.GetComponent<Image>();
            victoryImage.color = new Color(0f, 0.1f, 0f, 0.92f);

            // Teks Victory
            Text victoryText = CreateText(victoryPanel.transform, "VictoryText", "KEMENANGAN!", 72, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(800f, 120f));
            victoryText.color = new Color(1f, 0.9f, 0.3f);
            victoryText.fontStyle = FontStyle.Bold;

            // Teks subtitle
            Text subtitleText = CreateText(victoryPanel.transform, "SubtitleText", "Balai Desa telah direnovasi!\nTekan ESC untuk keluar.", 28, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(700f, 100f));
            subtitleText.color = Color.white;

            victoryPanel.SetActive(false);
            Debug.Log("[Phase7Builder] VictoryPanel dibuat (nonaktif).");
        }

        // 8. Assign referensi GameManager
        gm.balaiDesaRusak = balaiRusak;
        gm.balaiDesaMegah = balaiMegah;
        gm.victoryPanelUI = victoryPanel;
        gm.trashSpawner = spawnerObj?.GetComponent<TrashSpawner>();

        Debug.Log("[Phase7Builder] Referensi GameManager diisi.");

        // 9. Nonaktifkan ScoreHUD pratinjau agar tidak bentrok dengan UIManager
        ScoreHUD oldHUD = gameManagerObj.GetComponent<ScoreHUD>();
        if (oldHUD != null)
        {
            oldHUD.enabled = false;
            Debug.Log("[Phase7Builder] ScoreHUD pratinjau dinonaktifkan.");
        }

        EditorSceneManager.MarkSceneDirty(gameManagerObj.scene);
        Debug.Log("[Phase7Builder] Setup Fase 7 selesai. Scene ditandai dirty.");
    }

    private static GameObject FindIncludingInactive(string objectName)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            if (go.name == objectName && go.scene.IsValid() && go.scene.isLoaded)
            {
                return go;
            }
        }

        return null;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize,
        TextAnchor alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject existing = parent.Find(name)?.gameObject;
        if (existing != null)
        {
            return existing.GetComponent<Text>();
        }

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
}
