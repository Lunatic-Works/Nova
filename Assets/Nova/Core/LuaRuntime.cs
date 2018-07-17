using LuaInterface;
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
            if (isInited)
            {
                return;
            }

            new LuaResLoader();
            lua = new LuaState();
            lua.Start();
            LuaBinder.Bind(lua);
            lua.AddSearchPath(Application.dataPath + "/Nova/Lua");
            // do default includes
            lua.DoString(@"require 'requires'");

            // get the lua load string function
            lua_loadstring = lua.GetFunction("loadstring");
            if (lua_loadstring == null)
            {
                // loadstring is deprecated after Lua 5.2
                lua_loadstring = lua.GetFunction("load");
            }

            lua_bind_object = lua.GetFunction("__Nova.bind_object");

            isInited = true;
            isDisposed = false;
        }

        private void CheckInit()
        {
            Assert.IsTrue(isInited, "Nova: LuaRuntime methods should be called after Init");
        }

        private LuaState lua;
        private LuaFunction lua_loadstring;
        private LuaFunction lua_bind_object;

        /// <summary>
        /// Make an object visible in lua.
        /// </summary>
        /// <remarks>
        /// The object will be assigned as an entry of the global variable <code>__Nova</code>, with the given name,
        /// which can be accessed from lua scripts by <code>__Nova[name]</code>.
        /// </remarks>
        /// <param name="name">The name to assign</param>
        /// <param name="obj">The object to be assigned</param>
        public void BindObject(string name, object obj)
        {
            CheckInit();
            lua_bind_object.BeginPCall();
            lua_bind_object.Push(name);
            lua_bind_object.Push(obj);
            lua_bind_object.PCall();
            lua_bind_object.EndPCall();
        }

        /// <summary>
        /// Wrap the given code in a closure
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

        private bool isDisposed = true;

        /// <summary>
        /// Dispose the lua runtime environment
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            CheckInit();
            lua_bind_object.Dispose();
            lua_loadstring.Dispose();
            lua.Dispose();

            isDisposed = true;
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