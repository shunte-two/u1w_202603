using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using U1W.Game;

GameObject FindByName(string name)
{
    var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    for (int i = 0; i < all.Length; i++)
    {
        if (all[i].name == name)
        {
            return all[i];
        }
    }

    return null;
}

var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
var panel = FindByName("ConversationLogPanel");
var window = FindByName("Window");
var scrollView = FindByName("ScrollView");
var content = FindByName("Content");
if (panel == null || window == null || scrollView == null || content == null)
{
    throw new System.InvalidOperationException("Conversation log base objects are missing.");
}

var canvasGroup = panel.GetComponent<CanvasGroup>();
if (canvasGroup == null)
{
    canvasGroup = panel.AddComponent<CanvasGroup>();
}
canvasGroup.alpha = 0f;
canvasGroup.interactable = false;
canvasGroup.blocksRaycasts = false;
panel.SetActive(true);

var scrollbarObject = FindByName("VerticalScrollbar");
if (scrollbarObject == null)
{
    scrollbarObject = new GameObject(
        "VerticalScrollbar",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image),
        typeof(Scrollbar));
    scrollbarObject.transform.SetParent(scrollView.transform, false);
}

var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
scrollbarRect.anchorMin = new Vector2(1f, 0f);
scrollbarRect.anchorMax = new Vector2(1f, 1f);
scrollbarRect.pivot = new Vector2(1f, 1f);
scrollbarRect.offsetMin = new Vector2(-18f, 12f);
scrollbarRect.offsetMax = new Vector2(-4f, -12f);

var scrollbarImage = scrollbarObject.GetComponent<Image>();
scrollbarImage.color = new Color(1f, 1f, 1f, 0.14f);
scrollbarImage.raycastTarget = true;

var handleArea = scrollbarObject.transform.Find("SlidingArea");
GameObject handleAreaObject;
if (handleArea == null)
{
    handleAreaObject = new GameObject("SlidingArea", typeof(RectTransform));
    handleAreaObject.transform.SetParent(scrollbarObject.transform, false);
}
else
{
    handleAreaObject = handleArea.gameObject;
}

var handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
handleAreaRect.anchorMin = Vector2.zero;
handleAreaRect.anchorMax = Vector2.one;
handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
handleAreaRect.offsetMin = Vector2.zero;
handleAreaRect.offsetMax = Vector2.zero;

var handle = handleAreaObject.transform.Find("Handle");
GameObject handleObject;
if (handle == null)
{
    handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
    handleObject.transform.SetParent(handleAreaObject.transform, false);
}
else
{
    handleObject = handle.gameObject;
}

var handleRect = handleObject.GetComponent<RectTransform>();
handleRect.anchorMin = Vector2.zero;
handleRect.anchorMax = Vector2.one;
handleRect.pivot = new Vector2(0.5f, 0.5f);
handleRect.offsetMin = Vector2.zero;
handleRect.offsetMax = Vector2.zero;

var handleImage = handleObject.GetComponent<Image>();
handleImage.color = new Color(1f, 1f, 1f, 0.72f);
handleImage.raycastTarget = true;

var scrollRect = scrollView.GetComponent<ScrollRect>();
var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
scrollbar.handleRect = handleRect;
scrollbar.targetGraphic = handleImage;
scrollbar.direction = Scrollbar.Direction.BottomToTop;
scrollbar.numberOfSteps = 0;
scrollbar.size = 0.2f;
scrollbar.value = 0f;
scrollRect.verticalScrollbar = scrollbar;
scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
scrollRect.verticalScrollbarSpacing = 8f;

var contentRect = content.GetComponent<RectTransform>();
contentRect.offsetMax = new Vector2(-28f, 0f);

var logPanel = panel.GetComponent<ConversationLogPanel>();
var serialized = new SerializedObject(logPanel);
serialized.FindProperty("panelRoot").objectReferenceValue = window;
serialized.FindProperty("panelCanvasGroup").objectReferenceValue = canvasGroup;
serialized.ApplyModifiedPropertiesWithoutUndo();

EditorUtility.SetDirty(panel);
EditorUtility.SetDirty(scrollView);
EditorUtility.SetDirty(scrollbarObject);
EditorUtility.SetDirty(handleObject);
EditorUtility.SetDirty(logPanel);
EditorSceneManager.MarkSceneDirty(scene);
EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
return "Conversation log visibility and scrollbar wired.";
