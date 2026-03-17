using System;
using UnityEngine;
using UnityEngine.Localization;

namespace U1W.Game
{
    [CreateAssetMenu(
        fileName = "OperationChapterAsset",
        menuName = "U1W/Game/Operation Chapter Asset")]
    public sealed class OperationChapterAsset : ScriptableObject
    {
        [SerializeField] private OperationCardDefinition[] cards = Array.Empty<OperationCardDefinition>();

        public OperationCardDefinition[] Cards => cards;
    }

    [Serializable]
    public sealed class OperationCardDefinition
    {
        [SerializeField] private string id = "card";
        [SerializeField] private LocalizedTextReference factText = new();
        [SerializeField] private LocalizedTextReference frontInterpretation = new();
        [SerializeField] private LocalizedTextReference backInterpretation = new();
        [SerializeField] private LocalizedTextReference description = new();
        [SerializeField] private bool canFlip = true;
        [SerializeField] private bool canReorder = true;
        [SerializeField] private bool startsFlipped;
        [SerializeField] private bool correctIsFlipped;
        [SerializeField] private int correctTimelineOrder;
        [SerializeField] private string[] tags = Array.Empty<string>();

        public string Id => id;
        public LocalizedTextReference FactText => factText;
        public LocalizedTextReference FrontInterpretation => frontInterpretation;
        public LocalizedTextReference BackInterpretation => backInterpretation;
        public LocalizedTextReference Description => description;
        public bool CanFlip => canFlip;
        public bool CanReorder => canReorder;
        public bool StartsFlipped => startsFlipped;
        public bool CorrectIsFlipped => correctIsFlipped;
        public int CorrectTimelineOrder => correctTimelineOrder;
        public string[] Tags => tags;
    }

    [Serializable]
    public sealed class LocalizedTextReference
    {
        [SerializeField] private LocalizedString localizedString;
        [SerializeField] [TextArea(2, 4)] private string fallback;

        public LocalizedString LocalizedString => localizedString;
        public string Fallback => fallback;
        public bool IsMissing => localizedString == null || localizedString.IsEmpty;
    }
}
