using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class OperationCardView : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI factText;
        [SerializeField] private TextMeshProUGUI interpretationText;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Sprite backSprite;

        [Header("Visuals")]
        [SerializeField] private Color frontColor = new(0.231f, 0.243f, 0.349f, 0.98f);
        [SerializeField] private Color backColor = new(0.396f, 0.231f, 0.133f, 0.98f);
        [SerializeField] private Color frontFactTextColor = Color.white;
        [SerializeField] private Color backFactTextColor = Color.white;
        [SerializeField] private Color frontInterpretationTextColor = Color.white;
        [SerializeField] private Color backInterpretationTextColor = Color.white;
        [SerializeField] private float disabledAlpha = 0.72f;
        [SerializeField] private float flipDuration = 0.24f;
        [SerializeField] private Ease flipEase = Ease.OutCubic;

        private IOperationCardInteractionHandler owner;
        private bool canFlip = true;
        private bool canReorder = true;
        private bool suppressNextClick;
        private Tween flipTween;

        public RectTransform RectTransform => rectTransform;

        private void Reset()
        {
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            backgroundImage = GetComponent<Image>();
        }

        private void OnDestroy()
        {
            flipTween?.Kill();
        }

        public void Bind(IOperationCardInteractionHandler interactionHandler)
        {
            owner = interactionHandler;
        }

        public void SetContent(
            string fact,
            string interpretation,
            bool isFlipped,
            bool isFlipEnabled,
            bool isReorderEnabled)
        {
            canFlip = isFlipEnabled;
            canReorder = isReorderEnabled;

            if (factText != null)
            {
                factText.text = fact;
            }

            if (interpretationText != null)
            {
                interpretationText.text = interpretation;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isFlipped ? backColor : frontColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = (canFlip || canReorder) ? 1f : disabledAlpha;
                canvasGroup.interactable = canFlip || canReorder;
            }

            ApplyVisualState(isFlipped);
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
            }
        }

        public Tween AnimateTo(Vector2 position, float duration, Ease ease)
        {
            if (rectTransform == null)
            {
                return null;
            }

            return rectTransform.DOAnchorPos(position, duration).SetEase(ease);
        }

        public void SetDragging(bool isDragging)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.blocksRaycasts = !isDragging;
            canvasGroup.alpha = isDragging ? 0.92f : ((canFlip || canReorder) ? 1f : disabledAlpha);
        }

        public void PlayFlipFeedback(
            string fact,
            string interpretation,
            bool isFlipped,
            bool isFlipEnabled,
            bool isReorderEnabled)
        {
            flipTween?.Kill();

            canFlip = isFlipEnabled;
            canReorder = isReorderEnabled;
            transform.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(transform.DOScaleX(0f, flipDuration * 0.5f).SetEase(flipEase));
            sequence.AppendCallback(() => SetContent(fact, interpretation, isFlipped, isFlipEnabled, isReorderEnabled));
            sequence.Append(transform.DOScaleX(1f, flipDuration * 0.5f).SetEase(flipEase));
            flipTween = sequence;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            if (!canFlip)
            {
                return;
            }

            owner?.HandleCardClicked(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandleCardHoverStarted(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandleCardHoverEnded(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!canReorder)
            {
                return;
            }

            owner?.HandleCardBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!canReorder)
            {
                return;
            }

            suppressNextClick = true;
            owner?.HandleCardDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!canReorder)
            {
                return;
            }

            suppressNextClick = false;
            owner?.HandleCardEndDrag(this, eventData);
        }

        private void ApplyVisualState(bool isFlipped)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isFlipped ? backColor : frontColor;

                Sprite targetSprite = isFlipped ? backSprite : frontSprite;
                if (targetSprite != null)
                {
                    backgroundImage.sprite = targetSprite;
                    backgroundImage.preserveAspect = true;
                }
            }

            if (factText != null)
            {
                factText.color = isFlipped ? backFactTextColor : frontFactTextColor;
            }

            if (interpretationText != null)
            {
                interpretationText.color = isFlipped
                    ? backInterpretationTextColor
                    : frontInterpretationTextColor;
            }
        }
    }
}
