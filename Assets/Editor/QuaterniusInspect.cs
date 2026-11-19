using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class QuaterniusInspect
{
    const string Root = "Assets/ThirdParty/Quaternius";

    [MenuItem("Tools/Quaternius/1. Inspect Models")]
    public static void Inspect()
    {
        var sb = new StringBuilder();
        var files = AssetDatabase.FindAssets("t:Model", new[] { Root })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToArray();

        sb.AppendLine("[]".Replace("[]", ""));
        sb.AppendLine("{");
        sb.AppendLine("  \"files\": [");

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var f in files) AssetDatabase.ImportAsset(f, ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        var entries = new List<string>();
        foreach (var path in files)
        {
            var e = new StringBuilder();
            e.Append("    {");
            e.Append($"\"path\":\"{Escape(path)}\",");

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                e.Append($"\"humanoid\":{(importer.animationType == ModelImporterAnimationType.Human ? "true" : "false")},");
                e.Append($"\"scaleFactor\":{importer.globalScale.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var meshes = assets.OfType<Mesh>().ToArray();
            e.Append("\"meshes\":[");
            e.Append(string.Join(",", meshes.Select(m =>
            {
                int tris = 0;
                for (int i = 0; i < m.subMeshCount; i++) tris += (int)(m.GetIndexCount(i) / 3);
                var b = m.bounds;
                return "{" + $"\"name\":\"{Escape(m.name)}\",\"tris\":{tris},\"subs\":{m.subMeshCount},\"size\":[{N(b.size.x)},{N(b.size.y)},{N(b.size.z)}],\"center\":[{N(b.center.x)},{N(b.center.y)},{N(b.center.z)}]" + "}";
            })));
            e.Append("],");

            var mats = assets.OfType<Material>().ToArray();
            e.Append("\"materials\":[");
            e.Append(string.Join(",", mats.Select(m =>
            {
                string col = "";
                if (m.HasProperty("_Color")) col = C(m.GetColor("_Color"));
                else if (m.HasProperty("_BaseColor")) col = C(m.GetColor("_BaseColor"));
                return "{" + $"\"name\":\"{Escape(m.name)}\",\"shader\":\"{Escape(m.shader.name)}\",\"color\":\"{col}\",\"hasMap\":{(m.mainTexture != null ? "true" : "false")}" + "}";
            })));
            e.Append("],");

            var clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            e.Append("\"clips\":[");
            e.Append(string.Join(",", clips.Select(c => "\"" + Escape(c.name) + "\"")));
            e.Append("]");

            e.Append("}");
            entries.Add(e.ToString());
        }

        sb.AppendLine(string.Join(",\n", entries));
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/quaternius_report.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"[QuaterniusInspect] Wrote report for {files.Length} models to {outPath}");
    }

    static string N(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    static string C(Color c) => $"{N(c.r)},{N(c.g)},{N(c.b)},{N(c.a)}";
    static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    [MenuItem("Tools/Quaternius/2. Dump Scene Layout")]
    public static void DumpScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene");
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[QuaterniusInspect] SampleScene not loaded.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("{ \"objects\": [");
        var first = true;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                if (!first) sb.AppendLine(",");
                first = false;

                string path = GetPath(t);
                sb.Append("{");
                sb.Append($"\"path\":\"{Escape(path)}\",");
                sb.Append($"\"p\":[{N(t.position.x)},{N(t.position.y)},{N(t.position.z)}],");
                sb.Append($"\"r\":[{N(t.eulerAngles.x)},{N(t.eulerAngles.y)},{N(t.eulerAngles.z)}],");
                sb.Append($"\"s\":[{N(t.lossyScale.x)},{N(t.lossyScale.y)},{N(t.lossyScale.z)}],");
                sb.Append($"\"tag\":\"{Escape(go.tag)}\",\"layer\":\"{LayerMask.LayerToName(go.layer)}\",");
                sb.Append($"\"active\":{(go.activeSelf ? "true" : "false")},");
                sb.Append("\"comps\":[");

                var comps = go.GetComponents<Component>();
                var names = new List<string>();
                foreach (var c in comps)
                {
                    if (c == null) continue;
                    if (c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer) continue;
                    string d = c.GetType().Name;
                    if (c is Collider col)
                        d += $"(trigger={col.isTrigger},center={N(col.bounds.center.y)},enabled={col.enabled})";
                    if (c is Rigidbody rbb)
                        d += $"(mass={N(rbb.mass)},kinematic={rbb.isKinematic},gravity={rbb.useGravity})";
                    names.Add("\"" + Escape(d) + "\"");
                }
                sb.Append(string.Join(",", names));
                sb.Append("],");

                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var b = mf.sharedMesh.bounds;
                    sb.Append($"\"mesh\":\"{Escape(mf.sharedMesh.name)}\",\"meshSize\":[{N(b.size.x)},{N(b.size.y)},{N(b.size.z)}],");
                }

                var rend = go.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterials != null)
                {
                    sb.Append("\"mats\":[");
                    sb.Append(string.Join(",", rend.sharedMaterials.Where(m => m != null).Select(m => "\"" + Escape(m.name) + "\"")));
                    sb.Append("],");
                    var wb = rend.bounds;
                    sb.Append($"\"wsize\":[{N(wb.size.x)},{N(wb.size.y)},{N(wb.size.z)}],");
                    sb.Append($"\"wcenter\":[{N(wb.center.x)},{N(wb.center.y)},{N(wb.center.z)}]");
                }
                else sb.Append("\"mats\":[]");

                sb.Append("}");
            }
        }
        sb.AppendLine("] }");

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/scene_layout.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log($"[QuaterniusInspect] Wrote scene layout to {outPath}");
    }

    static string GetPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }

    [MenuItem("Tools/Quaternius/4. Avatar Debug")]
    public static void AvatarDebug()
    {
        string path = "Assets/ThirdParty/Quaternius/Characters/Suit.fbx";
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.SaveAndReimport();

        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var av = assets.OfType<Avatar>().FirstOrDefault();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"\"isValid\":{(av != null && av.isValid ? "true" : "false")},");
        sb.AppendLine($"\"isHuman\":{(av != null && av.isHuman ? "true" : "false")},");
        if (av != null)
        {
            var hd = av.humanDescription;
            sb.AppendLine("\"mapping\":[");
            sb.AppendLine(string.Join(",\n", hd.human.Select(h => "{\"human\":\"" + h.humanName + "\",\"bone\":\"" + h.boneName + "\"}")));
            sb.AppendLine("],");
            sb.AppendLine($"\"skeletonCount\":{hd.skeleton.Length}");
        }
        sb.AppendLine("}");

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var inst = UnityEngine.Object.Instantiate(model);
        var names = inst.GetComponentsInChildren<Transform>().Select(t => t.name + " < " + (t.parent != null ? t.parent.name : "-")).ToArray();
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/avatar_debug.json")),
            sb + "\nBONES: " + string.Join(", ", names));
        UnityEngine.Object.DestroyImmediate(inst);
        Debug.Log("[QuaterniusInspect] avatar debug written. valid=" + (av != null && av.isValid));
    }

    [MenuItem("Tools/Quaternius/5. Try Humanoid Map")]
    public static void TryHumanoidMap()
    {
        string path = "Assets/ThirdParty/Quaternius/Characters/Suit.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var inst = UnityEngine.Object.Instantiate(model);

        var sb = new StringBuilder();
        var hd = new HumanDescription();
        var humanBones = new List<HumanBone>();

        string Trait(string noSpaces)
        {
            for (int i = 0; i < HumanTrait.BoneName.Length; i++)
                if (HumanTrait.BoneName[i].Replace(" ", "") == noSpaces) return HumanTrait.BoneName[i];
            sb.AppendLine("TRAIT NOT FOUND: " + noSpaces);
            return noSpaces;
        }

        void M(string human, string bone)
        {
            string t = Trait(human);
            sb.AppendLine($"{t} -> {bone}");
            humanBones.Add(new HumanBone { humanName = t, boneName = bone, limit = new HumanLimit { useDefaultValues = true } });
        }

        M("Hips", "Body");
        M("Spine", "Hips");
        M("Chest", "Torso");
        M("UpperChest", "Chest");
        M("Neck", "Neck");
        M("Head", "Head");
        M("LeftShoulder", "Shoulder.L");
        M("LeftUpperArm", "UpperArm.L");
        M("LeftLowerArm", "LowerArm.L");
        M("LeftHand", "Wrist.L");
        M("RightShoulder", "Shoulder.R");
        M("RightUpperArm", "UpperArm.R");
        M("RightLowerArm", "LowerArm.R");
        M("RightHand", "Wrist.R");
        M("LeftUpperLeg", "UpperLeg.L");
        M("LeftLowerLeg", "LowerLeg.L");
        M("LeftFoot", "Foot.L");
        M("LeftToes", "PT.L");
        M("RightUpperLeg", "UpperLeg.R");
        M("RightLowerLeg", "LowerLeg.R");
        M("RightFoot", "Foot.R");
        M("RightToes", "PT.R");

        var transforms = inst.GetComponentsInChildren<Transform>();
        hd.skeleton = transforms.Select(t => new SkeletonBone
        {
            name = t.name,
            position = t.localPosition,
            rotation = t.localRotation,
            scale = t.localScale
        }).ToArray();
        hd.human = humanBones.ToArray();

        var avatar = AvatarBuilder.BuildHumanAvatar(inst, hd);
        sb.AppendLine($"buildValid={avatar.isValid} buildHuman={avatar.isHuman}");

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/humanoid_try.txt"));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[QuaterniusInspect] humanoid try: " + sb);
        UnityEngine.Object.DestroyImmediate(inst);
    }

    [MenuItem("Tools/Quaternius/6. Dump Anim Curves")]
    public static void DumpAnimCurves()
    {
        foreach (var f in new[] { "Farmer", "King", "Suit", "Worker" })
        {
            string p = "Assets/ThirdParty/Quaternius/Characters/" + f + ".fbx";
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp != null && imp.animationType != ModelImporterAnimationType.Generic)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.SaveAndReimport();
            }
        }

        string path = "Assets/ThirdParty/Quaternius/Characters/Farmer.fbx";
        var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .Where(c => c.name.Contains("Idle") || c.name.Contains("Walk")).ToArray();
        var sb = new StringBuilder();
        foreach (var clip in clips)
        {
            sb.AppendLine("CLIP " + clip.name);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var groups = bindings.GroupBy(b => b.path);
            foreach (var g in groups)
            {
                var props = g.Select(b => b.propertyName).Distinct().ToArray();
                bool hasPos = props.Any(p => p.StartsWith("m_LocalPosition"));
                sb.AppendLine($"  {g.Key} | {string.Join(",", props)} | POS={hasPos}");
            }
        }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/anim_curves.txt")), sb.ToString());
        Debug.Log("[QuaterniusInspect] anim curves written");
    }

    [MenuItem("Tools/Quaternius/7. Dump Roots")]
    public static void DumpRoots()
    {
        string[] paths =
        {
            "Characters/Suit",
            "Environment/MedievalVillage/Buildings/House_1",
            "Environment/MedievalVillage/Buildings/House_3",
            "Environment/MedievalVillage/Buildings/Inn",
            "Environment/MedievalVillage/Props/Fence",
            "Environment/MedievalVillage/Props/Path_Straight",
            "Environment/MedievalVillage/Props/Bench_1",
            "Environment/Nature/PalmTree_1",
            "Environment/Nature/Grass",
        };
        var sb = new StringBuilder();
        foreach (var rel in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/Quaternius/" + rel + ".fbx");
            if (asset == null) { sb.AppendLine("MISSING " + rel); continue; }
            var t = asset.transform;
            sb.AppendLine($"{rel} | rot={t.localRotation.eulerAngles} | scale={t.localScale} | pos={t.localPosition}");
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                sb.AppendLine($"   child {c.name} | rot={c.localRotation.eulerAngles} | scale={c.localScale}");
            }
        }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/roots.txt")), sb.ToString());
        Debug.Log("[QuaterniusInspect] roots written");
    }

    [MenuItem("Tools/Quaternius/8. Verify Controllers")]
    public static void VerifyControllers()
    {
        var sb = new StringBuilder();
        foreach (var p in new[] { "Assets/Animations/Kades.controller", "Assets/Animations/TetuaDesa.controller" })
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(p);
            if (ctrl == null) { sb.AppendLine(p + " MISSING"); continue; }
            sb.AppendLine("CTRL " + p);
            var sm = ctrl.layers[0].stateMachine;
            sb.AppendLine($"  default={sm.defaultState?.name} states={sm.states.Length} params={string.Join(",", ctrl.parameters.Select(x => x.name))}");
            foreach (var s in sm.states)
            {
                var m = s.state.motion as AnimationClip;
                sb.AppendLine($"  state {s.state.name}: motion={(m == null ? "NULL" : m.name)} len={(m == null ? 0f : m.length):0.00}s legacy={(m != null && m.legacy)}");
            }
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene");
        foreach (var rootName in new[] { "Kades", "TetuaDesa" })
        {
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == rootName);
            var anim = root != null ? root.GetComponentInChildren<Animator>() : null;
            if (anim == null) { sb.AppendLine(rootName + " Animator MISSING"); continue; }
            sb.AppendLine($"{rootName}: avatar={(anim.avatar == null ? "NULL" : anim.avatar.name + " valid=" + anim.avatar.isValid)} ctrl={(anim.runtimeAnimatorController == null ? "NULL" : anim.runtimeAnimatorController.name)}");
        }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/verify.txt")), sb.ToString());
        Debug.Log("[QuaterniusInspect] verify written:\n" + sb);
    }

    [MenuItem("Tools/Quaternius/9. Play Report")]
    public static void PlayReport()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        var kades = GameObject.Find("Kades");
        if (kades == null) { Debug.Log("[QR] Kades missing"); return; }
        var anim = kades.GetComponentInChildren<Animator>();
        var rb = kades.GetComponent<Rigidbody>();
        var st = anim.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"[QR] t={Time.time:0.0} pos={kades.transform.position} vel={rb.linearVelocity} idle={st.IsName("Idle")} walk={st.IsName("Walk")} avatarValid={(anim.avatar != null && anim.avatar.isValid)} ctrl={(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null")}");
        foreach (var tag in new[] { "Organik", "Anorganik", "B3" })
            foreach (var g in GameObject.FindGameObjectsWithTag(tag))
                Debug.Log($"[QR] trash {g.name} at {g.transform.position}");
    }

    [MenuItem("Tools/Quaternius/10. Toggle Walk")]
    public static void ToggleWalk()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        var kades = GameObject.Find("Kades");
        var anim = kades.GetComponentInChildren<Animator>();
        bool cur = anim.GetBool("isWalking");
        anim.SetBool("isWalking", !cur);
        Debug.Log("[QR] isWalking -> " + !cur);
    }

    [MenuItem("Tools/Quaternius/11. Fix Audio Listeners")]
    public static void FixAudioListeners()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var al in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (al.gameObject.scene != active)
            {
                al.enabled = false;
                Debug.Log("[QR] disabled AudioListener on " + al.gameObject.name + " (scene=" + al.gameObject.scene.name + ")");
            }
            else Debug.Log("[QR] kept AudioListener on " + al.gameObject.name);
        }
    }

    [MenuItem("Tools/Quaternius/15. Run Diagnostics")]
    public static void RunDiagnostics()
    {
        Debug.Log($"[QR-DIAG] playing={Application.isPlaying} time={Time.time:0.000} scale={Time.timeScale} frame={Time.frameCount} delta={Time.deltaTime:0.0000} paused={UnityEditor.EditorApplication.isPaused} active={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} loaded={UnityEngine.SceneManagement.SceneManager.loadedSceneCount}");
    }

    [MenuItem("Tools/Quaternius/12. Teleport Kades To Trash")]
    public static void TeleportToTrash()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        MoveKades(new Vector3(0f, 0.3f, 2.6f));
    }

    [MenuItem("Tools/Quaternius/13. Teleport Kades To Bin")]
    public static void TeleportToBin()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        MoveKades(new Vector3(0f, 0.3f, -7f));
    }

    [MenuItem("Tools/Quaternius/14. Teleport Kades To Shop")]
    public static void TeleportToShop()
    {
        if (!Application.isPlaying) { Debug.Log("[QR] Not in play mode"); return; }
        MoveKades(new Vector3(-5.5f, 0.3f, -7.5f));
    }

    static void MoveKades(Vector3 pos)
    {
        var kades = GameObject.Find("Kades");
        var rb = kades.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = pos;
        kades.transform.position = pos;
        Debug.Log("[QR] Kades moved to " + pos);
    }

    [MenuItem("Tools/Quaternius/3. Probe Models")]
    public static void Probe()
    {
        string[] probes =
        {
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Path_Straight.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Path_Square.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Fence.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/MarketStand_1.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Well.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Cart.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Bench_1.fbx",
            "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Buildings/BalaiTest.fbx",
        };

        var sb = new StringBuilder();
        sb.AppendLine("[");
        var first = true;
        foreach (var path in probes)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) { sb.AppendLine($"  {{ \"path\":\"{Escape(path)}\", \"error\":\"null\" }}"); continue; }
            var inst = (GameObject)UnityEngine.Object.Instantiate(model);
            var rends = inst.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(inst.transform.position, Vector3.zero);
            bool has = false;
            foreach (var r in rends)
            {
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            var anim = inst.GetComponentInChildren<Animator>();
            if (!first) sb.AppendLine(",");
            first = false;
            sb.Append("{");
            sb.Append($"\"path\":\"{Escape(path)}\",");
            sb.Append($"\"boundsSize\":[{N(b.size.x)},{N(b.size.y)},{N(b.size.z)}],");
            sb.Append($"\"boundsCenter\":[{N(b.center.x)},{N(b.center.y)},{N(b.center.z)}],");
            sb.Append($"\"renderers\":{rends.Length},");
            sb.Append($"\"animator\":{(anim != null ? "true" : "false")},");
            if (anim != null) sb.Append($"\"animatorAvatar\":\"{(anim.avatar != null ? Escape(anim.avatar.name) : "null")}\",\"animatorCtl\":\"{(anim.runtimeAnimatorController != null ? Escape(anim.runtimeAnimatorController.name) : "null")}\",");
            sb.Append("\"children\":[");
            sb.Append(string.Join(",", inst.GetComponentsInChildren<Transform>().Take(20).Select(t => "\"" + Escape(t.name) + "\"")));
            sb.Append("]}");
            UnityEngine.Object.DestroyImmediate(inst);
        }
        sb.AppendLine("]");
        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/probe.json"));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[QuaterniusInspect] Wrote probe to " + outPath);
    }
}
