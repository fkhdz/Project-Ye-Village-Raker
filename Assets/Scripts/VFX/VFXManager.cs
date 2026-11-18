using UnityEngine;

/// <summary>
/// Pusat efek visual / "juice" (sesuai dokumen 10).
/// Dipasang pada GameObject kosong bernama "VFXManager".
/// Pola singleton agar script lain cukup memanggil VFXManager.instance.PlayCorrectVFX(...).
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager instance;

    [Header("Prefab Efek")]
    [Tooltip("Konfeti hijau saat pemilahan benar.")]
    public GameObject vfxCorrect;

    [Tooltip("Asap merah saat pemilahan salah.")]
    public GameObject vfxWrong;

    [Tooltip("Kilauan bintang saat membeli upgrade.")]
    public GameObject vfxUpgrade;

    [Tooltip("Efek besar saat Balai Desa direnovasi (victory).")]
    public GameObject vfxRenovation;

    private void Awake()
    {
        // Pola singleton dasar: hanya satu VFXManager yang aktif.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    /// <summary>
    /// Memunculkan prefab VFX di posisi tertentu, memutarnya,
    /// lalu menghancurkannya setelah durasi selesai agar tidak menumpuk.
    /// </summary>
    public void PlayVFX(GameObject vfxPrefab, Vector3 spawnPosition)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
        ParticleSystem particle = vfxInstance.GetComponent<ParticleSystem>();

        if (particle != null)
        {
            particle.Play();
            Destroy(vfxInstance, particle.main.duration);
        }
        else
        {
            // Fallback jika prefab tidak memiliki ParticleSystem.
            Destroy(vfxInstance, 2f);
        }
    }

    public void PlayCorrectVFX(Vector3 position)
    {
        PlayVFX(vfxCorrect, position);
    }

    public void PlayWrongVFX(Vector3 position)
    {
        PlayVFX(vfxWrong, position);
    }

    public void PlayUpgradeVFX(Vector3 position)
    {
        PlayVFX(vfxUpgrade, position);
    }

    public void PlayRenovationVFX(Vector3 position)
    {
        PlayVFX(vfxRenovation, position);
    }
}
