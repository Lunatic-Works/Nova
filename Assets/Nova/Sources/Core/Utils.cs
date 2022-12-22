using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityObject = UnityEngine.Object;

namespace Nova
{
    public static class Utils
    {
        public static Sprite Texture2DToSprite(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        private static Texture2D _ClearTexture;

        public static Texture2D ClearTexture
        {
            get
            {
                if (_ClearTexture == null)
                {
                    _ClearTexture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                    _ClearTexture.SetPixel(0, 0, Color.clear);
                    _ClearTexture.Apply();
                }

                return _ClearTexture;
            }
        }

        public static void RenderToTexture(this Camera camera, RenderTexture to)
        {
            var old = camera.targetTexture;
            camera.targetTexture = to;
            camera.Render();
            camera.targetTexture = old;
        }

        public static void Clear(this RenderTexture rt)
        {
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = old;
        }

        public static void RuntimeAssert(bool condition, string msg)
        {
            if (!condition)
            {
                throw new AssertionException($"Nova: {msg}", null);
            }
        }

        public static void RuntimeAssert(this MonoBehaviour mb, bool condition, string msg)
        {
            if (!condition)
            {
                throw new AssertionException($"Nova - {mb.name}: {msg}", null);
            }
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            return ((List<T>)list).IndexOf(item);
        }

        public static TValue Ensure<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            if (dict.TryGetValue(key, out TValue value))
            {
                return value;
            }

            value = new TValue();
            dict[key] = value;
            return value;
        }

        public static T Ensure<T>(this GameObject go) where T : Component
        {
            if (!go.TryGetComponent<T>(out var x))
            {
                x = go.AddComponent<T>();
            }

            return x;
        }

        public static void Ensure<T>(this List<T> list, int size)
        {
            if (list.Count < size)
            {
                list.AddRange(Enumerable.Repeat(default(T), size - list.Count));
            }
        }

        public static string Dump<T>(this IEnumerable<T> list)
        {
            return "[ " + String.Join(", ", list.Select(x => x.ToString())) + " ]";
        }

        public static Rect ToRect(this RectInt rectInt)
        {
            return new Rect(rectInt.min, rectInt.size);
        }

        public static Vector2 GetContainerSize(Vector2 contentSize, float containerAspectRatio)
        {
            float contentAspectRatio = contentSize.x / contentSize.y;
            if (contentAspectRatio > containerAspectRatio)
            {
                return new Vector2(contentSize.x, contentSize.x / containerAspectRatio);
            }
            else
            {
                return new Vector2(contentSize.y * containerAspectRatio, contentSize.y);
            }
        }

        public static Vector2 GetContentSize(Vector2 containerSize, float contentAspectRatio)
        {
            float containerAspectRatio = containerSize.x / containerSize.y;
            if (contentAspectRatio > containerAspectRatio)
            {
                return new Vector2(containerSize.x, containerSize.x / contentAspectRatio);
            }
            else
            {
                return new Vector2(containerSize.y * contentAspectRatio, containerSize.y);
            }
        }

        /// <remarks>
        /// This is usually called in Awake or Start
        /// Do not call this when the game is quitting, because NovaController may be already destroyed
        /// </remarks>
        public static NovaController FindNovaController()
        {
            var go = GameObject.FindWithTag("NovaController");
            if (go == null)
            {
                throw new InvalidAccessException(
                    "Nova: Cannot find NovaController game object by tag. " +
                    "Maybe you should put NovaCreator prefab in your scene.");
            }

            if (!go.TryGetComponent<NovaController>(out var controller))
            {
                throw new InvalidAccessException("Nova: No NovaController component in NovaController game object.");
            }

            return controller;
        }

        public static RenderManager FindRenderManager()
        {
            var go = GameObject.FindGameObjectWithTag("RenderManager");
            if (go == null)
            {
                throw new InvalidAccessException("Nova: Cannot find RenderManager game object by tag.");
            }

            if (!go.TryGetComponent<RenderManager>(out var renderManager))
            {
                throw new InvalidAccessException("Nova: No RenderManager component in RenderManager game object.");
            }

            return renderManager;
        }

        public static ViewManager FindViewManager()
        {
            var go = GameObject.FindWithTag("UIRoot");
            if (go == null)
            {
                throw new InvalidAccessException("Nova: Cannot find UI root game object by tag.");
            }

            if (!go.TryGetComponent<ViewManager>(out var viewManager))
            {
                throw new InvalidAccessException("Nova: No ViewManager component in UI root game object.");
            }

            return viewManager;
        }

        public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }

            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
        {
            var viewportPosition = new Vector3(
                screenPosition.x / RealScreen.width,
                screenPosition.y / RealScreen.height,
                0.0f
            );
            return canvas.ViewportToCanvasPosition(viewportPosition);
        }

