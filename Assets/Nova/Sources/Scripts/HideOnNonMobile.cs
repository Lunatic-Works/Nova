using UnityEngine;

namespace Nova
{
    public class HideOnNonMobile : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(Application.isMobilePlatform);
        }
    }
}
