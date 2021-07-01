using LuaInterface;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// A singleton that offers lua runtime environment
    /// </summary>
    public class LuaRuntime : MonoBehaviour
    {
        private LuaState lua;
        private LuaFunction luaLoadString;
        private bool inited;

        /// <summary>
        /// Inititialize the lua runtime environment
        /// </summary>
        /// <remarks>
        /// This method should be called before every Nova related work happens. The begining of the Start or Awake
        /// of the GameController might be a good choice
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
            lua.AddSearchPath(Application.dataPath + "/Nova/Lua");

            InitRequires();

            // get the lua load string function
            luaLoadString = lua.GetFunction("loadstring");
            if (luaLoadString == null)
            {
                // loadstring is deprecated after Lua 5.2
                luaLoadString = lua.GetFunction("load");
            }

            inited = true;
        }

        public void InitRequires()
        {
            lua.DoString("require 'requires'");
        }

        private void CheckInit()
        {
            this.RuntimeAssert(inited, "LuaRuntime methods should be called after Init().");
        }

        /// <summary>
        /// Make an object visible in lua.
        /// </summary>
        /// <remarks>
        /// The object will be assigned as an entry of the global variable <code>__Nova</code>, with the given name,
        /// which can be accessed from lua scripts by <code>__Nova[name]</code>.
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
            return luaLoadString.Invoke<string, LuaFunction>(code);
        }

        public void DoFile(string name)
        {
            CheckInit();
            lua.DoFile(name);
        }

        public void DoString(string chunk)
        {
            CheckInit();
            lua.DoString(chunk);
        }

        public T DoString<T>(string chunk)
        {
            CheckInit();
            return lua.DoString<T>(chunk);
        }

        /// <summary>
        /// Dispose the lua runtime environment
        /// This method will be called when the Lua Runtime is destroyed
        /// </summary>
        private void Dispose()
        {
            CheckInit();
            luaLoadString.Dispose();
            lua.Dispose();
        }

        #region Singleton pattern

        static LuaRuntime() { }

        private LuaRuntime() { }

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
                    if (_instance == null)
                    {
                        _instance = (LuaRuntime)FindObjectOfType(typeof(LuaRuntime));

                        if (FindObjectsOfType(typeof(LuaRuntime)).Length > 1)
                        {
                            Debug.LogError("Nova: [Singleton] Something went really wrong --- " +
                                           "there should never be more than 1 singleton! " +
                                           "Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            var singleton = new GameObject();
                            _instance = singleton.AddComponent<LuaRuntime>();
                            _instance.Init();
                            singleton.name = "(singleton) " + typeof(LuaRuntime);

                            DontDestroyOnLoad(singleton);

                            // Debug.LogFormat("Nova: [Singleton] An instance of {0} " +
                            //                 "is needed in the scene, so {1} " +
                            //                 "was created with DontDestroyOnLoad.",
                            //     typeof(LuaRuntime), singleton);
                        }
                        else
                        {
                            // Debug.LogFormat("Nova: [Singleton] Using instance already created: {0}",
                            //     _instance.gameObject.name);
                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool ApplicationIsQuitting = false;

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed,
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
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