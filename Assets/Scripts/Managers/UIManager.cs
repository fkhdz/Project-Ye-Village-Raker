using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD pemain: menampilkan poin dan kapasitas tas (sesuai dokumen 07).
/// Dipasang pada Canvas utama atau GameObject kosong dengan Canvas runtime.
/// Menggantikan ScoreHUD pratinjau Fase 6.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Referensi UI")]
    public Text scoreText;
    public Text capacityText;

    [Header("Referensi Player")]
    public TrashInventory playerInventory;

    private void Start()
    {
        // Fallback otomatis: cari TrashInventory di scene jika belum di-assign.
        if (playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<TrashInventory>();
            }
        }

        if (scoreText == null || capacityText == null)
        {
            Debug.LogWarning("[UIManager] scoreText atau capacityText belum di-assign di Inspector.");
        }

        if (playerInventory == null)
        {
            Debug.LogError("[UIManager] playerInventory tidak ditemukan. HUD kapasitas tidak akan update.");
        }
    }

    private void Update()
    {
        // Update teks skor dari ScoreManager.
        if (scoreText != null && ScoreManager.instance != null)
        {
            scoreText.text = "Poin: " + ScoreManager.instance.currentPoin.ToString();
        }

        // Update teks kapasitas tas (Isi / Maksimal).
        if (capacityText != null && playerInventory != null)
        {
            capacityText.text = "Tas: " + playerInventory.carriedTrash.Count + " / " + playerInventory.maxCapacity;
        }
    }
}
