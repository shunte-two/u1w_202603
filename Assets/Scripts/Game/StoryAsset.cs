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
        ShowMessage = 0,
        ChangeExpression = 1,
        Wait = 2,
        ShowTitleSprite = 3,
        PlayBgm = 4,
        StopBgm = 5,
        PlaySe = 6,
        ShowFactCard = 7,
        StackFactCard = 8,
        ClearFactCards = 9,
        ShowEndSprite = 10
    }

    public enum StoryCharacterId
    {
        None,
        FortuneTeller
    }

    public enum StoryExpressionId
    {
        None,
        Normal,
        Joy,
        Angry,
        Sad,
        Surprised
    }

    public enum StoryFactCardFaceMode
    {
        UseCardDefault,
        Front,
        Back
    }

    [Serializable]
    public sealed class StoryStep
    {
        [SerializeField] private StoryStepType stepType = StoryStepType.ShowMessage;
        [SerializeField] private LocalizedString message;
        [SerializeField] [TextArea(2, 4)] private string messageFallback;
        [SerializeField] private bool waitForAdvance = true;
        [SerializeField] private StoryCharacterId characterId = StoryCharacterId.None;
        [SerializeField] private StoryExpressionId expressionId = StoryExpressionId.Normal;
        [SerializeField] private bool hideCharacterWhenExpressionMissing;
        [SerializeField] [Min(0f)] private float waitSeconds;
        [SerializeField] private bool showConversationWindowDuringWait = true;
        [SerializeField] private Sprite titleSprite;
        [SerializeField] private Sprite endSprite;
        [SerializeField] private OperationChapterAsset factCardChapterAsset;
        [SerializeField] private string factCardId = "card";
        [SerializeField] private StoryFactCardFaceMode factCardFaceMode = StoryFactCardFaceMode.UseCardDefault;
        [SerializeField] private string audioKey;
        [SerializeField] [Range(0f, 1f)] private float audioVolume = 1f;
        [SerializeField] [Min(0f)] private float audioFadeSeconds;
        [SerializeField] private bool loopBgm = true;

        public StoryStepType StepType => stepType;
        public LocalizedString Message => message;
        public string MessageFallback => messageFallback;
        public bool WaitForAdvance => waitForAdvance;
        public StoryCharacterId CharacterId => characterId;
        public StoryExpressionId ExpressionId => expressionId;
        public bool HideCharacterWhenExpressionMissing => hideCharacterWhenExpressionMissing;
        public float WaitSeconds => waitSeconds;
        public bool ShowConversationWindowDuringWait => showConversationWindowDuringWait;
        public Sprite TitleSprite => titleSprite;
        public Sprite EndSprite => endSprite;
        public OperationChapterAsset FactCardChapterAsset => factCardChapterAsset;
        public string FactCardId => factCardId;
        public StoryFactCardFaceMode FactCardFaceMode => factCardFaceMode;
        public string AudioKey => audioKey;
        public float AudioVolume => audioVolume;
        public float AudioFadeSeconds => audioFadeSeconds;
        public bool LoopBgm => loopBgm;
    }
}
