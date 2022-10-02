namespace Nova
{
    [ExportCustomType]
    public class CoroutineHelper
    {
        private readonly GameState gameState;

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

        public void StartInterrupt()
        {
            gameState.StartInterrupt();
        }

        public void StopInterrupt()
        {
            gameState.StopInterrupt();
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
