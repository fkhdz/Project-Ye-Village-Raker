using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Billboard label 3D yang selalu menghadap kamera (Modul 13, Fitur 4).
/// Menempel pada tong sampah dan Tetua Desa agar pemain mengenali dari kejauhan.
/// </summary>
public class WorldLabelBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }

    public static GameObject CreateLabel(Transform parent, string text, Color bgColor, Vector3 localOffset)
    {
        GameObject labelRoot = new GameObject("WorldLabel_" + text);
        labelRoot.transform.SetParent(parent, false);
        labelRoot.transform.localPosition = localOffset;
        labelRoot.transform.localScale = Vector3.one * 0.0055f;

        labelRoot.AddComponent<WorldLabelBillboard>();

        Canvas canvas = labelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = labelRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260f, 50f);

        GameObject bgObj = new GameObject("BG", typeof(RectTransform), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        bgObj.transform.SetParent(labelRoot.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(240f, 56f);

        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.92f);

        HorizontalLayoutGroup layout = bgObj.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = bgObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;

        Text label = textObj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null) label.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        label.text = text;
        label.fontSize = 36;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(bgRect);

        return labelRoot;
    }
}
