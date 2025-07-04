using System;
using System.Collections.Generic;
using System.Globalization;
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
            return "[" + string.Join(", ", list) + "]";
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

        private static GameObject FindSingletonWithTag(string tag, string hint = "")
        {
            var gos = GameObject.FindGameObjectsWithTag(tag);
            if (gos.Length == 0)
            {
                throw new InvalidAccessException($"Nova: Cannot find game object with tag {tag}. {hint}");
            }

            if (gos.Length > 1)
            {
                throw new InvalidAccessException(
                    $"Nova: Found multiple game objects with tag {tag}:\n" + string.Join("\n", gos.Select(GetPath)));
            }

            return gos[0];
        }

        private static T FindSingletonComponentWithTag<T>(string tag, string hint = "")
        {
            var go = FindSingletonWithTag(tag, hint);
            if (!go.TryGetComponent<T>(out var component))
            {
                throw new InvalidAccessException($"Nova: No {typeof(T)} component in {tag} game object.");
            }

            return component;
        }

        /// <remarks>
        /// This is usually called in Awake or Start
        /// Do not call this when the game is quitting, because NovaController may be already destroyed
        /// </remarks>
        public static NovaController FindNovaController()
        {
            return FindSingletonComponentWithTag<NovaController>("NovaController",
                "Maybe you should put NovaCreator prefab in your scene.");
        }

        public static RenderManager FindRenderManager()
        {
            return FindSingletonComponentWithTag<RenderManager>("RenderManager");
        }

        public static ViewManager FindViewManager()
        {
            return FindSingletonComponentWithTag<ViewManager>("UIRoot");
        }

        public static VideoController FindVideoController()
        {
            return FindSingletonComponentWithTag<VideoController>("Video");
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

        public static bool IsFinite(this float a)
        {
            return !float.IsInfinity(a);
        }

        public static bool IsFinite(this Vector2 a)
        {
            return a.x.IsFinite() && a.y.IsFinite();
        }

        public static bool IsFinite(this Vector3 a)
        {
            return a.x.IsFinite() && a.y.IsFinite() && a.z.IsFinite();
        }

        // Returns true if a.Equals(b) or |a - b|^2 < eps.
        // Addressing the case where Infinity is present.
        public static bool ApproxEquals(this Vector2 a, Vector2 b, float eps = 1e-6f)
        {
            return a.Equals(b) || (a - b).sqrMagnitude < eps;
        }

        // Returns true if a.Equals(b) or |a - b|^2 < eps.
        // Addressing the case where Infinity is present.
        public static bool ApproxEquals(this Vector3 a, Vector3 b, float eps = 1e-6f)
        {
            return a.Equals(b) || (a - b).sqrMagnitude < eps;
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
            return angle < 1e-6f ? 0.0f : Quaternion.Angle(a, value) / angle;
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

        public static void FlushAll()
        {
            var controller = FindNovaController();
            controller.CheckpointManager.UpdateGlobalSave();
            controller.ConfigManager.Flush();
            controller.InputManager.Flush();
        }

        public static void QuitWithAlert()
        {
            FindVideoController().ClearVideo();
            Alert.Show(null, "ingame.quit.confirm", Quit, null, "QuitConfirm");
        }

        public static bool ForceQuit = false;

        public static void Quit()
        {
            NovaAnimation.StopAll();

#if UNITY_EDITOR
            FlushAll();
            UnityEditor.EditorApplication.isPlaying = false;
#else
            ForceQuit = true;
            Application.Quit();
            // All components should flush themselves in OnDestroy
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
            if (x is UnityObject o)
            {
                return o != null;
            }

            return x != null;
        }

        public static int ConvertSamples(AudioClip source, AudioClip target)
        {
            return Mathf.RoundToInt((float)target.frequency / source.frequency * source.samples);
        }

        public static float LogToLinearVolume(float w)
        {
            return w * Mathf.Exp(w - 1f);
        }

        // https://stackoverflow.com/questions/60211021/lambert-w-function-in-c-sharp
        public static float LinearToLogVolume(float x)
        {
            x *= (float)Math.E;

            int nIter = Math.Max(4, Mathf.CeilToInt(Mathf.Log10(x) / 3f));
            float w = 3f * Mathf.Log(x + 1f) / 4f;
            for (int i = 0; i < nIter; ++i)
            {
                float expW = Mathf.Exp(w);
                float res = w * expW - x;
                w -= res / (expW * (w + 1f) - (w + 2f) * res / (2f * w + 2f));
            }

            return w;
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

        public static string GetPath(GameObject go)
        {
            return GetPath(go.transform);
        }

        public static string GetPath(Component component)
        {
            return GetPath(component.transform) + ":" + component.GetType() + ":" + component.GetInstanceID();
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

        public static string PrettyPrint(this ISerializedData data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static T[] FindObjectsOfType<T>() where T : UnityObject
        {
#if UNITY_2022_2_OR_NEWER
            return UnityObject.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return UnityObject.FindObjectsOfType<T>();
#endif
        }
    }

    public class TemporaryInvariantCulture : IDisposable
    {
        private CultureInfo oldCulture;

        public TemporaryInvariantCulture()
        {
            oldCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }
}
