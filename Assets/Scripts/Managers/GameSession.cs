using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menyimpan data sesi permainan (poin, kapasitas tas, kecepatan, upgrade)
/// agar tetap konsisten saat berpindah scene (mis. MainMenu -> Zona 1 -> Zona 2/SampleScene).
/// Otomatis mereset data saat kembali ke MainMenu, dan menerapkan data saat memuat scene gameplay.
/// </summary>
public static class GameSession
{
    public static bool HasActiveSession { get; private set; } = false;

    // Data sesi
    public static int CurrentPoin { get; set; } = 0;
    public static int MaxCapacity { get; set; } = 1;
    public static float MoveSpeed { get; set; } = 5f;
    public static bool HasThickGloves { get; set; } = false;
    public static bool HasCompostMachine { get; set; } = false;
    public static bool HasPressMachine { get; set; } = false;
    public static bool HasGerobak { get; set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        // Jika kembali ke MainMenu, reset sesi agar permainan berikutnya mulai dari awal
        if (scene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            ResetSession();
            return;
        }

        // Terapkan data sesi yang tersimpan ke scene yang baru dimuat
        if (HasActiveSession)
        {
            ApplyToCurrentScene();
        }
    }

    /// <summary>
    /// Menyimpan state pemain dan manager dari scene saat ini ke sesi.
    /// </summary>
    public static void SaveFromCurrentScene()
    {
        if (ScoreManager.instance != null)
        {
            CurrentPoin = ScoreManager.instance.currentPoin;
            HasCompostMachine = ScoreManager.instance.hasCompostMachine;
            HasPressMachine = ScoreManager.instance.hasPressMachine;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            TrashInventory inv = player.GetComponent<TrashInventory>();
            if (inv != null)
            {
                MaxCapacity = inv.maxCapacity;
                HasThickGloves = inv.hasThickGloves;
                HasGerobak = inv.maxCapacity >= 7;
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                MoveSpeed = pc.moveSpeed;
            }
        }

        HasActiveSession = true;
        Debug.Log("[GameSession] Sesi disimpan. Poin: " + CurrentPoin +
                  " | Tas: " + MaxCapacity + " | Speed: " + MoveSpeed +
                  " | SarungTangan: " + HasThickGloves +
                  " | Kompos: " + HasCompostMachine + " | Press: " + HasPressMachine);
    }

    /// <summary>
    /// Menerapkan state sesi ke pemain dan manager di scene saat ini.
    /// </summary>
    public static void ApplyToCurrentScene()
    {
        if (!HasActiveSession) return;

        // 1. Terapkan ke ScoreManager
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.currentPoin = CurrentPoin;
            ScoreManager.instance.hasCompostMachine = HasCompostMachine;
            ScoreManager.instance.hasPressMachine = HasPressMachine;
        }

        // 2. Terapkan ke Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            TrashInventory inv = player.GetComponent<TrashInventory>();
            if (inv != null)
            {
                inv.maxCapacity = MaxCapacity;
                inv.hasThickGloves = HasThickGloves;
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.moveSpeed = MoveSpeed;
            }
        }

        // 3. Terapkan visual upgrade pada ShopManager jika ada di scene
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>();
        if (shop != null)
        {
            if (HasCompostMachine && shop.mesinKomposVisual != null)
                shop.mesinKomposVisual.SetActive(true);

            if (HasPressMachine && shop.mesinPressVisual != null)
                shop.mesinPressVisual.SetActive(true);

            if (HasGerobak && shop.gerobakVisual != null)
                shop.gerobakVisual.SetActive(true);
        }

        Debug.Log("[GameSession] Sesi diterapkan ke scene aktif. Poin: " + CurrentPoin +
                  " | Tas: " + MaxCapacity + " | Speed: " + MoveSpeed);
    }

    /// <summary>
    /// Mereset data sesi ke kondisi awal baru (default).
    /// </summary>
    public static void ResetSession()
    {
        HasActiveSession = false;
        CurrentPoin = 0;
        MaxCapacity = 1;
        MoveSpeed = 5f;
        HasThickGloves = false;
        HasCompostMachine = false;
        HasPressMachine = false;
        HasGerobak = false;

        Debug.Log("[GameSession] Sesi di-reset ke nilai default.");
    }
}
