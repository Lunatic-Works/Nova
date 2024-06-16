using System.Collections.Generic;
using System.Linq;
using LuaInterface;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    // A Lua runtime that can be used whether the editor is in play mode or not
    public class EditorLuaRuntime
    {
        private LuaState lua;

        public void Init()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                InitLua();
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DisposeLua();
        }

        private void InitLua()
        {
            if (lua == null)
            {
                lua = new LuaState();
                lua.Start();
                lua.AddSearchPath(Application.dataPath + "/Nova/Lua");
            }
        }

        private void DisposeLua()
        {
            if (lua != null)
            {
                lua.Dispose();
                lua = null;
            }
        }

        public void Reload()
        {
            if (lua != null)
            {
                lua.Require("pose");
            }
        }

        private LuaFunction GetFunction(string name)
        {
            if (lua == null)
            {
                return LuaRuntime.Instance.GetFunction(name);
            }
            else
            {
                return lua.GetFunction(name);
            }
        }

        public List<string> GetAllPosesByName(string characterName)
        {
            return GetFunction("get_all_poses_by_name")
                .Invoke<string, LuaTable>(characterName).ToArrayTable().Cast<string>().ToList();
        }

        public string GetPoseByName(string characterName, string poseName)
        {
            return GetFunction("get_pose_by_name").Invoke<string, string, string>(characterName, poseName);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                InitLua();
            }
            else if (state == PlayModeStateChange.ExitingEditMode)
            {
                DisposeLua();
            }
        }
    }
}
