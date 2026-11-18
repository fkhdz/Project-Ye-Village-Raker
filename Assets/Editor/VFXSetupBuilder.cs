using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builder Fase 10: membuat tekstur/material/prefab Particle System untuk 4 efek,
/// lalu memasang VFXManager di SampleScene.
/// Menu: YeVillage > Setup Phase 10 VFX. Aman dijalankan berulang (idempotent).
/// </summary>
public static class VFXSetupBuilder
{
    private const string VfxRoot = "Assets/VFX";
    private const string PrefabFolder = "Assets/VFX/Prefabs";
    private const string MaterialFolder = "Assets/VFX/Materials";
    private const string TextureFolder = "Assets/VFX/Textures";
    private const string TexturePath = TextureFolder + "/VFX_SoftCircle.png";

    private const string CorrectPrefabPath = PrefabFolder + "/VFX_Correct.prefab";
    private const string WrongPrefabPath = PrefabFolder + "/VFX_Wrong.prefab";
    private const string UpgradePrefabPath = PrefabFolder + "/VFX_Upgrade.prefab";
    private const string RenovationPrefabPath = PrefabFolder + "/VFX_Renovation.prefab";

    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("YeVillage/Setup Phase 10 VFX")]
    public static void SetupPhase10()
    {
        EditorSceneManager.SaveOpenScenes();

        EnsureFolder("Assets", "VFX");
        EnsureFolder(VfxRoot, "Prefabs");
        EnsureFolder(VfxRoot, "Materials");
        EnsureFolder(VfxRoot, "Textures");

        EnsureParticleTexture();

        Material correctMaterial = EnsureMaterial(MaterialFolder + "/VFX_Correct.mat", Color.white);
        Material wrongMaterial = EnsureMaterial(MaterialFolder + "/VFX_Wrong.mat", Color.white);
        Material upgradeMaterial = EnsureMaterial(MaterialFolder + "/VFX_Upgrade.mat", Color.white);
        Material renovationMaterial = EnsureMaterial(MaterialFolder + "/VFX_Renovation.mat", Color.white);

        GameObject correctPrefab = EnsureParticlePrefab(
            CorrectPrefabPath, correctMaterial, 40, 1.0f, 0.8f, 4f, 0.18f, 0.8f, 35f, 0.15f,
            new Color(0.35f, 1f, 0.45f), new Color(0.95f, 1f, 0.35f));

        GameObject wrongPrefab = EnsureParticlePrefab(
            WrongPrefabPath, wrongMaterial, 20, 1.2f, 1.0f, 1.8f, 0.35f, -0.15f, 25f, 0.25f,
            new Color(1f, 0.16f, 0.12f), new Color(0.45f, 0.05f, 0.05f));

        GameObject upgradePrefab = EnsureParticlePrefab(
            UpgradePrefabPath, upgradeMaterial, 32, 1.5f, 1.2f, 2.5f, 0.16f, -0.4f, 30f, 0.2f,
            new Color(1f, 0.9f, 0.3f), Color.white);

        GameObject renovationPrefab = EnsureParticlePrefab(
            RenovationPrefabPath, renovationMaterial, 120, 3f, 2.2f, 8f, 0.3f, 1.2f, 40f, 0.4f,
            new Color(0.25f, 1f, 0.45f), new Color(1f, 0.85f, 0.25f));

        AssetDatabase.SaveAssets();

        // Pasang VFXManager di scene gameplay.
        Scene gameplayScene = SceneManager.GetActiveScene();
        if (gameplayScene.path != GameplayScenePath)
        {
            gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        GameObject managerObject = GameObject.Find("VFXManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("VFXManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create VFXManager");
            Debug.Log("[VFXSetup] GameObject VFXManager dibuat.");
        }

        VFXManager manager = managerObject.GetComponent<VFXManager>();
        if (manager == null)
        {
            manager = managerObject.AddComponent<VFXManager>();
            Debug.Log("[VFXSetup] VFXManager script dipasang.");
        }

        manager.vfxCorrect = correctPrefab;
        manager.vfxWrong = wrongPrefab;
        manager.vfxUpgrade = upgradePrefab;
        manager.vfxRenovation = renovationPrefab;

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(gameplayScene);
        EditorSceneManager.SaveScene(gameplayScene);
        Debug.Log("[VFXSetup] VFXManager terpasang di SampleScene dengan 4 prefab efek.");

        // Kembali ke MainMenu agar alur Play tetap dari menu utama.
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
        {
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        Selection.activeObject = correctPrefab;
        Debug.Log("[VFXSetup] Setup Fase 10 selesai. Scene MainMenu aktif.");
    }

    [MenuItem("YeVillage/Test VFX")]
    public static void TestVFX()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[VFXTest] Test VFX hanya bisa dijalankan saat Play mode.");
            return;
        }

        VFXManager manager = Object.FindFirstObjectByType<VFXManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[VFXTest] VFXManager tidak ditemukan di scene.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 basePos = player != null
            ? player.transform.position + Vector3.up * 2f
            : new Vector3(0f, 2f, 0f);

        manager.PlayCorrectVFX(basePos + Vector3.left * 2f);
        manager.PlayWrongVFX(basePos + Vector3.left * 4f);
        manager.PlayUpgradeVFX(basePos + Vector3.right * 2f);
        manager.PlayRenovationVFX(basePos + Vector3.up * 2f);

        Debug.Log("[VFXTest] 4 efek dipicu di sekitar Kades.");
    }

    private static void EnsureParticleTexture()
    {
        if (File.Exists(TexturePath))
        {
            return;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log("[VFXSetup] Tekstur partikel lembut dibuat: " + TexturePath);
    }

    private static Material EnsureMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;

        if (isNew)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            material = new Material(shader);
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        // Partikel wajib alpha blend + tidak menulis depth agar lembut.
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (isNew)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static GameObject EnsureParticlePrefab(string path, Material material, int count,
        float duration, float lifetime, float speed, float size, float gravity,
        float shapeAngle, float shapeRadius, Color colorA, Color colorB)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
        ParticleSystem particle = root.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = particle.main;
        main.duration = duration;
        main.loop = false;
        main.prewarm = false;
        main.startDelay = 0f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.maxParticles = count;
        main.useUnscaledTime = true;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = shapeAngle;
        shape.radius = shapeRadius;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particle.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)));

        ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particle.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = material;
        renderer.sortingOrder = 10;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("[VFXSetup] Prefab dibuat: " + path);
        return prefab;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
