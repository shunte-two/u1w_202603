using System;
using TMPro;
using U1W.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConversationFactCardSetup
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string SourceCardPrefabPath = "Assets/Prefabs/UI/Operation/OperationCard.prefab";
    private const string ConversationCardPrefabPath = "Assets/Prefabs/UI/Conversation/ConversationFactCard.prefab";

    [MenuItem("U1W/Setup/Setup Conversation Fact Card Overlay")]
    public static void SetupConversationFactCardOverlay()
    {
        EnsureSceneOpen(GameScenePath);
        ConversationFactCardView cardPrefab = EnsureConversationCardPrefab();
        ConversationFactCardOverlay overlay = EnsureOverlay(cardPrefab);
        WireConversationManager(overlay);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeObject = overlay.gameObject;
        Debug.Log("Conversation fact card overlay created and wired.");
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

    private static ConversationFactCardView EnsureConversationCardPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ConversationCardPrefabPath) == null)
        {
            AssetDatabase.CopyAsset(SourceCardPrefabPath, ConversationCardPrefabPath);
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ConversationCardPrefabPath);
        try
        {
            OperationCardView operationCardView = prefabRoot.GetComponent<OperationCardView>();
            if (operationCardView != null)
            {
                UnityEngine.Object.DestroyImmediate(operationCardView, true);
            }

            ConversationFactCardView factCardView =
                GetOrAddComponent<ConversationFactCardView>(prefabRoot);
            RectTransform rootRect = prefabRoot.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = prefabRoot.GetComponent<CanvasGroup>();
            Image backgroundImage = prefabRoot.GetComponent<Image>();
            TextMeshProUGUI factText = FindRequiredText(prefabRoot.transform, "FactText");
            TextMeshProUGUI interpretationText = FindRequiredText(prefabRoot.transform, "InterpretationText");

            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(140f, 200f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = false;
            }

            SetObjectReference(factCardView, "rectTransform", rootRect);
            SetObjectReference(factCardView, "canvasGroup", canvasGroup);
            SetObjectReference(factCardView, "backgroundImage", backgroundImage);
            SetObjectReference(factCardView, "factText", factText);
            SetObjectReference(factCardView, "interpretationText", interpretationText);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ConversationCardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConversationCardPrefabPath);
        ConversationFactCardView result = prefabAsset != null
            ? prefabAsset.GetComponent<ConversationFactCardView>()
            : null;
        if (result == null)
        {
            throw new InvalidOperationException("Conversation fact card prefab could not be prepared.");
        }

        return result;
    }

    private static ConversationFactCardOverlay EnsureOverlay(ConversationFactCardView cardPrefab)
    {
        Transform conversationPanel = GameObject.Find("Canvas/ConversationPanel")?.transform;
        if (conversationPanel == null)
        {
            throw new InvalidOperationException("Canvas/ConversationPanel was not found in Game scene.");
        }

        GameObject overlayObject = EnsureChild(
            conversationPanel,
            "FactCardOverlay",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(ConversationFactCardOverlay));
        overlayObject.transform.SetSiblingIndex(Mathf.Min(2, conversationPanel.childCount - 1));
        overlayObject.SetActive(false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        if (overlayRect != null)
        {
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.localScale = Vector3.one;
        }

        CanvasGroup overlayCanvasGroup = overlayObject.GetComponent<CanvasGroup>();
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        GameObject cardAreaObject = EnsureChild(overlayObject.transform, "CardAreaRoot", typeof(RectTransform));
        RectTransform cardAreaRect = cardAreaObject.GetComponent<RectTransform>();
        if (cardAreaRect != null)
        {
            cardAreaRect.anchorMin = Vector2.zero;
            cardAreaRect.anchorMax = Vector2.one;
            cardAreaRect.offsetMin = Vector2.zero;
            cardAreaRect.offsetMax = Vector2.zero;
            cardAreaRect.anchoredPosition = Vector2.zero;
            cardAreaRect.localScale = Vector3.one;
        }

        ConversationFactCardOverlay overlay = overlayObject.GetComponent<ConversationFactCardOverlay>();
        SetObjectReference(overlay, "overlayRoot", overlayObject);
        SetObjectReference(overlay, "cardAreaRoot", cardAreaRect);
        SetObjectReference(overlay, "factCardViewPrefab", cardPrefab);
        return overlay;
    }

    private static void WireConversationManager(ConversationFactCardOverlay overlay)
    {
        ConversationPartManager manager = UnityEngine.Object.FindFirstObjectByType<ConversationPartManager>();
        if (manager == null)
        {
            throw new InvalidOperationException("ConversationPartManager was not found in Game scene.");
        }

        SetObjectReference(manager, "factCardOverlay", overlay);
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

    private static TextMeshProUGUI FindRequiredText(Transform root, string childName)
    {
        TextMeshProUGUI text = root.Find(childName)?.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            throw new InvalidOperationException($"{childName} was not found.");
        }

        return text;
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

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
