using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistem tas sampah Kades (sesuai dokumen 03).
/// Dipasang pada GameObject "Kades" (Player).
/// Menangani deteksi sampah (trigger), pengambilan dengan [E],
/// kapasitas tas, dan debuff lambat saat membawa limbah B3.
/// Modul 13: Integrasi Interaction Prompt dan Toast Feedback.
/// Modul 18A: Ekspos data antrean (FIFO) untuk HUD slot warna (BUG-05).
/// </summary>
public class TrashInventory : MonoBehaviour
{
    /// <summary>
    /// Modul 18A: data ringan satu slot antrean tas (tanpa mengubah logika FIFO).
    /// Urutan list = urutan buang: index 0 adalah sampah yang keluar lebih dulu.
    /// </summary>
    public struct TrashQueueSlotInfo
    {
        public string category;    // "Organik", "Anorganik", "B3"
        public string displayName; // mis. "Kaleng Soda", "Jerigen Kimia"
    }

    [Header("Kapasitas Tas")]
    public int maxCapacity = 1;
    public List<string> carriedTrash = new List<string>();

    [Header("Upgrade")]
    public bool hasThickGloves = false;

    /// <summary>
    /// Modul 18A: dipanggil setiap sampah dipungut, dibuang, atau maxCapacity berubah.
    /// HUD memakai event ini agar tidak melakukan alokasi di Update().
    /// </summary>
    public event System.Action OnInventoryChanged;

    private readonly List<TrashQueueSlotInfo> queueSlots = new List<TrashQueueSlotInfo>();

    private PlayerController playerController;
    private TrashSpawner trashSpawner;
    private GameObject currentTargetTrash = null;

    /// <summary>
    /// Modul 17: status debuff B3 untuk VFX (membawa B3 tanpa Sarung Tangan Tebal).
    /// Read-only, tidak mengubah logika gameplay lama.
    /// </summary>
    public bool IsB3SlowActive => carriedTrash.Contains("B3") && !hasThickGloves;

