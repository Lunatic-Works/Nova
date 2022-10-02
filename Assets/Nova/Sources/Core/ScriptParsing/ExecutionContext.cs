namespace Nova
{
    [ExportCustomType]
    public enum DialogueActionStage
    {
        BeforeCheckpoint,
        Default,
        AfterDialogue
    }

    [ExportCustomType]
    public enum ExecutionMode
    {
        Eager,
        Lazy
    }

    [ExportCustomType]
    public class ExecutionContext
    {
        public readonly ExecutionMode mode;
        public readonly DialogueActionStage stage;
        public readonly bool isRestoring;

        public ExecutionContext(ExecutionMode mode, DialogueActionStage stage, bool isRestoring)
        {
            this.stage = stage;
            this.mode = mode;
            this.isRestoring = isRestoring;
        }
    }
}
