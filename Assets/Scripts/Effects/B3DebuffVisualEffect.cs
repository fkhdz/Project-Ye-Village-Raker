using UnityEngine;

/// <summary>
/// Modul 17: Efek visual debuff sampah B3 (toxic mist + cincin peringatan).
/// Dipasang pada GameObject "Kades" (Player).
/// Aktif hanya saat TrashInventory.IsB3SlowActive bernilai true
/// (membawa minimal 1 sampah B3 dan belum memiliki Sarung Tangan Tebal).
/// </summary>
public class B3DebuffVisualEffect : MonoBehaviour
{
    [Header("Referensi")]
    [SerializeField] private TrashInventory inventory;
    [SerializeField] private ParticleSystem toxicMist;
    [SerializeField] private Transform warningRing;
    [SerializeField] private Renderer warningRingRenderer;

    [Header("Cincin Peringatan")]
    [SerializeField] private Color warningRingColor = new Color(0.93f, 0.45f, 0.13f, 0.55f);
    [SerializeField] private float ringPulseSpeed = 2.2f;
    [SerializeField] private Vector2 ringScaleRange = new Vector2(0.95f, 1.12f);

    private bool wasActive;
    private bool ringColorApplied;
    private Vector3 ringBaseScale = Vector3.one;
    private MaterialPropertyBlock ringBlock;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponentInParent<TrashInventory>();
        }

        if (warningRing == null)
        {
            Transform found = transform.Find("B3_WarningRing");
            if (found != null)
            {
                warningRing = found;
            }
        }

        if (warningRing != null)
        {
            ringBaseScale = warningRing.localScale;
            if (ringBaseScale.sqrMagnitude < 0.0001f)
            {
                ringBaseScale = Vector3.one;
            }

            if (warningRingRenderer == null)
            {
                warningRingRenderer = warningRing.GetComponent<Renderer>();
            }
        }

        ringBlock = new MaterialPropertyBlock();
        ApplyRingColor();

        // Modul 18A (BUG-08): asap toksik tidak boleh menyala saat idle.
        if (toxicMist != null)
        {
            ParticleSystem.MainModule main = toxicMist.main;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = toxicMist.emission;
            emission.enabled = false;

            toxicMist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Start()
    {
        ApplyState(inventory != null && inventory.IsB3SlowActive);
    }

    private void Update()
    {
        bool active = inventory != null && inventory.IsB3SlowActive;

        if (active != wasActive)
        {
            ApplyState(active);
        }

        if (active && warningRing != null)
        {
            float t = Mathf.Sin(Time.time * ringPulseSpeed) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(ringScaleRange.x, ringScaleRange.y, t);
            warningRing.localScale = ringBaseScale * scale;
        }
    }

    private void ApplyState(bool active)
    {
        wasActive = active;

        if (toxicMist != null)
        {
            ParticleSystem.EmissionModule emission = toxicMist.emission;

            if (active)
            {
                emission.enabled = true;

                if (!toxicMist.isPlaying)
                {
                    toxicMist.Play(true);
                }
            }
            else
            {
                // Hentikan emisi baru, biarkan partikel lama memudar halus.
                emission.enabled = false;
                toxicMist.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (warningRing != null)
        {
            warningRing.gameObject.SetActive(active);
        }
    }

    private void ApplyRingColor()
    {
        if (ringColorApplied || warningRingRenderer == null)
        {
            return;
        }

        warningRingRenderer.GetPropertyBlock(ringBlock);
        ringBlock.SetColor(BaseColorId, warningRingColor);
        ringBlock.SetColor(ColorId, warningRingColor);
        warningRingRenderer.SetPropertyBlock(ringBlock);
        ringColorApplied = true;
    }
}
