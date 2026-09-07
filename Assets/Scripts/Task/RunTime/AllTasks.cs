using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlexibleTaskSystem
{
    public enum TaskFailurePolicy
    {
        StopSequence,
        SkipFailedTask
    }

    public sealed class AllTasks : MonoBehaviour
    {
        [SerializeReference] private List<GameTask> tasks = new();
        [SerializeField] private bool playOnStart = true;
        [SerializeField]private TaskFailurePolicy failurePolicy = TaskFailurePolicy.StopSequence;

        private TaskContext context;
        private GameTask current;
        private int currentIndex = -1;
        private bool advanceRequested;
        private bool isAdvancing;

        public IReadOnlyList<GameTask> Tasks => tasks;
        public GameTask Current => current;
        public int CurrentIndex => currentIndex;
        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public TaskContext Context => context;

        public event Action<int, GameTask> TaskStarted;
        public event Action<int, GameTask> TaskCompleted;
        public event Action<int, GameTask, string> TaskFailed;
        public event Action<int, GameTask, int, int> TaskProgressChanged;
        public event Action SequenceCompleted;

        private void Awake()
        {
            EnsureContext();
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartSequence();
            }
        }

        private void Update()
        {
            if (IsRunning && !IsPaused)
            {
                current?.TickTask(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            StopSequence();
        }

        public void StartSequence()
        {
            if (IsRunning)
            {
                return;
            }

            EnsureContext();
            DetachCurrent();

            foreach (GameTask task in tasks)
            {
                task?.ResetTask();
            }

            context.Reset();

            currentIndex = -1;
            current = null;
            advanceRequested = false;
            IsPaused = false;
            IsRunning = true;

            RequestAdvance();
        }

        public void Pause()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsRunning)
            {
                IsPaused = false;
            }
        }

        public void StopSequence()
        {
            DetachCurrent();
            current?.CancelTask();

            current = null;
            currentIndex = -1;
            IsRunning = false;
            IsPaused = false;
            advanceRequested = false;

            context?.Reset();
        }

        public void SkipCurrentTask()
        {
            if (!IsRunning || current == null)
            {
                return;
            }

            DetachCurrent();
            current.CancelTask();
            RequestAdvance();
        }

        public void RetryCurrentTask()
        {
            if (IsRunning ||
                current == null ||
                current.State != GameTaskState.Failed)
            {
                return;
            }

            current.ResetTask();
            AttachCurrent();

            IsRunning = true;
            IsPaused = false;
            advanceRequested = false;

            current.StartTask(context);
        }

        public void SendSignal(string signal, object payload = null)
        {
            EnsureContext();
            context.Signals.Publish(signal, payload);
        }

        public T FindTask<T>() where T : GameTask
        {
            foreach (GameTask task in tasks)
            {
                if (task is T result)
                {
                    return result;
                }
            }

            return null;
        }

        private void EnsureContext()
        {
            context ??= new TaskContext(this);
        }

        private void RequestAdvance()
        {
            advanceRequested = true;
            AdvanceWhileRequested();
        }

        private void AdvanceWhileRequested()
        {
            if (isAdvancing)
            {
                return;
            }

            isAdvancing = true;

            try
            {
                while (IsRunning && advanceRequested)
                {
                    advanceRequested = false;
                    DetachCurrent();
                    currentIndex++;

                    while (currentIndex < tasks.Count &&
                           tasks[currentIndex] == null)
                    {
                        currentIndex++;
                    }

                    if (currentIndex >= tasks.Count)
                    {
                        current = null;
                        IsRunning = false;
                        IsPaused = false;
                        SequenceCompleted?.Invoke();
                        break;
                    }

                    current = tasks[currentIndex];
                    AttachCurrent();
                    current.StartTask(context);
                }
            }
            finally
            {
                isAdvancing = false;
            }
        }

        private void AttachCurrent()
        {
            if (current == null)
            {
                return;
            }

            current.Started += OnCurrentStarted;
            current.Completed += OnCurrentCompleted;
            current.Failed += OnCurrentFailed;
            current.ProgressChanged += OnCurrentProgressChanged;
        }

        private void DetachCurrent()
        {
            if (current == null)
            {
                return;
            }

            current.Started -= OnCurrentStarted;
            current.Completed -= OnCurrentCompleted;
            current.Failed -= OnCurrentFailed;
            current.ProgressChanged -= OnCurrentProgressChanged;
        }

        private void OnCurrentStarted(GameTask task)
        {
            TaskStarted?.Invoke(currentIndex, task);
        }

        private void OnCurrentCompleted(GameTask task)
        {
            TaskCompleted?.Invoke(currentIndex, task);
            RequestAdvance();
        }

        private void OnCurrentFailed(GameTask task, string reason)
        {
            TaskFailed?.Invoke(currentIndex, task, reason);

            if (failurePolicy == TaskFailurePolicy.SkipFailedTask)
            {
                RequestAdvance();
                return;
            }

            DetachCurrent();
            IsRunning = false;
            IsPaused = false;
            advanceRequested = false;
        }

        private void OnCurrentProgressChanged(
            GameTask task,
            int currentAmount,
            int requiredAmount)
        {
            TaskProgressChanged?.Invoke(
                currentIndex,
                task,
                currentAmount,
                requiredAmount);
        }
    }
}