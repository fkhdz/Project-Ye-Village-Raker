using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem tas sampah Kades (sesuai dokumen 03).
/// Dipasang pada GameObject "Kades" (Player).
/// Menangani deteksi sampah (trigger), pengambilan dengan [E],
/// kapasitas tas, dan debuff lambat saat membawa limbah B3.
/// Modul 13: Integrasi Interaction Prompt dan Toast Feedback.
/// </summary>
public class TrashInventory : MonoBehaviour
{
    [Header("Kapasitas Tas")]
    public int maxCapacity = 1;
    public List<string> carriedTrash = new List<string>();

    [Header("Upgrade")]
    public bool hasThickGloves = false;

    private PlayerController playerController;
    private GameObject currentTargetTrash = null;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Organik") || other.CompareTag("Anorganik") || other.CompareTag("B3"))
        {
            currentTargetTrash = other.gameObject;
            Debug.Log("Tekan [E] untuk mengambil sampah " + currentTargetTrash.tag + ".");

            if (UIManager.instance != null)
            {
                if (carriedTrash.Count < maxCapacity)
                    UIManager.instance.SetInteractionPrompt("TrashPickup", "[E] Pungut Sampah " + currentTargetTrash.tag);
                else
                    UIManager.instance.SetInteractionPrompt("TrashPickup", "Tas Penuh! Buang ke Tong atau Upgrade di Toko");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentTargetTrash)
        {
            currentTargetTrash = null;
            UIManager.instance?.ClearInteractionPrompt("TrashPickup");
        }
    }

    private void Update()
    {
        if (currentTargetTrash != null && Input.GetKeyDown(KeyCode.E))
        {
            if (carriedTrash.Count < maxCapacity)
            {
                string trashTag = currentTargetTrash.tag;

                carriedTrash.Add(trashTag);

                Destroy(currentTargetTrash);
                currentTargetTrash = null;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxPickup);
                }

                EvaluateB3Debuff();

                // Toast feedback: berhasil pungut
                if (UIManager.instance != null)
                {
                    string msg = "Memungut " + trashTag + " (" + carriedTrash.Count + "/" + maxCapacity + ")";
                    if (trashTag == "B3" && !hasThickGloves)
                        msg += " - Gerak Melambat!";
                    UIManager.instance.ShowFeedback(msg, UIManager.GetTrashColor(trashTag));
                }

                UIManager.instance?.ClearInteractionPrompt("TrashPickup");

                Debug.Log("[Inventory] Mengambil sampah " + trashTag + ". Tas: " + carriedTrash.Count + " / " + maxCapacity);
            }
            else
            {
                // Toast feedback: tas penuh
                UIManager.instance?.ShowFeedback("Tas Penuh! Pilah sampah ke tong dulu.", new Color(1f, 0.65f, 0f));

                Debug.Log("Tas Penuh!");
            }
        }
    }

    /// <summary>
    /// Mengaktifkan efek lambat jika membawa B3 tanpa Sarung Tangan.
    /// Dipanggil saat pickup, saat membuang sampah (RemoveTopTrash),
    /// dan oleh ShopManager setelah membeli Sarung Tangan.
    /// </summary>
    public void EvaluateB3Debuff()
    {
        bool carryingB3 = carriedTrash.Contains("B3");

        if (carryingB3 && !hasThickGloves)
        {
            playerController.isSlowed = true;
        }
        else
        {
            playerController.isSlowed = false;
        }
    }

    /// <summary>
    /// Mengeluarkan sampah paling atas dari tas (dipanggil oleh TrashBin).
    /// Mengembalikan tag sampah, atau string kosong jika tas kosong.
    /// </summary>
    public string RemoveTopTrash()
    {
        if (carriedTrash.Count > 0)
        {
            string topTrash = carriedTrash[0];
            carriedTrash.RemoveAt(0);

            EvaluateB3Debuff();

            return topTrash;
        }

        return "";
    }
}
