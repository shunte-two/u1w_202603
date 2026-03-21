using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using U1W.Audio;

namespace U1W.Game
{
    public sealed class ChapterIntroOverlay : MonoBehaviour
    {
        private const string ChapterStartSeKey = "ChapterStart";

        [Header("References")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform bannerRoot;
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private TextMeshProUGUI chapterTitleText;

        [Header("Animation")]
        [SerializeField] [Min(0f)] private float dimAlpha = 0.72f;
        [SerializeField] [Min(0f)] private float fadeInDuration = 0.2f;
        [SerializeField] [Min(0f)] private float bannerRevealDuration = 0.25f;
        [SerializeField] [Min(0f)] private float textFadeDuration = 0.18f;
        [SerializeField] [Min(0f)] private float holdDuration = 0.9f;
        [SerializeField] [Min(0f)] private float fadeOutDuration = 0.22f;
        [SerializeField] [Min(0f)] private float endWaitDuration = 0.3f;
        [SerializeField] private Ease fadeEase = Ease.OutSine;
        [SerializeField] private Ease bannerRevealEase = Ease.OutCubic;
        [SerializeField] private Ease fadeOutEase = Ease.InSine;
        [SerializeField] [Min(0f)] private float textEnterOffsetY = 18f;

        private Sequence activeSequence;
        private bool hasCachedTextPosition;

        private void Awake()
        {
            ValidateReferences();
            HideImmediate();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            KillSequence(false);
        }

        public async UniTask PlayAsync(
            LocalizedString chapterTitle,
            string chapterTitleFallback,
            CancellationToken cancellationToken)
        {
            if (overlayRoot == null)
            {
                return;
            }

            KillSequence(false);
            SetOverlayVisible(true);
            ApplyInitialVisualState();
            SetChapterTitle(await ResolveTextAsync(chapterTitle, chapterTitleFallback, cancellationToken));
            AudioManager.PlaySe(ChapterStartSeKey);

            bool completed = false;
            activeSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(overlayCanvasGroup.DOFade(dimAlpha, fadeInDuration).SetEase(fadeEase))
                .Join(bannerRoot.DOScaleY(1f, bannerRevealDuration).SetEase(bannerRevealEase))
                .Join(bannerCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase))
                .Join(chapterTitleText.DOFade(1f, textFadeDuration).SetDelay(0.08f).SetEase(fadeEase))
                .AppendInterval(holdDuration)
                .Append(chapterTitleText.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase))
                .Join(overlayCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase))
                .Join(bannerCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase))
                .AppendInterval(endWaitDuration)
                .OnComplete(() => completed = true)
                .OnKill(() =>
                {
                    activeSequence = null;
                    completed = true;
                });

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                HideImmediate();
                throw;
            }

            HideImmediate();
        }

        public void HideImmediate()
        {
            KillSequence(false);
            ApplyInitialVisualState();
        }

        private void ApplyInitialVisualState()
        {
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = false;
            }

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
                bannerCanvasGroup.interactable = false;
                bannerCanvasGroup.blocksRaycasts = false;
            }

            if (bannerRoot != null)
            {
                Vector3 scale = bannerRoot.localScale;
                bannerRoot.localScale = new Vector3(scale.x, 0f, scale.z);
            }

            if (chapterTitleText != null)
            {
                chapterTitleText.alpha = 0f;
            }
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
                overlayCanvasGroup.blocksRaycasts = isVisible;
            }
        }

        private void SetChapterTitle(string value)
        {
            if (chapterTitleText != null)
            {
                chapterTitleText.text = value ?? string.Empty;
            }
        }

        private void KillSequence(bool complete)
        {
            if (activeSequence == null)
            {
                return;
            }

            Sequence sequence = activeSequence;
            activeSequence = null;
            if (sequence.IsActive())
            {
                sequence.Kill(complete);
            }
        }

        private static async UniTask<string> ResolveTextAsync(
            LocalizedString localizedString,
            string fallbackValue,
            CancellationToken cancellationToken)
        {
            if (localizedString == null || localizedString.IsEmpty)
            {
                return fallbackValue ?? string.Empty;
            }

            return await localizedString.GetLocalizedStringAsync().Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        private void ValidateReferences()
        {
            WarnIfMissing(overlayRoot, nameof(overlayRoot));
            WarnIfMissing(overlayCanvasGroup, nameof(overlayCanvasGroup));
            WarnIfMissing(bannerRoot, nameof(bannerRoot));
            WarnIfMissing(bannerCanvasGroup, nameof(bannerCanvasGroup));
            WarnIfMissing(chapterTitleText, nameof(chapterTitleText));
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"ChapterIntroOverlay requires {fieldName} to be assigned via SerializeField.");
            }
        }
    }
}
