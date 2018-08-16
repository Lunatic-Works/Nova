using UnityEngine;

namespace Nova.Examples.Colorless.Scripts
{
    public class ContinueButtonController : MonoBehaviour
    {
        public SaveViewController SaveView;

        public void OnClick()
        {
            SaveView.ShowLoad();
        }
    }
}