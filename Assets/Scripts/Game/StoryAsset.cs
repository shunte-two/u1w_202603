using System;
using UnityEngine;
using UnityEngine.Localization;

namespace U1W.Game
{
    [CreateAssetMenu(
        fileName = "StoryAsset",
        menuName = "U1W/Game/Story Asset")]
    public sealed class StoryAsset : ScriptableObject
    {
        [SerializeField] private StoryStep[] steps = Array.Empty<StoryStep>();

        public StoryStep[] Steps => steps;
    }

    public enum StoryStepType
    {
        ShowMessage,
        ChangeExpression,
        Wait
    }

    [Serializable]
    public sealed class StoryStep
    {
        [SerializeField] private StoryStepType stepType = StoryStepType.ShowMessage;
        [SerializeField] private LocalizedString message;
        [SerializeField] [TextArea(2, 4)] private string messageFallback;
        [SerializeField] private bool waitForAdvance = true;
        [SerializeField] private string characterId;
        [SerializeField] private Sprite expressionSprite;
        [SerializeField] private bool setNativeSize;
        [SerializeField] private bool hideWhenSpriteMissing = true;
        [SerializeField] [Min(0f)] private float waitSeconds;

        public StoryStepType StepType => stepType;
        public LocalizedString Message => message;
        public string MessageFallback => messageFallback;
        public bool WaitForAdvance => waitForAdvance;
        public string CharacterId => characterId;
        public Sprite ExpressionSprite => expressionSprite;
        public bool SetNativeSize => setNativeSize;
        public bool HideWhenSpriteMissing => hideWhenSpriteMissing;
        public float WaitSeconds => waitSeconds;
    }
}
