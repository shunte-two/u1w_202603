using System;
using TMPro;
using U1W.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConversationSkipHintOverlaySetup
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("U1W/Setup/Setup Conversation Skip Hint Overlay")]
    public static void SetupConversationSkipHintOverlay()
    {
        EnsureSceneOpen(GameScenePath);
        ConversationSkipHintOverlay overlay = EnsureOverlay();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeObject = overlay.gameObject;
        Debug.Log("Conversation skip hint overlay created and wired.");
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

    private static ConversationSkipHintOverlay EnsureOverlay()
    {
        Transform canvas = GameObject.Find("Canvas")?.transform;
        if (canvas == null)
        {
            throw new InvalidOperationException("Canvas was not found in Game scene.");
        }

        GameObject overlayObject = EnsureChild(
            canvas,
            "ConversationSkipHintOverlay",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(ConversationSkipHintOverlay));
        overlayObject.transform.SetAsLastSibling();

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        if (overlayRect != null)
        {
            overlayRect.anchorMin = new Vector2(0f, 1f);
            overlayRect.anchorMax = new Vector2(0f, 1f);
            overlayRect.pivot = new Vector2(0f, 1f);
            overlayRect.anchoredPosition = new Vector2(32f, -32f);
            overlayRect.sizeDelta = new Vector2(520f, 56f);
            overlayRect.localScale = Vector3.one;
        }

        CanvasGroup overlayCanvasGroup = overlayObject.GetComponent<CanvasGroup>();
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        GameObject textObject = EnsureChild(
            overlayObject.transform,
            "HintText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
        }

        TextMeshProUGUI hintText = textObject.GetComponent<TextMeshProUGUI>();
        ConfigureHintText(hintText);

        ConversationSkipHintOverlay overlay = overlayObject.GetComponent<ConversationSkipHintOverlay>();
        SetObjectReference(overlay, "overlayRoot", overlayObject);
        SetObjectReference(overlay, "overlayCanvasGroup", overlayCanvasGroup);
        SetObjectReference(overlay, "hintText", hintText);
        return overlay;
    }

    private static void ConfigureHintText(TextMeshProUGUI hintText)
    {
        if (hintText == null)
        {
            return;
        }

        hintText.text =
            "\u30B9\u30DA\u30FC\u30B9\u30AD\u30FC\u9577\u62BC\u3057\u3067\u4F1A\u8A71\u30B9\u30AD\u30C3\u30D7";
        hintText.fontSize = 24f;
        hintText.enableWordWrapping = false;
        hintText.alignment = TextAlignmentOptions.TopLeft;
        hintText.color = new Color32(255, 255, 255, 255);
        hintText.raycastTarget = false;
    }

    private static GameObject EnsureChild(Transform parent, string name, params Type[] componentTypes)
    {
        Transform child = parent.Find(name);
        GameObject gameObject;
        if (child == null)
        {
            gameObject = new GameObject(name, componentTypes);
            gameObject.transform.SetParent(parent, false);
        }
        else
        {
            gameObject = child.gameObject;
            for (int i = 0; i < componentTypes.Length; i++)
            {
                Type componentType = componentTypes[i];
                if (gameObject.GetComponent(componentType) == null)
                {
                    gameObject.AddComponent(componentType);
                }
            }
        }

        return gameObject;
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' was not found on {target.name}.");
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
