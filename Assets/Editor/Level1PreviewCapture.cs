using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Membuat beberapa gambar preview dari scene Level 1 untuk membandingkan hasil redesign.
/// Output: My project/Backups/preview_*.png
/// Menu: YeVillage/Level1/Capture Preview
/// </summary>
public static class Level1PreviewCapture
{
    [MenuItem("YeVillage/Level1/Capture Preview")]
    public static void Capture()
    {
        CaptureAt("preview_level1", new Vector3(36f, 46f, -40f), new Vector3(-4f, 0f, 6f), 42f);
        CaptureAt("preview_tps", new Vector3(2f, 15f, 2f), new Vector3(-18f, 1f, 17f), 40f);
        CaptureAt("preview_south", new Vector3(12f, 13f, -32f), new Vector3(0f, 0f, -14f), 45f);
        CaptureAt("preview_player", new Vector3(2f, 2.4f, -17f), new Vector3(-18f, 1.2f, 17f), 55f);
        Debug.Log("[Desert] Preview disimpan di My project/Backups/");
    }

    private static void CaptureAt(string fileName, Vector3 position, Vector3 lookAt, float fov)
    {
        const int width = 1600;
        const int height = 900;

        GameObject camObject = new GameObject("PreviewCamera_Temp");
        Camera cam = camObject.AddComponent<Camera>();
        camObject.AddComponent<UniversalAdditionalCameraData>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.98f, 0.82f, 0.55f, 1f);
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        camObject.transform.position = position;
        camObject.transform.LookAt(lookAt);

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest
        {
            destination = rt
        };
        RenderPipeline.SubmitRenderRequest(cam, request);

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObject);

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string folder = Path.Combine(projectRoot, "Backups");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, fileName + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
