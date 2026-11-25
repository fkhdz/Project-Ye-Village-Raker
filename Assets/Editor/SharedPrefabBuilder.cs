using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SharedPrefabBuilder
{
    private const string SharedFolder = "Assets/Prefabs/Shared";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string WoodMaterialPath = "Assets/Materials/Ye_Village/M_Wood.mat";

    [MenuItem("YeVillage/Level1/Build Shared Prefabs")]
    public static void BuildSharedPrefabs()
    {
        EnsureFolder(SharedFolder);

        Scene sampleScene = SceneManager.GetSceneByPath(SampleScenePath);
        if (!sampleScene.IsValid() || !sampleScene.isLoaded)
        {
            Debug.LogError("[SharedPrefabs] SampleScene belum terbuka. Buka scene tersebut lalu jalankan menu lagi.");
            return;
        }

        List<string> created = new List<string>();

        Scene previousActive = SceneManager.GetActiveScene();
        Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(tempScene);

        try
        {
            SaveCopy(sampleScene, "Kades", "PF_Player_Kades", created);
            SaveCopy(sampleScene, "Main Camera", "PF_MainCamera", created);
            SaveCopy(sampleScene, "Canvas_HUD", "PF_Canvas_HUD", created);
            SaveCopy(sampleScene, "AudioManager", "PF_AudioManager", created);
            SaveCopy(sampleScene, "VFXManager", "PF_VFXManager", created);
            SaveCopy(sampleScene, "TetuaDesa", "PF_TetuaDesa", created);
            SaveCopy(sampleScene, "TongHijau", "PF_Tong_Hijau", created);
            SaveCopy(sampleScene, "TongKuning", "PF_Tong_Kuning", created);
            SaveCopy(sampleScene, "TongMerah", "PF_Tong_Merah", created);
            SaveCopy(sampleScene, "MesinKomposVisual", "PF_Mesin_Kompos", created);
            SaveCopy(sampleScene, "MesinPressVisual", "PF_Mesin_Press", created);

            CreateScoreManagerPrefab(created);
            CreatePalletPrefab(created);
        }
        finally
        {
            SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(tempScene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (created.Count == 0)
        {
            Debug.Log("[SharedPrefabs] Semua prefab sudah ada, tidak ada yang dibuat.");
        }
        else
        {
            Debug.Log("[SharedPrefabs] Selesai. Prefab dibuat: " + string.Join(", ", created));
        }
    }

    private static void SaveCopy(Scene sourceScene, string sourceName, string prefabName, List<string> created)
    {
        string path = SharedFolder + "/" + prefabName + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject source = FindInScene(sourceScene, sourceName);
        if (source == null)
        {
            Debug.LogWarning("[SharedPrefabs] Sumber tidak ditemukan: " + sourceName);
            return;
        }

        GameObject copy = Object.Instantiate(source);
        copy.name = prefabName;
        copy.transform.SetParent(null, true);
        copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        copy.transform.localScale = Vector3.one;
        StripExternalReferences(copy);

        bool success;
        PrefabUtility.SaveAsPrefabAsset(copy, path, out success);
        Object.DestroyImmediate(copy);

        if (success)
        {
            created.Add(prefabName);
        }
        else
        {
            Debug.LogError("[SharedPrefabs] Gagal menyimpan prefab: " + path);
        }
    }

    private static void CreateScoreManagerPrefab(List<string> created)
    {
        string path = SharedFolder + "/PF_ScoreManager.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject temp = new GameObject("PF_ScoreManager");
        temp.AddComponent<ScoreManager>();
        SaveTemporary(temp, path, "PF_ScoreManager", created);
    }

    private static void CreatePalletPrefab(List<string> created)
    {
        string path = SharedFolder + "/PF_Pallet_TPS.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        GameObject root = new GameObject("PF_Pallet_TPS");

        GameObject wood = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wood.name = "Pallet_Wood";
        wood.transform.SetParent(root.transform, false);
        wood.transform.localScale = new Vector3(8f, 0.2f, 4f);
        wood.transform.localPosition = new Vector3(0f, -0.1f, 0f);

        Material woodMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoodMaterialPath);
        if (woodMaterial != null)
        {
            wood.GetComponent<Renderer>().sharedMaterial = woodMaterial;
        }
        else
        {
            Debug.LogWarning("[SharedPrefabs] Material kayu tidak ditemukan: " + WoodMaterialPath);
        }

        SaveTemporary(root, path, "PF_Pallet_TPS", created);
    }

    private static void SaveTemporary(GameObject temp, string path, string prefabName, List<string> created)
    {
        bool success;
        PrefabUtility.SaveAsPrefabAsset(temp, path, out success);
        Object.DestroyImmediate(temp);

        if (success)
        {
            created.Add(prefabName);
        }
        else
        {
            Debug.LogError("[SharedPrefabs] Gagal menyimpan prefab: " + path);
        }
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static void StripExternalReferences(GameObject root)
    {
        HashSet<int> localIds = new HashSet<int>();
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            localIds.Add(transform.gameObject.GetInstanceID());
        }

        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();
            bool changed = false;

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                Object reference = property.objectReferenceValue;
                if (reference == null)
                {
                    continue;
                }

                GameObject referenceObject = reference as GameObject;
                if (referenceObject == null && reference is Component referenceComponent)
                {
                    referenceObject = referenceComponent.gameObject;
                }

                if (referenceObject != null && !localIds.Contains(referenceObject.GetInstanceID()))
                {
                    property.objectReferenceValue = null;
                    changed = true;
                }
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = System.IO.Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
