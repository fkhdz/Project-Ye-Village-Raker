using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 14: Demo Presenter Mode untuk pengujian cepat dan presentasi kelas.
/// F7 = +3 langkah purifikasi air Zona 2 (Modul 17).
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

        if (Input.GetKeyDown(KeyCode.F7))
        {
            AddDemoWaterCleanStep();
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

    private void AddDemoWaterCleanStep()
    {
        Zona2WaterPurificationController water = UnityEngine.Object.FindFirstObjectByType<Zona2WaterPurificationController>();
        if (water == null)
        {
            ShowToast("[DEMO] Kontroler purifikasi air Zona 2 belum siap.", HexColor("#FF5252"));
            return;
        }

        water.AddDebugCleanStep(3);
        ShowToast("[DEMO] Air Sungai Zona 2 +3 Langkah Purifikasi (SDG 6)", HexColor("#4DD0E1"));
    }

    private void SpawnDemoTrash()
    {
        TrashSpawner spawner = UnityEngine.Object.FindFirstObjectByType<TrashSpawner>();
        if (spawner == null || !spawner.HasAnyPrefab())
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

        // Modul 16: ambil dari koleksi prefab terbaru agar demo memamerkan model 3D baru.
        GameObject organik = spawner.GetRandomPrefabForCategory("Organik");
        GameObject anorganik = spawner.GetRandomPrefabForCategory("Anorganik");
        GameObject b3 = spawner.GetRandomPrefabForCategory("B3");

        if (organik == null) organik = PickPrefab(spawner.trashPrefabs, "Organik", 0);
        if (anorganik == null) anorganik = PickPrefab(spawner.trashPrefabs, "Anorganik", 1);
        if (b3 == null) b3 = PickPrefab(spawner.trashPrefabs, "B3", 2);

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = player.position + forward * 2.2f;

        // Modul 18A (BUG-01): tiga titik melengkung 1.6m kiri-tengah-kanan,
        // lalu ditempel ke tanah via raycast agar tidak melayang setinggi pinggang.
        Vector3 leftPoint = center - right * 1.6f;
        Vector3 rightPoint = center + right * 1.6f;

        int spawned = 0;
        if (SpawnTrash(organik, SnapToGround(leftPoint))) spawned++;
        if (SpawnTrash(anorganik, SnapToGround(center))) spawned++;
        if (SpawnTrash(b3, SnapToGround(rightPoint))) spawned++;

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

    /// <summary>
    /// Modul 18A (BUG-01): raycast ke bawah dari Y+5 terhadap permukaan tanah,
    /// abaikan Player dan sampah lain, lalu kembalikan posisi yang menempel di tanah.
    /// </summary>
    private static Vector3 SnapToGround(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            new Vector3(position.x, position.y + 5f, position.z),
            Vector3.down,
            20f,
            ~0,
            QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null)
                {
                    continue;
                }

                if (col.GetComponentInParent<PlayerController>() != null)
                {
                    continue;
                }

                if (col.GetComponentInParent<TrashItem>() != null)
                {
                    continue;
                }

                if (col.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                {
                    continue;
                }

                return new Vector3(position.x, hits[i].point.y, position.z);
            }
        }

        return position;
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

            // Cek Tag lebih dulu: "Anorganik" mengandung kata "Organik".
            if (string.Equals(prefab.tag, keyword, StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }

            if (prefab.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
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
