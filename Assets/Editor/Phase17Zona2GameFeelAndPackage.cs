using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Modul 17: Setup Zona 2 Clean Water, Game Feel (B3 + highlight) &amp; ekspor Prefab mandiri.
/// Menu: YeVillage &gt; 17 - Setup Zona 2 Clean Water, Game Feel &amp; Export Prefab
///
/// Idempoten &amp; TIDAK memakai EditorUtility.DisplayDialog agar aman diotomasi lewat MCP.
/// Tidak mengubah tata letak Zona 1/Zona 3 dan tidak menyentuh backlog BUG-01..05.
/// </summary>
public static class Phase17Zona2GameFeelAndPackage
{
    private const string TextureFolder = "Assets/VFX/Textures";
    private const string VfxMaterialFolder = "Assets/Materials/Zona2_VFX";
    private const string TrashPrefabFolder = "Assets/Prefabs/Trash";
    private const string ZonesPrefabFolder = "Assets/Prefabs/Zones";
    private const string Zona2PrefabPath = ZonesPrefabFolder + "/Zona2_BantaranSungai.prefab";

    private const string SoftCirclePath = TextureFolder + "/Tex_SoftCircle.png";
    private const string SoftRingPath = TextureFolder + "/Tex_SoftRing.png";

    private const string SoftParticleMatPath = VfxMaterialFolder + "/Mat_SoftParticle_URP.mat";
    private const string ToxicMistMatPath = VfxMaterialFolder + "/Mat_ToxicMist_URP.mat";
    private const string HighlightRingMatPath = VfxMaterialFolder + "/Mat_HighlightRing_URP.mat";

    private const string WaterObjectName = "Zona2_RiverWater";
    private const string SparklesName = "RiverCleanSparkles";
    private const string ToxicMistName = "B3_ToxicMistVFX";
    private const string WarningRingName = "B3_WarningRing";
    private const string HighlightRingName = "HighlightRing";

