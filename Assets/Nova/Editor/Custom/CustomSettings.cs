using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;
using BindType = ToLuaMenu.BindType;

public static class CustomSettings
{
    public static string saveDir = Application.dataPath + "/Nova/Sources/ThirdParty/ToLua/Source/Generate/";
    public static string toluaBaseType = Application.dataPath + "/Nova/Sources/ThirdParty/ToLua/BaseType/";
    public static string baseLuaDir = Application.dataPath + "/Nova/Lua/";
    public static string injectionFilesPath = Application.dataPath + "/Nova/Sources/ThirdParty/ToLua/Injection/";

    // 导出时强制做为静态类的类型（注意customTypeList还要添加这个类型才能导出）
    // Unity有些类作为sealed class，其实完全等价于静态类
    public static List<Type> staticClassTypes = new List<Type>
    {
        // typeof(Application),
        // typeof(GL),
        // typeof(Graphics),
        // typeof(Input),
        // typeof(Physics),
        // typeof(QualitySettings),
        // typeof(RenderSettings),
        // typeof(Resources),
        // typeof(Screen),
        // typeof(SleepTimeout),
        // typeof(Time),
    };

    // 附加导出委托类型（在导出委托时，customTypeList中牵扯的委托类型都会导出，无需写在这里）
    public static DelegateType[] customDelegateList =
    {
        // _DT(typeof(Action)),
        // _DT(typeof(Action<int>)),
        // _DT(typeof(Comparison<int>)),
        // _DT(typeof(Func<int, int>)),
        // _DT(typeof(Predicate<int>)),
        // _DT(typeof(UnityEngine.Events.UnityAction)),
    };

    // 在这里添加你要导出注册到Lua的类型列表
    private static readonly BindType[] _customTypeList =
    {
        // ------------------------为例子导出--------------------------------
        // _GT(typeof(Dictionary<int, TestAccount>)).SetLibName("AccountMap"),
        // _GT(typeof(Dictionary<int, TestAccount>.KeyCollection)),
        // _GT(typeof(Dictionary<int, TestAccount>.ValueCollection)),
        // _GT(typeof(KeyValuePair<int, TestAccount>)),
        // _GT(typeof(TestAccount)),
        // _GT(typeof(TestEventListener)),
        // _GT(typeof(TestExport)),
        // _GT(typeof(TestExport.Space)),
        // _GT(typeof(TestProtol)),
        // -------------------------------------------------------------------

        // _GT(typeof(LuaInjectionStation)),
        // _GT(typeof(InjectType)),
        // _GT(typeof(Debugger)).SetNameSpace(null),

#if USING_DOTWEENING
        // _GT(typeof(DG.Tweening.DOTween)),
        // _GT(typeof(DG.Tweening.LoopType)),
        // _GT(typeof(DG.Tweening.PathMode)),
        // _GT(typeof(DG.Tweening.PathType)),
        // _GT(typeof(DG.Tweening.RotateMode)),
        // _GT(typeof(DG.Tweening.Sequence)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        // _GT(typeof(DG.Tweening.Tween)).SetBaseType(typeof(System.Object)).AddExtendType(typeof(DG.Tweening.TweenExtensions)),
        // _GT(typeof(DG.Tweening.Tweener)).AddExtendType(typeof(DG.Tweening.TweenSettingsExtensions)),
        // _GT(typeof(AudioSource)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Camera)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Component)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Light)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Material)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Rigidbody)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(Transform)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),

        // _GT(typeof(LineRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
        // _GT(typeof(TrailRenderer)).AddExtendType(typeof(DG.Tweening.ShortcutExtensions)),
#else
        // _GT(typeof(AudioSource)),
        // _GT(typeof(Camera)),
        // _GT(typeof(Component)),
        // _GT(typeof(Light)),
        // _GT(typeof(Material)),
        // _GT(typeof(Rigidbody)),
        // _GT(typeof(Transform)),

        // _GT(typeof(LineRenderer)),
        // _GT(typeof(TrailRenderer)),
#endif

        // _GT(typeof(Application)),
        // _GT(typeof(AssetBundle)),
        // _GT(typeof(AsyncOperation)).SetBaseType(typeof(System.Object)),
        // _GT(typeof(AudioClip)),
        // _GT(typeof(Behaviour)),
        // _GT(typeof(CameraClearFlags)),
        // _GT(typeof(Collider)),
        _GT(typeof(GameObject)),
        // _GT(typeof(LightType)),
        // _GT(typeof(MonoBehaviour)),
        // _GT(typeof(ParticleSystem)),
        // _GT(typeof(Physics)),
        // _GT(typeof(Renderer)),
        // _GT(typeof(Screen)),
        // _GT(typeof(Shader)),
        // _GT(typeof(SleepTimeout)),
        // _GT(typeof(Texture)),
        _GT(typeof(Texture2D)),
        // _GT(typeof(Time)),
        // _GT(typeof(TrackedReference)),
        // _GT(typeof(WWW)),

#if UNITY_5_3_OR_NEWER && !UNITY_5_6_OR_NEWER
        // _GT(typeof(UnityEngine.Experimental.Director.DirectorPlayer)),
#endif

        // _GT(typeof(Animator)),
        // _GT(typeof(Input)),
        // _GT(typeof(KeyCode)),
        // _GT(typeof(MeshRenderer)),
        // _GT(typeof(SkinnedMeshRenderer)),
        // _GT(typeof(Space)),

#if !UNITY_5_4_OR_NEWER
        // _GT(typeof(ParticleAnimator)),
        // _GT(typeof(ParticleEmitter)),
        // _GT(typeof(ParticleRenderer)),
#endif

        // _GT(typeof(BoxCollider)),
        // _GT(typeof(CapsuleCollider)),
        // _GT(typeof(CharacterController)),
        // _GT(typeof(MeshCollider)),
        // _GT(typeof(SphereCollider)),

        // _GT(typeof(Animation)),
        // _GT(typeof(AnimationBlendMode)),
        // _GT(typeof(AnimationClip)).SetBaseType(typeof(UnityEngine.Object)),
        // _GT(typeof(AnimationState)),
        // _GT(typeof(PlayMode)),
        // _GT(typeof(QueueMode)),
        // _GT(typeof(WrapMode)),

        // _GT(typeof(BlendWeights)),
        // _GT(typeof(LuaProfiler)),
        // _GT(typeof(QualitySettings)),
        // _GT(typeof(RenderSettings)),
        _GT(typeof(RenderTexture)),
        // _GT(typeof(Resources)),

        #region Nova exported types

        _GT(typeof(Image)),
        _GT(typeof(PlayableDirector)),
        _GT(typeof(RawImage)),
        _GT(typeof(RectTransform)),
        // _GT(typeof(Sprite)),
        _GT(typeof(SpriteRenderer)),
        _GT(typeof(VideoClip)),
        _GT(typeof(VideoPlayer)),

        _GT(typeof(TMPro.TextAlignmentOptions)),

        #endregion
    };

