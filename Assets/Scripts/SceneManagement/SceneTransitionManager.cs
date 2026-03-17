using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace U1W.SceneManagement
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager instance;

        [Header("Fade")]
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] [Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField] [Min(0f)] private float fadeInDuration = 0.35f;

        private CanvasGroup canvasGroup;
        private Image fadeImage;
        private int transitionVersion;
        private bool isTransitioning;

        public static bool IsTransitioning => instance != null && instance.isTransitioning;

        public static void LoadScene(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneTransitionManager.LoadScene failed: scene name is empty.");
                return;
            }

            SceneTransitionManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.StartTransition(new SceneLoadRequest(sceneName, loadSceneMode));
        }

        public static void LoadScene(int buildIndex, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            SceneTransitionManager manager = EnsureInstance();
            if (manager == null)
            {
                return;
            }

            manager.StartTransition(new SceneLoadRequest(buildIndex, loadSceneMode));
        }

        public static void ReloadCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            LoadScene(currentScene.buildIndex);
        }

        public static SceneTransitionManager EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<SceneTransitionManager>();
            if (instance != null)
            {
                instance.Initialize();
                return instance;
            }

            Debug.LogError("SceneTransitionManager is not present in the scene. Place a SceneTransitionManager in the scene before using scene transition APIs.");
            return null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Initialize()
        {
            DontDestroyOnLoad(gameObject);

            if (canvasGroup != null && fadeImage != null)
            {
                return;
            }

            CreateOverlay();
            SetOverlayAlpha(0f);
            canvasGroup.blocksRaycasts = false;
        }

        private void CreateOverlay()
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("FadeCanvas");
                canvasObject.transform.SetParent(transform, false);

                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue;

                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasObject.AddComponent<GraphicRaycaster>();
            }

            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            fadeImage = canvas.GetComponentInChildren<Image>(true);
            if (fadeImage == null)
            {
                GameObject imageObject = new GameObject("FadeImage");
                imageObject.transform.SetParent(canvas.transform, false);

                RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                fadeImage = imageObject.AddComponent<Image>();
            }

            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = false;
        }

        private void StartTransition(in SceneLoadRequest request)
        {
            if (!CanLoad(request))
            {
                return;
            }

            transitionVersion++;
            RunTransitionAsync(request, transitionVersion).Forget();
        }

        private bool CanLoad(in SceneLoadRequest request)
        {
            if (request.SceneName != null)
            {
                if (Application.CanStreamedLevelBeLoaded(request.SceneName))
                {
                    return true;
                }

                Debug.LogError($"SceneTransitionManager.LoadScene failed: scene '{request.SceneName}' is not in Build Settings.");
                return false;
            }

            if (request.BuildIndex >= 0 && request.BuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                return true;
            }

            Debug.LogError($"SceneTransitionManager.LoadScene failed: build index '{request.BuildIndex}' is out of range.");
            return false;
        }

        private async UniTaskVoid RunTransitionAsync(SceneLoadRequest request, int version)
        {
            isTransitioning = true;
            canvasGroup.blocksRaycasts = true;
            fadeImage.color = fadeColor;

            await FadeAsync(0f, 1f, fadeOutDuration, version);
            if (!IsCurrentTransition(version))
            {
                return;
            }

            AsyncOperation loadOperation = request.SceneName != null
                ? SceneManager.LoadSceneAsync(request.SceneName, request.LoadSceneMode)
                : SceneManager.LoadSceneAsync(request.BuildIndex, request.LoadSceneMode);

            if (loadOperation == null)
            {
                Debug.LogError("SceneTransitionManager.LoadScene failed: Unity did not return an AsyncOperation.");
                await FadeAsync(1f, 0f, fadeInDuration, version);
                if (IsCurrentTransition(version))
                {
                    CompleteTransition();
                }

                return;
            }

            await loadOperation.ToUniTask();
            await UniTask.Yield(PlayerLoopTiming.Update);

            if (!IsCurrentTransition(version))
            {
                return;
            }

            await FadeAsync(1f, 0f, fadeInDuration, version);

            if (IsCurrentTransition(version))
            {
                CompleteTransition();
            }
        }

        private async UniTask FadeAsync(float from, float to, float duration, int version)
        {
            if (duration <= 0f)
            {
                SetOverlayAlpha(to);
                return;
            }

            float elapsed = 0f;
            SetOverlayAlpha(from);

            while (elapsed < duration)
            {
                if (!IsCurrentTransition(version))
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetOverlayAlpha(Mathf.Lerp(from, to, progress));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            SetOverlayAlpha(to);
        }

        private void SetOverlayAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }

        private void CompleteTransition()
        {
            canvasGroup.blocksRaycasts = false;
            isTransitioning = false;
        }

        private bool IsCurrentTransition(int version)
        {
            return version == transitionVersion;
        }

        private readonly struct SceneLoadRequest
        {
            public SceneLoadRequest(string sceneName, LoadSceneMode loadSceneMode)
            {
                SceneName = sceneName;
                BuildIndex = -1;
                LoadSceneMode = loadSceneMode;
            }

            public SceneLoadRequest(int buildIndex, LoadSceneMode loadSceneMode)
            {
                SceneName = null;
                BuildIndex = buildIndex;
                LoadSceneMode = loadSceneMode;
            }

            public string SceneName { get; }
            public int BuildIndex { get; }
            public LoadSceneMode LoadSceneMode { get; }
        }
    }
}
