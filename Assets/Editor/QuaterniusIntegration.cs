using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class QuaterniusIntegration
{
    const string Q = "Assets/ThirdParty/Quaternius";
    const string MatDir = "Assets/Materials/Quaternius";
    const string AnimDir = "Assets/Animations";
    const string MeshDir = "Assets/Models/Trash";
    const string LogPath = "../Logs/integration.log";

    static StringBuilder log;

    static void L(string s)
    {
        log.AppendLine(s);
        Debug.Log("[Q-Integration] " + s);
    }

    [MenuItem("Tools/Quaternius/4. Integrate All")]
    public static void IntegrateAll()
    {
        log = new StringBuilder();
        AssetDatabase.Refresh();

        try
        {
            BuildTrashMeshes();
            ConfigureImports();
            BuildAnimators();
            BuildScene();
            SaveScene();
            WriteLog();
        }
        catch (Exception e)
        {
            L("EXCEPTION: " + e);
            WriteLog();
            throw;
        }

        L("DONE");
        WriteLog();
    }

    static void WriteLog()
    {
        var p = Path.GetFullPath(Path.Combine(Application.dataPath, LogPath));
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllText(p, log.ToString());
    }

    // ---------------------------------------------------------------- imports

    static void ConfigureImports()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);

        var files = AssetDatabase.FindAssets("t:Model", new[] { Q })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToArray();

        L($"Configuring {files.Length} models...");
        int idx = 0;
        foreach (var path in files)
        {
            idx++;
            EditorUtility.DisplayProgressBar("Quaternius", path, idx / (float)files.Length);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;

            bool isCharacter = path.Contains("/Characters/");
            bool changed = false;

            var embedded = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
            var remap = imp.GetExternalObjectMap();
            foreach (var m in embedded)
            {
                var um = GetOrCreateMaterial(m);
                if (um == null) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name);
                if (!remap.TryGetValue(id, out var cur) || cur != um)
                {
                    imp.AddRemap(id, um);
                    changed = true;
                }
            }

            if (imp.importCameras) { imp.importCameras = false; changed = true; }
            if (imp.importLights) { imp.importLights = false; changed = true; }

            if (isCharacter)
            {
                if (imp.animationType != ModelImporterAnimationType.Generic)
                {
                    imp.animationType = ModelImporterAnimationType.Generic;
                    changed = true;
                }
                if (!imp.importAnimation) { imp.importAnimation = true; changed = true; }
                if (changed) imp.SaveAndReimport();
                continue;
            }

            if (changed) imp.SaveAndReimport();
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        L("Imports done.");
    }

    static Material GetOrCreateMaterial(Material src)
    {
        string shaderName = src.shader != null ? src.shader.name : "";
        Color c = Color.white;
        if (src.HasProperty("_Color")) c = src.GetColor("_Color");
        else if (src.HasProperty("_BaseColor")) c = src.GetColor("_BaseColor");

        string key = "Q_" + Sanitize(src.name) + "_" + ColorUtility.ToHtmlStringRGB(c);
        string path = MatDir + "/" + key + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            L("URP Lit shader not found!");
            shader = Shader.Find("Standard");
        }
        var mat = new Material(shader);
        mat.name = key;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", shaderName.Contains("Fire") ? 0f : 0.12f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static string Sanitize(string s)
    {
        var sb = new StringBuilder();
        foreach (char ch in s) sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
        return sb.ToString();
    }

    // --------------------------------------------------------------- animators

    static void BuildAnimators()
    {
        EnsureFolder(AnimDir);

        BuildController("Assets/Animations/Kades.controller",
            Q + "/Characters/Suit.fbx", true, "CharacterArmature|Idle", "CharacterArmature|Walk");
        BuildController("Assets/Animations/TetuaDesa.controller",
            Q + "/Characters/Worker.fbx", false, "CharacterArmature|Idle", null);

        L("Animator controllers built.");
    }

    static void BuildController(string outPath, string modelPath, bool twoStates, string idleClip, string walkClip)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(outPath) != null)
            AssetDatabase.DeleteAsset(outPath);

        var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().ToArray();
        var idle = clips.FirstOrDefault(c => c.name == idleClip) ?? clips.FirstOrDefault(c => c.name.EndsWith("|Idle"));
        AnimationClip walk = null;
        if (twoStates && walkClip != null)
            walk = clips.FirstOrDefault(c => c.name == walkClip) ?? clips.FirstOrDefault(c => c.name.EndsWith("|Walk"));

        if (idle == null) { L("No idle clip for " + modelPath); return; }

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(outPath);
        var sm = ctrl.layers[0].stateMachine;
        var idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        if (walk != null)
        {
            ctrl.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
            var walkState = sm.AddState("Walk");
            walkState.motion = walk;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.12f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "isWalking");

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "isWalking");
        }
    }

    static Avatar LoadAvatar(string modelPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(a => a.isValid);
    }

    // ---------------------------------------------------------------- scene

    static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static GameObject InstantiateModel(string rel, Transform parent, Vector3 pos, float rotY, float scale = 1f)
    {
        string path = Q + "/" + rel + ".fbx";
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            L("MISSING MODEL: " + path);
            return null;
        }
        var go = (GameObject)Object.Instantiate(asset, parent);
        go.name = Path.GetFileNameWithoutExtension(rel);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * go.transform.localRotation;
        go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * scale);
        return go;
    }

    static void ClearIfExists(Transform parent, string name)
    {
        var t = parent.Find(name);
        while (t != null)
        {
            Object.DestroyImmediate(t.gameObject);
            t = parent.Find(name);
        }
    }

    static void BuildScene()
    {
        var scene = SceneManager.GetSceneByName("SampleScene");
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("SampleScene not loaded");
        SceneManager.SetActiveScene(scene);
        L("Active scene = SampleScene");

        EditorSceneManager.MarkSceneDirty(scene);

        // Ground bigger so the village hall sits on land.
        var ground = FindRoot(scene, "Ground");
        if (ground != null) ground.transform.localScale = new Vector3(3f, 1f, 3f);

        // ---- Nature
        var nature = FindRoot(scene, "YeVillage_Nature");
        if (nature == null)
        {
            nature = new GameObject("YeVillage_Nature");
            SceneManager.MoveGameObjectToScene(nature, scene);
        }
        ClearChildren(nature.transform);

        AddNature(nature.transform, "Environment/Nature/PalmTree_1", -5.6f, -4.6f, 25f, 1.05f);
        AddNature(nature.transform, "Environment/Nature/PalmTree_3", 6.6f, -6.3f, 135f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/PalmTree_2", -6.2f, 3.2f, 300f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/PalmTree_4", 6.4f, 4.0f, 210f, 1.05f);

        AddNature(nature.transform, "Environment/Nature/CommonTree_1", -8.6f, 7.6f, 15f, 1.15f);
        AddNature(nature.transform, "Environment/Nature/CommonTree_2", 8.6f, 7.9f, 100f, 1.1f);
        AddNature(nature.transform, "Environment/Nature/CommonTree_3", 8.9f, 0.2f, 200f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/Willow_1", -7.8f, -0.8f, 40f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/Willow_2", 5.2f, 1.1f, 220f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/Willow_3", 4.9f, 6.4f, 150f, 0.95f);
        AddNature(nature.transform, "Environment/Nature/Bush_1", -4.3f, 5.3f, 0f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/Bush_2", 4.4f, 5.1f, 0f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/BushBerries_1", -8.4f, 6.2f, 30f, 1.1f);
        AddNature(nature.transform, "Environment/Nature/Bush_1", 7.4f, -7.2f, 80f, 1.1f);

        AddNature(nature.transform, "Environment/Nature/Plant_1", 1.2f, -4.55f, 10f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/Plant_2", -0.9f, 4.3f, 40f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/Plant_3", -2.3f, -2.6f, 0f, 1.1f);
        AddNature(nature.transform, "Environment/Nature/Plant_1", -2.0f, 1.8f, 80f, 1.0f);
        AddNature(nature.transform, "Environment/Nature/Plant_2", 4.9f, -3.0f, 120f, 1.0f);

        float[][] grass = {
            new[] {-1.9f,-1.6f, 1.0f}, new[] {2.2f,0.5f, 1.1f}, new[] {-2.4f,3.4f, 1.0f},
            new[] {3.1f,4.1f, 1.1f}, new[] {6.4f,-1.9f, 1.0f}, new[] {-6.0f,1.5f, 1.0f},
            new[] {0.8f,-6.2f, 1.0f}, new[] {-7.0f,-4.0f, 1.0f}, new[] {7.8f,2.9f, 1.0f},
            new[] {3.4f,-6.7f, 1.0f}, new[] {-4.8f,-2.2f, 1.1f}, new[] {5.9f,3.0f, 0.9f}
        };
        string[] grassModels = { "Grass", "Grass_2", "Grass_Short" };
        for (int i = 0; i < grass.Length; i++)
            AddNature(nature.transform, "Environment/Nature/" + grassModels[i % 3], grass[i][0], grass[i][1], i * 37f, grass[i][2]);

        AddNature(nature.transform, "Environment/Nature/Rock_1", -2.6f, -2.9f, 30f, 1.3f);
        AddNature(nature.transform, "Environment/Nature/Rock_2", 4.7f, -2.2f, 120f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/Rock_3", 0.9f, 3.4f, 0f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/Rock_Moss_1", -0.4f, -3.9f, 0f, 1.3f);
        AddNature(nature.transform, "Environment/Nature/Rock_1", 7.5f, -1.0f, 210f, 1.1f);
        AddNature(nature.transform, "Environment/Nature/Rock_3", -6.8f, -2.0f, 75f, 1.4f);
        AddNature(nature.transform, "Environment/Nature/TreeStump", 6.8f, -8.0f, 0f, 1.2f);
        AddNature(nature.transform, "Environment/Nature/WoodLog", 7.8f, -8.6f, 30f, 1.3f);

        AddNature(nature.transform, "Environment/Nature/Lilypad", 1.7f, -1.1f, 0f, 1.0f, 0.045f);
        AddNature(nature.transform, "Environment/Nature/Lilypad", 2.9f, -2.3f, 60f, 1.0f, 0.045f);
        AddNature(nature.transform, "Environment/Nature/Lilypad", 2.3f, -0.6f, 140f, 1.0f, 0.045f);

        BuildPaths(nature.transform);
        BuildFences(nature.transform);

        // ---- Decor group
        var decor = FindRoot(scene, "Village_Decor");
        if (decor == null)
        {
            decor = new GameObject("Village_Decor");
            SceneManager.MoveGameObjectToScene(decor, scene);
        }
        ClearChildren(decor.transform);

        AddDecor(decor.transform, "Environment/MedievalVillage/Buildings/House_1", -8.6f, 2.2f, 90f, 1f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Buildings/House_2", 8.8f, 3.2f, -90f, 1f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Buildings/Blacksmith", -8.4f, -3.0f, 90f, 1f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Buildings/House_3", 8.6f, -4.6f, -90f, 1.15f, true);

        AddDecor(decor.transform, "Environment/MedievalVillage/Props/MarketStand_1", -7.4f, -5.9f, 100f, 1.55f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/MarketStand_2", 7.3f, 1.8f, -100f, 1.4f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Well", -3.5f, -6.2f, 20f, 1.3f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Bench_1", -2.1f, 4.0f, 0f, 1.2f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Bench_2", 2.1f, 4.0f, 180f, 1.2f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Barrel", -2.7f, -6.6f, 0f, 1.2f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Barrel", 7.9f, -6.4f, 30f, 1.2f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Crate", 6.7f, -8.6f, 15f, 1.3f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Crate", 7.6f, -8.0f, 35f, 1.1f, true);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Bag", -3.0f, -7.1f, 20f, 1.2f, false);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Cart", 5.2f, -8.4f, 25f, 1.3f, false);
        AddDecor(decor.transform, "Environment/MedievalVillage/Props/Bonfire", 3.6f, 5.7f, 0f, 1.3f, false);

        BuildBalai(scene, "BalaiDesa_Rusak", "Environment/MedievalVillage/Buildings/House_3", 1.7f, new Vector3(0f, 0f, 12.5f), 180f);
        BuildBalai(scene, "BalaiDesa_Megah", "Environment/MedievalVillage/Buildings/Inn", 1.3f, new Vector3(0f, 0f, 11.5f), 180f);

        BuildCharacters(scene);
        BuildBins(scene);
        BuildTrashSamples(scene, nature.transform);
        SetSpawnPointHeights(scene, 0.02f);

        EditorSceneManager.MarkSceneDirty(scene);
        L("Scene rebuilt.");
    }

    static void AddNature(Transform parent, string rel, float x, float z, float rotY, float scale, float y = 0f)
    {
        InstantiateModel(rel, parent, new Vector3(x, y, z), rotY, scale);
    }

    static void AddDecor(Transform parent, string rel, float x, float z, float rotY, float scale, bool collider)
    {
        var go = InstantiateModel(rel, parent, new Vector3(x, 0f, z), rotY, scale);
        if (go != null && collider) FitBoxCollider(go, true);
    }

    static void BuildPaths(Transform parent)
    {
        for (int i = 0; i < 11; i++)
        {
            float z = -5f + i;
            InstantiateModel("Environment/MedievalVillage/Props/Path_Straight", parent, new Vector3(-0.26f, 0.01f, z), 0f, 1f);
            InstantiateModel("Environment/MedievalVillage/Props/Path_Straight", parent, new Vector3(0.26f, 0.01f, z), 0f, 1f);
        }
        // plaza tiles under key interactables
        InstantiateModel("Environment/MedievalVillage/Props/Path_Square", parent, new Vector3(-2.6f, 0.008f, -7f), 0f, 2.3f);
        InstantiateModel("Environment/MedievalVillage/Props/Path_Square", parent, new Vector3(0f, 0.008f, -7f), 0f, 2.3f);
        InstantiateModel("Environment/MedievalVillage/Props/Path_Square", parent, new Vector3(2.6f, 0.008f, -7f), 0f, 2.3f);
        InstantiateModel("Environment/MedievalVillage/Props/Path_Square", parent, new Vector3(-5.5f, 0.008f, -7.5f), 0f, 2.3f);
        InstantiateModel("Environment/MedievalVillage/Props/Path_Square", parent, new Vector3(5.2f, 0.008f, -8.4f), 0f, 2.3f);
    }

    static void BuildFences(Transform parent)
    {
        float[] xs = { -3.2f, -2.4f, -1.6f, -0.8f, 0.8f, 1.6f, 2.4f, 3.2f };
        foreach (float x in xs)
            InstantiateModel("Environment/MedievalVillage/Props/Fence", parent, new Vector3(x, 0.01f, 4.6f), 0f, 1f);
    }

    static void FitBoxCollider(GameObject root, bool keepExisting)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        var bc = root.GetComponent<BoxCollider>();
        if (bc == null) bc = root.AddComponent<BoxCollider>();
        bc.center = root.transform.InverseTransformPoint(b.center);
        var v = root.transform.InverseTransformVector(b.size);
        bc.size = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        bc.isTrigger = false;
    }

    static void BuildBalai(Scene scene, string rootName, string modelRel, float scale, Vector3 pos, float rotY)
    {
        var root = FindRoot(scene, rootName);
        if (root == null) { L("Missing root " + rootName); return; }

        ClearIfExists(root.transform, "Model_Quaternius");
        var mf = root.GetComponent<MeshFilter>();
        if (mf != null) Object.DestroyImmediate(mf);
        var mr = root.GetComponent<MeshRenderer>();
        if (mr != null) Object.DestroyImmediate(mr);

        bool wasActive = root.activeSelf;
        root.SetActive(true);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        root.transform.localScale = Vector3.one;

        var go = InstantiateModel(modelRel, root.transform, Vector3.zero, 0f, scale);
        if (go != null) go.name = "Model_Quaternius";
        FitBoxCollider(root, true);
        root.SetActive(wasActive);
        L("Balai replaced: " + rootName);
    }

    static void BuildCharacters(Scene scene)
    {
        // Kades
        var kades = FindRoot(scene, "Kades");
        if (kades != null)
        {
            foreach (Transform c in kades.transform)
                if (c.name.StartsWith("Visual_")) c.gameObject.SetActive(false);
            ClearIfExists(kades.transform, "Model_Quaternius");

            var model = InstantiateModel("Characters/Suit", kades.transform, Vector3.zero, 0f, 1f);
            if (model != null)
            {
                model.name = "Model_Quaternius";
                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Kades.controller");
                anim.avatar = LoadAvatar(Q + "/Characters/Suit.fbx");
                anim.applyRootMotion = false;
            }

            var cap = kades.GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                cap.center = new Vector3(0f, 0.95f, 0f);
                cap.height = 1.9f;
                cap.radius = 0.35f;
            }

            // Gerobak visual: swap primitive parts for the Quaternius cart.
            var gerobak = kades.transform.Find("GerobakVisual");
            if (gerobak != null)
            {
                foreach (Transform c in gerobak)
                    if (!c.name.StartsWith("Model_")) c.gameObject.SetActive(false);
                ClearIfExists(gerobak, "Model_Quaternius");
                var cart = InstantiateModel("Environment/MedievalVillage/Props/Cart", gerobak, new Vector3(0f, -0.2f, -0.1f), 0f, 1.3f);
                if (cart != null) cart.name = "Model_Quaternius";
            }
        }

        // Tetua Desa
        var tetua = FindRoot(scene, "TetuaDesa");
        if (tetua != null)
        {
            foreach (Transform c in tetua.transform)
                if (c.name.StartsWith("Visual_")) c.gameObject.SetActive(false);
            ClearIfExists(tetua.transform, "Model_Quaternius");

            var model = InstantiateModel("Characters/Worker", tetua.transform, Vector3.zero, 0f, 1f);
            if (model != null)
            {
                model.name = "Model_Quaternius";
                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/TetuaDesa.controller");
                anim.avatar = LoadAvatar(Q + "/Characters/Worker.fbx");
                anim.applyRootMotion = false;
            }

            var cap = tetua.GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                cap.isTrigger = true;
                cap.center = new Vector3(0f, 1.1f, 0f);
                cap.height = 2.2f;
                cap.radius = 0.7f;
            }
        }
        L("Characters replaced.");
    }

    static void BuildBins(Scene scene)
    {
        var bins = FindRoot(scene, "TrashBins");
        if (bins == null) return;
        foreach (Transform bin in bins.transform)
        {
            if (!bin.name.StartsWith("Tong")) continue;
            ClearIfExists(bin, "Platform");
        }
        L("Bins kept.");
    }

    static void BuildTrashSamples(Scene scene, Transform unused)
    {
        var samples = FindRoot(scene, "Trash_Samples");
        if (samples == null) return;

        foreach (Transform t in samples.transform)
        {
            string tag = t.tag;
            Mesh mesh;
            Material mat;
            if (tag == "Organik") { mesh = LoadMesh(MeshDir + "/AppleCore.asset"); mat = LoadMat("M_Trash_Organik"); }
            else if (tag == "Anorganik") { mesh = LoadMesh(MeshDir + "/PlasticBottle.asset"); mat = LoadMat("M_Trash_Anorganik"); }
            else { mesh = LoadMesh(MeshDir + "/Battery.asset"); mat = LoadMat("M_Trash_B3"); }

            ApplyTrashVisual(t.gameObject, tag, mesh);
            t.localScale = Vector3.one;
            var p = t.position; p.y = 0.02f; t.position = p;
        }
        L("Trash samples updated.");
    }

    static void SetSpawnPointHeights(Scene scene, float y)
    {
        var sp = FindRoot(scene, "SpawnPoints");
        if (sp == null) return;
        foreach (Transform t in sp.transform)
        {
            var p = t.position;
            p.y = y;
            t.position = p;
        }
    }

    static Mesh LoadMesh(string path) => AssetDatabase.LoadAssetAtPath<Mesh>(path);
    static Material LoadMat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ye_Village/" + name + ".mat");

    static void ApplyTrashVisual(GameObject go, string tag, Mesh mesh)
    {
        if (mesh == null) { L("Missing trash mesh for " + tag); return; }
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        var bodyMat = LoadMat(tag == "Organik" ? "M_Trash_Organik" : tag == "Anorganik" ? "M_Trash_Anorganik" : "M_Trash_B3");
        if (mesh.subMeshCount > 1)
        {
            var second = tag == "Organik" ? LoadMat("M_Wood") : LoadMat("M_Tong_Lid");
            mr.sharedMaterials = new[] { bodyMat, second };
        }
        else mr.sharedMaterials = new[] { bodyMat };

        foreach (var c in go.GetComponents<Collider>()) Object.DestroyImmediate(c);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        if (tag == "Organik") { col.center = new Vector3(0f, 0.09f, 0f); col.size = new Vector3(0.24f, 0.20f, 0.24f); }
        else if (tag == "Anorganik") { col.center = new Vector3(0f, 0.14f, 0f); col.size = new Vector3(0.12f, 0.30f, 0.12f); }
        else { col.center = new Vector3(0f, 0.10f, 0f); col.size = new Vector3(0.14f, 0.22f, 0.14f); }
    }

    // ---------------------------------------------------------------- trash meshes

    [MenuItem("Tools/Quaternius/4b. Build Trash Meshes")]
    public static void BuildTrashMeshes()
    {
        log = log ?? new StringBuilder();
        EnsureFolder("Assets/Models");
        EnsureFolder(MeshDir);

        var apple = BuildAppleMesh();
        SaveMesh(apple, MeshDir + "/AppleCore.asset");
        var bottle = BuildBottleMesh();
        SaveMesh(bottle, MeshDir + "/PlasticBottle.asset");
        var battery = BuildBatteryMesh();
        SaveMesh(battery, MeshDir + "/Battery.asset");

        BuildTrashPrefabs(apple, bottle, battery);
        L("Trash meshes + prefabs built.");
        WriteLog();
    }

    static void SaveMesh(Mesh mesh, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    static void BuildTrashPrefabs(Mesh apple, Mesh bottle, Mesh battery)
    {
        var o1 = BuildTrashGo("Trash_Organik", "Organik", apple);
        PrefabUtility.SaveAsPrefabAsset(o1, "Assets/Prefabs/Trash/Trash_Organik.prefab", out _);
        Object.DestroyImmediate(o1);

        var o2 = BuildTrashGo("Trash_Anorganik", "Anorganik", bottle);
        PrefabUtility.SaveAsPrefabAsset(o2, "Assets/Prefabs/Trash/Trash_Anorganik.prefab", out _);
        Object.DestroyImmediate(o2);

        var o3 = BuildTrashGo("Trash_B3", "B3", battery);
        PrefabUtility.SaveAsPrefabAsset(o3, "Assets/Prefabs/Trash/Trash_B3.prefab", out _);
        Object.DestroyImmediate(o3);
    }

    static GameObject BuildTrashGo(string name, string tag, Mesh mesh)
    {
        var go = new GameObject(name);
        go.tag = tag;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var bodyMat = LoadMat(tag == "Organik" ? "M_Trash_Organik" : tag == "Anorganik" ? "M_Trash_Anorganik" : "M_Trash_B3");
        mr.sharedMaterials = mesh.subMeshCount > 1
            ? new[] { bodyMat, tag == "Organik" ? LoadMat("M_Wood") : LoadMat("M_Tong_Lid") }
            : new[] { bodyMat };

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        if (tag == "Organik") { col.center = new Vector3(0f, 0.09f, 0f); col.size = new Vector3(0.24f, 0.20f, 0.24f); }
        else if (tag == "Anorganik") { col.center = new Vector3(0f, 0.14f, 0f); col.size = new Vector3(0.12f, 0.30f, 0.12f); }
        else { col.center = new Vector3(0f, 0.10f, 0f); col.size = new Vector3(0.14f, 0.22f, 0.14f); }

        var temp = go;
        return temp;
    }

    static Mesh BuildAppleMesh()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var tris2 = new List<int>(); // stem
        var uvs = new List<Vector2>();

        int seg = 10;
        float[] ys = { 0.00f, 0.03f, 0.10f, 0.16f, 0.19f, 0.21f };
        float[] rs = { 0.015f, 0.075f, 0.10f, 0.085f, 0.045f, 0.012f };

        int ringStart = verts.Count;
        for (int r = 0; r < ys.Length; r++)
        {
            for (int s = 0; s < seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * rs[r], ys[r], Mathf.Sin(a) * rs[r]));
                uvs.Add(new Vector2(s / (float)seg, r / (float)(ys.Length - 1)));
            }
        }
        for (int r = 0; r < ys.Length - 1; r++)
        {
            for (int s = 0; s < seg; s++)
            {
                int a = ringStart + r * seg + s;
                int b = ringStart + r * seg + (s + 1) % seg;
                int c = ringStart + (r + 1) * seg + s;
                int d = ringStart + (r + 1) * seg + (s + 1) % seg;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }
        // caps
        int bottomCenter = verts.Count;
        verts.Add(new Vector3(0f, 0f, 0f)); uvs.Add(new Vector2(0.5f, 0f));
        int topCenter = verts.Count;
        verts.Add(new Vector3(0f, 0.21f, 0f)); uvs.Add(new Vector2(0.5f, 1f));
        for (int s = 0; s < seg; s++)
        {
            int a = ringStart + s;
            int b = ringStart + (s + 1) % seg;
            tris.Add(bottomCenter); tris.Add(a); tris.Add(b);

            int t1 = ringStart + (ys.Length - 1) * seg + s;
            int t2 = ringStart + (ys.Length - 1) * seg + (s + 1) % seg;
            tris.Add(topCenter); tris.Add(t1); tris.Add(t2);
        }
        // stem
        int stemStart = verts.Count;
        int sseg = 5;
        for (int s = 0; s < sseg; s++)
        {
            float a = s / (float)sseg * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * 0.012f;
            float z = Mathf.Sin(a) * 0.012f;
            verts.Add(new Vector3(x, 0.20f, z)); uvs.Add(new Vector2(s / (float)sseg, 0f));
            verts.Add(new Vector3(x, 0.27f, z)); uvs.Add(new Vector2(s / (float)sseg, 1f));
        }
        for (int s = 0; s < sseg; s++)
        {
            int i = stemStart + s * 2;
            int j = stemStart + ((s + 1) % sseg) * 2;
            tris2.Add(i); tris2.Add(i + 1); tris2.Add(j);
            tris2.Add(j); tris2.Add(i + 1); tris2.Add(j + 1);
        }

        var mesh = new Mesh { name = "AppleCore" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(tris, 0);
        mesh.SetTriangles(tris2, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildBottleMesh()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        int seg = 10;
        float[] ys = { 0.00f, 0.02f, 0.14f, 0.19f, 0.22f, 0.30f };
        float[] rs = { 0.010f, 0.042f, 0.042f, 0.032f, 0.016f, 0.016f };

        int start = 0;
        for (int r = 0; r < ys.Length; r++)
            for (int s = 0; s < seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * rs[r], ys[r], Mathf.Sin(a) * rs[r]));
                uvs.Add(new Vector2(s / (float)seg, r / (float)(ys.Length - 1)));
            }
        for (int r = 0; r < ys.Length - 1; r++)
            for (int s = 0; s < seg; s++)
            {
                int a = start + r * seg + s;
                int b = start + r * seg + (s + 1) % seg;
                int c = start + (r + 1) * seg + s;
                int d = start + (r + 1) * seg + (s + 1) % seg;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        int bc = verts.Count; verts.Add(Vector3.zero); uvs.Add(new Vector2(0.5f, 0f));
        int tc = verts.Count; verts.Add(new Vector3(0f, 0.30f, 0f)); uvs.Add(new Vector2(0.5f, 1f));
        for (int s = 0; s < seg; s++)
        {
            tris.Add(bc); tris.Add(start + s); tris.Add(start + (s + 1) % seg);
            int t1 = start + (ys.Length - 1) * seg + s;
            int t2 = start + (ys.Length - 1) * seg + (s + 1) % seg;
            tris.Add(tc); tris.Add(t2); tris.Add(t1);
        }

        var mesh = new Mesh { name = "PlasticBottle" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildBatteryMesh()
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var body = new List<int>();
        var cap = new List<int>();

        float hx = 0.045f, hz = 0.045f, hy = 0.18f;
        // box
        Vector3[] c =
        {
            new Vector3(-hx,0,-hz), new Vector3(hx,0,-hz), new Vector3(hx,0,hz), new Vector3(-hx,0,hz),
            new Vector3(-hx,hy,-hz), new Vector3(hx,hy,-hz), new Vector3(hx,hy,hz), new Vector3(-hx,hy,hz)
        };
        int bstart = verts.Count;
        foreach (var v in c) { verts.Add(v); uvs.Add(Vector2.zero); }
        int[] boxTris = { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,5,1, 0,4,5, 1,6,2, 1,5,6, 2,7,3, 2,6,7, 3,4,0, 3,7,4 };
        body.AddRange(boxTris.Select(i => bstart + i));

        // terminal
        int seg = 8;
        int cstart = verts.Count;
        float ty0 = hy, ty1 = hy + 0.035f, tr = 0.017f;
        for (int s = 0; s < seg; s++)
        {
            float a = s / (float)seg * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * tr, z = Mathf.Sin(a) * tr;
            verts.Add(new Vector3(x, ty0, z)); uvs.Add(Vector2.zero);
            verts.Add(new Vector3(x, ty1, z)); uvs.Add(Vector2.zero);
        }
        for (int s = 0; s < seg; s++)
        {
            int i = cstart + s * 2;
            int j = cstart + ((s + 1) % seg) * 2;
            cap.Add(i); cap.Add(i + 1); cap.Add(j);
            cap.Add(j); cap.Add(i + 1); cap.Add(j + 1);
        }
        int ctop = verts.Count; verts.Add(new Vector3(0f, ty1, 0f)); uvs.Add(Vector2.zero);
        for (int s = 0; s < seg; s++)
        {
            int i = cstart + s * 2 + 1;
            int j = cstart + ((s + 1) % seg) * 2 + 1;
            cap.Add(ctop); cap.Add(j); cap.Add(i);
        }

        var mesh = new Mesh { name = "Battery" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(body, 0);
        mesh.SetTriangles(cap, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------------------------------------------------------------- preview

    [MenuItem("Tools/Quaternius/5. Render Previews")]
    public static void RenderPreviews()
    {
        var scene = SceneManager.GetSceneByName("SampleScene");
        if (!scene.IsValid() || !scene.isLoaded) { Debug.LogError("SampleScene not loaded"); return; }
        SceneManager.SetActiveScene(scene);

        RenderView(new Vector3(0f, 2.6f, -6f), new Vector3(0f, 1.0f, 2f), "Logs/preview_player.png");
        RenderView(new Vector3(11f, 9f, -11f), new Vector3(0f, 1.5f, 2f), "Logs/preview_overview.png");
        RenderView(new Vector3(-7f, 3f, 3f), new Vector3(0f, 1.2f, -1f), "Logs/preview_plaza.png");
    }

    static void RenderView(Vector3 pos, Vector3 target, string outRel)
    {
        var camGo = new GameObject("PreviewCam_TMP");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 250f;
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(target - pos, Vector3.up);

        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + outRel));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log("[Q-Integration] preview -> " + path);

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGo);
    }

    // ---------------------------------------------------------------- save

    static void SaveScene()
    {
        var scene = SceneManager.GetSceneByName("SampleScene");
        if (!scene.IsValid() || !scene.isLoaded) throw new Exception("SampleScene not loaded");
        EditorSceneManager.MarkSceneDirty(scene);
        bool ok = EditorSceneManager.SaveScene(scene);
        L("Save SampleScene: " + ok);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
