using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Nova.Editor
{
    public class NovaBuildHooks : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Please comment out this if you want to build Lua files
            ToLuaMenu.CopyLuaFilesToRes();
        }
    }
}
