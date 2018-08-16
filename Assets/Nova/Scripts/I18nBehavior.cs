using System.Collections;
using System.Collections.Generic;
using Nova;
using UnityEngine;
using UnityEngine.UI;


namespace Nova
{
    // Attach this to provide text content according to the translation key.
    [RequireComponent(typeof(Text))]
    public class I18nBehavior : MonoBehaviour
    {
        public string InflateTextKey;

        // Use this for initialization
        void Awake()
        {
            GetComponent<Text>().text = I18n.__(InflateTextKey);
        }
    }
}