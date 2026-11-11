using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Utilitas editor sementara untuk Fase 6:
/// - Membuat NPC TetuaDesa (visual placeholder + trigger).
/// - Membuat GerobakVisual (child Kades, nonaktif).
/// - Membuat MesinKomposVisual dan MesinPressVisual (nonaktif).
/// - Memasang ShopManager dan mengisi referensinya.
/// Jalankan via menu: YeVillage > Setup Tetua Shop.
/// </summary>
public static class ShopSetupBuilder
{
    private const string MaterialFolder = "Assets/Materials/Ye_Village";

    [MenuItem("YeVillage/Setup Tetua Shop")]
    public static void SetupTetuaShop()
    {
        GameObject kades = GameObject.Find("Kades");
        if (kades == null)
        {
            Debug.LogError("[ShopSetupBuilder] GameObject 'Kades' tidak ditemukan.");
            return;
        }

        PlayerController playerController = kades.GetComponent<PlayerController>();
        TrashInventory playerInventory = kades.GetComponent<TrashInventory>();
        if (playerController == null || playerInventory == null)
        {
            Debug.LogError("[ShopSetupBuilder] PlayerController / TrashInventory tidak lengkap di Kades.");
            return;
        }

        Material wood = LoadMaterial("M_Wood");
        Material skin = LoadMaterial("M_Kades_Skin");
        Material hat = LoadMaterial("M_Kades_Hat");
        Material green = LoadMaterial("M_Tong_Hijau");
        Material yellow = LoadMaterial("M_Tong_Kuning");
        Material red = LoadMaterial("M_Tong_Merah");

        GameObject tetua = GameObject.Find("TetuaDesa");
        if (tetua == null)
        {
            tetua = new GameObject("TetuaDesa");
            tetua.transform.position = new Vector3(-5.5f, 0f, -7.5f);
            tetua.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CreatePrimitiveChild(tetua.transform, "Visual_Body", PrimitiveType.Capsule,
                new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.75f, 0.55f), wood);
            CreatePrimitiveChild(tetua.transform, "Visual_Head", PrimitiveType.Sphere,
                new Vector3(0f, 1.62f, 0f), new Vector3(0.42f, 0.42f, 0.42f), skin);
            CreatePrimitiveChild(tetua.transform, "Visual_Hat_Brim", PrimitiveType.Cylinder,
                new Vector3(0f, 1.86f, 0f), new Vector3(0.62f, 0.02f, 0.62f), hat);
            CreatePrimitiveChild(tetua.transform, "Visual_Hat_Top", PrimitiveType.Cylinder,
                new Vector3(0f, 1.93f, 0f), new Vector3(0.42f, 0.12f, 0.42f), hat);
            CreatePrimitiveChild(tetua.transform, "Visual_Staff", PrimitiveType.Cylinder,
                new Vector3(0.42f, 0.9f, 0.12f), new Vector3(0.05f, 0.9f, 0.05f), wood);
        }

        EnsureTrigger(tetua);

        GameObject gerobak = kades.transform.Find("GerobakVisual")?.gameObject;
        if (gerobak == null)
        {
            gerobak = new GameObject("GerobakVisual");
            gerobak.transform.SetParent(kades.transform, false);
            gerobak.transform.localPosition = new Vector3(0f, 0f, -0.65f);

            CreatePrimitiveChild(gerobak.transform, "Gerobak_Body", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 0.35f, 0.5f), wood);
            CreatePrimitiveChild(gerobak.transform, "Gerobak_WheelL", PrimitiveType.Cylinder,
                new Vector3(-0.42f, 0.3f, 0.05f), new Vector3(0.3f, 0.06f, 0.3f), wood,
                Quaternion.Euler(0f, 0f, 90f));
            CreatePrimitiveChild(gerobak.transform, "Gerobak_WheelR", PrimitiveType.Cylinder,
                new Vector3(0.42f, 0.3f, 0.05f), new Vector3(0.3f, 0.06f, 0.3f), wood,
                Quaternion.Euler(0f, 0f, 90f));
            CreatePrimitiveChild(gerobak.transform, "Gerobak_Handle", PrimitiveType.Cube,
                new Vector3(0f, 0.62f, -0.42f), new Vector3(0.05f, 0.05f, 0.55f), wood);

