using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builder Fase 8: menyiapkan GameObject "AudioManager", dua AudioSource,
/// serta 7 klip audio placeholder (WAV) agar sistem suara bisa langsung diuji.
/// Menu: YeVillage/Setup Audio System. Aman dijalankan berulang (idempotent).
/// </summary>
public static class AudioSetupBuilder
{
    private const string AudioRoot = "Assets/Audio";
    private const string BgmFolder = "Assets/Audio/BGM";
    private const string SfxFolder = "Assets/Audio/SFX";
    private const int SampleRate = 44100;

    [MenuItem("YeVillage/Setup Audio System")]
    public static void SetupAudioSystem()
    {
        EnsureFolder("Assets", "Audio");
        EnsureFolder(AudioRoot, "BGM");
        EnsureFolder(AudioRoot, "SFX");

        string bgmGameplayPath = BgmFolder + "/BGM_Gameplay.wav";
        string bgmVictoryPath = BgmFolder + "/BGM_Victory.wav";
        string pickupPath = SfxFolder + "/SFX_Pickup.wav";
        string correctPath = SfxFolder + "/SFX_Correct.wav";
        string wrongPath = SfxFolder + "/SFX_Wrong.wav";
        string buyPath = SfxFolder + "/SFX_Buy.wav";
        string footstepPath = SfxFolder + "/SFX_Footstep.wav";

        GenerateIfMissing(bgmGameplayPath, 8f, GameplayMusic);
        GenerateIfMissing(bgmVictoryPath, 3.2f, VictoryMusic);
        GenerateIfMissing(pickupPath, 0.22f, PickupSFX);
        GenerateIfMissing(correctPath, 0.55f, CorrectSFX);
        GenerateIfMissing(wrongPath, 0.5f, WrongSFX);
        GenerateIfMissing(buyPath, 0.4f, BuySFX);
        GenerateIfMissing(footstepPath, 0.12f, FootstepSFX);

        AssetDatabase.Refresh();

        AudioManager manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        GameObject managerObject;

        if (manager == null)
        {
            managerObject = new GameObject("AudioManager");
            manager = managerObject.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(managerObject, "Create AudioManager");
        }
        else
        {
            managerObject = manager.gameObject;
        }

        AudioSource[] sources = managerObject.GetComponents<AudioSource>();
        AudioSource bgm = sources.Length > 0 ? sources[0] : managerObject.AddComponent<AudioSource>();
        AudioSource sfx = sources.Length > 1 ? sources[1] : managerObject.AddComponent<AudioSource>();

        manager.bgmSource = bgm;
        manager.sfxSource = sfx;

        manager.bgmGameplay = LoadClip(bgmGameplayPath);
        manager.bgmVictory = LoadClip(bgmVictoryPath);
        manager.sfxPickup = LoadClip(pickupPath);
        manager.sfxCorrect = LoadClip(correctPath);
        manager.sfxWrong = LoadClip(wrongPath);
        manager.sfxBuy = LoadClip(buyPath);
        manager.sfxFootstep = LoadClip(footstepPath);

        bgm.loop = true;
        bgm.playOnAwake = true;
        bgm.clip = manager.bgmGameplay;

        sfx.loop = false;
        sfx.playOnAwake = false;

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = managerObject;

        Debug.Log("[AudioSetup] AudioManager siap: 2 AudioSource + 7 klip placeholder di Assets/Audio.");
    }

