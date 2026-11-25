using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 14: Demo Presenter Mode untuk pengujian cepat dan presentasi kelas.
/// F8 = spawn 3 sampah uji, F9 = +500 poin, F10 = teleport ke Balai Desa.
/// Shift + F10 = langsung TriggerVictory().
/// </summary>
public class DemoPresenterHelper : MonoBehaviour
{
    public bool enableDemoShortcuts = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Pastikan helper tetap ada setelah scene di-reload (Main Lagi / Menu Utama).
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstanceExists();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstanceExists();
    }

    private static void EnsureInstanceExists()
    {
        if (UnityEngine.Object.FindFirstObjectByType<DemoPresenterHelper>() != null)
        {
            return;
        }

        GameObject helperObj = new GameObject("DemoPresenterHelper");
        helperObj.AddComponent<DemoPresenterHelper>();
    }

    private void Update()
    {
        if (!enableDemoShortcuts)
        {
            return;
        }

        // Hanya aktif di scene gameplay, bukan saat pause atau setelah victory.
        if (ScoreManager.instance == null)
        {
            return;
        }

        if (PauseMenu.isPaused)
        {
            return;
        }

        if (GameManager.instance != null && GameManager.instance.IsVictory)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            SpawnDemoTrash();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            AddDemoPoints();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            HandleTeleportOrVictory();
        }
    }

    private void SpawnDemoTrash()
    {
        TrashSpawner spawner = UnityEngine.Object.FindFirstObjectByType<TrashSpawner>();
        if (spawner == null || spawner.trashPrefabs == null || spawner.trashPrefabs.Length == 0)
        {
            ShowToast("[DEMO] TrashSpawner / prefab sampah belum siap.", HexColor("#FF5252"));
            return;
        }

        Transform player = GetPlayerTransform();
        if (player == null)
        {
            ShowToast("[DEMO] Player Kades tidak ditemukan.", HexColor("#FF5252"));
            return;
        }

        GameObject organik = PickPrefab(spawner.trashPrefabs, "Organik", 0);
        GameObject anorganik = PickPrefab(spawner.trashPrefabs, "Anorganik", 1);
        GameObject b3 = PickPrefab(spawner.trashPrefabs, "B3", 2);

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = player.position + forward * 2.5f;
        center.y = player.position.y + 0.5f;

        int spawned = 0;
        if (SpawnTrash(organik, center - right * 1.2f)) spawned++;
        if (SpawnTrash(anorganik, center)) spawned++;
        if (SpawnTrash(b3, center + right * 1.2f)) spawned++;

        if (spawned == 3)
        {
            ShowToast("[DEMO] 3 Sampah Uji (Organik, Anorganik, B3) Disiapkan", HexColor("#4DD0E1"));
        }
        else
        {
            ShowToast("[DEMO] Hanya " + spawned + "/3 sampah uji yang berhasil dibuat.", HexColor("#FFB74D"));
        }
    }

    private static bool SpawnTrash(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return false;
        }

        GameObject spawned = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        spawned.name = "[DEMO] " + prefab.name;
        return true;
    }

    private static GameObject PickPrefab(GameObject[] prefabs, string keyword, int fallbackIndex)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            if (prefab.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return prefab;
            }

            if (string.Equals(prefab.tag, keyword, StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }

        return fallbackIndex >= 0 && fallbackIndex < prefabs.Length ? prefabs[fallbackIndex] : null;
    }

    private void AddDemoPoints()
    {
        ScoreManager.instance.currentPoin += 500;

        if (AudioManager.Instance != null && AudioManager.Instance.sfxCorrect != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxCorrect);
        }

        ShowToast("[DEMO] +500 Poin Ditambahkan (Total: " + ScoreManager.instance.currentPoin + ")",
            HexColor("#FFD54F"));
    }

    private void HandleTeleportOrVictory()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shiftHeld)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.TriggerVictory();
            }
            return;
        }

        TeleportToBalaiDesa();
    }

    private void TeleportToBalaiDesa()
    {
        Transform player = GetPlayerTransform();
        if (player == null)
        {
            ShowToast("[DEMO] Player Kades tidak ditemukan.", HexColor("#FF5252"));
            return;
        }

        Transform building = FindBalaiDesaTransform();
        if (building == null)
        {
            ShowToast("[DEMO] Balai Desa tidak ditemukan di scene.", HexColor("#FF5252"));
            return;
        }

        Vector3 forward = building.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 targetPosition = building.position + forward * 6f;
        targetPosition.y = player.position.y;

        Rigidbody playerBody = player.GetComponent<Rigidbody>();
        if (playerBody != null)
        {
            playerBody.position = targetPosition;
        }
        else
        {
            player.position = targetPosition;
        }

        Vector3 lookDirection = building.position - targetPosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            player.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        ShowToast("[DEMO] Pindah ke Zona Balai Desa", Color.white);
    }

    private static Transform FindBalaiDesaTransform()
    {
        GameManager gm = GameManager.instance;
        if (gm != null)
        {
            if (gm.balaiDesaMegah != null && gm.balaiDesaMegah.activeInHierarchy)
            {
                return gm.balaiDesaMegah.transform;
            }

            if (gm.balaiDesaRusak != null)
            {
                return gm.balaiDesaRusak.transform;
            }

            if (gm.balaiDesaMegah != null)
            {
                return gm.balaiDesaMegah.transform;
            }
        }

        GameObject fallback = GameObject.Find("BalaiDesa_Rusak");
        return fallback != null ? fallback.transform : null;
    }

    private static Transform GetPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform;
        }

        PlayerController controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        return controller != null ? controller.transform : null;
    }

    private static void ShowToast(string message, Color color)
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowFeedback(message, color, 2.2f);
        }
    }

    private static Color HexColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }

        return Color.white;
    }
}
