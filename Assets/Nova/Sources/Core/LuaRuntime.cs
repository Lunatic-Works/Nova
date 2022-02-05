using LuaInterface;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// A singleton that offers the Lua runtime
    /// </summary>
    public class LuaRuntime : MonoBehaviour
    {
        private LuaState lua;
        private readonly Dictionary<string, LuaFunction> cachedLuaFunctions = new Dictionary<string, LuaFunction>();

        private bool inited;

        /// <summary>
        /// Inititialize the Lua runtime environment
        /// </summary>
        /// <remarks>
        /// This method should be called before any Nova-related code runs. The beginning of Start or Awake
        /// of GameController might be a good choice.
        /// </remarks>
        private void Init()
        {
            if (inited)
            {
                return;
            }

            new LuaResLoader();
            DelegateFactory.Init();
            lua = new LuaState();
            lua.Start();
            LuaBinder.Bind(lua);

            // Enable coroutine
            var looper = gameObject.AddComponent<LuaLooper>();
            looper.luaState = lua;

            lua.AddSearchPath(Application.dataPath + "/Nova/Lua");
            Reset();

            inited = true;
        }

        public void Reset()
        {
            foreach (var func in cachedLuaFunctions.Values)
            {
                func.Dispose();
            }

            cachedLuaFunctions.Clear();

            lua.Require("requires");
        }

        private void CheckInit()
        {
            this.RuntimeAssert(inited, "LuaRuntime methods should be called after Init.");
        }

        /// <summary>
        /// Make an object visible in Lua.
        /// </summary>
        /// <remarks>
        /// The object will be assigned as an entry of the global variable __Nova or another namespace,
        /// and can be accessed from Lua scripts by __Nova[name].
        /// </remarks>
        /// <param name="name">The name to assign</param>
        /// <param name="obj">The object to be assigned</param>
        /// <param name="tableName">The namespace to be assigned</param>
        public void BindObject(string name, object obj, string tableName = "__Nova")
        {
            CheckInit();
            var table = lua.GetTable(tableName);
            table[name] = obj;
            table.Dispose();
        }

        public void UpdateExecutionContext(ExecutionContext executionContext)
        {
            BindObject("executionContext", executionContext);
        }

        /// <summary>
        /// Wrap the given code in a closure
        /// </summary>
        /// <param name="code">
        /// The code that should be wrapped
        /// </param>
        /// <returns>
        /// The wrapped Lua function
        /// </returns>
        public LuaFunction WrapClosure(string code)
        {
            CheckInit();
            // loadstring is deprecated after Lua 5.2
            return GetFunction("loadstring").Invoke<string, LuaFunction>(code);
        }

        public void DoString(string chunk)
        {
            CheckInit();
            lua.DoString(chunk);
        }

        // The LuaTable will not be cached, and the user needs to dispose it
        public LuaTable GetTable(string name)
        {
            CheckInit();
            return lua.GetTable(name);
        }

        // The LuaFunction will be cached, and will be disposed in Dispose
        public LuaFunction GetFunction(string name)
        {
            CheckInit();
            if (!cachedLuaFunctions.TryGetValue(name, out var func))
            {
                func = lua.GetFunction(name);
                if (func != null)
                {
                    cachedLuaFunctions[name] = func;
                }
            }

            return func;
        }

        /// <summary>
        /// Dispose the Lua runtime environment
        /// This method will be called when LuaRuntime is destroyed
        /// </summary>
        private void Dispose()
        {
            CheckInit();
            foreach (var func in cachedLuaFunctions.Values)
            {
                func.Dispose();
            }

            lua.Dispose();
        }

        #region Singleton pattern

        // Codes from http://wiki.unity3d.com/index.php/Singleton
        private static LuaRuntime _instance;

        private static readonly object Lock = new object();

        public static LuaRuntime Instance
        {
            get
            {
                if (ApplicationIsQuitting)
                {
                    Debug.LogWarningFormat("Nova: [Singleton] Instance {0} " +
                                           "already destroyed on application quit. " +
                                           "Won't create again, return null.",
                        typeof(LuaRuntime));
                    return null;
                }

                lock (Lock)
                {
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    var instances = FindObjectsOfType<LuaRuntime>();
                    if (instances.Length == 0)
                    {
                        var singleton = new GameObject();
                        singleton.name = $"[Singleton] {typeof(LuaRuntime)}";
                        DontDestroyOnLoad(singleton);
                        _instance = singleton.AddComponent<LuaRuntime>();
                        _instance.Init();
                        // Debug.Log($"Nova: [Singleton] An instance of {typeof(LuaRuntime)} is needed in the scene, " +
                        //     $"so {singleton} is created with DontDestroyOnLoad.");
                    }
                    else if (instances.Length == 1)
                    {
                        // Debug.Log($"Nova: [Singleton] Using instance already created: {_instance}");
                        _instance = instances[0];
                    }
                    else
                    {
                        Debug.LogError("Nova: [Singleton] There should never be more than one instance of " +
                            $"{typeof(LuaRuntime)}! Reopening the scene might fix it.");
                        _instance = instances[0];
                    }

                    return _instance;
                }
            }
        }

        private static bool ApplicationIsQuitting = false;

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a singleton is only destroyed when the application quits.
        /// If any script calls Instance after it has been destroyed,
        /// it will create a buggy ghost object that will stay in the editor scene
        /// even after stopping playing the application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        private void OnDestroy()
        {
            ApplicationIsQuitting = true;
            Dispose();
        }

        #endregion
    }
}