    private static AudioClip LoadClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogError("[AudioSetup] Gagal memuat klip: " + path);
        }

        return clip;
    }

    [MenuItem("YeVillage/Test Audio State")]
    public static void TestAudioState()
    {
        AudioManager manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[AudioTest] AudioManager tidak ditemukan di scene.");
            return;
        }

        AudioSource bgm = manager.bgmSource;
        string clipName = bgm != null && bgm.clip != null ? bgm.clip.name : "null";

        Debug.Log("[AudioTest] PlayMode=" + Application.isPlaying
            + " Instance=" + (AudioManager.Instance != null)
            + " clip=" + clipName
            + " isPlaying=" + (bgm != null && bgm.isPlaying)
            + " listenerVolume=" + AudioListener.volume
            + " listenerPause=" + AudioListener.pause);

        if (Application.isPlaying)
        {
            manager.PlaySFX(manager.sfxPickup);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void GenerateIfMissing(string path, float duration, Func<float, float> sampler)
    {
        if (File.Exists(path))
        {
            return;
        }

        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            samples[i] = Mathf.Clamp(sampler(t), -1f, 1f);
        }

        WriteWav(path, samples);
    }

    private static void WriteWav(string path, float[] samples)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        using (FileStream stream = new FileStream(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int dataSize = samples.Length * 2;

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
            {
                short pcm = (short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), -32768, 32767);
                writer.Write(pcm);
            }
        }
    }

    private static float Noise(float t)
    {
        float value = Mathf.Sin(t * 12345.6789f) * 43758.5453f;
        return (value - Mathf.Floor(value)) * 2f - 1f;
    }

    private static float GameplayMusic(float t)
    {
        // Pad akor lembut, 4 akor x 2 detik (loop 8 detik) + arpeggio ringan.
        float[][] chords =
        {
            new[] { 261.63f, 329.63f, 392.00f },
            new[] { 220.00f, 261.63f, 329.63f },
            new[] { 174.61f, 220.00f, 261.63f },
            new[] { 196.00f, 246.94f, 293.66f }
        };

        int chordIndex = Mathf.FloorToInt(t / 2f) % chords.Length;
        float localChord = t % 2f;
        float chordFade = Mathf.Clamp01(localChord / 0.08f) * Mathf.Clamp01((2f - localChord) / 0.08f);

        float value = 0f;
        foreach (float freq in chords[chordIndex])
        {
            value += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.055f;
        }

        value *= chordFade;

        float[] melody = { 523.25f, 659.25f, 783.99f, 659.25f };
        int noteIndex = Mathf.FloorToInt(t * 2f) % melody.Length;
        float noteTime = (t * 2f) % 1f;
        value += Mathf.Sin(2f * Mathf.PI * melody[noteIndex] * t) * Mathf.Exp(-noteTime * 7f) * 0.06f;

        return value;
    }

    private static float VictoryMusic(float t)
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float value = 0f;

        for (int i = 0; i < notes.Length; i++)
        {
            float start = i * 0.28f;
            if (t >= start && t < start + 0.55f)
            {
                float local = t - start;
                float envelope = Mathf.Exp(-local * 4f);
                value += Mathf.Sin(2f * Mathf.PI * notes[i] * local) * envelope * 0.18f;
                value += Mathf.Sin(2f * Mathf.PI * notes[i] * 2f * local) * envelope * 0.05f;
            }
        }

        if (t >= 1.12f)
        {
            float local = t - 1.12f;
            float envelope = Mathf.Exp(-local * 1.2f);
            value += (Mathf.Sin(2f * Mathf.PI * 523.25f * local)
                + Mathf.Sin(2f * Mathf.PI * 659.25f * local)
                + Mathf.Sin(2f * Mathf.PI * 783.99f * local)) * envelope * 0.06f;
        }

        return value;
    }

    private static float PickupSFX(float t)
    {
        float envelope = Mathf.Exp(-t * 22f);
        return (Noise(t * 400f) * 0.32f + Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.08f) * envelope;
    }

    private static float CorrectSFX(float t)
    {
        if (t < 0.16f)
        {
            return Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 10f) * 0.3f;
        }

        float local = t - 0.16f;
        return Mathf.Sin(2f * Mathf.PI * 1318.5f * local) * Mathf.Exp(-local * 8f) * 0.3f;
    }

    private static float WrongSFX(float t)
    {
        float freq = t < 0.18f ? 220f : 165f;
        float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
        return square * Mathf.Exp(-t * 4f) * 0.18f;
    }

    private static float BuySFX(float t)
    {
        float[] notes = { 988f, 1319f };
        float value = Noise(t * 250f) * Mathf.Exp(-t * 30f) * 0.05f;

        for (int i = 0; i < notes.Length; i++)
        {
            float start = i * 0.11f;
            if (t >= start)
            {
                float local = t - start;
                value += Mathf.Sin(2f * Mathf.PI * notes[i] * local) * Mathf.Exp(-local * 12f) * 0.25f;
            }
        }

        return value;
    }

    private static float FootstepSFX(float t)
    {
        float envelope = Mathf.Exp(-t * 35f);
        return (Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.25f + Noise(t * 900f) * 0.15f) * envelope;
    }
}
