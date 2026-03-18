using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace U1W.Game
{
    public interface IOperationCardInteractionHandler
    {
        void HandleCardClicked(OperationCardView view);
        void HandleCardHoverStarted(OperationCardView view);
        void HandleCardHoverEnded(OperationCardView view);
        void HandleCardBeginDrag(OperationCardView view, PointerEventData eventData);
        void HandleCardDrag(OperationCardView view, PointerEventData eventData);
        void HandleCardEndDrag(OperationCardView view, PointerEventData eventData);
    }

    public sealed class OperationCardInteractionController : IOperationCardInteractionHandler, IDisposable
    {
        private sealed class CardRuntime
        {
            public OperationCardDefinition Definition;
            public OperationCardView View;
            public int InitialOrderIndex;
            public bool IsFlipped;
            public string FactText;
            public string FrontInterpretation;
            public string BackInterpretation;
            public string Description;
        }

        private readonly RectTransform cardAreaRoot;
        private readonly OperationCardView cardViewPrefab;
        private readonly RectTransform cardDescriptionPopupRoot;
        private readonly TextMeshProUGUI cardDescriptionText;
        private readonly float cardSpacing;
        private readonly float cardSlideDuration;
        private readonly Ease cardSlideEase;
        private readonly float popupVerticalOffset;
        private readonly string emptyDescriptionFallback;
        private readonly List<CardRuntime> activeCards = new();
        
        private CardRuntime draggedCard;
        private Tween popupTween;

        public OperationCardInteractionController(
            RectTransform cardAreaRoot,
            OperationCardView cardViewPrefab,
            RectTransform cardDescriptionPopupRoot,
            TextMeshProUGUI cardDescriptionText,
            float cardSpacing,
            float cardSlideDuration,
            Ease cardSlideEase,
            float popupVerticalOffset,
            string emptyDescriptionFallback)
        {
            this.cardAreaRoot = cardAreaRoot;
            this.cardViewPrefab = cardViewPrefab;
            this.cardDescriptionPopupRoot = cardDescriptionPopupRoot;
            this.cardDescriptionText = cardDescriptionText;
            this.cardSpacing = cardSpacing;
            this.cardSlideDuration = cardSlideDuration;
            this.cardSlideEase = cardSlideEase;
            this.popupVerticalOffset = popupVerticalOffset;
            this.emptyDescriptionFallback = emptyDescriptionFallback;
        }

        public bool HasCards => activeCards.Count > 0;

        public void Dispose()
        {
            popupTween?.Kill();
            Clear();
        }

        public async UniTask InitializeAsync(
            OperationChapterAsset operationAsset,
            string frontLabel,
            string backLabel,
            Func<LocalizedTextReference, CancellationToken, UniTask<string>> resolveTextAsync,
            CancellationToken cancellationToken)
        {
            Clear();
            HideCardDescription();

            if (operationAsset == null || operationAsset.Cards == null || operationAsset.Cards.Length == 0)
            {
                return;
            }

            for (int i = 0; i < operationAsset.Cards.Length; i++)
            {
                OperationCardDefinition definition = operationAsset.Cards[i];
                if (definition == null || cardViewPrefab == null || cardAreaRoot == null)
                {
                    continue;
                }

                OperationCardView view = UnityEngine.Object.Instantiate(cardViewPrefab, cardAreaRoot);
                view.Bind(this);

                CardRuntime card = new CardRuntime
                {
                    Definition = definition,
                    View = view,
                    InitialOrderIndex = i,
                    IsFlipped = definition.StartsFlipped,
                    FactText = await resolveTextAsync(definition.FactText, cancellationToken),
                    FrontInterpretation = await resolveTextAsync(definition.FrontInterpretation, cancellationToken),
                    BackInterpretation = await resolveTextAsync(definition.BackInterpretation, cancellationToken),
                    Description = await resolveTextAsync(definition.Description, cancellationToken)
                };

                activeCards.Add(card);
                RefreshCardContent(card);
            }

            SnapCardsToLayout();
        }

        public void Clear()
        {
            draggedCard = null;

            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.View != null)
                {
                    UnityEngine.Object.Destroy(card.View.gameObject);
                }
            }

            activeCards.Clear();
        }

        public void ResetToChapterStartLayout()
        {
            if (activeCards.Count == 0)
            {
                return;
            }

            HideCardDescription();
            draggedCard = null;
            activeCards.Sort(CompareByInitialOrder);

            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.Definition == null)
                {
                    continue;
                }

                card.IsFlipped = card.Definition.StartsFlipped;
                card.View?.SetDragging(false);
                RefreshCardContent(card);
            }

            AnimateCardsToLayout(null);
        }

        public string ResolveJudgementId(string successJudgementId, string defaultJudgementId)
        {
            if (activeCards.Count == 0)
            {
                return defaultJudgementId;
            }

            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card == null || card.Definition == null)
                {
                    continue;
                }

                if (card.IsFlipped != card.Definition.CorrectIsFlipped)
                {
                    return defaultJudgementId;
                }

                if (card.Definition.CorrectTimelineOrder != i)
                {
                    return defaultJudgementId;
                }
            }

            return successJudgementId;
        }

        public void HandleCardClicked(OperationCardView view)
        {
            CardRuntime card = FindCard(view);
            if (card == null || draggedCard != null || !card.Definition.CanFlip)
            {
                return;
            }

            card.IsFlipped = !card.IsFlipped;
            RefreshCardContent(card);
            card.View.PlayFlipFeedback();

            if (cardDescriptionPopupRoot != null && cardDescriptionPopupRoot.gameObject.activeSelf)
            {
                ShowCardDescription(card);
            }
        }

        public void HandleCardHoverStarted(OperationCardView view)
        {
            CardRuntime card = FindCard(view);
            if (card == null || draggedCard == card)
            {
                return;
            }

            ShowCardDescription(card);
        }

        public void HandleCardHoverEnded(OperationCardView view)
        {
            CardRuntime card = FindCard(view);
            if (card == null || draggedCard == card)
            {
                return;
            }

            HideCardDescription();
        }

        public void HandleCardBeginDrag(OperationCardView view, PointerEventData eventData)
        {
            CardRuntime card = FindCard(view);
            if (card == null || !card.Definition.CanReorder)
            {
                return;
            }

            draggedCard = card;
            draggedCard.View.SetDragging(true);
            draggedCard.View.transform.SetAsLastSibling();
            HideCardDescription();
            UpdateDraggedCardPosition(eventData);
        }

        public void HandleCardDrag(OperationCardView view, PointerEventData eventData)
        {
            CardRuntime card = FindCard(view);
            if (card == null || draggedCard != card)
            {
                return;
            }

            UpdateDraggedCardPosition(eventData);

            int currentIndex = activeCards.IndexOf(card);
            int targetMovableSlotIndex = GetClosestReorderableSlotIndex(card.View.RectTransform.anchoredPosition.x);
            if (targetMovableSlotIndex < 0)
            {
                return;
            }

            List<int> reorderableIndices = GetReorderableIndices();
            int currentMovableSlotIndex = reorderableIndices.IndexOf(currentIndex);
            if (currentMovableSlotIndex < 0 || currentMovableSlotIndex == targetMovableSlotIndex)
            {
                return;
            }

            ApplyReorderableCardOrder(reorderableIndices, currentMovableSlotIndex, targetMovableSlotIndex);
            AnimateCardsToLayout(card);
        }

        public void HandleCardEndDrag(OperationCardView view, PointerEventData eventData)
        {
            CardRuntime card = FindCard(view);
            if (card == null || draggedCard != card)
            {
                return;
            }

            draggedCard = null;
            card.View.SetDragging(false);
            AnimateCardsToLayout(null);
        }

        public void HideCardDescription()
        {
            popupTween?.Kill();
            if (cardDescriptionPopupRoot != null)
            {
                cardDescriptionPopupRoot.gameObject.SetActive(false);
            }
        }

        private void RefreshCardContent(CardRuntime card)
        {
            if (card?.View == null)
            {
                return;
            }

            string interpretation = card.IsFlipped ? card.BackInterpretation : card.FrontInterpretation;
            card.View.SetContent(
                card.FactText,
                interpretation,
                card.IsFlipped,
                card.Definition.CanFlip,
                card.Definition.CanReorder);
        }

        private CardRuntime FindCard(OperationCardView view)
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.View == view)
                {
                    return card;
                }
            }

            return null;
        }

        private void SnapCardsToLayout()
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.View == null)
                {
                    continue;
                }

                card.View.SetAnchoredPosition(GetCardPosition(i));
            }
        }

        private void AnimateCardsToLayout(CardRuntime skippedCard)
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.View == null || card == skippedCard)
                {
                    continue;
                }

                card.View.AnimateTo(GetCardPosition(i), cardSlideDuration, cardSlideEase);
            }

            if (skippedCard == null)
            {
                for (int i = 0; i < activeCards.Count; i++)
                {
                    CardRuntime card = activeCards[i];
                    if (card?.View == null)
                    {
                        continue;
                    }

                    card.View.AnimateTo(GetCardPosition(i), cardSlideDuration, cardSlideEase);
                }
            }
        }

        private Vector2 GetCardPosition(int index)
        {
            if (cardViewPrefab == null || cardViewPrefab.RectTransform == null)
            {
                return Vector2.zero;
            }

            float cardWidth = cardViewPrefab.RectTransform.sizeDelta.x;
            float stride = cardWidth + cardSpacing;
            float totalWidth = cardWidth + stride * Mathf.Max(0, activeCards.Count - 1);
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;
            return new Vector2(startX + stride * index, 0f);
        }

        private int GetClosestReorderableSlotIndex(float anchoredX)
        {
            List<int> reorderableIndices = GetReorderableIndices();
            if (reorderableIndices.Count <= 1)
            {
                return reorderableIndices.Count - 1;
            }

            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < reorderableIndices.Count; i++)
            {
                float distance = Mathf.Abs(anchoredX - GetCardPosition(reorderableIndices[i]).x);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private List<int> GetReorderableIndices()
        {
            List<int> reorderableIndices = new();

            for (int i = 0; i < activeCards.Count; i++)
            {
                CardRuntime card = activeCards[i];
                if (card?.Definition != null && card.Definition.CanReorder)
                {
                    reorderableIndices.Add(i);
                }
            }

            return reorderableIndices;
        }

        private void ApplyReorderableCardOrder(
            List<int> reorderableIndices,
            int currentMovableSlotIndex,
            int targetMovableSlotIndex)
        {
            if (reorderableIndices == null || reorderableIndices.Count <= 1)
            {
                return;
            }

            List<CardRuntime> reorderableCards = new(reorderableIndices.Count);
            for (int i = 0; i < reorderableIndices.Count; i++)
            {
                reorderableCards.Add(activeCards[reorderableIndices[i]]);
            }

            CardRuntime draggedReorderableCard = reorderableCards[currentMovableSlotIndex];
            reorderableCards.RemoveAt(currentMovableSlotIndex);
            reorderableCards.Insert(targetMovableSlotIndex, draggedReorderableCard);

            for (int i = 0; i < reorderableIndices.Count; i++)
            {
                activeCards[reorderableIndices[i]] = reorderableCards[i];
            }
        }

        private void UpdateDraggedCardPosition(PointerEventData eventData)
        {
            if (draggedCard?.View?.RectTransform == null || cardAreaRoot == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cardAreaRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            draggedCard.View.SetAnchoredPosition(new Vector2(localPoint.x, 0f));
        }

        private void ShowCardDescription(CardRuntime card)
        {
            if (cardDescriptionPopupRoot == null || cardDescriptionText == null || card?.View == null)
            {
                return;
            }

            cardDescriptionText.text = string.IsNullOrWhiteSpace(card.Description)
                ? emptyDescriptionFallback
                : card.Description;
            cardDescriptionPopupRoot.anchoredPosition =
                new Vector2(card.View.RectTransform.anchoredPosition.x, popupVerticalOffset);

            if (!cardDescriptionPopupRoot.gameObject.activeSelf)
            {
                cardDescriptionPopupRoot.gameObject.SetActive(true);
            }

            popupTween?.Kill();
            cardDescriptionPopupRoot.localScale = Vector3.one * 0.96f;
            popupTween = cardDescriptionPopupRoot.DOScale(1f, 0.12f).SetEase(Ease.OutBack);
        }

        private static int CompareByInitialOrder(CardRuntime left, CardRuntime right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            if (ReferenceEquals(left.Definition, right.Definition))
            {
                return 0;
            }

            if (left.Definition == null)
            {
                return -1;
            }

            if (right.Definition == null)
            {
                return 1;
            }

            return left.InitialOrderIndex.CompareTo(right.InitialOrderIndex);
        }
    }
}
