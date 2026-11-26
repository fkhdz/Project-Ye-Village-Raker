using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ZoneSplitBuilder
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string Level1Path = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string SamplePath = "Assets/Scenes/SampleScene.unity";
    private const string Level2Path = "Assets/Scenes/Level2_Zona2_Pinggiran.unity";
    private const string Level3Path = "Assets/Scenes/Level3_Zona3_BalaiDesa.unity";

    private const string MainMenuName = "MainMenu";
    private const string Level1Name = "Level1_Zona1_Pinggiran";
    private const string Level2Name = "Level2_Zona2_Pinggiran";
    private const string Level3Name = "Level3_Zona3_BalaiDesa";

    private const string StoryRoot = "--- STORY / GATE ---";

    [MenuItem("YeVillage/Story/Split Zone 2 & Zone 3")]
    public static void Split()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[ZoneSplit] Ada scene belum disimpan: " + open.name);
                return;
            }
        }

        if (!File.Exists(SamplePath))
        {
            Debug.LogError("[ZoneSplit] Scene sumber tidak ditemukan: " + SamplePath);
            return;
        }

        if (!File.Exists(Level2Path)) AssetDatabase.CopyAsset(SamplePath, Level2Path);
        if (!File.Exists(Level3Path)) AssetDatabase.CopyAsset(SamplePath, Level3Path);
        AssetDatabase.Refresh();

        string returnPath = SceneManager.GetActiveScene().path;

        ConfigureLevel1();
        ConfigureLevel2();
        ConfigureLevel3();
        ConfigureBuildSettings();

        if (!string.IsNullOrEmpty(returnPath) && File.Exists(returnPath))
        {
            EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ZoneSplit] Zona 2 dan Zona 3 sekarang terpisah scene + portal kembali aktif.");
    }

    private static void ConfigureLevel1()
    {
        Scene scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

        foreach (ZoneGate gate in FindAll<ZoneGate>(scene))
        {
            if (gate.gateId == "Zone2")
            {
                gate.targetSceneName = Level2Name;
                EditorUtility.SetDirty(gate);
            }
        }

        foreach (ZoneExitMarker exit in FindAll<ZoneExitMarker>(scene))
        {
            exit.targetSceneName = Level2Name;
            exit.portalLabel = "Lanjut ke Zona 2";
            exit.requireZoneAccess = true;
            EditorUtility.SetDirty(exit);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZoneSplit] Level 1 sekarang menuju " + Level2Name + ".");
    }

    private static void ConfigureLevel2()
    {
        Scene scene = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);
        Transform story = FindOrCreateRoot(scene, StoryRoot);

        DestroyByPath(scene, "Environment_Expanded_P15|Zona3_BalaiDesa_Area");
        DestroyByPath(scene, "SpawnPoints_Expanded|Zona3_BalaiDesa");
        DestroyByName(scene, "Zona3_BalaiDesa");
        DestroyByName(scene, "BalaiDesa_Rusak");
        DestroyByName(scene, "BalaiDesa_Megah");
        DestroyByPath(scene, StoryRoot + "|Zone3_IntroTrigger");
        DestroyByName(scene, "SP_Zona3_01");
        DestroyByName(scene, "SP_Zona3_02");
        DestroyByName(scene, "SP_Zona3_03");

        foreach (ZoneGate gate in FindAll<ZoneGate>(scene))
        {
            if (gate.gateId == "Zone3")
            {
                gate.targetSceneName = Level3Name;
                EditorUtility.SetDirty(gate);
            }
        }

        CreateExit(story, "Exit_To_Zona3", new Vector3(2f, 1.5f, 27.5f), new Vector3(12f, 4f, 3f),
            Level3Name, "Lanjut ke Zona 3", true, false);

        Transform back = CreateExit(story, "Exit_Back_To_Zone1", new Vector3(0f, 1.5f, -47f), new Vector3(8f, 4f, 3f),
            Level1Name, "Kembali ke Zona 1", false, true);

        ConfigureSpawner(scene, false, true, false);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZoneSplit] " + Level2Name + " disimpan (Zona 3 dihapus, portal ke Zona 1 & 3 ditambahkan).");
    }

    private static void ConfigureLevel3()
    {
        Scene scene = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        Transform story = FindOrCreateRoot(scene, StoryRoot);

        DestroyByPath(scene, StoryRoot + "|Zone2_IntroTrigger");
        DestroyByPath(scene, StoryRoot + "|Gate_Zone2_To_Zone3");
        DestroyByPath(scene, "SpawnPoints_Expanded|Zona2_Riverbank");
        DestroyByPath(scene, "SpawnPoints_Expanded|Zona1_TPS");
        DestroyByPath(scene, "Environment_Expanded_P15|Zona1_TPS_Area");
        DestroyByName(scene, "SP_Zona1_01");
        DestroyByName(scene, "SP_Zona1_02");
        DestroyByName(scene, "SP_Zona1_03");
        DestroyByName(scene, "SP_Zona2_01");
        DestroyByName(scene, "SP_Zona2_02");
        DestroyByName(scene, "SP_Zona2_03");

        Transform player = FindByName(scene, "Kades");
        if (player != null) player.position = new Vector3(0f, 0.1f, 27f);

        CreateExit(story, "Exit_Back_To_Zone2", new Vector3(0f, 1.5f, 23f), new Vector3(10f, 4f, 3f),
            Level2Name, "Kembali ke Zona 2", false, true);

        ConfigureSpawner(scene, false, false, true);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZoneSplit] " + Level3Name + " disimpan (spawn di Zona 3, portal ke Zona 2 ditambahkan).");
    }

    private static void ConfigureSpawner(Scene scene, bool zone1, bool zone2, bool zone3)
    {
        TrashSpawner spawner = FindFirst<TrashSpawner>(scene);
        if (spawner == null) return;

        spawner.mode = TrashSpawner.SpawnerMode.ZonaQuota;
        SerializedObject serialized = new SerializedObject(spawner);
        SetTransformArray(serialized, "zona1SpawnPoints", zone1 ? CollectChildren(scene, "Zona1_TPS") : new Transform[0]);
        SetTransformArray(serialized, "zona2SpawnPoints", zone2 ? CollectChildren(scene, "Zona2_Riverbank") : new Transform[0]);
        SetTransformArray(serialized, "zona3SpawnPoints", zone3 ? CollectChildren(scene, "Zona3_BalaiDesa") : new Transform[0]);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);
    }

    private static Transform[] CollectChildren(Scene scene, string parentName)
    {
        Transform parent = FindByName(scene, parentName);
        if (parent == null) return new Transform[0];

        List<Transform> result = new List<Transform>();
        for (int i = 0; i < parent.childCount; i++) result.Add(parent.GetChild(i));
        return result.ToArray();
    }

    private static void SetTransformArray(SerializedObject serialized, string propertyName, Transform[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static Transform CreateExit(Transform parent, string name, Vector3 position, Vector3 size,
        string target, string label, bool requireAccess, bool showSign)
    {
        Transform found = parent.Find(name);
        GameObject go;
        if (found != null)
        {
            go = found.gameObject;
        }
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }

        go.transform.position = position;
        BoxCollider collider = go.GetComponent<BoxCollider>();
        if (collider == null) collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        collider.center = Vector3.zero;

        ZoneExitMarker marker = go.GetComponent<ZoneExitMarker>();
        if (marker == null) marker = go.AddComponent<ZoneExitMarker>();
        marker.targetSceneName = target;
        marker.portalLabel = label;
        marker.requireZoneAccess = requireAccess;
        marker.loadOnEnter = false;
        marker.requireTargetScore = false;
        EditorUtility.SetDirty(marker);

        if (showSign)
        {
            WorldLabelBillboard.CreateLabel(go.transform, label.ToUpperInvariant(),
                new Color(0.12f, 0.34f, 0.22f), new Vector3(0f, 3.6f, 0f));
        }

        return go.transform;
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(Level1Path, true),
            new EditorBuildSettingsScene(Level2Path, true),
            new EditorBuildSettingsScene(Level3Path, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Transform FindOrCreateRoot(Scene scene, string name)
    {
        Transform found = FindByName(scene, name);
        if (found != null) return found;

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go.transform;
    }

    private static Transform FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root.transform;
            Transform found = root.transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindByPath(Scene scene, string path)
    {
        string[] parts = path.Split('|');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != parts[0]) continue;

            Transform current = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null) return null;
            }
            return current;
        }
        return null;
    }

    private static void DestroyByPath(Scene scene, string path)
    {
        Transform found = FindByPath(scene, path);
        if (found != null) Object.DestroyImmediate(found.gameObject);
    }

    private static void DestroyByName(Scene scene, string name)
    {
        Transform found = FindByName(scene, name);
        if (found != null) Object.DestroyImmediate(found.gameObject);
    }

    private static List<T> FindAll<T>(Scene scene) where T : Component
    {
        List<T> result = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<T>(true));
        }
        return result;
    }

    private static T FindFirst<T>(Scene scene) where T : Component
    {
        List<T> found = FindAll<T>(scene);
        return found.Count > 0 ? found[0] : null;
    }
}
