using UnityEngine;

namespace FlexibleTaskSystem
{
    public sealed class TaskDebugListener : MonoBehaviour
    {
        [SerializeField] private AllTasks taskSequence;

        private void OnEnable()
        {
            if (taskSequence == null)
            {
                return;
            }

            taskSequence.TaskStarted += OnTaskStarted;
            taskSequence.TaskCompleted += OnTaskCompleted;
            taskSequence.TaskFailed += OnTaskFailed;
            taskSequence.TaskProgressChanged += OnTaskProgressChanged;
            taskSequence.SequenceCompleted += OnSequenceCompleted;
        }

        private void OnDisable()
        {
            if (taskSequence == null)
            {
                return;
            }

            taskSequence.TaskStarted -= OnTaskStarted;
            taskSequence.TaskCompleted -= OnTaskCompleted;
            taskSequence.TaskFailed -= OnTaskFailed;
            taskSequence.TaskProgressChanged -= OnTaskProgressChanged;
            taskSequence.SequenceCompleted -= OnSequenceCompleted;
        }

        private void OnTaskStarted(int index, GameTask task)
        {
            Debug.Log($"Task started: index={index}, title={task.Title}, state={task.State}", this);
        }

        private void OnTaskCompleted(int index, GameTask task)
        {
            Debug.Log($"Task completed: index={index}, title={task.Title}", this);
        }

        private void OnTaskFailed(int index, GameTask task, string reason)
        {
            Debug.LogError($"Task failed: index={index}, title={task.Title}, reason={reason}", this);
        }

        private void OnTaskProgressChanged(int index, GameTask task, int currentAmount, int requiredAmount)
        {
            Debug.Log($"Task progress: {task.Title} {currentAmount}/{requiredAmount}", this);
        }

        private void OnSequenceCompleted()
        {
            Debug.Log("Task sequence completed.", this);
        }
    }
}