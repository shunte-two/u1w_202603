using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace U1W.Game
{
    public sealed class ConversationFactCardView : MonoBehaviour
    {
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

        public RectTransform RectTransform => rectTransform;

        private void Reset()
        {
            rectTransform = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            backgroundImage = GetComponent<Image>();
        }

        public void SetContent(string factValue, string interpretationValue, bool isFlipped)
        {
            if (factText != null)
            {
                factText.text = factValue ?? string.Empty;
            }

            if (interpretationText != null)
            {
                interpretationText.text = interpretationValue ?? string.Empty;
                interpretationText.gameObject.SetActive(!string.IsNullOrWhiteSpace(interpretationValue));
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
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
