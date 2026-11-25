using System;
using UnityEngine;

/// <summary>
/// Komponen yang dipasang pada setiap objek sampah di scene.
/// Memegang referensi TrashData, menangani animasi bobbing/rotasi halus,
/// dan memanggil event saat spawn & destroy agar Spawner v2 dapat melacak
/// jumlah sampah aktif secara akurat dan efisien tanpa FindGameObjectsWithTag.
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
        // Pastikan tag sinkron dengan data jika ada
        if (data != null && !string.IsNullOrEmpty(data.TagName))
        {
            if (!gameObject.CompareTag(data.TagName))
            {
                gameObject.tag = data.TagName;
            }
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

        // Rotasi halus sumbu Y
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Bobbing halus naik-turun
        float newY = initialPosition.y + Mathf.Sin((Time.time * bobbingSpeed) + randomPhase) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnDestroy()
    {
        OnTrashDestroyed?.Invoke(this);
    }
}
