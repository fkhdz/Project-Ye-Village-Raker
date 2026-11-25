using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mengukur ukuran asli (world bounds pada scale 1) aset Kenney & Quaternius
/// supaya skala penempatan di scene bisa dikoreksi dengan tepat.
/// Menu: YeVillage/Level1/Measure Assets
/// </summary>
public static class Level1AssetMeasure
{
    private static readonly string[] Paths =
    {
        "Assets/ThirdParty/KenneyNatureKit/Models/fence_planks.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/fence_gate.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/cliff_large_rock.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/rock_largeA.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/plant_bush.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/cactus_tall.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/sign.fbx",
        "Assets/ThirdParty/KenneyNatureKit/Models/grass.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/MarketStand_1.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/MarketStand_2.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Cart.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Crate.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Barrel.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Package_1.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/Fence.fbx",
        "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Buildings/House_1.fbx",
        "Assets/Prefabs/Trash/Organic/PF_Trash_Apel.prefab",
        "Assets/Prefabs/Trash/Organic/PF_Trash_KulitPisang.prefab",
        "Assets/Prefabs/Trash/Inorganic/PF_Trash_BotolPlastik.prefab",
        "Assets/Prefabs/Trash/Inorganic/PF_Trash_KalengSoda.prefab"
    };

    [MenuItem("YeVillage/Level1/Measure Assets")]
    public static void Measure()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== UKURAN ASET (world bounds pada scale 1) ===");

        foreach (string path in Paths)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                sb.AppendLine("MISSING  " + path);
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                sb.AppendLine("NO RENDERER  " + path);
                Object.DestroyImmediate(instance);
                continue;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            sb.AppendLine(string.Format("{0,-22} size=({1:F3}, {2:F3}, {3:F3})  renderers={4}",
                System.IO.Path.GetFileNameWithoutExtension(path),
                bounds.size.x, bounds.size.y, bounds.size.z, renderers.Length));

            Object.DestroyImmediate(instance);
        }

        Debug.Log(sb.ToString());
    }
}
