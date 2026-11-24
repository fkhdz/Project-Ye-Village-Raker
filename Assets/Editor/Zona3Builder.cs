using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Membangun kompleks Zona 3 "Balai Desa" secara utuh:
/// - Alun-alun (lantai, jalur masuk, pagar, gapura, bangku, lampu, bendera)
/// - Dua versi Balai Desa (rusak & megah)
/// - Taman kiri & kanan (gazebo, sumur, pohon, semak, bunga, batu, bangku)
/// - Grid titik spawn sampah campuran Zona 3 (alun-alun + halaman balai desa)
/// - Dekorasi pendukung
///
/// Seluruh hasil dibungkus satu parent <c>Zona3_BalaiDesa</c> lalu disimpan sebagai
/// <c>Assets/Prefabs/Zona3_BalaiDesa.prefab</c>. Scene kemudian memakai instance prefab
/// tersebut dan referensi GameManager/TrashSpawner diarahkan ulang otomatis.
///
/// Menu: YeVillage > Build Zona 3 Balai Desa
/// Idempotent: aman dijalankan berulang.
/// </summary>
public static class Zona3Builder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabPath = "Assets/Prefabs/Zona3_BalaiDesa.prefab";
    private const string RootName = "Zona3_BalaiDesa";
    private const string Q = "Assets/ThirdParty/Quaternius";
    private const string MatDir = "Assets/Materials/Ye_Village";

    private const string Square = "Environment/MedievalVillage/Props/Path_Square";
    private const string Straight = "Environment/MedievalVillage/Props/Path_Straight";
    private const string Fence = "Environment/MedievalVillage/Props/Fence";
    private const string Bench1 = "Environment/MedievalVillage/Props/Bench_1";
    private const string Bench2 = "Environment/MedievalVillage/Props/Bench_2";
    private const string Barrel = "Environment/MedievalVillage/Props/Barrel";
    private const string Crate = "Environment/MedievalVillage/Props/Crate";
    private const string Well = "Environment/MedievalVillage/Props/Well";
    private const string Bonfire = "Environment/MedievalVillage/Props/Bonfire";
    private const string Gazebo = "Environment/MedievalVillage/Props/Gazebo";

    [MenuItem("YeVillage/Build Zona 3 Balai Desa")]
    public static void BuildZona3()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        SceneManager.SetActiveScene(scene);

        // Bersihkan hasil build sebelumnya agar idempotent.
        DestroyIfExists(scene, RootName);
        DestroyIfExists(scene, "BalaiDesa_Rusak");
        DestroyIfExists(scene, "BalaiDesa_Megah");
        DestroyObjects(scene, t => t.name.StartsWith("SP_Zona3_"));
        DestroyObjects(scene, t => t.name == "Fence" && Mathf.Abs(t.position.z - 4.6f) < 0.25f);
        DestroyObjects(scene, t => t.name == "CommonTree_1" && Vector3.Distance(t.position, new Vector3(-8.6f, 0f, 7.6f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "CommonTree_2" && Vector3.Distance(t.position, new Vector3(8.6f, 0f, 7.9f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "BushBerries_1" && Vector3.Distance(t.position, new Vector3(-8.4f, 0f, 6.2f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "Willow_3" && Vector3.Distance(t.position, new Vector3(4.9f, 0f, 6.4f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "Bush_1" && Vector3.Distance(t.position, new Vector3(-4.3f, 0f, 5.3f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "Bush_2" && Vector3.Distance(t.position, new Vector3(4.4f, 0f, 5.1f)) < 0.6f);
        DestroyObjects(scene, t => t.name == "Bonfire" && Vector3.Distance(t.position, new Vector3(3.6f, 0f, 5.7f)) < 0.6f);

        // Root tunggal Zona 3.
        GameObject root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        Transform bangunan = Group(root.transform, "Bangunan");
        GameObject rusak = BuildBalai(bangunan, "BalaiDesa_Rusak",
            "Environment/MedievalVillage/Buildings/House_3",
            1.7f, new Vector3(0f, 0f, 12.5f), 180f, true);
        GameObject megah = BuildBalai(bangunan, "BalaiDesa_Megah",
            "Environment/MedievalVillage/Buildings/Inn",
            1.3f, new Vector3(0f, 0f, 11.5f), 180f, false);

        DressRusak(rusak);
        DressMegah(megah);

        BuildAlunAlun(root.transform);
        BuildTamanKiri(root.transform);
        BuildTamanKanan(root.transform);
        BuildSpawnPoints(root.transform);
        BuildDekorasi(root.transform);

        // Simpan prefab.
        EnsureFolder("Assets/Prefabs");
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        if (prefabAsset == null)
        {
            Debug.LogError("[Zona3Builder] Gagal menyimpan prefab di " + PrefabPath);
            return;
        }

        // Ganti root sementara dengan instance prefab di scene.
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Object.DestroyImmediate(root);

        RewireScene(instance, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = instance;
        Debug.Log("[Zona3Builder] Zona 3 selesai. Prefab: " + PrefabPath +
                  " | Root: " + RootName);
    }

    [MenuItem("YeVillage/Validate Zona 3 Balai Desa")]
    public static void ValidateZona3()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[Zona3Builder] SampleScene tidak terbuka. Validasi dilewati.");
            return;
        }

        GameObject zona = FindInScene(scene, RootName);
        if (zona == null)
        {
            Debug.LogError("[Zona3Builder] Root 'Zona3_BalaiDesa' tidak ditemukan di scene.");
            return;
        }

        int children = zona.GetComponentsInChildren<Transform>(true).Length - 1;
        GameObject gmObj = FindInScene(scene, "GameManager");
        GameManager gm = gmObj != null ? gmObj.GetComponent<GameManager>() : null;
        TrashSpawner spawner = FindInScene(scene, "TrashSpawnerManager")?.GetComponent<TrashSpawner>();

        bool gmOk = gm != null && gm.balaiDesaRusak != null && gm.balaiDesaMegah != null;
        bool spawnerOk = spawner != null && spawner.spawnPoints != null;
        int zonaSpawnCount = 0;
        if (spawner != null && spawner.spawnPoints != null)
        {
            foreach (Transform t in spawner.spawnPoints)
            {
                if (t != null && t.name.StartsWith("SP_Zona3_")) zonaSpawnCount++;
            }
        }

        Debug.Log("[Zona3Builder] Validasi: root=" + zona.name +
                  " | child=" + children +
                  " | GameManager refs=" + gmOk +
                  " | spawner refs=" + spawnerOk +
                  " | spawn Zona3=" + zonaSpawnCount +
                  " | prefab=" + (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null));
    }

    // ------------------------------------------------------------------ bangunan

    private static GameObject BuildBalai(Transform parent, string name, string modelRel,
        float scale, Vector3 pos, float rotY, bool active)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        GameObject model = Model(modelRel, go.transform, Vector3.zero, 0f, scale);
        if (model != null) model.name = "Model_Quaternius";

        FitBoxCollider(go);
        go.SetActive(active);
        return go;
    }

    /// <summary>
    /// Nuansa kumuh untuk versi rusak: puing papan, batu, dan semak liar
    /// di sekitar kaki bangunan. Semua anak aktif bersama BalaiDesa_Rusak.
    /// </summary>
    private static void DressRusak(GameObject rusak)
    {
        if (rusak == null) return;

        Transform puing = Group(rusak.transform, "Puing");
        Material wood = LoadMaterial("M_Wood");
        Material rock = LoadMaterial("M_Rock");

        // Papan patah berserakan di halaman depan (local +Z = arah gerbang).
        GameObject plank1 = Primitive(puing, "Papan_1", PrimitiveType.Cube,
            new Vector3(-2.6f, 0.06f, 3.3f), new Vector3(1.7f, 0.09f, 0.28f), wood);
        plank1.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
        GameObject plank2 = Primitive(puing, "Papan_2", PrimitiveType.Cube,
            new Vector3(2.4f, 0.06f, 3.5f), new Vector3(1.4f, 0.09f, 0.26f), wood);
        plank2.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);
        GameObject plank3 = Primitive(puing, "Papan_3", PrimitiveType.Cube,
            new Vector3(0.6f, 0.06f, 3.9f), new Vector3(1.1f, 0.08f, 0.24f), wood);
        plank3.transform.localRotation = Quaternion.Euler(0f, 42f, 0f);

        Primitive(puing, "Batu_1", PrimitiveType.Cube,
            new Vector3(-1.2f, 0.1f, 3.7f), new Vector3(0.42f, 0.2f, 0.36f), rock);
        Primitive(puing, "Batu_2", PrimitiveType.Cube,
            new Vector3(1.8f, 0.08f, 3.2f), new Vector3(0.3f, 0.16f, 0.28f), rock);

        // Semak liar yang tumbuh di sisi bangunan.
        Model("Environment/Nature/Bush_1", puing, new Vector3(-3.8f, 0f, 3.0f), 12f, 1.25f);
        Model("Environment/Nature/Bush_2", puing, new Vector3(3.8f, 0f, 3.0f), 192f, 1.25f);
        Model("Environment/Nature/Rock_1", puing, new Vector3(-2.0f, 0f, 4.0f), 25f, 0.85f);
        Model("Environment/Nature/Rock_3", puing, new Vector3(2.2f, 0f, 4.0f), 205f, 0.8f);
    }

    /// <summary>
    /// Taman tertata untuk versi "Megah &amp; Asri": pohon simetris, semak
    /// berbunga, dan bedeng bunga di sepanjang teras. Semua anak otomatis
    /// muncul saat GameManager mengaktifkan BalaiDesa_Megah.
    /// </summary>
    private static void DressMegah(GameObject megah)
    {
        if (megah == null) return;

        Transform taman = Group(megah.transform, "TamanMegah");

        // Pohon peneduh simetris di kiri-kanan bangunan.
        Model("Environment/Nature/CommonTree_1", taman, new Vector3(-4.6f, 0f, 2.4f), 20f, 0.95f);
        Model("Environment/Nature/CommonTree_2", taman, new Vector3(4.6f, 0f, 2.4f), 200f, 0.95f);

        // Pagar hijau dari semak berbunga.
        Model("Environment/Nature/BushBerries_1", taman, new Vector3(-3.2f, 0f, 3.1f), 0f, 1.05f);
        Model("Environment/Nature/BushBerries_1", taman, new Vector3(3.2f, 0f, 3.1f), 180f, 1.05f);
        Model("Environment/Nature/Bush_1", taman, new Vector3(-2.2f, 0f, 3.4f), 40f, 1.0f);
        Model("Environment/Nature/Bush_2", taman, new Vector3(2.2f, 0f, 3.4f), 220f, 1.0f);

        // Bedeng bunga warna-warni di depan teras.
        Model("Environment/Nature/Plant_1", taman, new Vector3(-1.2f, 0f, 3.6f), 15f, 1.1f);
        Model("Environment/Nature/Plant_2", taman, new Vector3(1.2f, 0f, 3.6f), 195f, 1.1f);
        Model("Environment/Nature/Plant_3", taman, new Vector3(-0.4f, 0f, 3.8f), 90f, 1.0f);
        Model("Environment/Nature/Plant_1", taman, new Vector3(0.4f, 0f, 3.8f), 270f, 1.0f);

        // Pot bunga di kedua sisi tangga masuk.
        Model("Environment/Nature/Plant_2", taman, new Vector3(-2.6f, 0f, 4.2f), 30f, 1.15f);
        Model("Environment/Nature/Plant_3", taman, new Vector3(2.6f, 0f, 4.2f), 210f, 1.15f);
    }

    // ------------------------------------------------------------------ alun-alun

    private static void BuildAlunAlun(Transform root)
    {
        Transform alun = Group(root, "AlunAlun");

        // Lantai plaza: 5 x 2 ubin besar.
        // Ubin diturunkan agar permukaannya hampir rata dengan tanah (tidak membuat
        // tangga 25 cm yang membuat kaki pemain terbenam).
        Transform lantai = Group(alun, "Lantai");
        float[] xs = { -5f, -2.5f, 0f, 2.5f, 5f };
        float[] zs = { 6f, 8.5f };
        foreach (float z in zs)
        {
            foreach (float x in xs)
            {
                Model(Square, lantai, new Vector3(x, -0.21f, z), 0f, 5f);
            }
        }

        // Lanjutan jalur dari gerbang menuju Balai Desa.
        Transform jalur = Group(alun, "JalurMasuk");
        for (int z = 6; z <= 10; z++)
        {
            Model(Straight, jalur, new Vector3(-0.26f, 0.02f, z), 0f, 1f);
            Model(Straight, jalur, new Vector3(0.26f, 0.02f, z), 0f, 1f);
        }

        // Pagar depan dengan gerbang tengah.
        Transform pagar = Group(alun, "Pagar");
        for (float x = -6.4f; x <= 6.4f; x += 0.8f)
        {
            if (Mathf.Abs(x) <= 0.81f) continue; // celah gerbang
            Model(Fence, pagar, new Vector3(x, 0.01f, 4.6f), 0f, 1f);
        }

        BuildGapura(alun);
        BuildLampuTaman(alun);
        BuildBendera(alun);

        // Bangku dan tanaman pinggir plaza.
        Transform bangku = Group(alun, "Bangku");
        GameObject benchA = Model(Bench1, bangku, new Vector3(-4.7f, 0f, 7.2f), 90f, 1.8f);
        GameObject benchB = Model(Bench2, bangku, new Vector3(4.7f, 0f, 7.2f), -90f, 1.8f);
        if (benchA != null) FitBoxCollider(benchA);
        if (benchB != null) FitBoxCollider(benchB);

        Transform pot = Group(alun, "PotBunga");
        Model("Environment/Nature/Plant_1", pot, new Vector3(-5.6f, 0f, 5.2f), 20f, 1.2f);
        Model("Environment/Nature/Plant_2", pot, new Vector3(5.6f, 0f, 5.2f), 140f, 1.2f);
        Model("Environment/Nature/Plant_3", pot, new Vector3(-5.6f, 0f, 9.4f), 70f, 1.2f);
        Model("Environment/Nature/Plant_1", pot, new Vector3(5.6f, 0f, 9.4f), 250f, 1.2f);
    }

    private static void BuildGapura(Transform alun)
    {
        Transform gapura = Group(alun, "Gapura");
        Material wood = LoadMaterial("M_Wood");

        Primitive(gapura, "TiangKiri", PrimitiveType.Cube,
            new Vector3(-1.25f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), wood);
        Primitive(gapura, "TiangKanan", PrimitiveType.Cube,
            new Vector3(1.25f, 1.25f, 0f), new Vector3(0.18f, 2.5f, 0.18f), wood);
        Primitive(gapura, "BalokAtas", PrimitiveType.Cube,
            new Vector3(0f, 2.55f, 0f), new Vector3(2.9f, 0.18f, 0.22f), wood);
        Primitive(gapura, "PapanNama", PrimitiveType.Cube,
            new Vector3(0f, 2.18f, -0.02f), new Vector3(1.9f, 0.42f, 0.06f), wood);

        AddText(gapura, "TeksNama", "BALAI DESA", new Vector3(0f, 2.18f, -0.07f));
    }

    private static void BuildLampuTaman(Transform alun)
    {
        Transform lampu = Group(alun, "LampuTaman");
        Vector3[] positions =
        {
            new Vector3(-5.8f, 0f, 5.4f),
            new Vector3(5.8f, 0f, 5.4f),
            new Vector3(-5.8f, 0f, 9.2f),
            new Vector3(5.8f, 0f, 9.2f)
        };

        Material rock = LoadMaterial("M_Rock");
        Material wood = LoadMaterial("M_Wood");
        Material glow = LoadMaterial("M_LeafYellow");

        for (int i = 0; i < positions.Length; i++)
        {
            Transform t = Group(lampu, "Lampu_" + (i + 1).ToString("00"));
            Vector3 p = positions[i];
            Primitive(t, "Base", PrimitiveType.Cylinder,
                p + new Vector3(0f, 0.08f, 0f), new Vector3(0.24f, 0.08f, 0.24f), rock);
            Primitive(t, "Tiang", PrimitiveType.Cylinder,
                p + new Vector3(0f, 1.15f, 0f), new Vector3(0.07f, 1.1f, 0.07f), wood);
            Primitive(t, "Lentera", PrimitiveType.Cube,
                p + new Vector3(0f, 2.35f, 0f), new Vector3(0.3f, 0.36f, 0.3f), glow);
            Primitive(t, "Atap", PrimitiveType.Cube,
                p + new Vector3(0f, 2.58f, 0f), new Vector3(0.42f, 0.08f, 0.42f), rock);
        }
    }

    private static void BuildBendera(Transform alun)
    {
        Transform bendera = Group(alun, "Bendera");
        Material rock = LoadMaterial("M_Rock");
        Material red = LoadMaterial("M_Tong_Merah");
        Material white = EnsureMaterial("M_Bendera_Putih", Color.white);
        Material gold = LoadMaterial("M_LeafYellow");

        Vector3 p = new Vector3(3.6f, 0f, 9.5f);
        Primitive(bendera, "Tiang", PrimitiveType.Cylinder,
            p + new Vector3(0f, 2.25f, 0f), new Vector3(0.06f, 2.25f, 0.06f), rock);
        Primitive(bendera, "BenderaMerah", PrimitiveType.Cube,
            p + new Vector3(0.56f, 4.32f, 0f), new Vector3(1f, 0.25f, 0.02f), red);
        Primitive(bendera, "BenderaPutih", PrimitiveType.Cube,
            p + new Vector3(0.56f, 4.07f, 0f), new Vector3(1f, 0.25f, 0.02f), white);
        Primitive(bendera, "Puncak", PrimitiveType.Sphere,
            p + new Vector3(0f, 4.55f, 0f), new Vector3(0.12f, 0.12f, 0.12f), gold);
    }

    // ------------------------------------------------------------------ taman

    private static void BuildTamanKiri(Transform root)
    {
        Transform taman = Group(root, "TamanKiri");

        Model(Square, taman, new Vector3(-9f, -0.21f, 10.6f), 0f, 5f);
        Model(Gazebo, taman, new Vector3(-9f, 0f, 11f), 0f, 2.2f);

        // Pohon & semak yang sudah ada di area ini, dirapikan ke dalam prefab.
        Model("Environment/Nature/CommonTree_1", taman, new Vector3(-8.6f, 0f, 7.6f), 15f, 1.15f);
        Model("Environment/Nature/BushBerries_1", taman, new Vector3(-8.4f, 0f, 6.2f), 30f, 1.1f);
        Model("Environment/Nature/Bush_1", taman, new Vector3(-6.8f, 0f, 5.6f), 0f, 1.2f);

        Transform bunga = Group(taman, "Bunga");
        Model("Environment/Nature/Plant_1", bunga, new Vector3(-10.8f, 0f, 8.8f), 15f, 1.2f);
        Model("Environment/Nature/Plant_2", bunga, new Vector3(-6.4f, 0f, 9.2f), 120f, 1.1f);
        Model("Environment/Nature/Plant_3", bunga, new Vector3(-9f, 0f, 6f), 240f, 1.2f);

        Model("Environment/Nature/Rock_1", taman, new Vector3(-11f, 0f, 9.6f), 30f, 1.3f);
        GameObject bench = Model(Bench1, taman, new Vector3(-6.6f, 0f, 10.8f), 90f, 1.8f);
        if (bench != null) FitBoxCollider(bench);

        Transform pagar = Group(taman, "Pagar");
        for (float x = -11.2f; x <= -6.4f; x += 0.8f)
        {
            Model(Fence, pagar, new Vector3(x, 0.01f, 5.4f), 0f, 1f);
        }
    }

    private static void BuildTamanKanan(Transform root)
    {
        Transform taman = Group(root, "TamanKanan");

        Model(Square, taman, new Vector3(9f, -0.21f, 10.6f), 0f, 5f);
        GameObject well = Model(Well, taman, new Vector3(8.8f, 0f, 11f), 20f, 1.3f);
        if (well != null) FitBoxCollider(well);

        Model("Environment/Nature/CommonTree_2", taman, new Vector3(8.6f, 0f, 7.9f), 100f, 1.1f);
        Model("Environment/Nature/Willow_3", taman, new Vector3(10.6f, 0f, 6.4f), 150f, 0.95f);
        Model("Environment/Nature/Bush_2", taman, new Vector3(6.8f, 0f, 5.6f), 0f, 1.2f);

        Transform bunga = Group(taman, "Bunga");
        Model("Environment/Nature/Plant_1", bunga, new Vector3(10.8f, 0f, 8.8f), 210f, 1.2f);
        Model("Environment/Nature/Plant_2", bunga, new Vector3(6.4f, 0f, 9.2f), 300f, 1.1f);
        Model("Environment/Nature/Plant_3", bunga, new Vector3(9f, 0f, 6f), 80f, 1.2f);

        Model("Environment/Nature/Rock_3", taman, new Vector3(11f, 0f, 9.6f), 210f, 1.3f);
        Model(Bonfire, taman, new Vector3(10.4f, 0f, 11.6f), 0f, 3f);
        GameObject bench = Model(Bench2, taman, new Vector3(6.6f, 0f, 10.8f), -90f, 1.8f);
        if (bench != null) FitBoxCollider(bench);

        Transform pagar = Group(taman, "Pagar");
        for (float x = 6.4f; x <= 11.2f; x += 0.8f)
        {
            Model(Fence, pagar, new Vector3(x, 0.01f, 5.4f), 0f, 1f);
        }
    }

    // ------------------------------------------------------------------ spawn & dekorasi

    private static void BuildSpawnPoints(Transform root)
    {
        Transform group = Group(root, "SpawnPoints_Zona3");

        // Grid rapat menutupi alun-alun dan halaman Balai Desa agar sampah
        // terus muncul tersebar sampai renovasi dibeli.
        Vector3[] points =
        {
            new Vector3(-4.8f, 0.02f, 5.6f),
            new Vector3(-1.6f, 0.02f, 5.6f),
            new Vector3( 1.6f, 0.02f, 5.6f),
            new Vector3( 4.8f, 0.02f, 5.6f),

            new Vector3(-3.2f, 0.02f, 7.0f),
            new Vector3( 0.0f, 0.02f, 7.0f),
            new Vector3( 3.2f, 0.02f, 7.0f),

            new Vector3(-4.8f, 0.02f, 8.4f),
            new Vector3(-1.6f, 0.02f, 8.4f),
            new Vector3( 1.6f, 0.02f, 8.4f),
            new Vector3( 4.8f, 0.02f, 8.4f),

            new Vector3(-3.2f, 0.02f, 9.7f),
            new Vector3( 0.0f, 0.02f, 9.7f),
            new Vector3( 3.2f, 0.02f, 9.7f)
        };

        for (int i = 0; i < points.Length; i++)
        {
            AddSpawn(group, "SP_Zona3_" + (i + 1).ToString("00"), points[i]);
        }
    }

    private static void AddSpawn(Transform parent, string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
    }

    private static void BuildDekorasi(Transform root)
    {
        Transform dekor = Group(root, "Dekorasi");
        GameObject[] props =
        {
            Model(Barrel, dekor, new Vector3(-3f, 0f, 10.6f), 0f, 4f),
            Model(Barrel, dekor, new Vector3(3f, 0f, 10.6f), 30f, 4f),
            Model(Crate, dekor, new Vector3(-2.4f, 0f, 9.8f), 15f, 3.5f),
            Model(Crate, dekor, new Vector3(2.4f, 0f, 9.8f), 35f, 3.5f)
        };
        foreach (GameObject prop in props)
        {
            if (prop != null) FitBoxCollider(prop);
        }
    }

    // ------------------------------------------------------------------ referensi scene

    private static void RewireScene(GameObject zonaInstance, Scene scene)
    {
        GameObject gmObj = FindInScene(scene, "GameManager");
        GameManager gm = gmObj != null ? gmObj.GetComponent<GameManager>() : null;
        if (gm != null)
        {
            gm.balaiDesaRusak = zonaInstance.transform.Find("Bangunan/BalaiDesa_Rusak")?.gameObject;
            gm.balaiDesaMegah = zonaInstance.transform.Find("Bangunan/BalaiDesa_Megah")?.gameObject;
            EditorUtility.SetDirty(gm);
        }

        GameObject spawnerObj = FindInScene(scene, "TrashSpawnerManager");
        TrashSpawner spawner = spawnerObj != null ? spawnerObj.GetComponent<TrashSpawner>() : null;
        Transform spawnGroup = zonaInstance.transform.Find("SpawnPoints_Zona3");
        if (spawner != null && spawnGroup != null)
        {
            // Pertahankan titik spawn zona lain, lalu daftarkan ulang SELURUH
            // titik Zona 3 supaya penambahan titik baru ikut terpakai dan
            // tidak ada duplikat saat builder dijalankan berulang.
            List<Transform> points = new List<Transform>();
            if (spawner.spawnPoints != null)
            {
                foreach (Transform point in spawner.spawnPoints)
                {
                    if (point != null && !point.name.StartsWith("SP_Zona3_"))
                    {
                        points.Add(point);
                    }
                }
            }

            foreach (Transform point in spawnGroup)
            {
                points.Add(point);
            }

            spawner.spawnPoints = points.ToArray();
            EditorUtility.SetDirty(spawner);
        }
    }

    // ------------------------------------------------------------------ helper

    private static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Model(string rel, Transform parent, Vector3 localPosition,
        float rotY, float scale)
    {
        string path = Q + "/" + rel + ".fbx";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning("[Zona3Builder] Model tidak ditemukan: " + path);
            return null;
        }

        GameObject go = (GameObject)Object.Instantiate(asset, parent);
        go.name = Path.GetFileNameWithoutExtension(rel);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * go.transform.localRotation;
        go.transform.localScale = Vector3.Scale(go.transform.localScale, Vector3.one * scale);
        return go;
    }

    private static GameObject Primitive(Transform parent, string name, PrimitiveType type,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return go;
    }

    private static void AddText(Transform parent, string name, string content, Vector3 localPosition)
    {
        GameObject go = new GameObject(name, typeof(TextMesh));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;

        TextMesh text = go.GetComponent<TextMesh>();
        text.text = content;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 64;
        text.characterSize = 0.05f;
        text.color = new Color(0.96f, 0.9f, 0.72f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) return;

        text.font = font;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = font.material;
    }

    private static void FitBoxCollider(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null) collider = root.AddComponent<BoxCollider>();

        collider.center = root.transform.InverseTransformPoint(bounds.center);
        Vector3 size = root.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        collider.isTrigger = false;
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/" + name + ".mat");
        if (material == null) Debug.LogWarning("[Zona3Builder] Material tidak ditemukan: " + name);
        return material;
    }

    private static Material EnsureMaterial(string name, Color color)
    {
        string path = MatDir + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeep(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindDeep(Transform transform, string name)
    {
        if (transform.name == name) return transform;
        foreach (Transform child in transform)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void DestroyIfExists(Scene scene, string name)
    {
        GameObject go = FindInScene(scene, name);
        while (go != null)
        {
            Object.DestroyImmediate(go);
            go = FindInScene(scene, name);
        }
    }

    private static void DestroyObjects(Scene scene, System.Func<Transform, bool> predicate)
    {
        List<GameObject> targets = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectMatches(root.transform, predicate, targets);
        }

        foreach (GameObject target in targets)
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void CollectMatches(Transform transform, System.Func<Transform, bool> predicate,
        List<GameObject> targets)
    {
        if (predicate(transform)) targets.Add(transform.gameObject);
        foreach (Transform child in transform)
        {
            CollectMatches(child, predicate, targets);
        }
    }
}
