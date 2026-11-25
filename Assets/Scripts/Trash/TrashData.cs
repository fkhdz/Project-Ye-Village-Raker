using UnityEngine;

/// <summary>
/// Jenis kategori sampah sesuai GDD Ye Village Raker.
/// </summary>
public enum TrashType
{
    Organic,
    Inorganic,
    B3
}

/// <summary>
/// ScriptableObject untuk mendefinisikan variasi sampah (Organik, Anorganik, B3).
/// Memudahkan penambahan variasi jenis baru tanpa mengubah sistem Spawner.
/// </summary>
[CreateAssetMenu(fileName = "TrashData", menuName = "YeVillageRaker/Trash Data")]
public class TrashData : ScriptableObject
{
    [Tooltip("Nama tampilan sampah (contoh: Apel, Kulit Pisang, Botol Plastik).")]
    public string displayName;

    [Tooltip("Kategori sampah: Organic, Inorganic, atau B3.")]
    public TrashType type;

    [Tooltip("Prefab visual 3D untuk sampah ini.")]
    public GameObject prefab;

    [Tooltip("Poin dasar saat dipilah ke tong yang benar (default 10).")]
    public int pointValue = 10;

    /// <summary>
    /// Tag Unity yang sesuai dengan jenis sampah ("Organik", "Anorganik", "B3").
    /// </summary>
    public string TagName
    {
        get
        {
            switch (type)
            {
                case TrashType.Organic: return "Organik";
                case TrashType.Inorganic: return "Anorganik";
                case TrashType.B3: return "B3";
                default: return "Organik";
            }
        }
    }
}
