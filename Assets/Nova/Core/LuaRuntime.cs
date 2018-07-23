using LuaInterface;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// A singleton that offers lua runtime environment
    /// </summary>
    public class LuaRuntime : MonoBehaviour
    {
        private bool isInited = false;

        /// <summary>
        /// Inititialize the lua runtime environment
        /// </summary>
        /// <remarks>
        /// This method should be called before every Nova related work happens. The begining of the Start or Awake
        /// of the game controller might be a good choice
        /// </remarks>
        private void Init()
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

            isInited = true;
        }

        private void CheckInit()
        {
            Assert.IsTrue(isInited, "Nova: LuaRuntime methods should be called after Init");
        }

        private LuaState lua;
        private LuaFunction lua_loadstring;

        /// <summary>
        /// Make an object visible in lua.
        /// </summary>
        /// <remarks>
        /// The object will be assigned as an entry of the global variable <code>__Nova</code>, with the given name,
        /// which can be accessed from lua scripts by <code>__Nova[name]</code>.
        /// </remarks>
        /// <param name="name">The name to assign</param>
        /// <param name="obj">The object to be assigned</param>
        public void BindObject(string name, object obj, string tableName = "__Nova")
        {
            CheckInit();
            var table = lua.GetTable(tableName);
            table[name] = obj;
            table.Dispose();
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

        /// <summary>
        /// Dispose the lua runtime environment
        /// This method will be called when the Lua Runtime is destroyed
        /// </summary>
        private void Dispose()
        {
            CheckInit();
            lua_loadstring.Dispose();
            lua.Dispose();
        }

        #region Singleton Pattern

        static LuaRuntime()
        {
        }

        private LuaRuntime()
        {
        }

        // Codes from http://wiki.unity3d.com/index.php/Singleton
        private static LuaRuntime _instance;

        private static object _lock = new object();

        public static LuaRuntime Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(LuaRuntime) +
                                     "' already destroyed on application quit." +
                                     " Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (LuaRuntime) FindObjectOfType(typeof(LuaRuntime));

                        if (FindObjectsOfType(typeof(LuaRuntime)).Length > 1)
                        {
                            Debug.LogError("[Singleton] Something went really wrong " +
                                           " - there should never be more than 1 singleton!" +
                                           " Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<LuaRuntime>();
                            _instance.Init();
                            singleton.name = "(singleton) " + typeof(LuaRuntime).ToString();

                            DontDestroyOnLoad(singleton);

                            Debug.Log("[Singleton] An instance of " + typeof(LuaRuntime) +
                                      " is needed in the scene, so '" + singleton +
                                      "' was created with DontDestroyOnLoad.");
                        }
                        else
                        {
                            Debug.Log("[Singleton] Using instance already created: " +
                                      _instance.gameObject.name);
                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool applicationIsQuitting = false;

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public void OnDestroy()
        {
            applicationIsQuitting = true;
            Dispose();
        }

        #endregion
    }
}