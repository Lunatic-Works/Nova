using System.Collections.Generic;

namespace Nova
{
    [ExportCustomType]
    public class ChoiceList
    {
        private readonly List<ChoiceOccursData.Choice> _choices = new List<ChoiceOccursData.Choice>();
        public IReadOnlyList<ChoiceOccursData.Choice> choices => _choices;

        public void Add(ChoiceOccursData.Choice choice)
        {
            _choices.Add(choice);
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

        public string PopJump()
        {
            string last = jumpingDestination;
            jumpingDestination = null;
            return last;
        }

        public void FallThrough()
        {
            fallThrough = true;
        }

        public bool PopFallThrough()
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

        public void RaiseChoices(ChoiceList choices)
        {
            gameState.RaiseChoices(choices.choices);
        }
    }
}
