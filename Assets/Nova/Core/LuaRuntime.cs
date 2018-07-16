using System;
using LuaInterface;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// A singleton that offers lua runtime environment
    /// </summary>
    public class LuaRuntime
    {
        private bool isInited = false;

        /// <summary>
        /// Inititialize the lua runtime environment
        /// </summary>
        /// <remarks>
        /// This method should be called before every Nova related work happens. The begining of the Start or Awake
        /// of the game controller might be a good choice
        /// </remarks>
        public void Init()
        {
            new LuaResLoader();
            lua = new LuaState();
            lua.Start();
            LuaBinder.Bind(lua);
            lua.AddSearchPath(Application.dataPath + "/Nova/Lua");
            // do default includes
            lua.DoString(@"require 'defaults'");

            // get the lua load string function
            lua_loadstring = lua.GetFunction("loadstring");
            if (lua_loadstring == null)
            {
                // loadstring is deprecated after Lua 5.2
                lua_loadstring = lua.GetFunction("load");
            }

            isInited = true;
        }

        private void CheckInit()
        {
            Assert.IsTrue(isInited, "Nova: LuaRuntime methods should be called after Init");
        }

        private LuaState lua;
        private LuaFunction lua_loadstring;

        /// <summary>
        /// Make the script loader visible in lua
        /// </summary>
        /// <param name="scriptLoader">the script loader to bind</param>
        public void BindScriptLoader(ScriptLoader scriptLoader)
        {
            CheckInit();
            LuaFunction Lua_BindScriptLoader = lua.GetFunction("bindScriptLoader");
            Lua_BindScriptLoader.BeginPCall();
            Lua_BindScriptLoader.Push(scriptLoader);
            Lua_BindScriptLoader.PCall();
            Lua_BindScriptLoader.EndPCall();
            Lua_BindScriptLoader.Dispose();
        }

        /// <summary>
        /// Wrap the given code to closure
        /// </summary>
        /// <param name="code">
        /// The code that should be wrapped
        /// </param>
        /// <returns>
        /// The wrapped lua function
        /// </returns>
        public LuaFunction WrapClosure(string code)
        {
            CheckInit();
            Debug.Log("<color=blue>" + code + "</color>");
            return lua_loadstring.Invoke<string, LuaFunction>(code);
        }

        public void DoFile(string name)
        {
            CheckInit();
            lua.DoFile(name);
        }

        public void DoString(string code)
        {
            CheckInit();
            lua.DoString(code);
        }

        /// <summary>
        /// Dispose the lua runtime environment
        /// </summary>
        public void Dispose()
        {
            CheckInit();
            lua_loadstring.Dispose();
            lua.Dispose();
        }

        // ------------- Below are the essential codes for singleton pattern --------------- //

        private static readonly LuaRuntime instance = new LuaRuntime();

        static LuaRuntime()
        {
        }

        private LuaRuntime()
        {
        }

        public static LuaRuntime Instance
        {
            get { return instance; }
        }

        // ------------- Above are the essential codes for singleton pattern --------------- //
    }
}