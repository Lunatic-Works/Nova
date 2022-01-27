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

        public void AcquireActionPause()
        {
            gameState.AcquireActionPause();
        }

        public void ReleaseActionPause()
        {
            gameState.ReleaseActionPause();
        }

        public void SaveInterrupt()
        {
            gameState.SaveInterrupt();
        }

        public void SignalFence(object value)
        {
            fence = value;
        }

        public object TakeFence()
        {
            var v = fence;
            fence = null;
            return v;
        }

        public void Reset()
        {
            fence = null;
        }
    }
}