            gerobak.SetActive(false);
        }

        GameObject mesinKompos = GameObject.Find("MesinKomposVisual");
        if (mesinKompos == null)
        {
            mesinKompos = new GameObject("MesinKomposVisual");
            mesinKompos.transform.position = new Vector3(4.5f, 0f, -9f);
            CreatePrimitiveChild(mesinKompos.transform, "Kompos_Body", PrimitiveType.Cube,
                new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 1.2f, 1.1f), green);
            CreatePrimitiveChild(mesinKompos.transform, "Kompos_Drum", PrimitiveType.Cylinder,
                new Vector3(0f, 1.35f, 0f), new Vector3(0.45f, 0.35f, 0.45f), yellow,
                Quaternion.Euler(90f, 0f, 0f));
            CreatePrimitiveChild(mesinKompos.transform, "Kompos_Pipe", PrimitiveType.Cube,
                new Vector3(0f, 1.65f, 0.45f), new Vector3(0.18f, 0.6f, 0.18f), wood);

            mesinKompos.SetActive(false);
        }

        GameObject mesinPress = GameObject.Find("MesinPressVisual");
        if (mesinPress == null)
        {
            mesinPress = new GameObject("MesinPressVisual");
            mesinPress.transform.position = new Vector3(7f, 0f, -9f);
            CreatePrimitiveChild(mesinPress.transform, "Press_Body", PrimitiveType.Cube,
                new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 1.2f, 1.1f), yellow);
            CreatePrimitiveChild(mesinPress.transform, "Press_Plate", PrimitiveType.Cube,
                new Vector3(0f, 1.28f, 0f), new Vector3(0.9f, 0.12f, 0.9f), red);
            CreatePrimitiveChild(mesinPress.transform, "Press_Lever", PrimitiveType.Cylinder,
                new Vector3(0.55f, 1.55f, 0f), new Vector3(0.06f, 0.55f, 0.06f), wood,
                Quaternion.Euler(0f, 0f, 25f));

            mesinPress.SetActive(false);
        }

        ShopManager shop = tetua.GetComponent<ShopManager>();
        if (shop == null)
        {
            shop = tetua.AddComponent<ShopManager>();
        }

        // Panel UI dibangun otomatis oleh ShopManager saat runtime.
        shop.shopUIPanel = null;
        shop.playerController = playerController;
        shop.playerInventory = playerInventory;
        shop.gerobakVisual = gerobak;
        shop.mesinKomposVisual = mesinKompos;
        shop.mesinPressVisual = mesinPress;

        EditorSceneManager.MarkSceneDirty(tetua.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopSetupBuilder] TetuaDesa + ShopManager + visual upgrade siap.");
    }

    private static void EnsureTrigger(GameObject tetua)
    {
        CapsuleCollider trigger = tetua.GetComponent<CapsuleCollider>();
        if (trigger == null)
        {
            trigger = tetua.AddComponent<CapsuleCollider>();
        }

        trigger.isTrigger = true;
        trigger.radius = 2f;
        trigger.height = 2.4f;
        trigger.center = new Vector3(0f, 1.2f, 0f);
    }

    private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type,
        Vector3 localPosition, Vector3 localScale, Material material, Quaternion? localRotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation ?? Quaternion.identity;
        go.transform.localScale = localScale;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return go;
    }

    private static Material LoadMaterial(string materialName)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Debug.LogWarning("[ShopSetupBuilder] Material tidak ditemukan: " + path);
        }
        return material;
    }
}
