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
        labelRoot.transform.localScale = Vector3.one * 0.008f;

        labelRoot.AddComponent<WorldLabelBillboard>();

        Canvas canvas = labelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = labelRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(260f, 50f);

        GameObject bgObj = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(labelRoot.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.92f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(bgObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 2f);
        textRect.offsetMax = new Vector2(-6f, -2f);

        Text label = textObj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (label.font == null) label.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        label.text = text;
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return labelRoot;
    }
}
