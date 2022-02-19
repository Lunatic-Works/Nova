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

        private void Start()
        {
            var textBox = GetComponent<Text>();

#if UNITY_EDITOR
            var copyright = $"© {DateTime.Now.Year} {Application.companyName}";
            textBox.text = copyright;

            var path = AssetDatabase.GetAssetPath(textAsset);
            File.WriteAllText(path, copyright);
#else
            GetComponent<Text>().text = textAsset.text;
#endif
        }
    }
}