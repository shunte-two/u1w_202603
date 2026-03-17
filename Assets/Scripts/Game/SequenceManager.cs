using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace U1W.Game
{
    public sealed class SequenceManager : MonoBehaviour
    {
        private enum SequencePhase
        {
            None,
            Conversation,
            Operation,
            Completed
        }

        [Header("Managers")]
        [SerializeField] private ConversationPartManager conversationPartManager;
        [SerializeField] private OperationPartManager operationPartManager;

        private CancellationTokenSource sequenceCancellationTokenSource;
        private SequencePhase currentPhase;

        private void Awake()
        {
            ValidateReferences();
            BindListeners();
            ApplyIdleView();
        }

        private void OnValidate()
        {
            ValidateReferences();
        }

        private void Start()
        {
            RestartSequence();
        }

        private void OnDestroy()
        {
            UnbindListeners();
            CancelRunningSequence();
        }

        public void RestartSequence()
        {
            CancelRunningSequence();
            ApplyIdleView();

            sequenceCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            RunSequenceAsync(sequenceCancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid RunSequenceAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                currentPhase = SequencePhase.Conversation;
                await conversationPartManager.PlayOpeningAsync(cancellationToken);

                currentPhase = SequencePhase.Operation;
                await operationPartManager.PlayAsync(cancellationToken);

                currentPhase = SequencePhase.Conversation;
                await conversationPartManager.PlayResultAsync(cancellationToken);

                currentPhase = SequencePhase.Completed;
                conversationPartManager.Hide();
                operationPartManager.ShowCompleted();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ApplyIdleView()
        {
            currentPhase = SequencePhase.None;
            conversationPartManager?.Hide();
            operationPartManager?.Hide();
        }

        private void BindListeners()
        {
            if (operationPartManager == null)
            {
                return;
            }

            operationPartManager.RestartRequested -= RestartSequence;
            operationPartManager.RestartRequested += RestartSequence;
        }

        private void UnbindListeners()
        {
            if (operationPartManager == null)
            {
                return;
            }

            operationPartManager.RestartRequested -= RestartSequence;
        }

        private void CancelRunningSequence()
        {
            if (sequenceCancellationTokenSource == null)
            {
                return;
            }

            sequenceCancellationTokenSource.Cancel();
            sequenceCancellationTokenSource.Dispose();
            sequenceCancellationTokenSource = null;
        }

        private void ValidateReferences()
        {
            WarnIfMissing(conversationPartManager, nameof(conversationPartManager));
            WarnIfMissing(operationPartManager, nameof(operationPartManager));
        }

        private static void WarnIfMissing(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                Debug.LogWarning($"SequenceManager requires {fieldName} to be assigned via SerializeField.");
            }
        }
    }
}
