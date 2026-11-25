using System;
using UnityEngine;

/// <summary>
/// Komponen pada setiap objek sampah: metadata edukatif (Modul 16), referensi TrashData,
/// animasi bobbing/rotasi halus, serta event spawn/destroy agar TrashSpawner v2 dapat
/// melacak jumlah sampah aktif tanpa FindGameObjectsWithTag setiap frame.
/// </summary>
public class TrashItem : MonoBehaviour
{
    [Header("Data Sampah")]
    public TrashData data;

    [Header("Animasi Bobbing & Rotasi")]
    [Tooltip("Aktifkan animasi melayang dan berputar halus sebagai penanda dapat dipungut.")]
    public bool enableAnimation = true;
    public float rotationSpeed = 45f;
    public float bobbingSpeed = 2f;
    public float bobbingHeight = 0.08f;

    [Header("Display & Zone Metadata (Modul 16)")]
    [Tooltip("Nama spesifik yang tampil di HUD, mis. 'Kaleng Soda'.")]
    public string displayName = "";

    [HideInInspector] public int originZone = 0; // 1 = Zona1_TPS, 2 = Zona2_Riverbank, 3 = Zona3_BalaiDesa

    [Header("Kategori")]
    [Tooltip("Organik / Anorganik / B3. Jika kosong, memakai Tag GameObject.")]
    public string category = "";

    /// <summary>
    /// Event saat sampah dipungut/dihancurkan (dipakai TrashSpawner untuk mengurangi hitungan aktif).
    /// </summary>
    public static event Action<TrashItem> OnTrashDestroyed;

    /// <summary>
    /// Event saat sampah pertama kali aktif di scene.
    /// </summary>
    public static event Action<TrashItem> OnTrashSpawned;

    private Vector3 initialPosition;
    private float randomPhase;

    private void Awake()
    {
        if (data != null && !string.IsNullOrEmpty(data.TagName))
        {
            if (!gameObject.CompareTag(data.TagName))
            {
                gameObject.tag = data.TagName;
            }
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            if (gameObject.CompareTag("B3")) category = "B3";
            else if (gameObject.CompareTag("Anorganik")) category = "Anorganik";
            else if (gameObject.CompareTag("Organik")) category = "Organik";
        }
    }

    private void Start()
    {
        initialPosition = transform.position;
        randomPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        OnTrashSpawned?.Invoke(this);
    }

    private void Update()
    {
        if (!enableAnimation) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        float newY = initialPosition.y + Mathf.Sin((Time.time * bobbingSpeed) + randomPhase) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnDestroy()
    {
        OnTrashDestroyed?.Invoke(this);
    }

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
