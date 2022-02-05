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
        private string jumpingDestination;
        private bool fallThrough;
        private GameState gameState;

        public AdvancedDialogueHelper(GameState gameState)
        {
            this.gameState = gameState;
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

        public void FallThrough()
        {
            fallThrough = true;
        }

        public bool GetFallThrough()
        {
            bool last = fallThrough;
            fallThrough = false;
            return last;
        }

        public void Reset()
        {
            jumpingDestination = null;
            fallThrough = false;
        }

        public void RaiseSelection(SelectionList selections)
        {
            gameState.RaiseSelection(selections.selections);
        }
    }
}