using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YeVillage.EditorTools
{
    /// <summary>
    /// Setup Fase 1 "Ye Village Raker":
    /// standarisasi Tag, Layer Ground, struktur folder, dan penataan layer objek tanah.
    /// Menu: Tools > Ye Village Raker > Phase 1 - Setup Tags, Layers & Folders
    /// </summary>
    public static class YeVillageSetup
    {
        private static readonly string[] RequiredTags =
        {
            "Organik",
            "Anorganik",
            "B3",
            "TongHijau",
            "TongKuning",
            "TongMerah",
            "ShopNPC"
        };

        private static readonly string[] RequiredFolders =
        {
            "Assets/Scripts",
            "Assets/Scripts/Player",
            "Assets/Scripts/Trash",
            "Assets/Scripts/Managers",
            "Assets/Scripts/UI",
            "Assets/Scripts/Audio",
            "Assets/Scripts/VFX",
            "Assets/Prefabs",
            "Assets/Prefabs/Trash",
            "Assets/Prefabs/Environment",
            "Assets/Prefabs/Characters",
            "Assets/UI",
            "Assets/Audio",
            "Assets/Audio/BGM",
            "Assets/Audio/SFX",
            "Assets/VFX"
        };

        [MenuItem("Tools/Ye Village Raker/Phase 1 - Setup Tags, Layers & Folders")]
        public static void RunPhase1Setup()
        {
            foreach (string tagName in RequiredTags)
            {
                EnsureTag(tagName);
            }

            EnsureLayer("Ground");
            EnsureFolders();
            ApplyGroundLayer();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[YeVillage] Fase 1 selesai: Tag, Layer 'Ground', dan struktur folder sudah siap.");
        }

        private static SerializedObject GetTagManager()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[YeVillage] ProjectSettings/TagManager.asset tidak ditemukan.");
                return null;
            }

            return new SerializedObject(assets[0]);
        }

        private static void EnsureTag(string tagName)
        {
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
            {
                return;
            }

            SerializedProperty tags = tagManager.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log("[YeVillage] Tag '" + tagName + "' ditambahkan.");
        }

        private static void EnsureLayer(string layerName)
        {
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
            {
                return;
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[YeVillage] Layer '" + layerName + "' dibuat pada slot " + i + ".");
                    return;
                }
            }

            Debug.LogError("[YeVillage] Tidak ada slot layer kosong untuk '" + layerName + "'.");
        }

        private static void EnsureFolders()
        {
            foreach (string folder in RequiredFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                string parent = Path.GetDirectoryName(folder);
                string leaf = Path.GetFileName(folder);
                if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                {
                    continue;
                }

                parent = parent.Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    Debug.LogWarning("[YeVillage] Folder induk belum ada: " + parent);
                    continue;
                }

                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        private static void ApplyGroundLayer()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                Debug.LogWarning("[YeVillage] Layer 'Ground' belum tersedia, penataan layer dilewati.");
                return;
            }

            string[] groundObjectPaths =
            {
                "Ground",
                "YeVillage_Nature/Path_1",
                "YeVillage_Nature/Path_2",
                "YeVillage_Nature/Path_3",
                "YeVillage_Nature/Path_4",
                "YeVillage_Nature/Path_5"
            };

            foreach (string path in groundObjectPaths)
            {
                GameObject target = GameObject.Find(path);
                if (target != null)
                {
                    SetLayerRecursively(target, groundLayer);
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
