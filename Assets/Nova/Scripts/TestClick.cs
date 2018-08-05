using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    public class TestClick : MonoBehaviour, IPointerClickHandler
    {
        // Use this for initialization
        void Start () {

        }

        // Update is called once per frame
        void Update () {

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Click!");
        }
    }
}