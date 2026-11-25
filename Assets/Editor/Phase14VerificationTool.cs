using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modul 14: Auto-wiring referensi Inspector + verifikasi cepat SampleScene.
/// Menu: YeVillage / 14 - Verify & Auto-Wire Scene Managers
/// </summary>
public static class Phase14VerificationTool
{
    private struct AssetBinding
    {
        public string keyword;
        public UnityEngine.Object asset;
    }

    [MenuItem("YeVillage/14 - Verify & Auto-Wire Scene Managers")]
    public static void VerifyAndWire()
    {
        Scene scene = SceneManager.GetActiveScene();
        bool changed = false;
        int wiredFields = 0;

        // ---------------- GameManager ----------------
        GameManager gameManager = FindInScene<GameManager>();
        if (gameManager != null)
        {
            if (gameManager.balaiDesaRusak == null)
            {
                GameObject found = FindSceneGameObject("BalaiDesa_Rusak");
                if (found != null)
                {
                    gameManager.balaiDesaRusak = found;
                    changed = true;
                    wiredFields++;
                }
            }

            if (gameManager.balaiDesaMegah == null)
            {
                GameObject found = FindSceneGameObject("BalaiDesa_Megah");
                if (found != null)
                {
                    gameManager.balaiDesaMegah = found;
                    changed = true;
                    wiredFields++;
                }
            }

            if (gameManager.trashSpawner == null)
            {
                TrashSpawner spawner = FindInScene<TrashSpawner>();
                if (spawner != null)
                {
                    gameManager.trashSpawner = spawner;
                    changed = true;
                    wiredFields++;
                }
            }

            EditorUtility.SetDirty(gameManager);
        }

        // ---------------- VFXManager ----------------
        VFXManager vfxManager = FindInScene<VFXManager>();
        AssetBinding[] vfxBindings =
        {
            new AssetBinding { keyword = "correct", asset = LoadAsset<GameObject>("Assets/VFX/Prefabs/VFX_Correct.prefab") },
            new AssetBinding { keyword = "wrong", asset = LoadAsset<GameObject>("Assets/VFX/Prefabs/VFX_Wrong.prefab") },
            new AssetBinding { keyword = "upgrade", asset = LoadAsset<GameObject>("Assets/VFX/Prefabs/VFX_Upgrade.prefab") },
            new AssetBinding { keyword = "renovation", asset = LoadAsset<GameObject>("Assets/VFX/Prefabs/VFX_Renovation.prefab") }
        };

        if (vfxManager != null)
        {
            int wired = WireObjectAssetFields(vfxManager, vfxBindings);
            wiredFields += wired;
            changed |= wired > 0;
            EditorUtility.SetDirty(vfxManager);
        }

        // ---------------- AudioManager ----------------
        AudioManager audioManager = FindInScene<AudioManager>();
        AssetBinding[] audioBindings =
        {
            new AssetBinding { keyword = "gameplay", asset = LoadAsset<AudioClip>("Assets/Audio/BGM/BGM_Gameplay.wav") },
            new AssetBinding { keyword = "victory", asset = LoadAsset<AudioClip>("Assets/Audio/BGM/BGM_Victory.wav") },
            new AssetBinding { keyword = "pickup", asset = LoadAsset<AudioClip>("Assets/Audio/SFX/SFX_Pickup.wav") },
            new AssetBinding { keyword = "correct", asset = LoadAsset<AudioClip>("Assets/Audio/SFX/SFX_Correct.wav") },
            new AssetBinding { keyword = "wrong", asset = LoadAsset<AudioClip>("Assets/Audio/SFX/SFX_Wrong.wav") },
            new AssetBinding { keyword = "buy", asset = LoadAsset<AudioClip>("Assets/Audio/SFX/SFX_Buy.wav") },
            new AssetBinding { keyword = "footstep", asset = LoadAsset<AudioClip>("Assets/Audio/SFX/SFX_Footstep.wav") }
        };

        if (audioManager != null)
        {
            int wired = WireObjectAssetFields(audioManager, audioBindings);
            wiredFields += wired;
            changed |= wired > 0;
            EditorUtility.SetDirty(audioManager);
        }

        // ---------------- ShopManager ----------------
        ShopManager shopManager = FindInScene<ShopManager>();
        if (shopManager != null)
        {
            GameObject player = FindPlayerObject();

            if (shopManager.playerController == null && player != null)
            {
                shopManager.playerController = player.GetComponent<PlayerController>();
                changed |= shopManager.playerController != null;
                wiredFields += shopManager.playerController != null ? 1 : 0;
            }

            if (shopManager.playerInventory == null && player != null)
            {
                shopManager.playerInventory = player.GetComponent<TrashInventory>();
                changed |= shopManager.playerInventory != null;
                wiredFields += shopManager.playerInventory != null ? 1 : 0;
            }

            if (shopManager.mesinKomposVisual == null)
            {
                GameObject found = FindSceneGameObject("MesinKomposVisual");
                if (found != null)
                {
                    shopManager.mesinKomposVisual = found;
                    changed = true;
                    wiredFields++;
                }
            }

            if (shopManager.mesinPressVisual == null)
            {
                GameObject found = FindSceneGameObject("MesinPressVisual");
                if (found != null)
                {
                    shopManager.mesinPressVisual = found;
                    changed = true;
                    wiredFields++;
                }
            }

            EditorUtility.SetDirty(shopManager);
        }

        // ---------------- TrashSpawner & TrashBin ----------------
        TrashSpawner trashSpawner = FindInScene<TrashSpawner>();
        TrashBin[] trashBins = FindAllInScene<TrashBin>();

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // ---------------- Laporan Console ----------------
        string gmStatus = gameManager == null
            ? "⚠️ MISSING"
            : (gameManager.balaiDesaRusak != null &&
               gameManager.balaiDesaMegah != null &&
               gameManager.trashSpawner != null ? "✅ OK" : "⚠️ MISSING REF");

        int vfxMissing = CountMissingBindings(vfxManager, vfxBindings);
        string vfxStatus = vfxManager == null
            ? "⚠️ MISSING"
            : (vfxMissing == 0 ? "✅ OK" : "⚠️ MISSING (" + vfxMissing + " field)");

        int audioMissing = CountMissingBindings(audioManager, audioBindings);
        string audioStatus = audioManager == null
            ? "⚠️ MISSING"
            : (audioMissing == 0 ? "✅ OK" : "⚠️ MISSING (" + audioMissing + " field)");

        bool shopOk = shopManager != null &&
                      shopManager.playerController != null &&
                      shopManager.playerInventory != null &&
                      shopManager.mesinKomposVisual != null &&
                      shopManager.mesinPressVisual != null;
        string shopStatus = shopManager == null
            ? "⚠️ MISSING"
            : (shopOk ? "✅ OK" : "⚠️ MISSING REF");

        string spawnerStatus = trashSpawner == null
            ? "⚠️ MISSING"
            : (trashSpawner.trashPrefabs != null && trashSpawner.trashPrefabs.Length > 0
                ? "✅ OK (" + trashSpawner.trashPrefabs.Length + " prefab)"
                : "⚠️ MISSING PREFAB");

        string binStatus = trashBins.Length == 3
            ? "✅ OK (3 tong)"
            : "⚠️ MISSING (" + trashBins.Length + "/3)";

        string summary =
            "[Phase14] ===== VERIFY & AUTO-WIRE =====\n" +
            "GameManager   : " + gmStatus + "\n" +
            "VFXManager    : " + vfxStatus + "\n" +
            "AudioManager  : " + audioStatus + "\n" +
            "ShopManager   : " + shopStatus + "\n" +
            "TrashSpawner  : " + spawnerStatus + "\n" +
            "TrashBin      : " + binStatus + "\n" +
            "Field terisi  : " + wiredFields;

        Debug.Log(summary);
        if (vfxMissing > 0) Debug.LogWarning("[Phase14] Masih ada field VFX kosong / prefab tidak ditemukan.");
        if (audioMissing > 0) Debug.LogWarning("[Phase14] Masih ada field Audio kosong / file audio tidak ditemukan.");

        string dialogMessage = summary + "\n\nSimpan scene sekarang?";
        if (EditorUtility.DisplayDialog("Phase 14 - Auto-Wire", dialogMessage, "Save Scene", "Nanti"))
        {
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static T FindInScene<T>() where T : Component
    {
        Scene activeScene = SceneManager.GetActiveScene();
        T[] all = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == activeScene)
            {
                return all[i];
            }
        }

        return null;
    }

