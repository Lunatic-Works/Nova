namespace Nova
{
    [ExportCustomType]
    public class AdvancedDialogueHelper
    {
        private string overridingText;
        private string jumpingDestination;

        public void Override(string to)
        {
            overridingText = to;
        }

        public string GetOverride()
        {
            string last = overridingText;
            overridingText = null;
            return last;
        }

        public void Jump(string to)
        {
            jumpingDestination = to;
        }

        public string GetJump()
        {
            string last = jumpingDestination;
            jumpingDestination = null;
            return last;
        }
    }
}