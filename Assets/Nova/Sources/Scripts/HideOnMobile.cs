using UnityEngine;

namespace Nova
{
    public class HideOnMobile : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(!Application.isMobilePlatform);
        }
    }
}