    [MenuItem("YeVillage/17 - Setup Zona 2 Clean Water, Game Feel & Export Prefab")]
    public static void Run()
    {
        EnsureFolder("Assets/VFX");
        EnsureFolder(TextureFolder);
        EnsureFolder("Assets/Materials");
        EnsureFolder(VfxMaterialFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(ZonesPrefabFolder);

        Texture2D softCircle = EnsureSoftCircleTexture();
        Texture2D softRing = EnsureSoftRingTexture();

        Material particleMat = EnsureParticleMaterial(SoftParticleMatPath, softCircle, true);
        Material mistMat = EnsureParticleMaterial(ToxicMistMatPath, softCircle, false);
        Material ringMat = EnsureRingMaterial(HighlightRingMatPath, softRing);

        bool waterOk = SetupWaterPurification(particleMat);
        bool b3Ok = SetupB3DebuffVfx(mistMat, ringMat);
        int highlightCount = SetupTrashHighlights(ringMat);
        bool cameraOk = SetupCameraAndBlockers();
        int prefabChildCount = ExportZona2Prefab();

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Phase17] Selesai.\n" +
            "  - Purifikasi air Zona 2: " + (waterOk ? "OK (controller + RiverCleanSparkles)" : "GAGAL") + "\n" +
            "  - VFX debuff B3 pada Player: " + (b3Ok ? "OK (B3_ToxicMistVFX + B3_WarningRing)" : "GAGAL") + "\n" +
            "  - Prefab sampah ber-highlight: " + highlightCount + "\n" +
            "  - Camera collision: " + (cameraOk ? "OK (mask mengecualikan Layer 2)" : "GAGAL") + "\n" +
            "  - Prefab Zona 2: " + (prefabChildCount > 0 ? (Zona2PrefabPath + " (" + prefabChildCount + " child)") : "GAGAL") + "\n" +
            "  - SampleScene disimpan, wiring TrashSpawner tidak diubah.");
    }

    // ------------------------------------------------------------------ folders

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path);
        string leaf = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            parent = parent.Replace('\\', '/');
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    // ----------------------------------------------------------------- textures

    private static Texture2D EnsureSoftCircleTexture()
    {
        if (!System.IO.File.Exists(SoftCirclePath))
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size) * 2f - 1f;
                    float dy = ((y + 0.5f) / size) * 2f - 1f;
                    float radius = Mathf.Sqrt((dx * dx) + (dy * dy));
                    float alpha = Mathf.Clamp01(1f - radius);
                    alpha *= alpha;
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(SoftCirclePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SoftCirclePath, ImportAssetOptions.ForceUpdate);
        }

        ConfigureTexture(SoftCirclePath);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftCirclePath);
    }

    private static Texture2D EnsureSoftRingTexture()
    {
        if (!System.IO.File.Exists(SoftRingPath))
        {
            const int size = 128;
            const float inner = 0.58f;
            const float outer = 0.94f;
            const float feather = 0.07f;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size) * 2f - 1f;
                    float dy = ((y + 0.5f) / size) * 2f - 1f;
                    float radius = Mathf.Sqrt((dx * dx) + (dy * dy));

                    float innerFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner - feather, inner + feather, radius));
                    float outerFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outer - feather, outer, radius));
                    float alpha = Mathf.Clamp01(innerFade * outerFade);

                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            System.IO.File.WriteAllBytes(SoftRingPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SoftRingPath, ImportAssetOptions.ForceUpdate);
        }

        ConfigureTexture(SoftRingPath);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftRingPath);
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            changed = true;
        }

        if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    // ---------------------------------------------------------------- materials

    private static Material EnsureParticleMaterial(string path, Texture2D texture, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        if (texture != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", texture);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.white);
        }

        ConfigureTransparent(mat, additive);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material EnsureRingMaterial(string path, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        if (texture != null && mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", texture);
        }

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.white);
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.white);
        }

        ConfigureTransparent(mat, false);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ConfigureTransparent(Material mat, bool additive)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 2f : 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
        {
            mat.SetFloat("_DstBlend", (float)(additive
                ? UnityEngine.Rendering.BlendMode.One
                : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        }

        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // ------------------------------------------------------------- water feature

    private static bool SetupWaterPurification(Material particleMat)
    {
        GameObject water = GameObject.Find(WaterObjectName);
        if (water == null)
        {
            Debug.LogWarning("[Phase17] GameObject '" + WaterObjectName + "' tidak ditemukan.");
            return false;
        }

        Zona2WaterVisual visual = water.GetComponent<Zona2WaterVisual>();
        if (visual == null)
        {
            visual = water.AddComponent<Zona2WaterVisual>();
        }

        Zona2WaterPurificationController controller = water.GetComponent<Zona2WaterPurificationController>();
        if (controller == null)
        {
            controller = water.AddComponent<Zona2WaterPurificationController>();
        }

        Transform sparkles = water.transform.Find(SparklesName);
        if (sparkles == null)
        {
            GameObject sparkleObject = new GameObject(SparklesName);
            sparkles = sparkleObject.transform;
            sparkles.SetParent(water.transform, false);
        }

        sparkles.localPosition = new Vector3(0f, 0.04f, 0f);
        sparkles.localRotation = Quaternion.identity;
        sparkles.localScale = Vector3.one;

        ParticleSystem ps = sparkles.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = sparkles.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureSparkles(ps, particleMat);

        SerializedObject so = new SerializedObject(controller);
        SerializedProperty visualProp = so.FindProperty("waterVisual");
        if (visualProp != null)
        {
            visualProp.objectReferenceValue = visual;
        }

        SerializedProperty sparkleProp = so.FindProperty("cleanWaterSparkles");
        if (sparkleProp != null)
        {
            sparkleProp.objectReferenceValue = ps;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        return true;
    }

    private static void ConfigureSparkles(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
        main.startColor = HexColor("#E6F9FF");
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 320;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(48f, 0.05f, 9f);
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.alignment = ParticleSystemRenderSpace.View;
            psr.sharedMaterial = material;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }
    }

    // ---------------------------------------------------------------- B3 feature

    private static bool SetupB3DebuffVfx(Material mistMat, Material ringMat)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                player = controller.gameObject;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("[Phase17] Player Kades tidak ditemukan untuk VFX B3.");
            return false;
        }

        B3DebuffVisualEffect effect = player.GetComponent<B3DebuffVisualEffect>();
        if (effect == null)
        {
            effect = player.AddComponent<B3DebuffVisualEffect>();
        }

        // --- toxic mist ---
        Transform mist = player.transform.Find(ToxicMistName);
        if (mist == null)
        {
            GameObject mistObject = new GameObject(ToxicMistName);
            mist = mistObject.transform;
            mist.SetParent(player.transform, false);
        }

        mist.localPosition = new Vector3(0f, 0.45f, 0f);
        mist.localRotation = Quaternion.identity;
        mist.localScale = Vector3.one;

        ParticleSystem ps = mist.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = mist.gameObject.AddComponent<ParticleSystem>();
        }

        ConfigureToxicMist(ps, mistMat);

        // --- warning ring ---
        Transform ring = player.transform.Find(WarningRingName);
        if (ring == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = WarningRingName;
            Collider quadCollider = quad.GetComponent<Collider>();
            if (quadCollider != null)
            {
                Object.DestroyImmediate(quadCollider);
            }

            ring = quad.transform;
            ring.SetParent(player.transform, false);
        }

        Collider existingCollider = ring.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Object.DestroyImmediate(existingCollider);
        }

        ring.localPosition = new Vector3(0f, 0.04f, 0f);
        ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.localScale = new Vector3(1.1f, 1.1f, 1.1f);

        Renderer ringRenderer = ring.GetComponent<Renderer>();
        if (ringRenderer == null)
        {
            ringRenderer = ring.gameObject.AddComponent<MeshRenderer>();
        }

        ringRenderer.sharedMaterial = ringMat;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;

        TrashInventory inventory = player.GetComponent<TrashInventory>();
        SerializedObject so = new SerializedObject(effect);
        SetObjectReference(so, "inventory", inventory);
        SetObjectReference(so, "toxicMist", ps);
        SetObjectReference(so, "warningRing", ring);
        SetObjectReference(so, "warningRingRenderer", ringRenderer);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effect);
        return true;
    }

    private static void ConfigureToxicMist(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        main.startColor = HexColor("#7FA832");
        main.gravityModifier = -0.015f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(HexColor("#7FA832"), 0f),
                new GradientColorKey(HexColor("#594A6E"), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.48f, 0f),
                new GradientAlphaKey(0.18f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.alignment = ParticleSystemRenderSpace.View;
            psr.sharedMaterial = material;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }
    }

    // ------------------------------------------------------------ trash highlight

    private static int SetupTrashHighlights(Material ringMat)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TrashPrefabFolder });
        int applied = 0;
        List<string> processedPaths = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null)
            {
                continue;
            }

            try
            {
                TrashItem item = contents.GetComponent<TrashItem>();
                bool isTrash = item != null
                    || contents.CompareTag("Organik")
                    || contents.CompareTag("Anorganik")
                    || contents.CompareTag("B3");

                if (!isTrash)
                {
                    continue;
                }

                TrashHighlightVisual highlight = contents.GetComponent<TrashHighlightVisual>();
                if (highlight == null)
                {
                    highlight = contents.AddComponent<TrashHighlightVisual>();
                }

                Transform ring = contents.transform.Find(HighlightRingName);
                if (ring == null)
                {
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = HighlightRingName;
                    Collider quadCollider = quad.GetComponent<Collider>();
                    if (quadCollider != null)
                    {
                        Object.DestroyImmediate(quadCollider);
                    }

                    ring = quad.transform;
                    ring.SetParent(contents.transform, false);
                }

                Collider ringCollider = ring.GetComponent<Collider>();
                if (ringCollider != null)
                {
                    Object.DestroyImmediate(ringCollider);
                }

                float ringScale = ComputeRingScale(contents, ring);
                ring.localPosition = new Vector3(0f, 0.03f, 0f);
                ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.localScale = new Vector3(ringScale, ringScale, ringScale);

                Renderer ringRenderer = ring.GetComponent<Renderer>();
                if (ringRenderer == null)
                {
                    ringRenderer = ring.gameObject.AddComponent<MeshRenderer>();
                }

                ringRenderer.sharedMaterial = ringMat;
                ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ringRenderer.receiveShadows = false;

                SerializedObject so = new SerializedObject(highlight);
                SerializedProperty ringProp = so.FindProperty("highlightRing");
                if (ringProp != null)
                {
                    ringProp.objectReferenceValue = ring;
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                processedPaths.Add(path);
                applied++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // Aktifkan keyword emisi setelah semua prefab ditutup agar tidak ikut ter-revert
        // oleh proses unload prefab contents.
        for (int i = 0; i < processedPaths.Count; i++)
        {
            EnableEmissionOnPrefabMaterials(processedPaths[i]);
        }

        return applied;
    }

    private static float ComputeRingScale(GameObject contents, Transform ring)
    {
        Renderer[] renderers = contents.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(contents.transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (ring != null && renderer.transform.IsChildOf(ring))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        float size = hasBounds ? Mathf.Max(bounds.size.x, bounds.size.z) : 0.5f;
        return Mathf.Clamp(size * 1.5f, 0.55f, 1.2f);
    }

    private static void EnableEmissionOnPrefabMaterials(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return;
        }

        HashSet<Material> materials = new HashSet<Material>();
        int enabledCount = 0;
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] shared = renderers[i].sharedMaterials;
            for (int m = 0; m < shared.Length; m++)
            {
                if (shared[m] != null)
                {
                    materials.Add(shared[m]);
                }
            }
        }

        foreach (Material material in materials)
        {
            string assetPath = AssetDatabase.GetAssetPath(material);
            if (string.IsNullOrEmpty(assetPath)
                || !assetPath.StartsWith("Assets/Materials")
                || !assetPath.ToLowerInvariant().Contains("trash")
                || !material.shader.name.Contains("Universal Render Pipeline/Lit"))
            {
                continue;
            }

            if (!material.IsKeywordEnabled("_EMISSION"))
            {
                material.EnableKeyword("_EMISSION");
            }

            // Catatan: JANGAN menulis _EmissionColor di sini. Menyetel warna emisi
            // ke hitam membuat URP mematikan kembali keyword _EMISSION. Warna emisi
            // diatur per-frame lewat MaterialPropertyBlock oleh TrashHighlightVisual.
            EditorUtility.SetDirty(material);
            enabledCount++;
        }

        if (enabledCount > 0)
        {
            Debug.Log("[Phase17] Emisi ON untuk " + enabledCount + " material di " + prefabPath);
        }
    }

    // ------------------------------------------------------- camera & blockers

    private static bool SetupCameraAndBlockers()
    {
        GameObject cameraObject = GameObject.Find("Main Camera");
        if (cameraObject == null)
        {
            Debug.LogWarning("[Phase17] Main Camera tidak ditemukan.");
            return false;
        }

        ThirdPersonCamera camera = cameraObject.GetComponent<ThirdPersonCamera>();
        if (camera == null)
        {
            Debug.LogWarning("[Phase17] ThirdPersonCamera tidak ditemukan pada Main Camera.");
            return false;
        }

        SerializedObject so = new SerializedObject(camera);
        SetBool(so, "enableCameraCollision", true);
        SetInt(so, "collisionMask", ~(1 << 2)); // kecualikan layer 2 (Ignore Raycast)
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(camera);

        GameObject blockers = GameObject.Find("Environment_Expanded_P15/RiverChannel/RiverWaterBlockers");
        if (blockers == null)
        {
            blockers = GameObject.Find("RiverWaterBlockers");
        }

        MoveHierarchyToLayer(blockers, 2);

        GameObject playableBounds = GameObject.Find("PlayableBounds");
        MoveHierarchyToLayer(playableBounds, 2);

        return true;
    }

    private static void MoveHierarchyToLayer(GameObject root, int layer)
    {
        if (root == null)
        {
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].gameObject.layer = layer;
        }

        EditorUtility.SetDirty(root);
    }

    // ------------------------------------------------------------ prefab export

    private static int ExportZona2Prefab()
    {
        GameObject environment = GameObject.Find("Environment_Expanded_P15");
        if (environment == null)
        {
            Debug.LogWarning("[Phase17] Environment_Expanded_P15 tidak ditemukan.");
            return 0;
        }

        GameObject spawnRoot = GameObject.Find("SpawnPoints_Expanded/Zona2_Riverbank");

        GameObject tempRoot = new GameObject("Zona2_BantaranSungai");
        int childCount = 0;

        try
        {
            CloneInto(environment.transform.Find("RiverChannel"), tempRoot.transform, "RiverChannel");
            CloneInto(environment.transform.Find("Zona2_WoodenBridge"), tempRoot.transform, "Zona2_WoodenBridge");
            CloneInto(environment.transform.Find("Zona2_Decor"), tempRoot.transform, "Zona2_Decor");

            if (spawnRoot != null)
            {
                CloneInto(spawnRoot.transform, tempRoot.transform, "Zona2_SpawnPoints");
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(tempRoot, Zona2PrefabPath);
            if (saved == null)
            {
                Debug.LogWarning("[Phase17] Gagal menyimpan prefab " + Zona2PrefabPath);
                return 0;
            }

            childCount = saved.GetComponentsInChildren<Transform>(true).Length - 1;
        }
        finally
        {
            Object.DestroyImmediate(tempRoot);
        }

        return childCount;
    }

    private static void CloneInto(Transform source, Transform parent, string name)
    {
        if (source == null)
        {
            return;
        }

        GameObject clone = Object.Instantiate(source.gameObject, parent, true);
        clone.name = name;
        clone.transform.SetParent(parent, true);
    }

    // ---------------------------------------------------------------- utilities

    private static void SetObjectReference(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetInt(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
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
