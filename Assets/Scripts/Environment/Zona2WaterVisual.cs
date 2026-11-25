using UnityEngine;

/// <summary>
/// Modul 15: Animasi visual air sungai Zona 2 (Bantaran Sungai Desa).
/// Dipasang pada GameObject "Zona2_RiverWater".
/// - Mengalirkan offset tekstur riak dari satu sisi ke sisi lain.
/// - Menggerakkan foam/ripple strips secara sinusoidal + alpha pulse.
/// - Menyediakan hook kebersihan air untuk Modul 17 (SDG 6 - Clean Water).
/// Catatan: pewarnaan memakai MaterialPropertyBlock agar asset material tidak berubah permanen.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Zona2WaterVisual : MonoBehaviour
{
    [Header("Animasi Aliran")]
    [Tooltip("Arah aliran pada UV (X = sepanjang sungai, Y = melintang).")]
    [SerializeField] private Vector2 flowDirection = new Vector2(-1f, 0.15f);
    [Tooltip("Kecepatan offset tekstur per detik.")]
    [SerializeField] private float flowSpeed = 0.055f;
    [Tooltip("Amplitudo geser horizontal foam strips (meter).")]
    [SerializeField] private float rippleAmplitude = 0.09f;
    [Tooltip("Frekuensi gelombang foam strips (Hz).")]
    [SerializeField] private float rippleFrequency = 0.55f;

    [Header("Water Cleanliness Colors (Hook Modul 17)")]
    [SerializeField] private Color pollutedColor = new Color(0.36f, 0.46f, 0.41f, 0.86f);
    [SerializeField] private Color cleanColor = new Color(0.22f, 0.66f, 0.85f, 0.78f);
    [Range(0f, 1f)] [SerializeField] private float cleanliness = 0f;

    [Header("Foam / Ripple Strips")]
    [SerializeField] private Transform[] foamStrips;

    private Renderer waterRenderer;
    private MaterialPropertyBlock waterBlock;
    private Vector2 uvOffset;

    private MaterialPropertyBlock[] foamBlocks;
    private Vector3[] foamBasePositions;
    private float[] foamPhases;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

    private void Awake()
    {
        CacheReferences();
        ApplyWaterColor(Color.Lerp(pollutedColor, cleanColor, cleanliness));
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyWaterColor(Color.Lerp(pollutedColor, cleanColor, cleanliness));
    }

    private void CacheReferences()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }

        if (waterBlock == null)
        {
            waterBlock = new MaterialPropertyBlock();
        }

        if (foamBlocks != null)
        {
            return;
        }

        if (foamStrips == null || foamStrips.Length == 0)
        {
            foamBlocks = new MaterialPropertyBlock[0];
            foamBasePositions = new Vector3[0];
            foamPhases = new float[0];
            return;
        }

        foamBlocks = new MaterialPropertyBlock[foamStrips.Length];
        foamBasePositions = new Vector3[foamStrips.Length];
        foamPhases = new float[foamStrips.Length];

        for (int i = 0; i < foamStrips.Length; i++)
        {
            foamBlocks[i] = new MaterialPropertyBlock();
            foamPhases[i] = i * 1.37f;

            if (foamStrips[i] != null)
            {
                foamBasePositions[i] = foamStrips[i].localPosition;
            }
        }
    }

    private void Update()
    {
        AnimateFlow();
        AnimateFoam();
    }

    private void AnimateFlow()
    {
        if (waterRenderer == null)
        {
            return;
        }

        uvOffset += flowDirection * (flowSpeed * Time.deltaTime);

        waterRenderer.GetPropertyBlock(waterBlock);
        waterBlock.SetVector(BaseMapStId, new Vector4(1f, 1f, uvOffset.x, uvOffset.y));
        waterRenderer.SetPropertyBlock(waterBlock);
    }

    private void AnimateFoam()
    {
        if (foamStrips == null || foamStrips.Length == 0 || foamBlocks == null)
        {
            return;
        }

        float t = Time.time * rippleFrequency;

        for (int i = 0; i < foamStrips.Length; i++)
        {
            Transform strip = foamStrips[i];
            if (strip == null)
            {
                continue;
            }

            float wave = Mathf.Sin(t + foamPhases[i]);
            Vector3 pos = foamBasePositions[i];
            pos.x += wave * rippleAmplitude;
            pos.z += Mathf.Cos(t * 0.7f + foamPhases[i]) * rippleAmplitude * 0.35f;
            strip.localPosition = pos;

            Renderer stripRenderer = strip.GetComponent<Renderer>();
            if (stripRenderer == null)
            {
                continue;
            }

            Color foamColor = Color.white;
            foamColor.a = Mathf.Lerp(0.22f, 0.52f, (wave + 1f) * 0.5f);

            stripRenderer.GetPropertyBlock(foamBlocks[i]);
            foamBlocks[i].SetColor(BaseColorId, foamColor);
            foamBlocks[i].SetColor(ColorId, foamColor);
            stripRenderer.SetPropertyBlock(foamBlocks[i]);
        }
    }

    /// <summary>
    /// Hook Modul 17: 0 = tercemar (kusam), 1 = bersih (biru jernih).
    /// </summary>
    public void SetCleanliness(float normalizedValue)
    {
        cleanliness = Mathf.Clamp01(normalizedValue);
        ApplyWaterColor(Color.Lerp(pollutedColor, cleanColor, cleanliness));
    }

    public float GetCleanliness()
    {
        return cleanliness;
    }

    /// <summary>
    /// Dipakai Editor Tool Modul 15 untuk mendaftarkan foam/ripple strips tanpa akses serialized field.
    /// </summary>
    public void SetFoamStrips(Transform[] strips)
    {
        foamStrips = strips;
        foamBlocks = null;
        foamBasePositions = null;
        foamPhases = null;
        CacheReferences();
    }

    private void ApplyWaterColor(Color color)
    {
        if (waterRenderer == null)
        {
            return;
        }

        waterRenderer.GetPropertyBlock(waterBlock);
        waterBlock.SetColor(BaseColorId, color);
        waterBlock.SetColor(ColorId, color);
        waterRenderer.SetPropertyBlock(waterBlock);
    }
}
