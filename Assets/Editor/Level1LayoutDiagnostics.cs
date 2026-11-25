using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level1LayoutDiagnostics
{
    private const string ScenePath = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string Village = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Buildings/";

    [MenuItem("YeVillage/Level1/Diagnose Layout")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Vector3 spawn = Vector3.zero;
        GameObject sp = GameObject.Find("PlayerSpawnPoint");
        if (sp != null) spawn = sp.transform.position;
        Debug.Log("[Diag] Spawn=" + spawn);

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                if (t == go.transform) continue;
                float d = Vector3.Distance(t.position, spawn);
                if (d < 6f)
                {
                    Debug.Log("[Diag] Dekat spawn: " + t.name + " @ " + t.position.ToString("F2") + " jarak " + d.ToString("F2"));
                }
            }
        }

        string[] models = { "House_1.fbx", "House_2.fbx", "House_3.fbx", "House_4.fbx" };
        foreach (string file in models)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Village + file);
            if (model == null) continue;
            GameObject inst = PrefabUtility.InstantiatePrefab(model) as GameObject;
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            Renderer[] rs = inst.GetComponentsInChildren<Renderer>(true);
            Bounds wb = rs.Length > 0 ? rs[0].bounds : new Bounds();
            for (int i = 1; i < rs.Length; i++) wb.Encapsulate(rs[i].bounds);
            MeshFilter mf = inst.GetComponentInChildren<MeshFilter>(true);
            Bounds lb = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds();
            Debug.Log("[Diag] " + file + " rootRot=" + inst.transform.localEulerAngles.ToString("F1") +
                      " meshLocal minY=" + lb.min.y.ToString("F2") + " maxY=" + lb.max.y.ToString("F2") +
                      " | world(identity) minY=" + wb.min.y.ToString("F2") + " maxY=" + wb.max.y.ToString("F2") + " size=" + wb.size.ToString("F2"));
            inst.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            rs = inst.GetComponentsInChildren<Renderer>(true);
            wb = rs.Length > 0 ? rs[0].bounds : new Bounds();
            for (int i = 1; i < rs.Length; i++) wb.Encapsulate(rs[i].bounds);
            Debug.Log("[Diag] " + file + " world(X=-90) minY=" + wb.min.y.ToString("F2") + " maxY=" + wb.max.y.ToString("F2") + " size=" + wb.size.ToString("F2"));
            Object.DestroyImmediate(inst);
        }
    }
}
