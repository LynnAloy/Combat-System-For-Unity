namespace GameplayEvents.Tasks
{
    public readonly struct TaskCompletedEvent
    {
        public FlexibleTaskSystem.GameTask Task { get; }
        public FlexibleTaskSystem.AllTasks Runner { get; }

        public TaskCompletedEvent(FlexibleTaskSystem.GameTask task, FlexibleTaskSystem.AllTasks runner)
        {
            Task = task;
            Runner = runner;
        }
    }
}