using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using U1W.SceneManagement;

namespace U1W.Game
{
    public sealed class ConversationSkipHintOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private TextMeshProUGUI hintText;

        [Header("Display")]
        [SerializeField] private string targetChapterId = "chapter1";
        [SerializeField] private LocalizedString hintMessage;
        [SerializeField] private string hintMessageFallback =
            "\u30B9\u30DA\u30FC\u30B9\u30AD\u30FC\u9577\u62BC\u3057\u3067\u4F1A\u8A71\u30B9\u30AD\u30C3\u30D7";
        [SerializeField] [Min(0f)] private float visibleDuration = 2f;
        [SerializeField] [Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField] private Ease fadeOutEase = Ease.InSine;

        private Tween fadeTween;

        private void Awake()
        {
            ValidateReferences();
            HideImmediate();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void Start()
        {
            ShowIfEligibleAsync(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            KillFadeTween(false);
        }

        public void HideImmediate()
        {
            KillFadeTween(false);
            SetText(string.Empty);
            SetOverlayVisible(true);
            SetAlpha(0f);
        }

        private async UniTaskVoid ShowIfEligibleAsync(CancellationToken cancellationToken)
        {
            if (!ChapterProgressStore.IsReached(targetChapterId))
            {
                HideImmediate();
                return;
            }

            SetText(await ResolveTextAsync(cancellationToken));
            SetOverlayVisible(true);
            SetAlpha(1f);

            if (visibleDuration > 0f)
            {
                await UniTask.Delay(
                    Mathf.CeilToInt(visibleDuration * 1000f),
                    DelayType.UnscaledDeltaTime,
                    cancellationToken: cancellationToken);
            }

            if (fadeOutDuration <= 0f)
            {
                HideImmediate();
                return;
            }

            bool completed = false;
            fadeTween = overlayCanvasGroup
                .DOFade(0f, fadeOutDuration)
                .SetEase(fadeOutEase)
                .SetUpdate(true)
                .OnComplete(() => completed = true)
                .OnKill(() =>
                {
                    fadeTween = null;
                    completed = true;
                });

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                HideImmediate();
                throw;
            }

            SetAlpha(0f);
        }

        private async UniTask<string> ResolveTextAsync(CancellationToken cancellationToken)
        {
            if (hintMessage == null || hintMessage.IsEmpty)
            {
                return hintMessageFallback ?? string.Empty;
            }

            return await hintMessage.GetLocalizedStringAsync().Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        private void SetOverlayVisible(bool isVisible)
        {
            if (overlayRoot == null)
            {
                return;
            }

            if (!overlayRoot.activeSelf)
            {
                overlayRoot.SetActive(true);
            }

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.blocksRaycasts = false;
                overlayCanvasGroup.interactable = false;
            }
        }

        private void SetAlpha(float alpha)
        {
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = alpha;
            }
        }

        private void SetText(string value)
        {
            if (hintText != null)
            {
                hintText.text = value ?? string.Empty;
            }
        }

        private void KillFadeTween(bool complete)
        {
            if (fadeTween == null)
            {
                return;
            }

            Tween tween = fadeTween;
            fadeTween = null;
            if (tween.IsActive())
            {
                tween.Kill(complete);
            }
        }

        private void ValidateReferences()
        {
            WarnIfMissing(overlayRoot, nameof(overlayRoot));
            WarnIfMissing(overlayCanvasGroup, nameof(overlayCanvasGroup));
            WarnIfMissing(hintText, nameof(hintText));
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"ConversationSkipHintOverlay requires {fieldName} to be assigned via SerializeField.");
            }
        }
    }
}
