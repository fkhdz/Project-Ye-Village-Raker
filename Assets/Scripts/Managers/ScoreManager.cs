using UnityEngine;

/// <summary>
/// Menyimpan poin global dan mengevaluasi bonus dari mesin upgrade (sesuai dokumen 04).
/// Dipasang pada GameObject kosong bernama "GameManager".
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("Poin")]
    public int currentPoin = 0;

    [Header("Upgrade Mesin")]
    public bool hasCompostMachine = false;
    public bool hasPressMachine = false;

    private void Awake()
    {
        // Pola singleton dasar: hanya satu ScoreManager yang aktif.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        // HUD poin pratinjau Fase 7: dipasang otomatis agar tidak perlu setup scene manual.
        if (GetComponent<ScoreHUD>() == null)
        {
            gameObject.AddComponent<ScoreHUD>();
        }
    }

    /// <summary>
    /// Menambah poin sesuai jenis sampah.
    /// Organik/Anorganik bernilai 20 jika mesinnya sudah dibeli, selain itu 10.
    /// B3 selalu bernilai 10.
    /// </summary>
    public void AddScore(string trashType)
    {
        if (trashType == "Organik")
        {
            currentPoin += hasCompostMachine ? 20 : 10;
        }
        else if (trashType == "Anorganik")
        {
            currentPoin += hasPressMachine ? 20 : 10;
        }
        else if (trashType == "B3")
        {
            currentPoin += 10;
        }

        Debug.Log("Poin Bertambah! Total: " + currentPoin);
    }

    /// <summary>
    /// Mengurangi poin karena salah tong. Poin tidak pernah minus (mentok di 0).
    /// </summary>
    public void DeductScore(int penaltyAmount = 5)
    {
        currentPoin = Mathf.Max(0, currentPoin - penaltyAmount);

        Debug.Log("Salah Tong! Poin Dikurangi. Total: " + currentPoin);
    }
}
