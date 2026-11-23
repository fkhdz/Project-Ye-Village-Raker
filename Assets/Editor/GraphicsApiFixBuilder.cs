using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

public static class GraphicsApiFixBuilder
{
    [MenuItem("YeVillage/Fix/Force Direct3D11 (Windows)")]
    public static void ForceDirect3D11()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        Debug.Log("[GraphicsApiFix] Windows graphics API = Direct3D11 (auto off). Restart Unity to apply.");
    }
}