        public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0.0f);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }

        public static float IfNanThenZero(this float a)
        {
            return float.IsNaN(a) ? 0.0f : a;
        }

        public static Vector2 CloneScale(this Vector2 a, Vector2 b)
        {
            Vector2 result = a;
            result.Scale(b);
            return result;
        }

        public static Vector3 CloneScale(this Vector3 a, Vector3 b)
        {
            Vector3 result = a;
            result.Scale(b);
            return result;
        }

        public static Vector2 InverseScale(this Vector2 a, Vector2 b)
        {
            return new Vector2(
                (a.x / b.x).IfNanThenZero(),
                (a.y / b.y).IfNanThenZero()
            );
        }

        public static Vector3 InverseScale(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                (a.x / b.x).IfNanThenZero(),
                (a.y / b.y).IfNanThenZero(),
                (a.z / b.z).IfNanThenZero()
            );
        }

        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            float t = Mathf.InverseLerp(a.x, b.x, value.x);
            if (t != 0.0f) return t;
            t = Mathf.InverseLerp(a.y, b.y, value.y);
            if (t != 0.0f) return t;
            t = Mathf.InverseLerp(a.z, b.z, value.z);
            return t;
        }

        public static float InverseLerp(Color a, Color b, Color value)
        {
            float t = Mathf.InverseLerp(a.r, b.r, value.r);
            if (t != 0.0f) return t;
            t = Mathf.InverseLerp(a.g, b.g, value.g);
            if (t != 0.0f) return t;
            t = Mathf.InverseLerp(a.b, b.b, value.b);
            if (t != 0.0f) return t;
            t = Mathf.InverseLerp(a.a, b.a, value.a);
            return t;
        }

        public static float InverseSlerp(Quaternion a, Quaternion b, Quaternion value)
        {
            float angle = Quaternion.Angle(a, b);
            return angle < float.Epsilon ? 0.0f : Quaternion.Angle(a, value) / angle;
        }

        public static Color SetAlpha(Color color, float a)
        {
            color.a = a;
            return color;
        }

        public static Color32 SetAlpha32(Color32 color, byte a)
        {
            color.a = a;
            return color;
        }

        public static void SaveAll()
        {
            var controller = FindNovaController();
            controller.CheckpointManager.UpdateGlobalSave();
            controller.ConfigManager.Apply();
            controller.InputManager.Save();
        }

        public static void QuitWithAlert()
        {
            Alert.Show(null, "ingame.quit.confirm", Quit, null, "QuitConfirm");
        }

        public static bool ForceQuit = false;

        public static void Quit()
        {
            NovaAnimation.StopAll();

#if UNITY_EDITOR
            SaveAll();
            UnityEditor.EditorApplication.isPlaying = false;
#else
            ForceQuit = true;
            Application.Quit();
            // All components write to disk in OnDestroy
#endif
        }

        // Only use in editor code or when not inherited from MonoBehaviour
        public static void DestroyObject(UnityObject obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityObject.Destroy(obj);
            }
            else
            {
                UnityObject.DestroyImmediate(obj);
            }
#else
            UnityObject.Destroy(obj);
#endif
        }

        public static bool IsNotNullOrDestroyed(this object x)
        {
            if (x is UnityEngine.Object o)
            {
                return o != null;
            }

            return x != null;
        }

        public static int ConvertSamples(AudioClip source, AudioClip target)
        {
            return Mathf.RoundToInt(1.0f * target.frequency / source.frequency * source.samples);
        }

        // Avoid mutating the enumerable in the loop
        public static IEnumerable<Transform> GetChildren(Transform transform)
        {
            return transform.Cast<Transform>().ToList();
        }

        public static string GetPath(Transform current)
        {
            var parent = current.parent;
            return (parent == null ? "" : GetPath(parent) + "/") + current.name;
        }

        public static string GetPath(Component component)
        {
            return GetPath(component.transform) + ":" + component.GetType();
        }

        // Convert OS-dependent path separator to "/"
        public static string ConvertPathSeparator(string s)
        {
            return s.Replace('\\', '/');
        }

        public static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        public static T Next<T>(this System.Random random, IReadOnlyList<T> list)
        {
            var idx = random.Next(list.Count);
            return list[idx];
        }

        // Knuth's golden ratio multiplicative hashing
        public static ulong HashList(IEnumerable<ulong> hashes)
        {
            var r = 0UL;
            unchecked
            {
                foreach (var x in hashes)
                {
                    r += x;
                    r *= 11400714819323199563UL;
                }
            }

            return r;
        }

        public static ulong HashList<T>(IEnumerable<T> list) where T : class
        {
            return HashList(list.Select(x => (ulong)x.GetHashCode()));
        }

        public static ulong HashObjects(params object[] list)
        {
            return HashList(list);
        }

        public static string PrettyPrint(this ISerializedData data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}
