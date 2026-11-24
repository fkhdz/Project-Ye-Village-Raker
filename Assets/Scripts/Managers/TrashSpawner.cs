using System.Collections;
using UnityEngine;

/// <summary>
/// Memunculkan sampah baru secara berkala agar pemain tidak kehabisan sumber poin.
/// Dipasang pada GameObject kosong "TrashSpawnerManager" (sesuai dokumen 05).
/// </summary>
public class TrashSpawner : MonoBehaviour
{
    [Header("Prefab & Titik Spawn")]
    public GameObject[] trashPrefabs;
    public Transform[] spawnPoints;

    [Header("Pengaturan Spawn")]
    public float spawnInterval = 5f;
    public int maxTrashOnMap = 15;
    public bool isSpawning = true;

    private void Start()
    {
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogError("[TrashSpawner] trashPrefabs kosong. Isi array prefab sampah di Inspector.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[TrashSpawner] spawnPoints kosong. Isi array titik spawn di Inspector.");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (!isSpawning)
            {
                continue;
            }

            // Hitung total sampah aktif di scene berdasarkan tag (Organik + Anorganik + B3).
            int currentTrashCount = GameObject.FindGameObjectsWithTag("Organik").Length
                                  + GameObject.FindGameObjectsWithTag("Anorganik").Length
                                  + GameObject.FindGameObjectsWithTag("B3").Length;

            if (currentTrashCount >= maxTrashOnMap)
            {
                continue;
            }

            int randomPoint = Random.Range(0, spawnPoints.Length);
            int randomTrash = Random.Range(0, trashPrefabs.Length);

            if (spawnPoints[randomPoint] == null || trashPrefabs[randomTrash] == null)
            {
                continue;
            }

            // Instantiate prefab sampah acak di titik spawn acak.
            Instantiate(trashPrefabs[randomTrash], spawnPoints[randomPoint].position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Dipanggil saat ending (Fase 7) untuk menghentikan pemunculan sampah.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>
    /// Menghapus seluruh sampah yang masih berserakan di scene.
    /// Dipakai saat ending agar desa benar-benar bersih setelah renovasi.
    /// </summary>
    public void ClearAllTrash()
    {
        string[] trashTags = { "Organik", "Anorganik", "B3" };
        foreach (string tag in trashTags)
        {
            GameObject[] trashObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject trash in trashObjects)
            {
                Destroy(trash);
            }
        }
    }
}
