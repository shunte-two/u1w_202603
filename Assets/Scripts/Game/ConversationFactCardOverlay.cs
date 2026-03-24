using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization;

namespace U1W.Game
{
    public sealed class ConversationFactCardOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private RectTransform cardAreaRoot;
        [SerializeField] private ConversationFactCardView factCardViewPrefab;

        [Header("Layout")]
        [SerializeField] private Vector2 centerPosition = Vector2.zero;
        [SerializeField] private Vector2 stackOriginPosition = new(-360f, 0f);
        [SerializeField] private Vector2 stackOffset = new(28f, -18f);
        [SerializeField] private float moveDuration = 0.35f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        [SerializeField] private float clearStaggerSeconds = 0.04f;

        private readonly List<ConversationFactCardView> stackedCards = new();
        private ConversationFactCardView centerCard;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        public void ResetImmediate()
        {
            if (centerCard != null)
            {
                Destroy(centerCard.gameObject);
                centerCard = null;
            }

            for (int i = 0; i < stackedCards.Count; i++)
            {
                ConversationFactCardView stackedCard = stackedCards[i];
                if (stackedCard != null)
                {
                    Destroy(stackedCard.gameObject);
                }
            }

            stackedCards.Clear();
            SetOverlayVisible(false);
        }

        public async UniTask ShowCardAsync(
            OperationChapterAsset chapterAsset,
            string cardId,
            StoryFactCardFaceMode faceMode,
            CancellationToken cancellationToken)
        {
            if (!CanAnimate())
            {
                return;
            }

            if (centerCard != null)
            {
                await StackCurrentCardAsync(cancellationToken);
            }

            OperationCardDefinition definition = FindCardDefinition(chapterAsset, cardId);
            if (definition == null)
            {
                Debug.LogWarning(
                    $"ConversationFactCardOverlay could not resolve card '{cardId}' from '{chapterAsset?.name ?? "null"}'.",
                    this);
                return;
            }

            bool isFlipped = ResolveFaceState(definition, faceMode);
            string factText = await ResolveTextAsync(definition.FactText, cancellationToken);
            string interpretationText = await ResolveTextAsync(
                isFlipped ? definition.BackInterpretation : definition.FrontInterpretation,
                cancellationToken);
            GameObject createdObject = Instantiate(factCardViewPrefab.gameObject, cardAreaRoot);
            ConversationFactCardView createdView = createdObject != null
                ? createdObject.GetComponent<ConversationFactCardView>()
                : null;
            if (createdView == null)
            {
                Debug.LogWarning(
                    $"ConversationFactCardOverlay could not instantiate fact card view from '{factCardViewPrefab.name}'.",
                    this);
                if (createdObject != null)
                {
                    Destroy(createdObject);
                }

                return;
            }

            centerCard = createdView;
            centerCard.SetContent(factText, interpretationText, isFlipped);
            centerCard.SetAnchoredPosition(GetOffscreenPosition(createdView, enterFromRight: true));
            RefreshSiblingOrder();
            SetOverlayVisible(true);
            await AwaitTweenAsync(
                centerCard.AnimateTo(centerPosition, moveDuration, moveEase),
                cancellationToken);
        }

        public async UniTask StackCurrentCardAsync(CancellationToken cancellationToken)
        {
            if (!CanAnimate() || centerCard == null)
            {
                return;
            }

            stackedCards.Add(centerCard);
            centerCard = null;
            RefreshSiblingOrder();
            SetOverlayVisible(true);
            await AnimateStackLayoutAsync(cancellationToken);
        }

        public async UniTask ClearCardsAsync(CancellationToken cancellationToken)
        {
            if (!CanAnimate())
            {
                return;
            }

            if (centerCard != null)
            {
                stackedCards.Add(centerCard);
                centerCard = null;
            }

            if (stackedCards.Count == 0)
            {
                SetOverlayVisible(false);
                return;
            }

            RefreshSiblingOrder();

            List<UniTask> tasks = new(stackedCards.Count);
            for (int i = 0; i < stackedCards.Count; i++)
            {
                ConversationFactCardView stackedCard = stackedCards[i];
                if (stackedCard == null)
                {
                    continue;
                }

                tasks.Add(AnimateCardExitAsync(stackedCard, i, cancellationToken));
            }

            await UniTask.WhenAll(tasks);

            for (int i = 0; i < stackedCards.Count; i++)
            {
                ConversationFactCardView stackedCard = stackedCards[i];
                if (stackedCard != null)
                {
                    Destroy(stackedCard.gameObject);
                }
            }

            stackedCards.Clear();
            SetOverlayVisible(false);
        }

        private bool CanAnimate()
        {
            if (overlayRoot == null || cardAreaRoot == null || factCardViewPrefab == null)
            {
                Debug.LogWarning(
                    "ConversationFactCardOverlay requires overlayRoot, cardAreaRoot, and factCardViewPrefab via SerializeField.",
                    this);
                return false;
            }

            return true;
        }

