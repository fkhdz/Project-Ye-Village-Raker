using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Penanda titik keluar zona (Exit Marker) menuju zona berikutnya.
/// Dipasang pada GameObject Exit_To_Zona2 dengan BoxCollider (isTrigger).
/// Menyimpan sesi game (poin & upgrade) melalui GameSession sebelum memuat scene baru.
/// Sesuai spesifikasi Prompt Map Level 1 bagian 4.7.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ZoneExitMarker : MonoBehaviour
{
    [Header("Tujuan Scene")]
    [Tooltip("Nama scene yang akan dimuat saat keluar dari zona ini.")]
    public string targetSceneName = "SampleScene";

    [Header("Metode Transisi")]
    [Tooltip("Jika true, langsung pindah scene saat menyentuh trigger. Jika false, butuh menekan tombol [E].")]
    public bool loadOnEnter = false;

    [Header("Syarat Poin (Opsional)")]
    [Tooltip("Jika true, pemain harus memiliki minimal poin tertentu untuk dapat keluar.")]
    public bool requireTargetScore = false;

    [Tooltip("Jumlah poin minimal yang dibutuhkan jika requireTargetScore aktif.")]
    public int requiredScore = 250;

    [Header("Event")]
    // Sesuai prompt 4.7: UnityEvent OnPlayerEnter
    public UnityEvent OnPlayerEnter;
    public UnityEvent OnZoneExitTriggered;

    [Header("Visual Gizmo")]
    public Color gizmoColor = new Color(0.2f, 0.5f, 1f, 0.4f);

    private bool isPlayerInside = false;

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = true;
        OnPlayerEnter?.Invoke(); // Event sesuai spesifikasi prompt 4.7

        if (loadOnEnter)
        {
            TryExecuteExit();
        }
        else
        {
            string promptMsg = "[E] Lanjut ke Zona Berikutnya (" + targetSceneName + ")";
            if (requireTargetScore && ScoreManager.instance != null && ScoreManager.instance.currentPoin < requiredScore)
            {
                promptMsg = "Butuh " + requiredScore + " Poin untuk Lanjut (Saat ini: " + ScoreManager.instance.currentPoin + ")";
            }

            UIManager.instance?.SetInteractionPrompt("ZoneExit", promptMsg);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInside = false;
        UIManager.instance?.ClearInteractionPrompt("ZoneExit");
    }

    private void Update()
    {
        if (loadOnEnter || !isPlayerInside) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryExecuteExit();
        }
    }

    /// <summary>
    /// Memvalidasi syarat dan memuat scene tujuan jika valid.
    /// </summary>
    public void TryExecuteExit()
    {
        // Cek syarat poin jika aktif
        if (requireTargetScore && ScoreManager.instance != null)
        {
            if (ScoreManager.instance.currentPoin < requiredScore)
            {
                UIManager.instance?.ShowFeedback(
                    "Poin belum cukup! Kumpulkan minimal " + requiredScore + " poin untuk membuka jalan.",
                    new Color(1f, 0.4f, 0.4f));
                return;
            }
        }

        // Cek apakah scene tujuan terdaftar dan bisa dimuat
        if (string.IsNullOrEmpty(targetSceneName) || !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError("[ZoneExitMarker] Scene target '" + targetSceneName + "' tidak ditemukan atau belum ditambahkan ke Build Settings!");
            UIManager.instance?.ShowFeedback(
                "Zona berikutnya (" + targetSceneName + ") belum tersedia.",
                new Color(1f, 0.8f, 0.2f));
            return;
        }

        UIManager.instance?.ClearInteractionPrompt("ZoneExit");

        // Simpan state game ke sesi statis
        GameSession.SaveFromCurrentScene();

        OnZoneExitTriggered?.Invoke();

        Debug.Log("[ZoneExitMarker] Berpindah ke scene: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Color fill = gizmoColor;
        fill.a = 0.15f;
        Gizmos.color = fill;
        Gizmos.DrawCube(col.center, col.size);

        Color wire = gizmoColor;
        wire.a = 0.8f;
        Gizmos.color = wire;
        Gizmos.DrawWireCube(col.center, col.size);
    }
#endif
}
