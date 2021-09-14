using System.Collections.Generic;

namespace Nova
{
    [ExportCustomType]
    public class SelectionList
    {
        public readonly List<SelectionOccursData.Selection> selections = new List<SelectionOccursData.Selection>();

        public void Add(SelectionOccursData.Selection selection)
        {
            selections.Add(selection);
        }
    }

    [ExportCustomType]
    public class AdvancedDialogueHelper
    {
        private string overridingText;
        private string jumpingDestination;
        private bool fallThrough;
        private GameState gameState;

        public AdvancedDialogueHelper(GameState gameState)
        {
            this.gameState = gameState;
        }

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

        public void FallThrough()
        {
            fallThrough = true;
        }

        public bool GetFallThrough()
        {
            var ft = fallThrough;
            fallThrough = false;
            return ft;
        }

        public string GetJump()
        {
            string last = jumpingDestination;
            jumpingDestination = null;
            return last;
        }

        public void Reset()
        {
            overridingText = null;
            jumpingDestination = null;
            fallThrough = false;
        }

        public void RaiseSelection(SelectionList selections)
        {
            gameState.RaiseSelection(selections.selections);
        }
    }
}