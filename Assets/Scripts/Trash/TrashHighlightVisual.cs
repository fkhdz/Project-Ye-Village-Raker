using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modul 17: Highlight sampah halus dua tingkat agar mudah ditemukan di Zona 2
/// tanpa terlihat neon/kaku.
/// Tier 1: cincin tanah tipis selalu aktif (alpha rendah + denyut pelan).
/// Tier 2: saat Kades dalam focusRadius, cincin menguat, berputar lambat,
///         dan mesh sampah mendapat pendaran lembut lewat MaterialPropertyBlock.
/// Dipasang otomatis ke seluruh prefab sampah oleh Editor Tool Modul 17.
/// </summary>
public class TrashHighlightVisual : MonoBehaviour
{
    [Header("Jarak Fokus")]
    [Tooltip("Radius (meter) saat highlight fokus aktif.")]
    [SerializeField] private float focusRadius = 3.2f;

    [Header("Cincin Tanah")]
    [SerializeField] private Transform highlightRing;
    [Tooltip("Alpha cincin saat Kades jauh.")]
    [SerializeField] private float idleAlpha = 0.26f;
    [Tooltip("Alpha cincin saat Kades dekat.")]
    [SerializeField] private float focusAlpha = 0.72f;
    [SerializeField] private float idlePulseMin = 0.95f;
    [SerializeField] private float idlePulseMax = 1.05f;
    [SerializeField] private float focusScale = 1.22f;
    [Tooltip("Kecepatan rotasi cincin saat fokus (derajat/detik).")]
    [SerializeField] private float focusSpinSpeed = 25f;
    [SerializeField] private float pulseSpeed = 1.6f;

    [Header("Pendaran Fokus")]
    [Tooltip("Intensitas emisi lembut pada mesh sampah saat fokus.")]
    [SerializeField] private float emissionIntensity = 0.22f;

    [Header("Warna Kategori")]
    [SerializeField] private Color organikColor = new Color(0.357f, 0.761f, 0.357f);
    [SerializeField] private Color anorganikColor = new Color(0.949f, 0.788f, 0.298f);
    [SerializeField] private Color b3Color = new Color(0.922f, 0.341f, 0.341f);

    private Transform playerTransform;
    private Renderer ringRenderer;
    private Renderer[] visualRenderers;
    private MaterialPropertyBlock[] visualBlocks;
    private Color[] visualBaseColors;
    private MaterialPropertyBlock ringBlock;
    private Vector3 ringBaseScale = Vector3.one;
    private Color categoryColor;
    private float sqrFocusRadius;
    private bool lastFocusedState;
    private bool emissionApplied;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // Modul 18A (BUG-06): batas aman agar highlight tidak pernah memicu Bloom meledak.
    private const float RingMinWorldSize = 0.65f;
    private const float RingMaxWorldSize = 0.9f;

    private void Awake()
    {
        sqrFocusRadius = focusRadius * focusRadius;
        categoryColor = ClampLdr(ResolveCategoryColor());

        if (highlightRing == null)
        {
            Transform found = transform.Find("HighlightRing");
            if (found != null)
            {
                highlightRing = found;
            }
        }

        if (highlightRing != null)
        {
            ringRenderer = highlightRing.GetComponent<Renderer>();
            ringBaseScale = highlightRing.localScale;
            if (ringBaseScale.sqrMagnitude < 0.0001f)
            {
                ringBaseScale = Vector3.one;
            }
        }

        ringBlock = new MaterialPropertyBlock();

        List<Renderer> renderers = new List<Renderer>();
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null || r is ParticleSystemRenderer)
            {
                continue;
            }

            if (highlightRing != null && r.transform.IsChildOf(highlightRing))
            {
                continue;
            }

