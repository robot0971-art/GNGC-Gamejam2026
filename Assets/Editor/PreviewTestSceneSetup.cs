using System.IO;
using Gamejam2026.Core;
using Gamejam2026.Gameplay;
using Gamejam2026.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PreviewTestSceneSetup
{
    private const string GeneratedFolder = "Assets/Generated/PreviewTest";

    [MenuItem("Tools/GameJam/Setup Preview Test Scene")]
    public static void Setup()
    {
        EnsureFolder();

        Sprite humanSprite = CreateSpriteAsset("PreviewHuman", new Color(0.8f, 0.9f, 1f, 1f));
        Sprite aiSprite = CreateSpriteAsset("PreviewAI", new Color(1f, 0.25f, 0.35f, 1f));

        EntrantSlot[] slots = SetupEntrantSlots();
        WavePresenter wavePresenter = GetOrCreateComponent<WavePresenter>("WavePresenter");
        CameraDirector cameraDirector = GetOrCreateComponent<CameraDirector>("CameraDirector");
        ShootingController shootingController = GetOrCreateComponent<ShootingController>("ShootingController");
        EntrantDatabase entrantDatabase = GetOrCreateComponent<EntrantDatabase>("EntrantDatabase");
        GameManager gameManager = GetOrCreateComponent<GameManager>("GameManager");

        SerializedObject wavePresenterObject = new SerializedObject(wavePresenter);
        SetArray(wavePresenterObject.FindProperty("slots"), slots);
        wavePresenterObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject cameraDirectorObject = new SerializedObject(cameraDirector);
        cameraDirectorObject.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        cameraDirectorObject.FindProperty("focusSize").floatValue = 2.8f;
        cameraDirectorObject.FindProperty("overviewSize").floatValue = 5.8f;
        cameraDirectorObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject shootingObject = new SerializedObject(shootingController);
        shootingObject.FindProperty("raycastCamera").objectReferenceValue = Camera.main;
        shootingObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject databaseObject = new SerializedObject(entrantDatabase);
        SetEntrantArray(databaseObject.FindProperty("humans"), new[] { new EntrantData { displayName = "Human", type = EntrantType.Human, sprite = humanSprite } });
        SetEntrantArray(databaseObject.FindProperty("ais"), new[] { new EntrantData { displayName = "AI", type = EntrantType.AI, sprite = aiSprite } });
        databaseObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject gameManagerObject = new SerializedObject(gameManager);
        SetStageArray(gameManagerObject.FindProperty("stages"), new[]
        {
            new StageConfig
            {
                stageName = "Preview",
                waveCount = 1,
                entrantsPerWave = 5,
                minAiCount = 2,
                maxAiCount = 2,
                previewHoldSeconds = 0.45f,
                previewPanSeconds = 0.35f,
                maxMistakes = 3
            }
        });
        gameManagerObject.FindProperty("entrantDatabase").objectReferenceValue = entrantDatabase;
        gameManagerObject.FindProperty("wavePresenter").objectReferenceValue = wavePresenter;
        gameManagerObject.FindProperty("cameraDirector").objectReferenceValue = cameraDirector;
        gameManagerObject.FindProperty("shootingController").objectReferenceValue = shootingController;
        gameManagerObject.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(wavePresenter);
        EditorUtility.SetDirty(cameraDirector);
        EditorUtility.SetDirty(shootingController);
        EditorUtility.SetDirty(entrantDatabase);
        EditorUtility.SetDirty(gameManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Preview test scene setup complete. Press Play to preview camera scan.");
    }

    private static EntrantSlot[] SetupEntrantSlots()
    {
        EntrantSlot[] slots = new EntrantSlot[5];

        for (int i = 0; i < slots.Length; i++)
        {
            string name = $"Charactor{i + 1}";
            GameObject character = GameObject.Find(name);

            if (character == null)
            {
                character = new GameObject(name);
                character.transform.position = new Vector3(-5f + i * 2.5f, -0.55f, 0f);
            }

            SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = character.AddComponent<SpriteRenderer>();
            }

            BoxCollider2D collider = character.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = character.AddComponent<BoxCollider2D>();
            }

            EntrantSlot slot = character.GetComponent<EntrantSlot>();
            if (slot == null)
            {
                slot = character.AddComponent<EntrantSlot>();
            }

            SerializedObject slotObject = new SerializedObject(slot);
            slotObject.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            slotObject.FindProperty("hitCollider").objectReferenceValue = collider;
            slotObject.ApplyModifiedPropertiesWithoutUndo();
            slots[i] = slot;
        }

        return slots;
    }

    private static T GetOrCreateComponent<T>(string objectName) where T : Component
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            target = new GameObject(objectName);
        }

        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "PreviewTest");
        }
    }

    private static Sprite CreateSpriteAsset(string name, Color color)
    {
        string path = $"{GeneratedFolder}/{name}.png";

        if (!File.Exists(path))
        {
            Texture2D texture = new Texture2D(64, 128, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64 * 128];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetEntrantArray(SerializedProperty property, EntrantData[] values)
    {
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("displayName").stringValue = values[i].displayName;
            element.FindPropertyRelative("type").enumValueIndex = (int)values[i].type;
            element.FindPropertyRelative("sprite").objectReferenceValue = values[i].sprite;
        }
    }

    private static void SetStageArray(SerializedProperty property, StageConfig[] values)
    {
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("stageName").stringValue = values[i].stageName;
            element.FindPropertyRelative("waveCount").intValue = values[i].waveCount;
            element.FindPropertyRelative("entrantsPerWave").intValue = values[i].entrantsPerWave;
            element.FindPropertyRelative("minAiCount").intValue = values[i].minAiCount;
            element.FindPropertyRelative("maxAiCount").intValue = values[i].maxAiCount;
            element.FindPropertyRelative("previewHoldSeconds").floatValue = values[i].previewHoldSeconds;
            element.FindPropertyRelative("previewPanSeconds").floatValue = values[i].previewPanSeconds;
            element.FindPropertyRelative("maxMistakes").intValue = values[i].maxMistakes;
        }
    }
}
