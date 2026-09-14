using UnityEngine;

namespace FlexibleTaskSystem
{
    [DisallowMultipleComponent]
    public sealed class TaskNavigationDirector : MonoBehaviour
    {
        [SerializeField] private AllTasks taskSequence;
        [SerializeField]
        private WorldMapNavigationController navigationController;

        private void OnEnable()
        {
            if (taskSequence == null ||
                navigationController == null)
            {
                Debug.LogError(
                    "Task navigation references are not assigned.",
                    this);

                return;
            }

            taskSequence.TaskStarted += OnTaskStarted;
            taskSequence.TaskResumed += OnTaskResumed;
            taskSequence.TaskFailed += OnTaskFailed;
            taskSequence.SequenceCompleted += OnSequenceCompleted;
        }

        private void Start()
        {
            if (taskSequence != null &&
                taskSequence.IsRunning &&
                taskSequence.Current != null)
            {
                ApplyTaskNavigation(taskSequence.Current);
            }
        }

        private void OnDisable()
        {
            if (taskSequence == null)
            {
                return;
            }

            taskSequence.TaskStarted -= OnTaskStarted;
            taskSequence.TaskResumed -= OnTaskResumed;
            taskSequence.TaskFailed -= OnTaskFailed;
            taskSequence.SequenceCompleted -= OnSequenceCompleted;
        }

        private void OnTaskStarted(int index, GameTask task)
        {
            ApplyTaskNavigation(task);
        }

        private void OnTaskResumed(int index, GameTask task)
        {
            ApplyTaskNavigation(task);
        }

        private void OnTaskFailed(
            int index,
            GameTask task,
            string reason)
        {
            navigationController.ClearDestination();
        }

        private void OnSequenceCompleted()
        {
            navigationController.ClearDestination();
        }

        private void ApplyTaskNavigation(GameTask task)
        {
            ITaskNavigationSource source =
                task as ITaskNavigationSource;

            if (source == null ||
                !source.TryGetNavigationTarget(
                    out TaskNavigationTarget target))
            {
                navigationController.ClearDestination();
                return;
            }

            if (target.Kind ==
                TaskNavigationTargetKind.WorldPosition)
            {
                navigationController.TrySetDestination(
                    target.WorldPosition);

                return;
            }

            Transform origin =
                navigationController.NavigationOrigin;

            if (origin == null)
            {
                Debug.LogError(
                    "Map navigation origin is not assigned.",
                    this);

                return;
            }

            if (!TaskNavigationAnchor.TryResolve(
                    target.Definition,
                    origin.position,
                    out Transform navigationPoint))
            {
                Debug.LogWarning(
                    $"No active navigation anchor found for " +
                    $"'{target.Definition?.name}'.",
                    this);

                navigationController.ClearDestination();
                return;
            }

            navigationController.TrySetDestination(
                navigationPoint.position);
        }
    }
}