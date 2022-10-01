using UnityEngine;

public static class LuaConst
{
    public static string luaDir = Application.dataPath + "/Nova/Lua"; // Lua逻辑代码目录
    public static string toluaDir = Application.dataPath + "/Nova/Sources/ThirdParty/ToLua/ToLua/Lua"; // tolua Lua文件目录

#if UNITY_STANDALONE
    public static string osDir = "Win";
#elif UNITY_ANDROID
    public static string osDir = "Android";
#elif UNITY_IPHONE
    public static string osDir = "iOS";
#else
    public static string osDir = "";
#endif

    public static string luaResDir = string.Format("{0}/{1}/Lua", Application.persistentDataPath, osDir); // 手机运行时Lua文件下载目录

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public static string zbsDir = "D:/ZeroBraneStudio/lualibs/mobdebug"; // ZeroBrane Studio目录
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    public static string zbsDir = "/Applications/ZeroBraneStudio.app/Contents/ZeroBraneStudio/lualibs/mobdebug";
#else
    public static string zbsDir = luaResDir + "/mobdebug/";
#endif

    public static bool openLuaSocket = false; // 是否打开Lua socket库
    public static bool openLuaDebugger = false; // 是否连接Lua调试器
}
