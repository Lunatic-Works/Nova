using System.Reflection;
using LuaInterface;

public static partial class LuaBinder
{
    static partial void Bind_Gen(LuaState L);

    public static void Bind(LuaState L)
    {
        if (typeof(LuaBinder).GetMethod("Bind_Gen", BindingFlags.Static | BindingFlags.NonPublic) == null)
        {
            throw new LuaException("Please generate LuaBinder files first!");
        }

        Bind_Gen(L);
    }
}
