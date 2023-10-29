namespace Nova
{
    public interface IViewController : IPanelController
    {
        ViewManager viewManager { get; }
    }
}
