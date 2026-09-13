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

        //临时任务
        private GameTask suspendedTask;
        private bool interruptActive;

        public bool IsInterruptActive => interruptActive;
        public event Action<int, GameTask> TaskResumed;

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

            suspendedTask = null;
            interruptActive = false;

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

        //启动临时任务
        public bool TryStartInterrupt(GameTask interruptTask)
        {
            if (!IsRunning ||
                current == null ||
                interruptTask == null ||
                interruptActive)
            {
                return false;
            }

            DetachCurrent();

            suspendedTask = current;
            current = interruptTask;
            interruptActive = true;

            current.ResetTask();
            AttachCurrent();
            current.StartTask(context);

            return true;
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

        //从临时任务恢复到主任务
        private void RestoreInterruptedTask()
        {
            DetachCurrent();

            current = suspendedTask;
            suspendedTask = null;
            interruptActive = false;

            if (current == null)
            {
                RequestAdvance();
                return;
            }

            if (current.State == GameTaskState.Completed)
            {
                TaskCompleted?.Invoke(currentIndex, current);
                RequestAdvance();
                return;
            }

            if (current.State == GameTaskState.Failed)
            {
                TaskFailed?.Invoke(
                    currentIndex,
                    current,
                    "Task failed while interrupted.");

                IsRunning = false;
                IsPaused = false;
                return;
            }

            if (current.State != GameTaskState.Running)
            {
                RequestAdvance();
                return;
            }

            AttachCurrent();
            TaskResumed?.Invoke(currentIndex, current);
        }

        public void StopSequence()
        {
            DetachCurrent();
            current?.CancelTask();

            if (suspendedTask != null && suspendedTask != current)
            {
                suspendedTask.CancelTask();
            }

            current = null;
            suspendedTask = null;
            currentIndex = -1;
            interruptActive = false;
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
            int eventIndex = interruptActive ? -1 : currentIndex;
            TaskStarted?.Invoke(eventIndex, task);
        }

        private void OnCurrentCompleted(GameTask task)
        {
            int eventIndex = interruptActive ? -1 : currentIndex;
            TaskCompleted?.Invoke(eventIndex, task);

            if (interruptActive)
            {
                RestoreInterruptedTask();
                return;
            }

            RequestAdvance();
        }

        private void OnCurrentFailed(GameTask task, string reason)
        {
            int eventIndex = interruptActive ? -1 : currentIndex;
            TaskFailed?.Invoke(eventIndex, task, reason);

            if (interruptActive)
            {
                if (failurePolicy == TaskFailurePolicy.SkipFailedTask)
                {
                    RestoreInterruptedTask();
                    return;
                }

                StopSequence();
                return;
            }

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

        private void OnCurrentProgressChanged(GameTask task, int currentAmount, int requiredAmount)
        {
            int eventIndex = interruptActive ? -1 : currentIndex;

            TaskProgressChanged?.Invoke(eventIndex, task, currentAmount, requiredAmount);
        }
    }
}