    private static T[] FindAllInScene<T>() where T : Component
    {
        Scene activeScene = SceneManager.GetActiveScene();
        T[] all = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == activeScene)
            {
                count++;
            }
        }

        T[] result = new T[count];
        int index = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == activeScene)
            {
                result[index++] = all[i];
            }
        }

        return result;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindPlayerObject()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player;
        }

        PlayerController controller = FindInScene<PlayerController>();
        return controller != null ? controller.gameObject : null;
    }

    private static int WireObjectAssetFields(Component target, AssetBinding[] bindings)
    {
        if (target == null)
        {
            return 0;
        }

        int wired = 0;
        FieldInfo[] fields = target.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
            {
                continue;
            }

            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                continue;
            }

            UnityEngine.Object current = field.GetValue(target) as UnityEngine.Object;
            if (current != null)
            {
                continue;
            }

            string lowerName = field.Name.ToLowerInvariant();
            for (int b = 0; b < bindings.Length; b++)
            {
                AssetBinding binding = bindings[b];
                if (binding.asset == null || !lowerName.Contains(binding.keyword))
                {
                    continue;
                }

                if (field.FieldType == typeof(GameObject))
                {
                    GameObject prefab = binding.asset as GameObject;
                    if (prefab != null)
                    {
                        field.SetValue(target, prefab);
                        wired++;
                    }
                }
                else if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    GameObject prefab = binding.asset as GameObject;
                    if (prefab != null)
                    {
                        Component component = prefab.GetComponent(field.FieldType);
                        if (component != null)
                        {
                            field.SetValue(target, component);
                            wired++;
                        }
                    }
                }
                else if (typeof(AudioClip).IsAssignableFrom(field.FieldType))
                {
                    AudioClip clip = binding.asset as AudioClip;
                    if (clip != null)
                    {
                        field.SetValue(target, clip);
                        wired++;
                    }
                }

                break;
            }
        }

        if (wired > 0)
        {
            EditorUtility.SetDirty(target);
        }

        return wired;
    }

    private static int CountMissingBindings(Component target, AssetBinding[] bindings)
    {
        if (target == null)
        {
            return bindings.Length;
        }

        int missing = 0;
        FieldInfo[] fields = target.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int b = 0; b < bindings.Length; b++)
        {
            AssetBinding binding = bindings[b];
            bool matched = false;
            bool filled = false;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                {
                    continue;
                }

                if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                if (!IsFieldCompatible(field.FieldType, binding.asset))
                {
                    continue;
                }

                string lowerName = field.Name.ToLowerInvariant();
                if (!lowerName.Contains(binding.keyword))
                {
                    continue;
                }

                matched = true;
                UnityEngine.Object value = field.GetValue(target) as UnityEngine.Object;
                if (value != null)
                {
                    filled = true;
                    break;
                }
            }

            if (!matched || !filled)
            {
                missing++;
            }
        }

        return missing;
    }

    private static bool IsFieldCompatible(Type fieldType, UnityEngine.Object asset)
    {
        if (asset is GameObject)
        {
            return fieldType == typeof(GameObject) || typeof(Component).IsAssignableFrom(fieldType);
        }

        if (asset is AudioClip)
        {
            return typeof(AudioClip).IsAssignableFrom(fieldType);
        }

        return false;
    }

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        // Fallback: cari berdasarkan nama file di folder yang sama (mis. ekstensi .ogg).
        string folder = System.IO.Path.GetDirectoryName(path);
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrEmpty(folder))
        {
            string[] guids = AssetDatabase.FindAssets(fileName + " t:" + typeof(T).Name,
                new[] { folder.Replace('\\', '/') });
            if (guids.Length > 0)
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        if (asset == null)
        {
            Debug.LogWarning("[Phase14] Aset tidak ditemukan: " + path);
        }

        return asset;
    }
}
