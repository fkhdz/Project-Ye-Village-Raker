using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem tas sampah Kades (sesuai dokumen 03).
/// Dipasang pada GameObject "Kades" (Player).
/// Menangani deteksi sampah (trigger), pengambilan dengan [E],
/// kapasitas tas, dan debuff lambat saat membawa limbah B3.
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
        // Referensi ke pergerakan Kades untuk mengatur efek lambat.
        playerController = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Hanya sampah bertag Organik / Anorganik / B3 yang bisa diambil.
        if (other.CompareTag("Organik") || other.CompareTag("Anorganik") || other.CompareTag("B3"))
        {
            currentTargetTrash = other.gameObject;
            Debug.Log("Tekan [E] untuk mengambil sampah " + currentTargetTrash.tag + ".");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Jika sampah yang keluar adalah target saat ini, kosongkan target.
        if (other.gameObject == currentTargetTrash)
        {
            currentTargetTrash = null;
        }
    }

    private void Update()
    {
        if (currentTargetTrash != null && Input.GetKeyDown(KeyCode.E))
        {
            // Cek kapasitas tas terlebih dahulu.
            if (carriedTrash.Count < maxCapacity)
            {
                string trashTag = currentTargetTrash.tag;

                // Simpan tag sampah ke dalam tas.
                carriedTrash.Add(trashTag);

                // Hapus objek sampah dari scene.
                Destroy(currentTargetTrash);
                currentTargetTrash = null;

                // Evaluasi ulang debuff B3 setiap isi tas berubah.
                EvaluateB3Debuff();

                Debug.Log("[Inventory] Mengambil sampah " + trashTag + ". Tas: " + carriedTrash.Count + " / " + maxCapacity);
            }
            else
            {
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
            // B3 tanpa sarung tangan: kecepatan turun 40% (dihitung di PlayerController).
            playerController.isSlowed = true;
        }
        else
        {
            // Tidak membawa B3, atau sudah punya sarung tangan: kecepatan normal.
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

            // Isi tas berubah: evaluasi ulang debuff B3.
            EvaluateB3Debuff();

            return topTrash;
        }

        return "";
    }
}
