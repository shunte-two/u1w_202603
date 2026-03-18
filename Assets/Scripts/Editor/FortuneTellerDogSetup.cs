using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using U1W.Game;

public static class FortuneTellerDogSetup
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string SpriteDirectory = "Assets/Sprites/Characters/FortuneTellerTemp";
    private const string AnimationDirectory = "Assets/Animations/Characters/FortuneTellerTemp";
    private const string ControllerPath = AnimationDirectory + "/FortuneTellerPortrait.controller";
    private const int FortuneTellerCharacterIdIndex = (int)StoryCharacterId.FortuneTeller;

    [MenuItem("U1W/Setup/Create Temporary Fortune Teller Dog")]
    public static void CreateTemporaryFortuneTellerDog()
    {
        EnsureSceneOpen(GameScenePath);

        Sprite normalSprite = CreateDogSprite(
            SpriteDirectory + "/FortuneTellerDog_Normal.asset",
            "FortuneTellerDog_Normal",
            DogExpression.Normal);
        Sprite joySprite = CreateDogSprite(
            SpriteDirectory + "/FortuneTellerDog_Joy.asset",
            "FortuneTellerDog_Joy",
            DogExpression.Joy);
        Sprite angrySprite = CreateDogSprite(
            SpriteDirectory + "/FortuneTellerDog_Angry.asset",
            "FortuneTellerDog_Angry",
            DogExpression.Angry);

        AnimationClip normalClip = CreateSpriteClip(
            AnimationDirectory + "/FortuneTellerDog_Normal.anim",
            normalSprite);
        AnimationClip joyClip = CreateSpriteClip(
            AnimationDirectory + "/FortuneTellerDog_Joy.anim",
            joySprite);
        AnimationClip angryClip = CreateSpriteClip(
            AnimationDirectory + "/FortuneTellerDog_Angry.anim",
            angrySprite);

        AnimatorController controller = CreateController(normalClip, joyClip, angryClip);
        StoryCharacterPortrait portrait = CreateOrUpdatePortrait(normalSprite, controller);
        WireConversationPartManager(portrait);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeObject = portrait.gameObject;
        Debug.Log("Temporary fortune teller dog portrait created and wired.");
    }

    private static void EnsureSceneOpen(string scenePath)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == scenePath)
        {
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
    }

    private static StoryCharacterPortrait CreateOrUpdatePortrait(
        Sprite defaultSprite,
        RuntimeAnimatorController controller)
    {
        GameObject root = GameObject.Find("CharacterPortraitsRoot");
        if (root == null)
        {
            root = new GameObject("CharacterPortraitsRoot");
        }

        GameObject portraitObject = GameObject.Find("FortuneTellerPortrait");
        if (portraitObject == null)
        {
            portraitObject = new GameObject("FortuneTellerPortrait");
            portraitObject.transform.SetParent(root.transform, false);
        }

        portraitObject.transform.position = new Vector3(3.35f, -0.6f, 0f);
        portraitObject.transform.localScale = new Vector3(3.2f, 3.2f, 1f);

        SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(portraitObject);
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.sortingOrder = 10;

        Animator animator = GetOrAddComponent<Animator>(portraitObject);
        animator.runtimeAnimatorController = controller;

        StoryCharacterPortrait portrait = GetOrAddComponent<StoryCharacterPortrait>(portraitObject);
        SerializedObject portraitObjectData = new SerializedObject(portrait);
        portraitObjectData.FindProperty("characterId").enumValueIndex = FortuneTellerCharacterIdIndex;
        portraitObjectData.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        portraitObjectData.FindProperty("animator").objectReferenceValue = animator;
        portraitObjectData.FindProperty("defaultExpressionId").enumValueIndex = (int)StoryExpressionId.Normal;
        portraitObjectData.FindProperty("playDefaultOnEnable").boolValue = true;
        portraitObjectData.FindProperty("hideWhenExpressionMissing").boolValue = false;

        SerializedProperty expressions = portraitObjectData.FindProperty("expressions");
        expressions.arraySize = 3;
        SetExpression(expressions.GetArrayElementAtIndex(0), StoryExpressionId.Normal, "Normal");
        SetExpression(expressions.GetArrayElementAtIndex(1), StoryExpressionId.Joy, "Joy");
        SetExpression(expressions.GetArrayElementAtIndex(2), StoryExpressionId.Angry, "Angry");
        portraitObjectData.ApplyModifiedPropertiesWithoutUndo();

        return portrait;
    }

    private static void WireConversationPartManager(StoryCharacterPortrait portrait)
    {
        ConversationPartManager manager = UnityEngine.Object.FindFirstObjectByType<ConversationPartManager>();
        if (manager == null)
        {
            throw new InvalidOperationException("ConversationPartManager was not found in Game scene.");
        }

        SerializedObject managerData = new SerializedObject(manager);
        SerializedProperty portraits = managerData.FindProperty("characterPortraits");
        for (int i = 0; i < portraits.arraySize; i++)
        {
            if (portraits.GetArrayElementAtIndex(i).objectReferenceValue == portrait)
            {
                managerData.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        int newIndex = portraits.arraySize;
        portraits.arraySize++;
        portraits.GetArrayElementAtIndex(newIndex).objectReferenceValue = portrait;
        managerData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetExpression(SerializedProperty expressionProperty, StoryExpressionId expressionId, string stateName)
    {
        expressionProperty.FindPropertyRelative("expressionId").enumValueIndex = (int)expressionId;
        expressionProperty.FindPropertyRelative("animatorStateName").stringValue = stateName;
    }

    private static AnimatorController CreateController(
        AnimationClip normalClip,
        AnimationClip joyClip,
        AnimationClip angryClip)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        for (int i = stateMachine.states.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveState(stateMachine.states[i].state);
        }

        AnimatorState normalState = stateMachine.AddState("Normal");
        normalState.motion = normalClip;
        AnimatorState joyState = stateMachine.AddState("Joy");
        joyState.motion = joyClip;
        AnimatorState angryState = stateMachine.AddState("Angry");
        angryState.motion = angryClip;
        stateMachine.defaultState = normalState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip CreateSpriteClip(string path, Sprite sprite)
    {
        AssetDatabase.DeleteAsset(path);
        AnimationClip clip = new AnimationClip();
        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] keys =
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprite }
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keys);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static Sprite CreateDogSprite(string path, string assetName, DogExpression expression)
    {
        AssetDatabase.DeleteAsset(path);

        Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false)
        {
            name = assetName + "_Texture",
            filterMode = FilterMode.Point
        };

        Color32[] pixels = new Color32[256 * 256];
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        Color32 fur = new Color32(214, 170, 108, 255);
        Color32 ear = new Color32(126, 84, 50, 255);
        Color32 muzzle = new Color32(244, 226, 190, 255);
        Color32 dark = new Color32(43, 33, 33, 255);
        Color32 tongue = new Color32(214, 92, 118, 255);
        Color32 blush = new Color32(234, 142, 162, 255);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 brow = new Color32(87, 55, 38, 255);

        void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= 256 || y < 0 || y >= 256)
            {
                return;
            }

            pixels[y * 256 + x] = color;
        }

        void FillCircle(int cx, int cy, int radius, Color32 color)
        {
            int sqr = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if ((dx * dx) + (dy * dy) <= sqr)
                    {
                        SetPixel(x, y, color);
                    }
                }
            }
        }

        void FillEllipse(int cx, int cy, int rx, int ry, Color32 color)
        {
            int rx2 = rx * rx;
            int ry2 = ry * ry;
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if ((dx * dx * ry2) + (dy * dy * rx2) <= rx2 * ry2)
                    {
                        SetPixel(x, y, color);
                    }
                }
            }
        }

        void FillRect(int xMin, int yMin, int xMax, int yMax, Color32 color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    SetPixel(x, y, color);
                }
            }
        }

        void FillEar(int cx, int cy, int size, bool left, Color32 color)
        {
            for (int row = 0; row < size; row++)
            {
                int width = (row * 22) / size + 8;
                for (int col = 0; col < width; col++)
                {
                    int x = left ? cx - col : cx + col;
                    int y = cy - row;
                    SetPixel(x, y, color);
                }
            }
        }

        FillEar(84, 210, 72, true, ear);
        FillEar(172, 210, 72, false, ear);
        FillCircle(128, 134, 82, fur);
        FillEllipse(128, 98, 58, 46, fur);
        FillEllipse(128, 90, 44, 34, muzzle);
        FillEllipse(128, 86, 18, 11, dark);
        FillEllipse(94, 108, 16, 12, blush);
        FillEllipse(162, 108, 16, 12, blush);

        switch (expression)
        {
            case DogExpression.Joy:
                FillRect(80, 136, 108, 142, brow);
                FillRect(148, 136, 176, 142, brow);
                FillEllipse(128, 52, 28, 18, tongue);
                FillEllipse(128, 64, 36, 18, dark);
                break;

            case DogExpression.Angry:
                for (int i = 0; i < 20; i++)
                {
                    FillRect(77 + i, 138 - (i / 3), 88 + i, 141 - (i / 3), brow);
                    FillRect(168 - i, 138 - (i / 3), 179 - i, 141 - (i / 3), brow);
                }

                FillRect(88, 128, 104, 134, white);
                FillRect(152, 128, 168, 134, white);
                FillRect(92, 130, 100, 134, dark);
                FillRect(156, 130, 164, 134, dark);
                FillRect(110, 56, 146, 60, dark);
                break;

            default:
                FillEllipse(92, 132, 12, 8, dark);
                FillEllipse(164, 132, 12, 8, dark);
                FillRect(114, 58, 142, 62, dark);
                FillRect(112, 54, 118, 58, dark);
                FillRect(138, 54, 144, 58, dark);
                break;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        AssetDatabase.CreateAsset(texture, path);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 256f, 256f),
            new Vector2(0.5f, 0.15f),
            100f);
        sprite.name = assetName + "_Sprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        EditorUtility.SetDirty(texture);
        return sprite;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return target.AddComponent<T>();
    }

    private enum DogExpression
    {
        Normal,
        Joy,
        Angry
    }
}
