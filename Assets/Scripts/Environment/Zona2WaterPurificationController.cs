using UnityEngine;

/// <summary>
/// Modul 17: Purifikasi air sungai Zona 2 secara real-time (SDG 6 - Clean Water).
/// Dipasang pada GameObject "Zona2_RiverWater" (bersama Zona2WaterVisual).
/// - Mendengarkan TrashSpawner.OnZona2TrashCleaned dan menaikkan kebersihan air secara halus.
/// - Menghidupkan partikel kilau air bersih secara bertahap setelah 45% bersih.
/// - Memberi feedback milestone 50% dan 100% lewat toast UIManager.
/// - Menyediakan ForceFullClean() untuk sinkronisasi ending dan AddDebugCleanStep() untuk demo (F7).
/// </summary>
[RequireComponent(typeof(Zona2WaterVisual))]
public class Zona2WaterPurificationController : MonoBehaviour
{
    [Header("Progres Purifikasi (SDG 6)")]
    [Tooltip("Jumlah sampah Zona 2 yang perlu dipungut agar air 100% jernih.")]
    [SerializeField] private int trashTargetForFullClean = 12;
    [Tooltip("Kecepatan transisi kebersihan per detik (0.65 = ~1,5 detik untuk transisi penuh).")]
    [SerializeField] private float transitionSpeed = 0.65f;
    [SerializeField] private Zona2WaterVisual waterVisual;
    [SerializeField] private ParticleSystem cleanWaterSparkles;

    [Header("Emisi Partikel Kilau Air Bersih")]
    [Tooltip("Kebersihan minimum sebelum kilau air mulai muncul.")]
    [SerializeField] private float sparkleStartCleanliness = 0.45f;
    [Tooltip("Emisi lembut maksimum (partikel/detik) saat air 100% jernih.")]
    [SerializeField] private float sparkleMaxRate = 18f;

    [Header("Feedback Milestone")]
    [SerializeField] private bool showMilestoneToast = true;

    private int cleanedCount;
    private float currentCleanliness;
    private float targetCleanliness;
    private bool milestone50Shown;
    private bool milestone100Shown;
    private bool forceFullClean;
    private bool sparkleEmissionCached;
    private ParticleSystem.EmissionModule sparkleEmission;

    /// <summary>Kebersihan air saat ini (0 = keruh, 1 = jernih).</summary>
    public float CurrentCleanliness => currentCleanliness;

    /// <summary>Jumlah sampah Zona 2 yang sudah tercatat dibersihkan.</summary>
    public int CleanedCount => cleanedCount;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        TrashSpawner.OnZona2TrashCleaned += HandleZona2TrashCleaned;
    }

    private void OnDisable()
    {
        TrashSpawner.OnZona2TrashCleaned -= HandleZona2TrashCleaned;
    }

    private void CacheReferences()
    {
        if (waterVisual == null)
        {
            waterVisual = GetComponent<Zona2WaterVisual>();
        }

        if (!sparkleEmissionCached && cleanWaterSparkles != null)
        {
            sparkleEmission = cleanWaterSparkles.emission;
            sparkleEmission.enabled = true;
            sparkleEmissionCached = true;
        }
    }

    private void HandleZona2TrashCleaned(int totalCleaned)
    {
        // Max() agar langkah debug (F7) tidak diturunkan oleh hitungan event asli.
        cleanedCount = Mathf.Max(cleanedCount, totalCleaned);
    }

    private void Update()
    {
        if (waterVisual == null)
        {
            return;
        }

        int target = Mathf.Max(1, trashTargetForFullClean);
        targetCleanliness = forceFullClean ? 1f : Mathf.Clamp01((float)cleanedCount / target);

        currentCleanliness = Mathf.MoveTowards(currentCleanliness, targetCleanliness, transitionSpeed * Time.deltaTime);
        waterVisual.SetCleanliness(currentCleanliness);

        UpdateSparkles();
        CheckMilestones();
    }

    private void UpdateSparkles()
    {
        if (cleanWaterSparkles == null)
        {
            return;
        }

        if (!sparkleEmissionCached)
        {
            sparkleEmission = cleanWaterSparkles.emission;
            sparkleEmission.enabled = true;
            sparkleEmissionCached = true;
        }

        float t = Mathf.InverseLerp(sparkleStartCleanliness, 1f, currentCleanliness);
        float rate = Mathf.Lerp(0f, sparkleMaxRate, t * t);
        sparkleEmission.rateOverTime = rate;

        if (rate > 0.01f && !cleanWaterSparkles.isPlaying)
        {
            cleanWaterSparkles.Play();
        }
    }

    private void CheckMilestones()
    {
        if (!showMilestoneToast)
        {
            return;
        }

        if (!milestone50Shown && targetCleanliness >= 0.5f)
        {
            milestone50Shown = true;
            Notify("Air Sungai Mulai Jernih! (50%)", new Color(0.45f, 0.83f, 0.95f));
        }

        if (!milestone100Shown && targetCleanliness >= 1f)
        {
            milestone100Shown = true;
            Notify("Sungai Desa Kembali Jernih! (SDG 6 Tercapai!)", new Color(0.35f, 0.9f, 0.75f));
        }
    }

    private static void Notify(string message, Color color)
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowFeedback(message, color, 3.5f);
        }

        Debug.Log("[Zona2Water] " + message);
    }

    /// <summary>
    /// Sinkronisasi ending: paksa air jernih 100% saat Balai Desa selesai direnovasi.
    /// </summary>
    public void ForceFullClean()
    {
        forceFullClean = true;
        cleanedCount = Mathf.Max(cleanedCount, Mathf.Max(1, trashTargetForFullClean));
    }

    /// <summary>
    /// Pintasan demo/QA (F7): tambah progres pembersihan tanpa memungut sampah.
    /// </summary>
    public void AddDebugCleanStep(int steps = 3)
    {
        int safeSteps = Mathf.Max(1, steps);
        cleanedCount = Mathf.Max(0, cleanedCount + safeSteps);
        Debug.Log("[Zona2Water] Debug step +" + safeSteps + " -> " + cleanedCount + "/" + trashTargetForFullClean);
    }
}
