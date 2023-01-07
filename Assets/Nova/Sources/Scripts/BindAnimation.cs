using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(NovaAnimation))]
    public class BindAnimation : MonoBehaviour
    {
        public string luaGlobalName;

        private void Awake()
        {
            var luaHiddenName = "_" + luaGlobalName;
            LuaRuntime.Instance.BindObject(luaHiddenName, GetComponent<NovaAnimation>());
            LuaRuntime.Instance.DoString($"{luaGlobalName} = NovaAnimation:new {{ anim = __Nova.{luaHiddenName} }}");
        }
    }
}
