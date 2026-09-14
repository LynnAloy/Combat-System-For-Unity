using UnityEngine;

namespace FlexibleTaskSystem
{
    public enum TaskNavigationTargetKind
    {
        None,
        Definition,
        WorldPosition
    }

    public readonly struct TaskNavigationTarget
    {
        public TaskNavigationTargetKind Kind { get; }
        public ScriptableObject Definition { get; }
        public Vector3 WorldPosition { get; }

        private TaskNavigationTarget(
            TaskNavigationTargetKind kind,
            ScriptableObject definition,
            Vector3 worldPosition)
        {
            Kind = kind;
            Definition = definition;
            WorldPosition = worldPosition;
        }

        public static TaskNavigationTarget FromDefinition(
            ScriptableObject definition)
        {
            return new TaskNavigationTarget(
                TaskNavigationTargetKind.Definition,
                definition,
                default);
        }

        public static TaskNavigationTarget FromWorldPosition(
            Vector3 worldPosition)
        {
            return new TaskNavigationTarget(
                TaskNavigationTargetKind.WorldPosition,
                null,
                worldPosition);
        }
    }

    public interface ITaskNavigationSource
    {
        bool TryGetNavigationTarget(out TaskNavigationTarget target);
    }
}