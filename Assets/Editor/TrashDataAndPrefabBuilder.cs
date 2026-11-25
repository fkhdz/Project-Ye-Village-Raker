using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tahap 6 — Generator Data & Prefab Variasi Sampah (Organik, Anorganik, B3).
/// Membuat ScriptableObject (TrashData) dan Prefab 3D low-poly dengan trigger collider,
/// komponen TrashItem (bobbing/rotasi), dan tag yang sesuai.
/// Menu: YeVillage/Level1/Build Trash Data & Prefabs
/// </summary>
public static class TrashDataAndPrefabBuilder
{
    private const string PrefabRoot = "Assets/Prefabs/Trash";
    private const string SORoot = "Assets/ScriptableObjects/Trash";

    private const string MatYeVillage = "Assets/Materials/Ye_Village/";
    private const string MatQuaternius = "Assets/Materials/Quaternius/";
    private const string QuaternProps = "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Props/";

    [MenuItem("YeVillage/Level1/Build Trash Data & Prefabs")]
    public static void BuildAllTrashDataAndPrefabs()
    {
        EnsureFolders();

        Material matOrganik = LoadMaterial(MatYeVillage + "M_Trash_Organik.mat");
        Material matAnorganik = LoadMaterial(MatYeVillage + "M_Trash_Anorganik.mat");
        Material matB3 = LoadMaterial(MatYeVillage + "M_Trash_B3.mat");
        Material matLeafYellow = LoadMaterial(MatYeVillage + "M_LeafYellow.mat");
        Material matWood = LoadMaterial(MatYeVillage + "M_Wood.mat");
        Material matRedBerry = LoadMaterial(MatQuaternius + "Q_Berry_B62E37.mat");
        Material matDarkMetal = LoadMaterial(MatQuaternius + "Q_DarkMetal_47494E.mat");
        Material matWhite = LoadMaterial(MatQuaternius + "Q_Hair_White_C2C2C2.mat");
        Material matBlue = LoadMaterial(MatQuaternius + "Q_Blue_3F4758.mat");

        List<string> createdItems = new List<string>();

        // ================================================================
        // 1. ORGANIK
        // ================================================================

        // Apel
        CreateTrashItem("Apel", TrashType.Organic, "Organik", "Organic",
            (root) =>
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "Visual_Apple";
                body.transform.SetParent(root.transform, false);
                body.transform.localScale = new Vector3(0.35f, 0.32f, 0.35f);
                body.GetComponent<Renderer>().sharedMaterial = matRedBerry != null ? matRedBerry : matOrganik;
                Object.DestroyImmediate(body.GetComponent<Collider>());

                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Stem";
                stem.transform.SetParent(root.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                stem.transform.localScale = new Vector3(0.04f, 0.08f, 0.04f);
                stem.GetComponent<Renderer>().sharedMaterial = matWood;
                Object.DestroyImmediate(stem.GetComponent<Collider>());

                SphereCollider col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.3f;
            }, createdItems);

        // Kulit Pisang
        CreateTrashItem("Kulit Pisang", TrashType.Organic, "Organik", "Organic",
            (root) =>
            {
                GameObject visual = new GameObject("Visual_Banana");
                visual.transform.SetParent(root.transform, false);

                for (int i = 0; i < 3; i++)
                {
                    GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    petal.name = "Peel_" + i;
                    petal.transform.SetParent(visual.transform, false);
                    petal.transform.localPosition = new Vector3(Mathf.Sin(i * 2.094f) * 0.12f, 0.05f, Mathf.Cos(i * 2.094f) * 0.12f);
                    petal.transform.localRotation = Quaternion.Euler(35f, i * 120f, 0f);
                    petal.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
                    petal.GetComponent<Renderer>().sharedMaterial = matLeafYellow != null ? matLeafYellow : matOrganik;
                    Object.DestroyImmediate(petal.GetComponent<Collider>());
                }

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.45f, 0.25f, 0.45f);
            }, createdItems);

