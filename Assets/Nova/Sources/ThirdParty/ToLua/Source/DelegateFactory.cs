using System;
using System.Collections.Generic;
using System.Reflection;
using LuaInterface;

public partial class DelegateFactory
{
    public delegate Delegate DelegateCreate(LuaFunction func, LuaTable self, bool flag);

    // The following variables are used by the generated binding code
    private static readonly Dictionary<Type, DelegateCreate> dict = new Dictionary<Type, DelegateCreate>();
    private static readonly DelegateFactory factory = new DelegateFactory();

    static partial void Register();

    public static void Init()
    {
        if (typeof(DelegateFactory).GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic) == null)
        {
            throw new LuaException("Please generate DelegateFactory.Gen.cs first!");
        }

        Register();
    }

    public static Delegate CreateDelegate(Type t, LuaFunction func = null)
    {
        if (!dict.TryGetValue(t, out var create))
        {
            throw new LuaException($"Delegate {LuaMisc.GetTypeName(t)} not registered");
        }

        if (func != null)
        {
            LuaState state = func.GetLuaState();
            LuaDelegate target = state.GetLuaDelegate(func);

            if (target != null)
            {
                return Delegate.CreateDelegate(t, target, target.method);
            }
            else
            {
                Delegate d = create(func, null, false);
                target = d.Target as LuaDelegate;
                state.AddLuaDelegate(target, func);
                return d;
            }
        }

        return create(null, null, false);
    }

    public static Delegate RemoveDelegate(Delegate obj, Delegate dg)
    {
        LuaDelegate remove = dg.Target as LuaDelegate;

        if (remove == null)
        {
            obj = Delegate.Remove(obj, dg);
            return obj;
        }

        LuaState state = remove.func.GetLuaState();
        Delegate[] ds = obj.GetInvocationList();

        for (int i = 0; i < ds.Length; i++)
        {
            LuaDelegate ld = ds[i].Target as LuaDelegate;

            if (ld != null && ld == remove)
            {
                obj = Delegate.Remove(obj, ds[i]);
                state.DelayDispose(ld.func);
                state.DelayDispose(ld.self);
                break;
            }
        }

        return obj;
    }
}