            renderers.Add(r);
        }

        visualRenderers = renderers.ToArray();
        visualBlocks = new MaterialPropertyBlock[visualRenderers.Length];
        visualBaseColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualBlocks.Length; i++)
        {
            visualBlocks[i] = new MaterialPropertyBlock();

            Material shared = visualRenderers[i].sharedMaterial;
            visualBaseColors[i] = shared != null && shared.HasProperty(BaseColorId)
                ? shared.GetColor(BaseColorId)
                : Color.white;
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                playerTransform = controller.transform;
            }
        }

        ApplyEmission(false);
        emissionApplied = true;
        lastFocusedState = false;
    }

    private void Update()
    {
        if (highlightRing == null && visualRenderers.Length == 0)
        {
            return;
        }

        bool focused = false;
        if (playerTransform != null)
        {
            Vector3 delta = playerTransform.position - transform.position;
            focused = delta.sqrMagnitude <= sqrFocusRadius;
        }

        AnimateRing(focused);

        if (emissionApplied && focused != lastFocusedState)
        {
            ApplyEmission(focused);
            lastFocusedState = focused;
        }
    }

    private void AnimateRing(bool focused)
    {
        if (highlightRing == null)
        {
            return;
        }

        if (focused)
        {
            SetRingScale(ringBaseScale * focusScale);
            highlightRing.Rotate(0f, 0f, focusSpinSpeed * Time.deltaTime, Space.Self);
            ApplyRingColor(focusAlpha);
        }
        else
        {
            float pulse = Mathf.Lerp(idlePulseMin, idlePulseMax, Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f);
            SetRingScale(ringBaseScale * pulse);
            ApplyRingColor(idleAlpha);
        }
    }

    /// <summary>
    /// Modul 18A (BUG-06): jaga skala cincin di tanah tetap wajar (0.65m..0.9m)
    /// meskipun prefab punya localScale/induk yang ekstrem.
    /// </summary>
    private void SetRingScale(Vector3 desiredLocal)
    {
        if (highlightRing == null)
        {
            return;
        }

        if (float.IsNaN(desiredLocal.x) || float.IsNaN(desiredLocal.y) || float.IsNaN(desiredLocal.z) ||
            float.IsInfinity(desiredLocal.x) || float.IsInfinity(desiredLocal.y) || float.IsInfinity(desiredLocal.z))
        {
            desiredLocal = Vector3.one * RingMaxWorldSize;
        }

        Transform parent = highlightRing.parent;
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;

        float worldX = Mathf.Abs(desiredLocal.x * parentScale.x);
        float worldY = Mathf.Abs(desiredLocal.y * parentScale.y);
        float maxWorld = Mathf.Max(worldX, worldY);

        if (maxWorld <= 0.0001f)
        {
            highlightRing.localScale = Vector3.one * RingMaxWorldSize;
            return;
        }

        if (maxWorld > RingMaxWorldSize)
        {
            desiredLocal *= RingMaxWorldSize / maxWorld;
        }
        else if (maxWorld < RingMinWorldSize)
        {
            desiredLocal *= RingMinWorldSize / maxWorld;
        }

        highlightRing.localScale = desiredLocal;
    }

    private static Color ClampLdr(Color c)
    {
        if (float.IsNaN(c.r) || float.IsInfinity(c.r)) c.r = 0f;
        if (float.IsNaN(c.g) || float.IsInfinity(c.g)) c.g = 0f;
        if (float.IsNaN(c.b) || float.IsInfinity(c.b)) c.b = 0f;
        if (float.IsNaN(c.a) || float.IsInfinity(c.a)) c.a = 0f;
        c.r = Mathf.Clamp01(c.r);
        c.g = Mathf.Clamp01(c.g);
        c.b = Mathf.Clamp01(c.b);
        c.a = Mathf.Clamp01(c.a);
        return c;
    }

    private void ApplyRingColor(float alpha)
    {
        if (ringRenderer == null)
        {
            return;
        }

        Color color = ClampLdr(categoryColor);
        color.a = Mathf.Clamp01(alpha);

        ringRenderer.GetPropertyBlock(ringBlock);
        ringBlock.SetColor(BaseColorId, color);
        ringBlock.SetColor(ColorId, color);
        ringRenderer.SetPropertyBlock(ringBlock);
    }

    private void ApplyEmission(bool focused)
    {
        Color emission = focused ? ClampLdr(categoryColor * emissionIntensity) : Color.black;

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Renderer renderer = visualRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(visualBlocks[i]);
            visualBlocks[i].SetColor(EmissionColorId, emission);

            // Fallback tint: sebagian material lama tidak mempertahankan keyword _EMISSION,
            // jadi base color digeser tipis ke warna kategori agar fokus tetap terlihat.
            Color tint = focused
                ? ClampLdr(Color.Lerp(visualBaseColors[i], categoryColor, emissionIntensity))
                : ClampLdr(visualBaseColors[i]);
            visualBlocks[i].SetColor(BaseColorId, tint);

            renderer.SetPropertyBlock(visualBlocks[i]);
        }
    }

    private Color ResolveCategoryColor()
    {
        TrashItem item = GetComponent<TrashItem>();
        string label = item != null ? item.GetCategoryLabel() : tag;

        if (!string.IsNullOrEmpty(label))
        {
            string key = label.ToLowerInvariant();
            if (key.Contains("b3"))
            {
                return b3Color;
            }

            if (key.Contains("anorganik"))
            {
                return anorganikColor;
            }
        }

        return organikColor;
    }
}
