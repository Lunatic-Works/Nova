namespace Nova
{
    public interface IAbstractKeyDevice
    {
        bool GetKey(AbstractKey key);
        void Load(string json);
        string Json();

        // update on every frame
        void Update();
    }
}