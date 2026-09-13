using UnityEngine;

namespace FlexibleTaskSystem
{
    public sealed class TaskHUD : MonoBehaviour
    {
        [SerializeField] private AllTasks taskSequence;
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMPro.TMP_Text taskTitleText;
        [SerializeField] private string titlePrefix = "当前任务：";

        private void OnEnable()
        {
            if (taskSequence == null)
            {
                Debug.LogError("Task sequence is not assigned.", this);
                SetVisible(false);
                return;
            }

            taskSequence.TaskStarted += OnTaskStarted;
            taskSequence.TaskResumed += OnTaskResumed;
            taskSequence.TaskProgressChanged += OnTaskProgressChanged;
            taskSequence.TaskFailed += OnTaskFailed;
            taskSequence.SequenceCompleted += OnSequenceCompleted;

            Refresh();
        }

        private void OnDisable()
        {
            if (taskSequence == null)
            {
                return;
            }

            taskSequence.TaskStarted -= OnTaskStarted;
            taskSequence.TaskResumed -= OnTaskResumed;
            taskSequence.TaskProgressChanged -= OnTaskProgressChanged;
            taskSequence.TaskFailed -= OnTaskFailed;
            taskSequence.SequenceCompleted -= OnSequenceCompleted;
        }

        private void Refresh()
        {
            GameTask current = taskSequence.Current;

            if (current == null || !taskSequence.IsRunning)
            {
                SetVisible(false);
                return;
            }

            ShowTask(current);
        }

        private void OnTaskStarted(int index, GameTask task)
        {
            ShowTask(task);
        }

        private void OnTaskResumed(int index, GameTask task)
        {
            ShowTask(task);
        }

        private void OnTaskProgressChanged(
            int index,
            GameTask task,
            int currentAmount,
            int requiredAmount)
        {
            ShowTask(task);
        }

        private void OnTaskFailed(int index, GameTask task, string reason)
        {
            ShowText($"{titlePrefix}{task.Title}（失败）");
        }

        private void OnSequenceCompleted()
        {
            SetVisible(false);
        }

        private void ShowTask(GameTask task)
        {
            if (task == null)
            {
                SetVisible(false);
                return;
            }

            string progressText = string.Empty;

            if (task.HasProgress)
            {
                progressText = $"（{task.CurrentProgress}/{task.RequiredProgress}）";
            }

            ShowText($"{titlePrefix}{task.Title}{progressText}");
        }

        private void ShowText(string text)
        {
            if (taskTitleText == null)
            {
                Debug.LogError("Task title text is not assigned.", this);
                return;
            }

            taskTitleText.text = text;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (contentRoot != null)
            {
                contentRoot.SetActive(visible);
                return;
            }

            if (taskTitleText != null)
            {
                taskTitleText.gameObject.SetActive(visible);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (contentRoot == gameObject)
            {
                Debug.LogWarning("Content Root should be a child object, not the TaskHUD object itself.", this);
            }
        }
#endif
    }
}