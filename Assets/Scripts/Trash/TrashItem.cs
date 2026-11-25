using UnityEngine;

/// <summary>
/// Modul 16: Metadata edukatif untuk setiap sampah (nama tampil, kategori, zona asal).
/// Backward-compatible: sistem lama (TrashInventory) tetap mendeteksi sampah lewat Tag,
/// sehingga prefab lama tanpa komponen ini tetap berfungsi normal.
/// </summary>
public class TrashItem : MonoBehaviour
{
    [Header("Display & Zone Metadata (Modul 16)")]
    [Tooltip("Nama spesifik yang tampil di HUD, mis. 'Kaleng Soda'.")]
    public string displayName = "";

    [HideInInspector] public int originZone = 0; // 1 = Zona1_TPS, 2 = Zona2_Riverbank, 3 = Zona3_BalaiDesa

    [Header("Kategori")]
    [Tooltip("Organik / Anorganik / B3. Jika kosong, memakai Tag GameObject.")]
    public string category = "";

    /// <summary>
    /// Label nama untuk prompt/toast. Fallback ke nama kategori bila displayName kosong.
    /// </summary>
    public string GetDisplayLabel()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return "Sampah " + GetCategoryLabel();
    }

    /// <summary>
    /// Label kategori (Organik / Anorganik / B3), fallback ke Tag bila field category kosong.
    /// </summary>
    public string GetCategoryLabel()
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            return category;
        }

        if (gameObject.CompareTag("B3")) return "B3";
        if (gameObject.CompareTag("Anorganik")) return "Anorganik";
        if (gameObject.CompareTag("Organik")) return "Organik";

        return "Sampah";
    }
}
