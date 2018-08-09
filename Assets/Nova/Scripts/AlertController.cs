using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class AlertController : MonoBehaviour
    {
        public RectTransform UICanvas;

        public GameObject AlertPanelPrefab;

        // Pass title/bodyContent/ignore = null to hide these objects
        public void Alert(string title, string bodyContent,
            UnityAction onClickConfirm = null, UnityAction onClickCancel = null,
            Wrap<bool> ignore = null)
        {
            if (ignore != null && ignore.value)
            {
                if (onClickConfirm != null)
                {
                    onClickConfirm();
                }
                return;
            }

            var alertPanel = Instantiate(AlertPanelPrefab, UICanvas);
            alertPanel.transform.SetAsLastSibling();
            alertPanel.transform.localScale = Vector3.one;

            var alertPanelController = alertPanel.GetComponent<AlertPanelController>();
            alertPanelController.Init(title, bodyContent, onClickConfirm, onClickCancel, ignore);
        }
    }
}