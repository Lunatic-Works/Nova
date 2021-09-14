namespace Nova
{
    [ExportCustomType]
    public class CoroutineHelper
    {
        private GameState gameState;
        public object fence;

        public CoroutineHelper(GameState gameState)
        {
            this.gameState = gameState;
        }

        public void AcquireGameStateLock()
        {
            gameState.ActionAcquirePause();
        }

        public void ReleaseGameStateLock()
        {
            gameState.ActionReleasePause();
        }

        public void Reset()
        {
            fence = null;
        }

        public object Take()
        {
            var v = fence;
            fence = null;
            return v;
        }
    }
}