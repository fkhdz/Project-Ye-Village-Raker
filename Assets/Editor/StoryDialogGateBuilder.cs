using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoryDialogGateBuilder
{
    private const string Level1Path = "Assets/Scenes/Level1_Zona1_Pinggiran.unity";
    private const string Level2Path = "Assets/Scenes/Level2_Zona2_Pinggiran.unity";
    private const string Level3Path = "Assets/Scenes/Level3_Zona3_BalaiDesa.unity";
    private const string DataFolder = "Assets/Resources/Dialogue";
    private const string SpriteFolder = "Assets/Resources/Dialogue";
    private const string FontPath = "Assets/Resources/Dialogue/Alegreya-Bold SDF.asset";

    private const string OpeningId = "Opening";
    private const string Zone1Id = "Zona1";
    private const string Zone2Id = "Zona2";
    private const string Zone3Id = "Zona3";

    [MenuItem("YeVillage/Story/Build Story Dialog Gate")]
    public static void Build()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty)
            {
                Debug.LogError("[StoryBuilder] Ada scene belum disimpan: " + open.name);
                return;
            }
        }

        if (!File.Exists(FontPath))
        {
            Debug.LogError("[StoryBuilder] TMP Essential Resources belum ada. Import TMP Essential Resources dulu.");
            return;
        }

        Directory.CreateDirectory(DataFolder);

        ConfigureSprites();

        CreateSequence(OpeningId,
            L(DialogueSpeaker.KepalaDesa, "waduh kenapa aku di lengserkan seperti ini?"),
            L(DialogueSpeaker.TetuaDesa, "HAHAHA"),
            L(DialogueSpeaker.TetuaDesa, "Kamu masih belum tau kesalah kamu dimana?"),
            L(DialogueSpeaker.KepalaDesa, "Ha?"),
            L(DialogueSpeaker.KepalaDesa, "Memang nya aku salah apa? Aku sudah berusaha menjadi kepala desa yang baik"),
            L(DialogueSpeaker.TetuaDesa, "Memang benar dari banyak kebijakan mu memang baik namun apakah kamu pernah sesekali peduli terhadap lingkungan mu?"),
            L(DialogueSpeaker.KepalaDesa, "….."),
            L(DialogueSpeaker.TetuaDesa, "Kan.. kamu tidak bisa berkata apa apa setelah melihat banyak nya sampah yang ada di desa ini"),
            L(DialogueSpeaker.KepalaDesa, "Lalu aku harus bagaimana?"),
            L(DialogueSpeaker.TetuaDesa, "Hmm.. aku akan coba berunding bersama warga dulu"));

        CreateSequence(Zone1Id,
            L(DialogueSpeaker.KepalaDesa, "Bagaimana hasil nya?"),
            L(DialogueSpeaker.TetuaDesa, "Para warga tadi setuju kalau kamu bisa membuang sampah di 3 zona ini dan membuanya lebih bersih maka mereka akan menerima mu kembali sebagai pemimpin."),
            L(DialogueSpeaker.TetuaDesa, "Tugas kamu sederhana mulai dari ambil sampah dan buang sesuai dengan jenis sampah nya"),
            L(DialogueSpeaker.KepalaDesa, "Baiklah, aku akan berusaha lagi dan membuktikan kepada mereka kalau aku peduli terhadap lingkungan disekitar ku"),
            L(DialogueSpeaker.TetuaDesa, "Bagus sekarang kerjakan"),
            L(DialogueSpeaker.TetuaDesa, "Oiya nantinya setiap kamu mengumpulkan sampah dan membuangnya benar di tempat nya maka kamu bisa mendapatkan poin, dan poin tersebut bisa kamu tukarkan ke aku"));

        CreateSequence(Zone2Id,
            L(DialogueSpeaker.TetuaDesa, "Selamat datang di Zona 2"),
            L(DialogueSpeaker.KepalaDesa, "Hufftt jujur ini cukup melelahkan, tapi tak apa"),
            L(DialogueSpeaker.TetuaDesa, "Hahaha itulah mengapa banyak orang mengatakan merusak lebih mudah dari pada memperbaiki"),
            L(DialogueSpeaker.KepalaDesa, "baiklah itu kata-kata yang cukup bagus untuk ku saat ini"),
            L(DialogueSpeaker.TetuaDesa, "HAHAHA"),
            L(DialogueSpeaker.KepalaDesa, "Jadi disini ada apa saja sampah nya? Apakah ada yang berbeda dari sebelum nya?"),
            L(DialogueSpeaker.TetuaDesa, "Jadi di Zona 2 ini akan ada sampah B3 yang nantinya jika kamu tidak membeli sarung tangan maka kamu akan kena efek atau gerakan kamu melambat"),
            L(DialogueSpeaker.TetuaDesa, "Tapi sebelumnya apakah kamu tau apa itu sampah B3?"),
            L(DialogueSpeaker.KepalaDesa, "waduh aku belum banyak tau soal sampah B3"),
            L(DialogueSpeaker.TetuaDesa, "Sampah B3 adalah Sampah Bahan Berbahaya dan Beracun. Istilah itu merujuk pada Zat, energi, atau komponen lain yang dapat mencemari atau merusak lingkungan serta membahayakan kesehatan manusia dan makhluk hidup."),
            L(DialogueSpeaker.KepalaDesa, "J-jadi a-aku harus berhati-hati dong?"),
            L(DialogueSpeaker.TetuaDesa, "Ya benar sekali"),
            L(DialogueSpeaker.TetuaDesa, "Contoh sampah B3 ada Baterai bekas, aki, Oli bekas. sisa bahan kimia, dan lain nya"),
            L(DialogueSpeaker.KepalaDesa, "O-okay"),
            L(DialogueSpeaker.TetuaDesa, "Bagus lah jika kamu cepat paham"),
            L(DialogueSpeaker.TetuaDesa, "Silahkan kamu bisa lakukan pekerjaan mu saat ini"));

        CreateSequence(Zone3Id,
            L(DialogueSpeaker.TetuaDesa, "Kerja yang sangat bagus, aku sangatt mengapresiasi kamu"),
            L(DialogueSpeaker.KepalaDesa, "Terimakasih"),
            L(DialogueSpeaker.TetuaDesa, "Sekarang selamat datang di Zona 3"),
            L(DialogueSpeaker.KepalaDesa, "yaampun ga kerasa udah tinggal 1 Zona lagi, aku tidak sabar ingin kembali ke ke kediamanku"),
            L(DialogueSpeaker.TetuaDesa, "HAHAHA"),
            L(DialogueSpeaker.TetuaDesa, "Jadi aku jelaskan saja langsung"),
            L(DialogueSpeaker.TetuaDesa, "Disini jenis sampah nya masih seperti di Zona 2 namun disini lah sampah lebih banyak atau menumpuk jadi kamu harus lebih effort untuk membersihkan nya"),
            L(DialogueSpeaker.KepalaDesa, "Waduhh, tak apa. Aku sudah mulai terbiasa untuk memilah dan membuang sampah pada tempat nya"),
            L(DialogueSpeaker.TetuaDesa, "Bagus lah kalau begitu"));

        BuildLevel1();
        BuildZoneScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StoryBuilder] Story, Dialog 2D, dan Gate berhasil dipasang.");
    }

    private static DialogueLine L(DialogueSpeaker speaker, string text)
    {
        return new DialogueLine { speaker = speaker, text = text };
    }

    private static void ConfigureSprites()
    {
        SetSprite("SP_Kades.png", Vector4.zero);
        SetSprite("SP_Tetua.png", Vector4.zero);
        SetSprite("SP_Bubble.png", Vector4.zero);
    }

    private static void SetSprite(string fileName, Vector4 border)
    {
        string path = SpriteFolder + "/" + fileName;
        if (!File.Exists(path))
        {
            Debug.LogError("[StoryBuilder] Sprite tidak ditemukan: " + path);
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static void CreateSequence(string id, params DialogueLine[] lines)
    {
        string path = DataFolder + "/" + id + ".asset";
        DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (sequence != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        AssetDatabase.CreateAsset(sequence, path);
        sequence.sequenceId = id;
        sequence.lines = lines;
        EditorUtility.SetDirty(sequence);
    }

    private static void BuildLevel1()
    {
        Scene scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);
        GameObject root = GetOrCreateRoot("--- STORY / GATE ---");
        DialogueManager dialogue = InstallManagers(root);

        GameObject gate = BuildGate(root.transform, "Gate_Zone1_To_Zone2", new Vector3(0f, 0f, 25.7f),
            12f, 150, "Zone2", "Zona 2", "Level2_Zona2_Pinggiran", "GERBANG ZONA 2");
        gate.GetComponent<ZoneGate>().price = 150;

        ZoneExitMarker exit = Object.FindFirstObjectByType<ZoneExitMarker>();
        if (exit != null)
        {
            exit.requireTargetScore = false;
            exit.targetSceneName = "Level2_Zona2_Pinggiran";
            exit.portalLabel = "Lanjut ke Zona 2";
            EditorUtility.SetDirty(exit);
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StoryBuilder] Level1 story + Gate 1 tersimpan.");
    }

    private static void BuildZoneScenes()
    {
        Scene zone2 = EditorSceneManager.OpenScene(Level2Path, OpenSceneMode.Single);
        GameObject root2 = GetOrCreateRoot("--- STORY / GATE ---");
        InstallManagers(root2);

        BuildGate(root2.transform, "Gate_Zone2_To_Zone3", new Vector3(2f, 0f, 24.5f),
            70f, 300, "Zone3", "Zona 3", "Level3_Zona3_BalaiDesa", "GERBANG ZONA 3");

        CreateIntroTrigger(root2.transform, "Zone2_IntroTrigger", new Vector3(0f, 1f, 12f),
            new Vector3(50f, 5f, 4f), Zone2Id);

        CreatePortal(root2.transform, "Exit_To_Zona3", new Vector3(2f, 1.5f, 27.5f),
            new Vector3(12f, 4f, 3f), "Level3_Zona3_BalaiDesa", "Lanjut ke Zona 3", true, false);
        CreatePortal(root2.transform, "Exit_Back_To_Zone1", new Vector3(0f, 1.5f, -47f),
            new Vector3(8f, 4f, 3f), "Level1_Zona1_Pinggiran", "Kembali ke Zona 1", false, true);

        EditorSceneManager.SaveScene(zone2);
        Debug.Log("[StoryBuilder] Level2 story + Gate 2 + portal tersimpan.");

        Scene zone3 = EditorSceneManager.OpenScene(Level3Path, OpenSceneMode.Single);
        GameObject root3 = GetOrCreateRoot("--- STORY / GATE ---");
        InstallManagers(root3);

        CreateIntroTrigger(root3.transform, "Zone3_IntroTrigger", new Vector3(2f, 1f, 30f),
            new Vector3(50f, 5f, 4f), Zone3Id);
        CreatePortal(root3.transform, "Exit_Back_To_Zone2", new Vector3(0f, 1.5f, 23f),
            new Vector3(10f, 4f, 3f), "Level2_Zona2_Pinggiran", "Kembali ke Zona 2", false, true);

        EditorSceneManager.SaveScene(zone3);
        Debug.Log("[StoryBuilder] Level3 story + portal kembali tersimpan.");
    }

    private static void CreatePortal(Transform parent, string name, Vector3 position, Vector3 size,
        string targetScene, string label, bool requireAccess, bool showSign)
    {
        GameObject portal = GetOrCreateChild(name, parent);
        portal.transform.position = position;

        BoxCollider collider = portal.GetComponent<BoxCollider>();
        if (collider == null) collider = portal.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        collider.center = Vector3.zero;

        ZoneExitMarker marker = portal.GetComponent<ZoneExitMarker>();
        if (marker == null) marker = portal.AddComponent<ZoneExitMarker>();
        marker.targetSceneName = targetScene;
        marker.portalLabel = label;
        marker.requireZoneAccess = requireAccess;
        marker.loadOnEnter = false;
        marker.requireTargetScore = false;
        EditorUtility.SetDirty(marker);

        if (showSign)
        {
            WorldLabelBillboard.CreateLabel(portal.transform, label.ToUpperInvariant(),
                new Color(0.12f, 0.34f, 0.22f), new Vector3(0f, 3.6f, 0f));
        }
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);
        if (root == null) root = new GameObject(name);
        return root;
    }

    private static DialogueManager InstallManagers(GameObject root)
    {
        GameObject dialogueObject = GetOrCreateChild("DialogueManager", root.transform);
        DialogueManager dialogue = dialogueObject.GetComponent<DialogueManager>();
        if (dialogue == null) dialogue = dialogueObject.AddComponent<DialogueManager>();

        dialogue.openingSequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DataFolder + "/" + OpeningId + ".asset");
        dialogue.zone1Sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DataFolder + "/" + Zone1Id + ".asset");
        dialogue.zone2Sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DataFolder + "/" + Zone2Id + ".asset");
        dialogue.zone3Sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DataFolder + "/" + Zone3Id + ".asset");
        dialogue.portraitKepalaDesa = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/SP_Kades.png");
        dialogue.portraitTetuaDesa = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/SP_Tetua.png");
        dialogue.bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + "/SP_Bubble.png");
        dialogue.fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        EditorUtility.SetDirty(dialogue);

        GameObject flowObject = GetOrCreateChild("StoryFlow", root.transform);
        StoryFlow flow = flowObject.GetComponent<StoryFlow>();
        if (flow == null) flow = flowObject.AddComponent<StoryFlow>();
        flow.dialogueManager = dialogue;
        flow.levelSceneName = "Level1_Zona1_Pinggiran";
        EditorUtility.SetDirty(flow);

        return dialogue;
    }

    private static GameObject GetOrCreateChild(string name, Transform parent)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject BuildGate(Transform parent, string name, Vector3 position, float width, int price,
        string gateId, string zoneLabel, string targetScene, string signText)
    {
        GameObject gate = GetOrCreateChild(name, parent);
        gate.transform.position = position;
        gate.transform.rotation = Quaternion.identity;

        BoxCollider trigger = gate.GetComponent<BoxCollider>();
        if (trigger == null) trigger = gate.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(width, 4f, 3f);
        trigger.center = new Vector3(0f, 2f, 0f);

        ZoneGate zoneGate = gate.GetComponent<ZoneGate>();
        if (zoneGate == null) zoneGate = gate.AddComponent<ZoneGate>();
        zoneGate.gateId = gateId;
        zoneGate.zoneLabel = zoneLabel;
        zoneGate.price = price;
        zoneGate.targetSceneName = targetScene;
        zoneGate.barrier = CreateBarrier(gate.transform, width);
        zoneGate.lockedSign = WorldLabelBillboard.CreateLabel(gate.transform,
            signText + " — TERKUNCI", new Color(0.55f, 0.16f, 0.12f), new Vector3(0f, 3.6f, 0f));
        zoneGate.unlockedSign = WorldLabelBillboard.CreateLabel(gate.transform,
            signText + " — TERBUKA", new Color(0.12f, 0.45f, 0.2f), new Vector3(0f, 3.6f, 0f));
        EditorUtility.SetDirty(zoneGate);

        return gate;
    }

    private static GameObject CreateBarrier(Transform parent, float width)
    {
        Material wood = LoadGateMaterial();
        GameObject barrier = new GameObject("Barrier");
        barrier.transform.SetParent(parent, false);
        barrier.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Beam";
        beam.transform.SetParent(barrier.transform, false);
        beam.transform.localScale = new Vector3(width, 0.28f, 0.35f);
        beam.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        Object.DestroyImmediate(beam.GetComponent<Collider>());
        beam.GetComponent<Renderer>().sharedMaterial = wood;

        GameObject lowerBeam = Object.Instantiate(beam, barrier.transform);
        lowerBeam.name = "LowerBeam";
        lowerBeam.transform.localPosition = new Vector3(0f, -0.5f, 0f);

        int posts = Mathf.Max(3, Mathf.RoundToInt(width / 5f));
        for (int i = 0; i <= posts; i++)
        {
            float x = -width * 0.5f + width * i / posts;
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post_" + i;
            post.transform.SetParent(barrier.transform, false);
            post.transform.localPosition = new Vector3(x, 0f, 0f);
            post.transform.localScale = new Vector3(0.22f, 2.7f, 0.35f);
            Object.DestroyImmediate(post.GetComponent<Collider>());
            post.GetComponent<Renderer>().sharedMaterial = wood;
        }

        GameObject solid = new GameObject("SolidCollider");
        solid.transform.SetParent(barrier.transform, false);
        BoxCollider col = solid.AddComponent<BoxCollider>();
        col.size = new Vector3(width, 2.7f, 0.4f);
        return barrier;
    }

    private static Material LoadGateMaterial()
    {
        const string path = "Assets/Materials/Story/M_GateWood.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory("Assets/Materials/Story");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = "M_GateWood" };
        material.color = new Color(0.42f, 0.26f, 0.13f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateIntroTrigger(Transform parent, string name, Vector3 position, Vector3 size, string sequenceId)
    {
        GameObject triggerObject = GetOrCreateChild(name, parent);
        triggerObject.transform.position = position;
        BoxCollider col = triggerObject.GetComponent<BoxCollider>();
        if (col == null) col = triggerObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;

        ZoneIntroTrigger intro = triggerObject.GetComponent<ZoneIntroTrigger>();
        if (intro == null) intro = triggerObject.AddComponent<ZoneIntroTrigger>();
        intro.sequenceId = sequenceId;
        EditorUtility.SetDirty(intro);
    }
}
