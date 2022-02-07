using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
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

        public static Texture2D ClearTexture
        {
            get
            {
                var tex = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                tex.SetPixel(0, 0, Color.clear);
                tex.Apply();
                return tex;
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

        public static GameController FindNovaGameController()
        {
            var go = GameObject.FindWithTag("NovaGameController");
            if (go == null)
            {
                throw new InvalidAccessException(
                    "Nova: Cannot find NovaGameController game object by tag. Maybe you should put NovaCreator prefab in your scene.");
            }

            var gameController = go.GetComponent<GameController>();
            if (gameController == null)
            {
                throw new InvalidAccessException(
                    "Nova: No GameController component in NovaGameController game object.");
            }

            return gameController;
        }

        public static ViewManager FindViewManager()
        {
            var go = GameObject.FindWithTag("UIRoot");
            if (go == null)
            {
                throw new InvalidAccessException("Nova: Cannot find UI root game object by tag.");
            }

            var viewManager = go.GetComponent<ViewManager>();
            if (viewManager == null)
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
            var gameController = FindNovaGameController();
            gameController.CheckpointManager.UpdateGlobalSave();
            gameController.ConfigManager.Apply();
            gameController.InputMapper.Save();
        }

        public static void QuitWithConfirm()
        {
            Alert.Show(
                null,
                I18n.__("ingame.exit.confirm"),
                Quit,
                null,
                "QuitConfirm"
            );
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

        public static void DestroyMaterial(Material material, bool destroyInEditor = true)
        {
            DestroyObject(material, destroyInEditor);
        }

        public static void DestroyObject(UnityObject obj, bool destroyInEditor = true)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityObject.Destroy(obj);
            }
            else if (destroyInEditor)
            {
                UnityObject.DestroyImmediate(obj);
            }
#else
            UnityObject.Destroy(obj);
#endif
        }

        public static TextAlignmentOptions TextAnchor2TextAlignmentOptions(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopJustified;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineJustified;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomJustified;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static TextAnchor TextAlignmentOptions2TextAnchor(TextAlignmentOptions anchor)
        {
            switch (anchor)
            {
                case TextAlignmentOptions.TopJustified:
                    return TextAnchor.UpperLeft;
                case TextAlignmentOptions.Top:
                    return TextAnchor.UpperCenter;
                case TextAlignmentOptions.TopRight:
                    return TextAnchor.UpperRight;
                case TextAlignmentOptions.MidlineJustified:
                    return TextAnchor.MiddleLeft;
                case TextAlignmentOptions.Midline:
                    return TextAnchor.MiddleCenter;
                case TextAlignmentOptions.MidlineRight:
                    return TextAnchor.MiddleRight;
                case TextAlignmentOptions.BottomJustified:
                    return TextAnchor.LowerLeft;
                case TextAlignmentOptions.Bottom:
                    return TextAnchor.LowerCenter;
                case TextAlignmentOptions.BottomRight:
                    return TextAnchor.LowerRight;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<T> LazyList<T>(T first, Func<T, T> next)
        {
            var curr = first;
            while (curr != null)
            {
                yield return curr;
                curr = next(curr);
            }
        }

        public static Action<Action> AfterAll(params Action<Action>[] actions)
        {
            return callback =>
            {
                var callbackCalled = new bool[actions.Length];

                Action SetAndCheckIfAllCalled(int i) =>
                    () =>
                    {
                        callbackCalled[i] = true;
                        if (callbackCalled.All(x => x))
                        {
                            callback();
                        }
                    };

                for (int i = 0; i < actions.Length; i++)
                {
                    actions[i](SetAndCheckIfAllCalled(i));
                }
            };
        }

        public static Action<Action> InjectBeforeCall(Action<Action> action, Action advice)
        {
            return callback =>
            {
                advice();
                action(callback);
            };
        }

        public static Action<Action> InjectBeforeCallback(Action<Action> action, Action advice)
        {
            return callback => action(() =>
            {
                advice();
                callback();
            });
        }

        public static bool GetKeyInEditor(KeyCode key)
        {
#if UNITY_EDITOR
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        public static bool GetKeyDownInEditor(KeyCode key)
        {
#if UNITY_EDITOR
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool GetKeyUpInEditor(KeyCode key)
        {
#if UNITY_EDITOR
            return Input.GetKeyUp(key);
#else
            return false;
#endif
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
            return (parent == null ? "" : GetPath(parent)) + "/" + current.name;
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
    }

    public class XorStream : Stream
    {
        private readonly byte[] xorCodes;
        private readonly Stream realStream;

        public XorStream(Stream realStream, byte[] xorCodes)
        {
            this.xorCodes = xorCodes;
            this.realStream = realStream;
        }

        public override bool CanRead => realStream.CanRead;

        public override bool CanSeek => realStream.CanSeek;

        public override bool CanWrite => realStream.CanWrite;

        public override long Length => realStream.Length;

        public override long Position
        {
            get => realStream.Position;
            set => realStream.Position = value;
        }

        public override void Flush()
        {
            realStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int position = (int)(Position % xorCodes.Length);
            int ret = realStream.Read(buffer, offset, count);
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] ^= xorCodes[(position + i) % xorCodes.Length];
            }

            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return realStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            realStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] buf = new byte[count];
            int position = (int)(Position % xorCodes.Length);
            for (int i = 0; i < count; i++)
            {
                buf[i] = (byte)(buffer[i + offset] ^ xorCodes[(position + i) % xorCodes.Length]);
            }

            realStream.Write(buf, 0, count);
        }
    }

    public class AndGate
    {
        private readonly Action<bool> callback;
        private readonly AndGate targetGate;
        private AndGate parentGate;
        private bool value;

        public AndGate(Action<bool> callback)
        {
            this.callback = callback;
        }

        public AndGate(AndGate target)
        {
            targetGate = target;
            target.parentGate = this;
        }

        public bool chainValue
        {
            get
            {
                return Utils.LazyList(parentGate, g => g.parentGate).All(g => g.value)
                       && Utils.LazyList(this, g => g.targetGate).All(g => g.value);
            }
        }

        public Action<bool> finalCallback
        {
            get { return Utils.LazyList(this, g => g.targetGate).Last().callback; }
        }

        public void SetActive(bool to)
        {
            if (value == to)
                return;
            value = to;
            finalCallback?.Invoke(chainValue);
        }
    }
}