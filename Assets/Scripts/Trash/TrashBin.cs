using UnityEngine;

/// <summary>
/// Interaksi tong sampah (sesuai dokumen 04).
/// Dipasang pada ketiga tong: Hijau (Organik), Kuning (Anorganik), Merah (B3).
/// Pemain membuang sampah paling atas dari tas saat menekan [E] di dekat tong.
/// </summary>
public class TrashBin : MonoBehaviour
{
    [Header("Jenis Tong")]
    [Tooltip("Isi \"Organik\", \"Anorganik\", atau \"B3\" sesuai jenis tong.")]
    public string acceptedTrashTag;

    private bool isPlayerInRange = false;
    private TrashInventory playerInventory;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerInventory = other.GetComponent<TrashInventory>();

            Debug.Log("Tekan [E] untuk membuang sampah ke tong " + acceptedTrashTag + ".");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerInventory = null;
        }
    }

    private void Update()
    {
        // Hanya proses saat pemain berada di dekat tong dan menekan E.
        if (!isPlayerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (playerInventory == null)
        {
            return;
        }

        // Ambil sampah paling atas dari tas (jika ada).
        string thrownTrash = playerInventory.RemoveTopTrash();

        // Tas kosong: tidak ada yang perlu dilakukan.
        if (string.IsNullOrEmpty(thrownTrash))
        {
            return;
        }

        if (ScoreManager.instance == null)
        {
            Debug.LogError("[TrashBin] ScoreManager tidak ditemukan. Pastikan GameObject GameManager memiliki komponen ScoreManager.");
            return;
        }

        // Validasi: sampah harus cocok dengan jenis tong.
        if (thrownTrash == acceptedTrashTag)
        {
            ScoreManager.instance.AddScore(thrownTrash);
            Debug.Log("Buang sampah " + thrownTrash + " ke tong " + acceptedTrashTag + " (benar).");
        }
        else
        {
            ScoreManager.instance.DeductScore(5);
            Debug.Log("Buang sampah " + thrownTrash + " ke tong " + acceptedTrashTag + " (salah).");
        }
    }
}
