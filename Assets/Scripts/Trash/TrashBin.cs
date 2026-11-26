using UnityEngine;

/// <summary>
/// Interaksi tong sampah (sesuai dokumen 04).
/// Dipasang pada ketiga tong: Hijau (Organik), Kuning (Anorganik), Merah (B3).
/// Pemain membuang sampah paling atas dari tas saat menekan [E] di dekat tong.
/// Modul 13: Interaction Prompt, Toast Feedback, dan World Label 3D.
/// </summary>
public class TrashBin : MonoBehaviour
{
    [Header("Jenis Tong")]
    [Tooltip("Isi \"Organik\", \"Anorganik\", atau \"B3\" sesuai jenis tong.")]
    public string acceptedTrashTag;

    private bool isPlayerInRange = false;
    private TrashInventory playerInventory;

    private void Start()
    {
        // Fitur 4: World-Space 3D Billboard Label
        string labelText = "";
        Color labelColor = Color.gray;

        switch (acceptedTrashTag)
        {
            case "Organik":
                labelText = "TONG ORGANIK";
                ColorUtility.TryParseHtmlString("#2E7D32", out labelColor);
                break;
            case "Anorganik":
                labelText = "TONG ANORGANIK";
                ColorUtility.TryParseHtmlString("#F9A825", out labelColor);
                break;
            case "B3":
                labelText = "TONG B3";
                ColorUtility.TryParseHtmlString("#C62828", out labelColor);
                break;
        }

        if (!string.IsNullOrEmpty(labelText))
        {
            // Modul 14: tinggi label distandarkan agar tidak tenggelam / menabrak tutup tong.
            WorldLabelBillboard.CreateLabel(transform, labelText, labelColor, new Vector3(0f, 2.0f, 0f));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerInventory = other.GetComponent<TrashInventory>();

            UIManager.instance?.SetInteractionPrompt("Bin_" + acceptedTrashTag, "[E] Buang ke Tong " + acceptedTrashTag);

            Debug.Log("Tekan [E] untuk membuang sampah ke tong " + acceptedTrashTag + ".");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerInventory = null;

            UIManager.instance?.ClearInteractionPrompt("Bin_" + acceptedTrashTag);
        }
    }

    private void Update()
    {
        if (StoryProgress.IsGameplayInputBlocked) return;

        if (!isPlayerInRange || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (playerInventory == null)
        {
            return;
        }

        // Cek tas kosong sebelum RemoveTopTrash
        if (playerInventory.carriedTrash.Count == 0)
        {
            UIManager.instance?.ShowFeedback("Tas masih kosong! Pungut sampah dulu.", new Color(0.9f, 0.9f, 0.4f));
            Debug.Log("Tas kosong, tidak ada sampah untuk dibuang.");
            return;
        }

        string thrownTrash = playerInventory.RemoveTopTrash();

        if (string.IsNullOrEmpty(thrownTrash))
        {
            return;
        }

        if (ScoreManager.instance == null)
        {
            Debug.LogError("[TrashBin] ScoreManager tidak ditemukan. Pastikan GameObject GameManager memiliki komponen ScoreManager.");
            return;
        }

        if (thrownTrash == acceptedTrashTag)
        {
            ScoreManager.instance.AddScore(thrownTrash);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCorrect);
            }

            if (VFXManager.instance != null)
            {
                VFXManager.instance.PlayCorrectVFX(transform.position + Vector3.up * 2f);
            }

            // Toast feedback: pilah benar
            if (UIManager.instance != null)
            {
                bool bonusMesin = false;
                if (thrownTrash == "Organik" && ScoreManager.instance.hasCompostMachine) bonusMesin = true;
                if (thrownTrash == "Anorganik" && ScoreManager.instance.hasPressMachine) bonusMesin = true;

                string msg = "+Poin! Pilah " + acceptedTrashTag + " Benar!";
                Color feedColor = new Color(0.4f, 0.73f, 0.42f);
                if (bonusMesin)
                {
                    msg += " Bonus Mesin Aktif!";
                    feedColor = new Color(1f, 0.84f, 0.31f);
                }
                UIManager.instance.ShowFeedback(msg, feedColor);
            }

            Debug.Log("Buang sampah " + thrownTrash + " ke tong " + acceptedTrashTag + " (benar).");
        }
        else
        {
            ScoreManager.instance.DeductScore(5);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxWrong);
            }

            if (VFXManager.instance != null)
            {
                VFXManager.instance.PlayWrongVFX(transform.position + Vector3.up * 2f);
            }

            // Toast feedback: pilah salah
            UIManager.instance?.ShowFeedback("-5 Poin! Salah Tong! (" + thrownTrash + " bukan " + acceptedTrashTag + ")", new Color(1f, 0.32f, 0.32f));

            Debug.Log("Buang sampah " + thrownTrash + " ke tong " + acceptedTrashTag + " (salah).");
        }
    }
}
