using System.Collections.Generic;

namespace Nova
{
    [ExportCustomType]
    public class SelectionList
    {
        private readonly List<SelectionOccursData.Selection> _selections = new List<SelectionOccursData.Selection>();
        public IReadOnlyList<SelectionOccursData.Selection> selections => _selections;

        public void Add(SelectionOccursData.Selection selection)
        {
            _selections.Add(selection);
        }
    }

    [ExportCustomType]
    public class AdvancedDialogueHelper
    {
        private string jumpingDestination;
        private bool fallThrough;
        private readonly GameState gameState;

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

        public void RaiseSelections(SelectionList selections)
        {
            gameState.RaiseSelections(selections.selections);
        }
    }
}
