using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class BubbleChatThemeBuilder
{
    private const string BubblePath = "Assets/Resources/Dialogue/SP_Bubble.png";
    private const string FontTtfPath = "Assets/Fonts/Alegreya-Bold.ttf";
    private const string FontAssetPath = "Assets/Resources/Dialogue/Alegreya-Bold SDF.asset";

    private static readonly string[] ScenePaths =
    {
        "Assets/Scenes/Level1_Zona1_Pinggiran.unity",
        "Assets/Scenes/Level2_Zona2_Pinggiran.unity",
        "Assets/Scenes/Level3_Zona3_BalaiDesa.unity"
    };

    [MenuItem("YeVillage/Story/Apply Bubble Chat Theme")]
    public static void Apply()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[BubbleChatTheme] Ada scene belum disimpan: " + open.name);
                return;
            }
        }

        ConfigureBubbleSprite();

        TMP_FontAsset font = EnsureFontAsset();
        if (font == null)
        {
            Debug.LogError("[BubbleChatTheme] Font dialog gagal dibuat.");
            return;
        }

        Sprite bubble = AssetDatabase.LoadAssetAtPath<Sprite>(BubblePath);
        string previousScenePath = SceneManager.GetActiveScene().path;

        foreach (string path in ScenePaths)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("[BubbleChatTheme] Scene tidak ditemukan: " + path);
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            DialogueManager[] managers = Object.FindObjectsByType<DialogueManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            bool changed = false;
            foreach (DialogueManager manager in managers)
            {
                manager.fontAsset = font;
                if (bubble != null) manager.bubbleSprite = bubble;
                EditorUtility.SetDirty(manager);
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BubbleChatTheme] Bubble chat + font Alegreya Bold berhasil dipasang.");
    }

    private static void ConfigureBubbleSprite()
    {
        if (!File.Exists(BubblePath))
        {
            Debug.LogError("[BubbleChatTheme] Sprite bubble tidak ditemukan: " + BubblePath);
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(BubblePath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(BubblePath, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(BubblePath) as TextureImporter;
        }
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = Vector4.zero;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static TMP_FontAsset EnsureFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        if (!File.Exists(FontTtfPath))
        {
            Debug.LogError("[BubbleChatTheme] Font TTF tidak ditemukan: " + FontTtfPath);
            return null;
        }

        AssetDatabase.ImportAsset(FontTtfPath, ImportAssetOptions.ForceUpdate);
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
        if (font == null)
        {
            Debug.LogError("[BubbleChatTheme] Gagal memuat TTF sebagai Font.");
            return null;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null)
        {
            Debug.LogError("[BubbleChatTheme] TMP_FontAsset.CreateFontAsset gagal.");
            return null;
        }

        fontAsset.name = "Alegreya-Bold SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
        {
            Texture2D atlas = fontAsset.atlasTextures[0];
            atlas.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            EditorUtility.SetDirty(atlas);
        }

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset.material);
        }

        fontAsset.TryAddCharacters(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?;:'\"()-/…");

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    }
}
