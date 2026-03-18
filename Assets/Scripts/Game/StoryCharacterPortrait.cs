using System;
using UnityEngine;

namespace U1W.Game
{
    public sealed class StoryCharacterPortrait : MonoBehaviour
    {
        [Serializable]
        private sealed class ExpressionDefinition
        {
            [SerializeField] private StoryExpressionId expressionId = StoryExpressionId.Normal;
            [SerializeField] private string animatorStateName;

            public StoryExpressionId ExpressionId => expressionId;
            public string AnimatorStateName => animatorStateName;
        }

        [Header("Identity")]
        [SerializeField] private StoryCharacterId characterId = StoryCharacterId.None;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        [Header("Animation")]
        [SerializeField] private StoryExpressionId defaultExpressionId = StoryExpressionId.Normal;
        [SerializeField] private ExpressionDefinition[] expressions = Array.Empty<ExpressionDefinition>();
        [SerializeField] private bool playDefaultOnEnable = true;
        [SerializeField] private bool hideWhenExpressionMissing;

        public StoryCharacterId CharacterId => characterId;

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            if (playDefaultOnEnable)
            {
                ApplyExpression(defaultExpressionId, hideWhenExpressionMissing);
            }
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        public bool ApplyExpression(StoryExpressionId expressionId, bool hideIfMissing)
        {
            if (expressionId == StoryExpressionId.None)
            {
                if (hideIfMissing)
                {
                    SetVisible(false);
                    return true;
                }

                return false;
            }

            ExpressionDefinition definition = FindExpression(expressionId);
            if (definition == null || string.IsNullOrWhiteSpace(definition.AnimatorStateName))
            {
                if (hideIfMissing)
                {
                    SetVisible(false);
                    return true;
                }

                return false;
            }

            SetVisible(true);
            if (animator != null)
            {
                animator.Play(definition.AnimatorStateName, 0, 0f);
            }

            return true;
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private ExpressionDefinition FindExpression(StoryExpressionId expressionId)
        {
            if (expressions == null)
            {
                return null;
            }

            for (int i = 0; i < expressions.Length; i++)
            {
                ExpressionDefinition definition = expressions[i];
                if (definition == null)
                {
                    continue;
                }

                if (definition.ExpressionId == expressionId)
                {
                    return definition;
                }
            }

            return null;
        }

        private void SetVisible(bool isVisible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible;
            }
        }

        private void ValidateReferences()
        {
            WarnIfMissing(spriteRenderer, nameof(spriteRenderer));
            WarnIfMissing(animator, nameof(animator));
        }

        private void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"StoryCharacterPortrait on {name} requires {fieldName} to be assigned via SerializeField.",
                    this);
            }
        }
    }
}