        private async UniTask AnimateStackLayoutAsync(CancellationToken cancellationToken)
        {
            if (stackedCards.Count == 0)
            {
                return;
            }

            List<UniTask> tasks = new(stackedCards.Count);
            for (int i = 0; i < stackedCards.Count; i++)
            {
                ConversationFactCardView stackedCard = stackedCards[i];
                if (stackedCard == null)
                {
                    continue;
                }

                tasks.Add(AwaitTweenAsync(
                    stackedCard.AnimateTo(GetStackPosition(i), moveDuration, moveEase),
                    cancellationToken));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateCardExitAsync(
            ConversationFactCardView cardView,
            int index,
            CancellationToken cancellationToken)
        {
            if (cardView == null)
            {
                return;
            }

            float delaySeconds = clearStaggerSeconds * index;
            if (delaySeconds > 0f)
            {
                int delayMilliseconds = Mathf.CeilToInt(delaySeconds * 1000f);
                await UniTask.Delay(delayMilliseconds, cancellationToken: cancellationToken);
            }

            Vector2 currentPosition = cardView.RectTransform != null
                ? cardView.RectTransform.anchoredPosition
                : Vector2.zero;
            Vector2 exitPosition = GetOffscreenPosition(cardView, enterFromRight: false);
            exitPosition.y = currentPosition.y;
            await AwaitTweenAsync(cardView.AnimateTo(exitPosition, moveDuration, moveEase), cancellationToken);
        }

        private void RefreshSiblingOrder()
        {
            for (int i = 0; i < stackedCards.Count; i++)
            {
                ConversationFactCardView stackedCard = stackedCards[i];
                if (stackedCard != null)
                {
                    stackedCard.transform.SetSiblingIndex(i);
                }
            }

            if (centerCard != null)
            {
                centerCard.transform.SetAsLastSibling();
            }
        }

        private Vector2 GetStackPosition(int stackIndex)
        {
            return stackOriginPosition + stackOffset * stackIndex;
        }

        private Vector2 GetOffscreenPosition(ConversationFactCardView cardView, bool enterFromRight)
        {
            float cardWidth = cardView?.RectTransform != null
                ? cardView.RectTransform.rect.width
                : 0f;
            float containerWidth = cardAreaRoot != null ? cardAreaRoot.rect.width : 0f;
            float offsetX = containerWidth * 0.5f + cardWidth * 0.75f;
            float x = enterFromRight ? offsetX : -offsetX;
            return new Vector2(x, centerPosition.y);
        }

        private OperationCardDefinition FindCardDefinition(OperationChapterAsset chapterAsset, string cardId)
        {
            if (chapterAsset == null || chapterAsset.Cards == null || string.IsNullOrWhiteSpace(cardId))
            {
                return null;
            }

            for (int i = 0; i < chapterAsset.Cards.Length; i++)
            {
                OperationCardDefinition definition = chapterAsset.Cards[i];
                if (definition == null)
                {
                    continue;
                }

                if (string.Equals(definition.Id, cardId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool ResolveFaceState(
            OperationCardDefinition definition,
            StoryFactCardFaceMode faceMode)
        {
            return faceMode switch
            {
                StoryFactCardFaceMode.Front => false,
                StoryFactCardFaceMode.Back => true,
                _ => definition != null && definition.StartsFlipped
            };
        }

        private void SetOverlayVisible(bool isVisible)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(isVisible);
            }
        }

        private void ValidateReferences()
        {
            WarnIfMissing(overlayRoot, nameof(overlayRoot));
            WarnIfMissing(cardAreaRoot, nameof(cardAreaRoot));
            WarnIfMissing(factCardViewPrefab, nameof(factCardViewPrefab));
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"ConversationFactCardOverlay requires {fieldName} to be assigned via SerializeField.");
            }
        }

        private static async UniTask<string> ResolveLocalizedStringAsync(
            LocalizedString localizedString,
            CancellationToken cancellationToken)
        {
            return await localizedString.GetLocalizedStringAsync().Task.AsUniTask()
                .AttachExternalCancellation(cancellationToken);
        }

        private static async UniTask<string> ResolveTextAsync(
            LocalizedTextReference textReference,
            CancellationToken cancellationToken)
        {
            if (textReference == null || textReference.IsMissing)
            {
                return textReference?.Fallback ?? string.Empty;
            }

            return await ResolveLocalizedStringAsync(textReference.LocalizedString, cancellationToken);
        }

        private static async UniTask AwaitTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            if (tween == null)
            {
                return;
            }

            bool completed = false;
            tween.OnComplete(() => completed = true);
            tween.OnKill(() => completed = true);

            try
            {
                await UniTask.WaitUntil(() => completed, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (tween.IsActive())
                {
                    tween.Kill(false);
                }

                throw;
            }
        }
    }
}