    public static readonly BindType[] customTypeList =
        _customTypeList.Concat(
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.GetCustomAttribute<Nova.ExportCustomType>() != null)
                .Select(_GT)
        ).ToArray();

    public static readonly List<Type> dynamicList = new List<Type>()
    {
        // typeof(MeshRenderer),

#if !UNITY_5_4_OR_NEWER
        // typeof(ParticleAnimator),
        // typeof(ParticleEmitter),
        // typeof(ParticleRenderer),
#endif

        // typeof(BoxCollider),
        // typeof(CapsuleCollider),
        // typeof(CharacterController),
        // typeof(MeshCollider),
        // typeof(SphereCollider),

        // typeof(Animation),
        // typeof(AnimationClip),
        // typeof(AnimationState),

        // typeof(BlendWeights),
        // typeof(RenderTexture),
        // typeof(Rigidbody),
    };

    // 重载函数，相同参数个数，相同位置out参数匹配出问题时，需要强制匹配解决
    // 使用方法参见例子14
    public static readonly List<Type> outList = new List<Type>() { };

    // NGUI优化，下面的类没有派生类，可以作为sealed class
    public static readonly List<Type> sealedList = new List<Type>()
    {
        // typeof(Localization),
        // typeof(Transform),
        // typeof(TweenAlpha),
        // typeof(TweenColor),
        // typeof(TweenHeight),
        // typeof(TweenPosition),
        // typeof(TweenRotation),
        // typeof(TweenScale),
        // typeof(TweenWidth),
        // typeof(TypewriterEffect),
        // typeof(UIAnchor),
        // typeof(UIAtlas),
        // typeof(UIButton),
        // typeof(UICamera),
        // typeof(UICenterOnChild),
        // typeof(UIDragScrollView),
        // typeof(UIEventListener),
        // typeof(UIFont),
        // typeof(UIGrid),
        // typeof(UIInput),
        // typeof(UILabel),
        // typeof(UIPanel),
        // typeof(UIPlayTween),
        // typeof(UIRoot),
        // typeof(UIScrollBar),
        // typeof(UIScrollView),
        // typeof(UIScrollView),
        // typeof(UISprite),
        // typeof(UISpriteAnimation),
        // typeof(UITable),
        // typeof(UITextList),
        // typeof(UITexture),
        // typeof(UIToggle),
        // typeof(UIViewport),
        // typeof(UIWrapContent),
        // typeof(UIWrapGrid),
    };

    public static BindType _GT(Type t)
    {
        return new BindType(t);
    }

    public static DelegateType _DT(Type t)
    {
        return new DelegateType(t);
    }

    [MenuItem("Lua/Attach Profiler", false, 151)]
    private static void AttachProfiler()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "请在运行时执行此功能", "确定");
            return;
        }

        LuaClient.Instance.AttachProfiler();
    }

    [MenuItem("Lua/Detach Profiler", false, 152)]
    private static void DetachProfiler()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        LuaClient.Instance.DetachProfiler();
    }
}