        // Tulang Ikan
        CreateTrashItem("Tulang Ikan", TrashType.Organic, "Organik", "Organic",
            (root) =>
            {
                GameObject visual = new GameObject("Visual_FishBone");
                visual.transform.SetParent(root.transform, false);

                // Tulang belakang
                GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                spine.name = "Spine";
                spine.transform.SetParent(visual.transform, false);
                spine.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                spine.transform.localScale = new Vector3(0.04f, 0.25f, 0.04f);
                spine.GetComponent<Renderer>().sharedMaterial = matWhite != null ? matWhite : matOrganik;
                Object.DestroyImmediate(spine.GetComponent<Collider>());

                // Duri samping
                for (int i = -2; i <= 2; i++)
                {
                    GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rib.name = "Rib_" + i;
                    rib.transform.SetParent(visual.transform, false);
                    rib.transform.localPosition = new Vector3(i * 0.07f, 0f, 0f);
                    rib.transform.localScale = new Vector3(0.02f, 0.02f, 0.18f);
                    rib.GetComponent<Renderer>().sharedMaterial = matWhite != null ? matWhite : matOrganik;
                    Object.DestroyImmediate(rib.GetComponent<Collider>());
                }

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.55f, 0.15f, 0.3f);
            }, createdItems);

        // Daun Kering
        CreateTrashItem("Daun Kering", TrashType.Organic, "Organik", "Organic",
            (root) =>
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual_DryLeaf";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.35f, 0.02f, 0.25f);
                visual.transform.localRotation = Quaternion.Euler(15f, 25f, -10f);
                visual.GetComponent<Renderer>().sharedMaterial = matOrganik;
                Object.DestroyImmediate(visual.GetComponent<Collider>());

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.45f, 0.2f, 0.45f);
            }, createdItems);

        // ================================================================
        // 2. ANORGANIK
        // ================================================================

        // Botol Plastik
        CreateTrashItem("Botol Plastik", TrashType.Inorganic, "Anorganik", "Inorganic",
            (root) =>
            {
                GameObject visual = new GameObject("Visual_Bottle");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 75f); // Botol tergeletak miring

                // Badan botol
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Bottle_Body";
                body.transform.SetParent(visual.transform, false);
                body.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
                body.GetComponent<Renderer>().sharedMaterial = matBlue != null ? matBlue : matAnorganik;
                Object.DestroyImmediate(body.GetComponent<Collider>());

                // Leher & tutup botol
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Bottle_Cap";
                cap.transform.SetParent(visual.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.26f, 0f);
                cap.transform.localScale = new Vector3(0.09f, 0.05f, 0.09f);
                cap.GetComponent<Renderer>().sharedMaterial = matWhite != null ? matWhite : matAnorganik;
                Object.DestroyImmediate(cap.GetComponent<Collider>());

                CapsuleCollider col = root.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.radius = 0.15f;
                col.height = 0.55f;
                col.direction = 0; // Sumbu X
            }, createdItems);

        // Kaleng Soda
        CreateTrashItem("Kaleng Soda", TrashType.Inorganic, "Anorganik", "Inorganic",
            (root) =>
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "Visual_Can";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.20f, 0.16f, 0.20f);
                visual.GetComponent<Renderer>().sharedMaterial = matRedBerry != null ? matRedBerry : matAnorganik;
                Object.DestroyImmediate(visual.GetComponent<Collider>());

                CylinderColliderOrCapsule(root, 0.18f, 0.35f);
            }, createdItems);

        // Kardus Bekas
        CreateTrashItem("Kardus Bekas", TrashType.Inorganic, "Anorganik", "Inorganic",
            (root) =>
            {
                // Cek apakah ada Package_1 FBX dari Quaternius
                GameObject packageFbx = AssetDatabase.LoadAssetAtPath<GameObject>(QuaternProps + "Package_1.fbx");
                if (packageFbx != null)
                {
                    GameObject model = Object.Instantiate(packageFbx, root.transform);
                    model.name = "Visual_Package";
                    model.transform.localScale = Vector3.one * 0.45f;
                    foreach (Collider c in model.GetComponentsInChildren<Collider>())
                    {
                        Object.DestroyImmediate(c);
                    }
                }
                else
                {
                    GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.name = "Visual_Cardboard";
                    visual.transform.SetParent(root.transform, false);
                    visual.transform.localScale = new Vector3(0.38f, 0.28f, 0.35f);
                    visual.GetComponent<Renderer>().sharedMaterial = matWood;
                    Object.DestroyImmediate(visual.GetComponent<Collider>());
                }

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.45f, 0.35f, 0.45f);
            }, createdItems);

        // ================================================================
        // 3. LIMBAH B3 (Siap untuk Level 2)
        // ================================================================

        // Baterai Bekas
        CreateTrashItem("Baterai Bekas", TrashType.B3, "B3", "B3",
            (root) =>
            {
                GameObject visual = new GameObject("Visual_Battery");
                visual.transform.SetParent(root.transform, false);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Battery_Body";
                body.transform.SetParent(visual.transform, false);
                body.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
                body.GetComponent<Renderer>().sharedMaterial = matB3;
                Object.DestroyImmediate(body.GetComponent<Collider>());

                GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                top.name = "Terminal";
                top.transform.SetParent(visual.transform, false);
                top.transform.localPosition = new Vector3(0f, 0.24f, 0f);
                top.transform.localScale = new Vector3(0.04f, 0.03f, 0.04f);
                top.GetComponent<Renderer>().sharedMaterial = matDarkMetal;
                Object.DestroyImmediate(top.GetComponent<Collider>());

                CylinderColliderOrCapsule(root, 0.12f, 0.45f);
            }, createdItems);

        // Jerigen Oli
        CreateTrashItem("Jerigen Oli", TrashType.B3, "B3", "B3",
            (root) =>
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual_Jerrycan";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.24f, 0.34f, 0.18f);
                visual.GetComponent<Renderer>().sharedMaterial = matB3;
                Object.DestroyImmediate(visual.GetComponent<Collider>());

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.35f, 0.4f, 0.3f);
            }, createdItems);

        // Bohlam Pecah
        CreateTrashItem("Bohlam Pecah", TrashType.B3, "B3", "B3",
            (root) =>
            {
                GameObject visual = new GameObject("Visual_Bulb");
                visual.transform.SetParent(root.transform, false);

                GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glass.name = "Glass";
                glass.transform.SetParent(visual.transform, false);
                glass.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                glass.transform.localScale = new Vector3(0.22f, 0.25f, 0.22f);
                glass.GetComponent<Renderer>().sharedMaterial = matWhite != null ? matWhite : matB3;
                Object.DestroyImmediate(glass.GetComponent<Collider>());

                GameObject screw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                screw.name = "Base";
                screw.transform.SetParent(visual.transform, false);
                screw.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                screw.transform.localScale = new Vector3(0.1f, 0.06f, 0.1f);
                screw.GetComponent<Renderer>().sharedMaterial = matDarkMetal;
                Object.DestroyImmediate(screw.GetComponent<Collider>());

                SphereCollider col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.25f;
            }, createdItems);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TrashBuilder] Selesai! Berhasil membuat " + createdItems.Count + " variasi TrashData & Prefab:\n- " +
                  string.Join("\n- ", createdItems));
    }

    private static void CreateTrashItem(string displayName, TrashType type, string tag, string folderName,
        System.Action<GameObject> visualBuilder, List<string> createdItems)
    {
        string safeName = displayName.Replace(" ", "");
        string prefabFolder = PrefabRoot + "/" + folderName;
        string soFolder = SORoot + "/" + folderName;
        string prefabPath = prefabFolder + "/PF_Trash_" + safeName + ".prefab";
        string soPath = soFolder + "/SO_Trash_" + safeName + ".asset";

        // 1. Buat GameObject root
        GameObject root = new GameObject("PF_Trash_" + safeName);
        root.tag = tag;

        // Tambah Kinematic Rigidbody agar trigger detection stabil di Unity Physics
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Bangun visual 3D & trigger collider
        visualBuilder(root);

        // Tambah komponen TrashItem
        TrashItem itemComponent = root.AddComponent<TrashItem>();
        itemComponent.enableAnimation = true;
        itemComponent.rotationSpeed = 40f;
        itemComponent.bobbingSpeed = 2.2f;
        itemComponent.bobbingHeight = 0.08f;

        // Simpan sebagai Prefab Asset
        bool prefabSuccess;
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out prefabSuccess);
        Object.DestroyImmediate(root);

        if (!prefabSuccess || prefabAsset == null)
        {
            Debug.LogError("[TrashBuilder] Gagal membuat prefab: " + prefabPath);
            return;
        }

        // 2. Buat atau update ScriptableObject TrashData
        TrashData trashData = AssetDatabase.LoadAssetAtPath<TrashData>(soPath);
        if (trashData == null)
        {
            trashData = ScriptableObject.CreateInstance<TrashData>();
            AssetDatabase.CreateAsset(trashData, soPath);
        }

        trashData.displayName = displayName;
        trashData.type = type;
        trashData.prefab = prefabAsset;
        trashData.pointValue = 10;
        EditorUtility.SetDirty(trashData);

        // 3. Tautkan referensi TrashData ke komponen TrashItem di dalam Prefab
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject prefabRootObj = editScope.prefabContentsRoot;
            TrashItem ti = prefabRootObj.GetComponent<TrashItem>();
            if (ti != null)
            {
                ti.data = trashData;
            }
        }

        createdItems.Add(displayName + " [" + type + "] -> " + prefabPath);
    }

    private static void CylinderColliderOrCapsule(GameObject root, float radius, float height)
    {
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = radius;
        col.height = height;
        col.direction = 1; // Sumbu Y
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/Prefabs",
            "Assets/Prefabs/Trash",
            "Assets/Prefabs/Trash/Organic",
            "Assets/Prefabs/Trash/Inorganic",
            "Assets/Prefabs/Trash/B3",
            "Assets/ScriptableObjects",
            "Assets/ScriptableObjects/Trash",
            "Assets/ScriptableObjects/Trash/Organic",
            "Assets/ScriptableObjects/Trash/Inorganic",
            "Assets/ScriptableObjects/Trash/B3"
        };

        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                string name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
