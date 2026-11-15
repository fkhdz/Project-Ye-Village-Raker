using UnityEngine;

/// <summary>
/// Pusat kontrol seluruh audio game (sesuai dokumen 08).
/// Dipasang pada GameObject kosong bernama "AudioManager".
/// Pola singleton agar skrip lain cukup memanggil AudioManager.Instance.PlaySFX(...).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sumber Audio")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Musik Latar")]
    public AudioClip bgmGameplay;
    public AudioClip bgmVictory;

    [Header("Efek Suara")]
    public AudioClip sfxPickup;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;
    public AudioClip sfxBuy;
    public AudioClip sfxFootstep;

    private bool victoryMusicActive;

    private void Awake()
    {
        // Pola singleton dasar: hanya satu AudioManager yang aktif.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Pastikan kedua AudioSource tersedia.
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup sesuai dokumen 08: BGM loop, SFX tidak loop.
        bgmSource.loop = true;
        bgmSource.playOnAwake = true;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        // Mulai BGM gameplay jika belum diputar.
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            if (bgmSource.clip == null)
            {
                bgmSource.clip = bgmGameplay;
            }

            if (bgmSource.clip != null)
            {
                bgmSource.Play();
            }
        }
    }

    private void Update()
    {
        // Jaga BGM gameplay tetap berjalan (mis. audio device baru siap setelah Start).
        // Tidak dilakukan setelah victory agar fanfare yang selesai tidak diulang.
        if (victoryMusicActive || bgmSource == null || bgmSource.clip == null)
        {
            return;
        }

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    /// <summary>
    /// Memutar efek suara sekali tanpa memotong SFX lain (overlap).
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Mengganti BGM menjadi musik kemenangan (tidak loop).
    /// Dipanggil GameManager saat renovasi Balai Desa selesai.
    /// </summary>
    public void PlayVictoryMusic()
    {
        if (bgmSource == null || bgmVictory == null)
        {
            return;
        }

        victoryMusicActive = true;
        bgmSource.Stop();
        bgmSource.clip = bgmVictory;
        bgmSource.loop = false;
        bgmSource.Play();
    }

    /// <summary>
    /// Suara langkah kaki.
    /// Catatan: idealnya dipanggil melalui Animation Event pada animasi Walk,
    /// atau melalui cooldown/timer di PlayerController saat karakter bergerak
    /// (implementasi saat ini memakai timer cooldown di PlayerController).
    /// </summary>
    public void PlayFootstep()
    {
        PlaySFX(sfxFootstep);
    }
}
