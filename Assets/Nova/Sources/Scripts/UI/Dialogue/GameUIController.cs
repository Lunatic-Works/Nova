using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class GameUIController : PanelController
    {
        protected override void OnHideComplete()
        {
            NovaAnimation.StopAll(AnimationType.Text);
        }
    }
}