    /// <summary>Jumlah slot antrean yang sedang terisi.</summary>
    public int QueueCount => carriedTrash.Count;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        trashSpawner = FindFirstObjectByType<TrashSpawner>();
    }

    // ================================================================
    //  MODUL 18A: DATA ANTREAN (BUG-05)
    // ================================================================

    /// <summary>
    /// Salin isi antrean tas ke buffer (index 0 = siap buang lebih dulu).
    /// Buffer dipakai ulang oleh HUD agar tidak ada alokasi per frame.
    /// </summary>
    public void GetQueueSnapshot(List<TrashQueueSlotInfo> buffer)
    {
        if (buffer == null)
        {
            return;
        }

        buffer.Clear();
        SyncQueueSlots();

        for (int i = 0; i < queueSlots.Count; i++)
        {
            buffer.Add(queueSlots[i]);
        }
    }

    /// <summary>Slot paling depan (yang keluar saat menekan [E] di tong).</summary>
    public bool TryGetTopSlot(out TrashQueueSlotInfo slot)
    {
        SyncQueueSlots();

        if (queueSlots.Count > 0)
        {
            slot = queueSlots[0];
            return true;
        }

        slot = default(TrashQueueSlotInfo);
        return false;
    }

    /// <summary>
    /// Dipanggil ShopManager setelah membeli Kantong / Gerobak agar HUD
    /// langsung menggambar ulang deretan slot sesuai kapasitas baru.
    /// </summary>
    public void NotifyCapacityChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Menjaga queueSlots tetap sejajar dengan carriedTrash meskipun list lama
    /// diubah dari luar (mis. save/load atau kode legacy).
    /// </summary>
    private void SyncQueueSlots()
    {
        if (queueSlots.Count == carriedTrash.Count)
        {
            return;
        }

        List<TrashQueueSlotInfo> rebuilt = new List<TrashQueueSlotInfo>(carriedTrash.Count);
        for (int i = 0; i < carriedTrash.Count; i++)
        {
            TrashQueueSlotInfo info;
            if (i < queueSlots.Count && queueSlots[i].category == carriedTrash[i])
            {
                // Pertahankan nama tampil yang sudah tersimpan.
                info = queueSlots[i];
            }
            else
            {
                info.category = carriedTrash[i];
                info.displayName = "Sampah " + carriedTrash[i];
            }

            rebuilt.Add(info);
        }

        queueSlots.Clear();
        queueSlots.AddRange(rebuilt);
    }

    // ================================================================
    //  DETEKSI & PENGAMBILAN
    // ================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Organik") || other.CompareTag("Anorganik") || other.CompareTag("B3"))
        {
            currentTargetTrash = other.gameObject;
            Debug.Log("Tekan [E] untuk mengambil sampah " + currentTargetTrash.tag + ".");

            if (UIManager.instance != null)
            {
                if (carriedTrash.Count < maxCapacity)
                    UIManager.instance.SetInteractionPrompt("TrashPickup", BuildPickupPrompt(currentTargetTrash));
                else
                    UIManager.instance.SetInteractionPrompt("TrashPickup", "Tas Penuh! Buang ke Tong atau Upgrade di Toko");
            }
        }
    }

    /// <summary>
    /// Modul 16: Format prompt edukatif, mis. "[E] Pungut Kaleng Soda (Anorganik)".
    /// Fallback mulus ke format lama bila prefab tidak punya displayName.
    /// </summary>
    private static string BuildPickupPrompt(GameObject trash)
    {
        TrashItem item = trash.GetComponent<TrashItem>();
        if (item != null && !string.IsNullOrWhiteSpace(item.displayName))
        {
            return "[E] Pungut " + item.displayName + " (" + item.GetCategoryLabel() + ")";
        }

        return "[E] Pungut Sampah " + trash.tag;
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
                TrashItem pickedItem = currentTargetTrash.GetComponent<TrashItem>();
                string pickedLabel = pickedItem != null && !string.IsNullOrWhiteSpace(pickedItem.displayName)
                    ? pickedItem.displayName
                    : "Sampah " + trashTag;

                // Modul 18A: sinkronkan antrean lama DULU, lalu tambah ke keduanya.
                SyncQueueSlots();
                carriedTrash.Add(trashTag);
                queueSlots.Add(new TrashQueueSlotInfo
                {
                    category = trashTag,
                    displayName = pickedLabel
                });

                // Modul 16: hook progres pembersihan Zona 2 (kesiapan Modul 17).
                if (trashSpawner == null)
                {
                    trashSpawner = FindFirstObjectByType<TrashSpawner>();
                }

                if (trashSpawner != null)
                {
                    trashSpawner.NotifyTrashPickedUp(pickedItem);
                }

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
                    string msg = "Memungut " + pickedLabel + " (" + carriedTrash.Count + "/" + maxCapacity + ")";
                    if (trashTag == "B3" && !hasThickGloves)
                        msg += " - Gerak Melambat!";
                    UIManager.instance.ShowFeedback(msg, UIManager.GetTrashColor(trashTag));
                }

                UIManager.instance?.ClearInteractionPrompt("TrashPickup");

                OnInventoryChanged?.Invoke();

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
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController != null)
        {
            playerController.isSlowed = IsB3SlowActive;
        }
    }

    /// <summary>
    /// Mengeluarkan sampah paling atas dari tas (dipanggil oleh TrashBin).
    /// Mengembalikan tag sampah, atau string kosong jika tas kosong.
    /// FIFO: index 0 keluar lebih dulu (sama dengan slot #1 di HUD).
    /// </summary>
    public string RemoveTopTrash()
    {
        if (carriedTrash.Count > 0)
        {
            string topTrash = carriedTrash[0];
            carriedTrash.RemoveAt(0);

            if (queueSlots.Count > 0)
            {
                queueSlots.RemoveAt(0);
            }

            SyncQueueSlots();

            EvaluateB3Debuff();

            OnInventoryChanged?.Invoke();

            return topTrash;
        }

        return "";
    }
}
