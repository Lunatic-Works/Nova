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

        public void Alert(string title, string bodyContent,
            UnityAction onClickConfirm = null, UnityAction onClickCancel = null)
        {
            var alertPanel = Instantiate(AlertPanelPrefab, UICanvas);
            alertPanel.transform.SetAsLastSibling();
            var alertPanelController = alertPanel.GetComponent<AlertPanelController>();
            alertPanelController.Init(title, bodyContent, onClickConfirm, onClickCancel);
        }
    }
}