using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Text))]
    public class TitleCopyright : MonoBehaviour
    {
        public TextAsset textAsset;
        public bool autoUpdate = true;

        private void Start()
        {
            if (textAsset == null)
            {
                return;
            }

            string copyright;

#if UNITY_EDITOR
            if (autoUpdate)
            {
                copyright = $"© {DateTime.Now.Year} {Application.companyName}";

                var path = AssetDatabase.GetAssetPath(textAsset);
                File.WriteAllText(path, copyright);
            }
            else
            {
                copyright = textAsset.text;
            }
#else
            copyright = textAsset.text;
#endif

            GetComponent<Text>().text = copyright;
        }
    }
}
