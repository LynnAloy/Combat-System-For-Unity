using System;
using UnityEngine;

namespace FlexibleTaskSystem
{
    public enum GameTaskState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    [Serializable]
    public abstract class GameTask
    {
        [SerializeField] private string id = Guid.NewGuid().ToString();
        [SerializeField] private string title = "New Task";

        [NonSerialized] private GameTaskState state = GameTaskState.Pending;
        [NonSerialized] private TaskContext context;
        [NonSerialized] private bool hasProgress;
        [NonSerialized] private int currentProgress;
        [NonSerialized] private int requiredProgress;

        public string Id => id;
        public string Title => title;
        public GameTaskState State => state;
        public bool HasProgress => hasProgress;
        public int CurrentProgress => currentProgress;
        public int RequiredProgress => requiredProgress;

        protected TaskContext Context => context;

        public event Action<GameTask> Started;
        public event Action<GameTask> Completed;
        public event Action<GameTask, string> Failed;
        public event Action<GameTask, int, int> ProgressChanged;

        internal void StartTask(TaskContext taskContext)
        {
            if (state != GameTaskState.Pending)
            {
                throw new InvalidOperationException(
                    $"Task '{title}' is {state}, not Pending.");
            }

            context = taskContext ?? throw new ArgumentNullException(nameof(taskContext));
            state = GameTaskState.Running;

            Started?.Invoke(this);

            if (state == GameTaskState.Running)
            {
                OnStart();
            }
        }

        internal void TickTask(float deltaTime)
        {
            if (state == GameTaskState.Running)
            {
                OnTick(deltaTime);
            }
        }

        internal void ResetTask()
        {
            if (state == GameTaskState.Running)
            {
                CancelTask();
            }

            context = null;
            state = GameTaskState.Pending;
            hasProgress = false;
            currentProgress = 0;
            requiredProgress = 0;

            OnReset();
        }

        internal void CancelTask()
        {
            if (state != GameTaskState.Running)
            {
                return;
            }

            state = GameTaskState.Cancelled;
            OnEnd(state);
        }

        protected void SetProgress(int current, int required)
        {
            if (state != GameTaskState.Running)
            {
                return;
            }

            int validatedRequired = Mathf.Max(1, required);
            int validatedCurrent = Mathf.Clamp(current, 0, validatedRequired);

            bool changed = !hasProgress ||
                           currentProgress != validatedCurrent ||
                           requiredProgress != validatedRequired;

            hasProgress = true;
            currentProgress = validatedCurrent;
            requiredProgress = validatedRequired;

            if (changed)
            {
                ProgressChanged?.Invoke(
                    this,
                    currentProgress,
                    requiredProgress);
            }
        }

        protected void Complete()
        {
            if (state != GameTaskState.Running)
            {
                return;
            }

            if (hasProgress && currentProgress < requiredProgress)
            {
                SetProgress(requiredProgress, requiredProgress);
            }

            state = GameTaskState.Completed;
            OnEnd(state);
            Completed?.Invoke(this);
        }

        protected void Fail(string reason)
        {
            if (state != GameTaskState.Running)
            {
                return;
            }

            state = GameTaskState.Failed;
            OnEnd(state);
            Failed?.Invoke(this, reason);
        }

        protected abstract void OnStart();

        protected virtual void OnTick(float deltaTime)
        {
        }

        protected virtual void OnEnd(GameTaskState finalState)
        {
        }

        protected virtual void OnReset()
        {
        }
    }
}