using UnityEngine;

/// <summary>
/// Pengendali kondisi menang (Victory) sesuai dokumen 07.
/// Dipasang pada GameObject "GameManager" yang sama dengan ScoreManager.
/// Dipanggil oleh ShopManager saat pemain membeli "Renovasi Balai Desa" (1500 poin).
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
    [Tooltip("Panel layar kemenangan. Akan dimunculkan saat victory.")]
    public GameObject victoryPanelUI;

    [Header("Trash Spawner")]
    [Tooltip("Referensi ke TrashSpawner untuk menghentikan pemunculan sampah.")]
    public TrashSpawner trashSpawner;

    private bool victoryTriggered;

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
    }

    private void Start()
    {
        // Pastikan bangunan megah dan panel victory nonaktif di awal.
        if (balaiDesaMegah != null)
        {
            balaiDesaMegah.SetActive(false);
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
    /// Memicu transisi menang: tukar bangunan, hentikan spawner, tampilkan panel victory.
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

        // Tutup toko jika sedang terbuka agar panel victory tidak tertutup panel toko.
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>();
        if (shop != null)
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
            Debug.Log("[GameManager] Trash spawner dihentikan.");
        }

        // 4. Tampilkan layar kemenangan.
        if (victoryPanelUI != null)
        {
            victoryPanelUI.SetActive(true);
            Debug.Log("[GameManager] Victory panel ditampilkan.");
        }

        // 5. Hentikan waktu permainan dan munculkan kursor.
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 6. Musik kemenangan (Fase 8).
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictoryMusic();
        }

        Debug.Log("[GameManager] VICTORY! Permainan berakhir.");
    }
}
