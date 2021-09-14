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
        public DialogueActionStage stage;
        public ExecutionMode mode;
        public bool isRestore;
    }
}