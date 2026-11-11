using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD poin sementara (pratinjau Fase 7).
/// Membuat Canvas + teks poin otomatis saat Play, tanpa setup scene manual.
/// Nanti akan digantikan/diperluas oleh UIManager pada Fase 7.
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    private const string CanvasName = "HUD_Canvas";

    private Text scoreText;
    private int lastPoin = -1;

    private void Awake()
    {
        BuildHud();
    }

    private void BuildHud()
    {
        GameObject canvasObj = GameObject.Find(CanvasName);

        if (canvasObj == null)
        {
            canvasObj = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("ScorePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(240f, 64f);

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject textObj = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            scoreText = textObj.GetComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (scoreText.font == null)
            {
                scoreText.font = Font.CreateDynamicFontFromOSFont("Arial", 30);
            }

            scoreText.fontSize = 30;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleLeft;
            scoreText.color = Color.white;
            scoreText.text = "Poin: 0";

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
        else
        {
            Transform textTransform = canvasObj.transform.Find("ScorePanel/ScoreText");
            if (textTransform != null)
            {
                scoreText = textTransform.GetComponent<Text>();
            }
        }
    }

    private void Update()
    {
        if (scoreText == null)
        {
            return;
        }

        int poin = ScoreManager.instance != null ? ScoreManager.instance.currentPoin : 0;
        if (poin == lastPoin)
        {
            return;
        }

        lastPoin = poin;
        scoreText.text = "Poin: " + poin;
    }
}
