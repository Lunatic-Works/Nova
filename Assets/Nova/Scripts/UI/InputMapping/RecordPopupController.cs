namespace Nova
{
    public class RecordPopupController : ViewControllerBase
    {
        public RecordPopupLabel label;

        public InputMappingListEntry entry
        {
            set => label.entry = value;
        }
    }
}