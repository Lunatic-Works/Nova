using UnityEngine;

namespace Nova
{
    public class TestScriptLoader : MonoBehaviour
    {
        public string scriptPath;

        void Start()
        {
            var scriptLoader = new ScriptLoader();
            scriptLoader.Init(scriptPath);
        }
    }
}