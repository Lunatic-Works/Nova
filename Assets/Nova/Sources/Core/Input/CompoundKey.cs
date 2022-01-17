using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// A compound key is a normal key with prefix of Ctrl, Win, Alt, Shift
    /// </summary>
    public class CompoundKey
    {
        public bool Ctrl = false;
        public bool Win = false;
        public bool Alt = false;
        public bool Shift = false;
        public KeyCode Key = KeyCode.None;

        public CompoundKey() { }

        public CompoundKey(CompoundKey key)
        {
            Ctrl = key.Ctrl;
            Win = key.Win;
            Alt = key.Alt;
            Shift = key.Shift;
            Key = key.Key;
        }

        public static IEnumerable<KeyCode> CtrlKeys
        {
            get
            {
                yield return KeyCode.LeftControl;
                yield return KeyCode.RightControl;
            }
        }

        public static bool CtrlIsHolding => CtrlKeys.Any(Input.GetKey);

        public static IEnumerable<KeyCode> WinKeys
        {
            get
            {
                yield return KeyCode.LeftApple;
                yield return KeyCode.RightApple;
                yield return KeyCode.LeftWindows;
                yield return KeyCode.RightWindows;
                yield return KeyCode.LeftCommand;
                yield return KeyCode.RightCommand;
            }
        }

        public static bool WinIsHolding => WinKeys.Any(Input.GetKey);

        public static IEnumerable<KeyCode> AltKeys
        {
            get
            {
                yield return KeyCode.LeftAlt;
                yield return KeyCode.RightAlt;
            }
        }

        public static bool AltIsHolding => AltKeys.Any(Input.GetKey);

        public static IEnumerable<KeyCode> ShiftKeys
        {
            get
            {
                yield return KeyCode.LeftShift;
                yield return KeyCode.RightShift;
            }
        }

        public static bool ShiftIsHolding => ShiftKeys.Any(Input.GetKey);

        public static IEnumerable<KeyCode> PrefixKeys
        {
            get
            {
                foreach (var key in CtrlKeys)
                {
                    yield return key;
                }

                foreach (var key in WinKeys)
                {
                    yield return key;
                }

                foreach (var key in AltKeys)
                {
                    yield return key;
                }

                foreach (var key in ShiftKeys)
                {
                    yield return key;
                }
            }
        }

        public static IEnumerable<KeyCode> KeyboardKeys
        {
            get
            {
                yield return KeyCode.Backspace;
                yield return KeyCode.Tab;
                yield return KeyCode.Clear;
                yield return KeyCode.Return;
                yield return KeyCode.Pause;
                yield return KeyCode.Escape;
                yield return KeyCode.Space;
                yield return KeyCode.Exclaim;
                yield return KeyCode.DoubleQuote;
                yield return KeyCode.Hash;
                yield return KeyCode.Dollar;
                yield return KeyCode.Percent;
                yield return KeyCode.Ampersand;
                yield return KeyCode.Quote;
                yield return KeyCode.LeftParen;
                yield return KeyCode.RightParen;
                yield return KeyCode.Asterisk;
                yield return KeyCode.Plus;
                yield return KeyCode.Comma;
                yield return KeyCode.Minus;
                yield return KeyCode.Period;
                yield return KeyCode.Slash;
                yield return KeyCode.Alpha0;
                yield return KeyCode.Alpha1;
                yield return KeyCode.Alpha2;
                yield return KeyCode.Alpha3;
                yield return KeyCode.Alpha4;
                yield return KeyCode.Alpha5;
                yield return KeyCode.Alpha6;
                yield return KeyCode.Alpha7;
                yield return KeyCode.Alpha8;
                yield return KeyCode.Alpha9;
                yield return KeyCode.Colon;
                yield return KeyCode.Semicolon;
                yield return KeyCode.Less;
                yield return KeyCode.Equals;
                yield return KeyCode.Greater;
                yield return KeyCode.Question;
                yield return KeyCode.At;
                yield return KeyCode.LeftBracket;
                yield return KeyCode.Backslash;
                yield return KeyCode.RightBracket;
                yield return KeyCode.Caret;
                yield return KeyCode.Underscore;
                yield return KeyCode.BackQuote;
                yield return KeyCode.A;
                yield return KeyCode.B;
                yield return KeyCode.C;
                yield return KeyCode.D;
                yield return KeyCode.E;
                yield return KeyCode.F;
                yield return KeyCode.G;
                yield return KeyCode.H;
                yield return KeyCode.I;
                yield return KeyCode.J;
                yield return KeyCode.K;
                yield return KeyCode.L;
                yield return KeyCode.M;
                yield return KeyCode.N;
                yield return KeyCode.O;
                yield return KeyCode.P;
                yield return KeyCode.Q;
                yield return KeyCode.R;
                yield return KeyCode.S;
                yield return KeyCode.T;
                yield return KeyCode.U;
                yield return KeyCode.V;
                yield return KeyCode.W;
                yield return KeyCode.X;
                yield return KeyCode.Y;
                yield return KeyCode.Z;
                yield return KeyCode.LeftCurlyBracket;
                yield return KeyCode.Pipe;
                yield return KeyCode.RightCurlyBracket;
                yield return KeyCode.Tilde;
                yield return KeyCode.Delete;
                yield return KeyCode.Keypad0;
                yield return KeyCode.Keypad1;
                yield return KeyCode.Keypad2;
                yield return KeyCode.Keypad3;
                yield return KeyCode.Keypad4;
                yield return KeyCode.Keypad5;
                yield return KeyCode.Keypad6;
                yield return KeyCode.Keypad7;
                yield return KeyCode.Keypad8;
                yield return KeyCode.Keypad9;
                yield return KeyCode.KeypadPeriod;
                yield return KeyCode.KeypadDivide;
                yield return KeyCode.KeypadMultiply;
                yield return KeyCode.KeypadMinus;
                yield return KeyCode.KeypadPlus;
                yield return KeyCode.KeypadEnter;
                yield return KeyCode.KeypadEquals;
                yield return KeyCode.UpArrow;
                yield return KeyCode.DownArrow;
                yield return KeyCode.RightArrow;
                yield return KeyCode.LeftArrow;
                yield return KeyCode.Insert;
                yield return KeyCode.Home;
                yield return KeyCode.End;
                yield return KeyCode.PageUp;
                yield return KeyCode.PageDown;
                yield return KeyCode.F1;
                yield return KeyCode.F2;
                yield return KeyCode.F3;
                yield return KeyCode.F4;
                yield return KeyCode.F5;
                yield return KeyCode.F6;
                yield return KeyCode.F7;
                yield return KeyCode.F8;
                yield return KeyCode.F9;
                yield return KeyCode.F10;
                yield return KeyCode.F11;
                yield return KeyCode.F12;
                yield return KeyCode.F13;
                yield return KeyCode.F14;
                yield return KeyCode.F15;
                yield return KeyCode.Numlock;
                yield return KeyCode.CapsLock;
                yield return KeyCode.ScrollLock;
                yield return KeyCode.RightShift;
                yield return KeyCode.LeftShift;
                yield return KeyCode.RightControl;
                yield return KeyCode.LeftControl;
                yield return KeyCode.RightAlt;
                yield return KeyCode.LeftAlt;
                yield return KeyCode.RightApple;
                yield return KeyCode.RightCommand;
                yield return KeyCode.LeftApple;
                yield return KeyCode.LeftCommand;
                yield return KeyCode.LeftWindows;
                yield return KeyCode.RightWindows;
                yield return KeyCode.AltGr;
                yield return KeyCode.Help;
                yield return KeyCode.Print;
                yield return KeyCode.SysReq;
                yield return KeyCode.Break;
                yield return KeyCode.Menu;
            }
        }

        public bool isNone => !Ctrl && !Win && !Shift && !Alt && Key == KeyCode.None;

        public void Clear()
        {
            Ctrl = false;
            Win = false;
            Shift = false;
            Alt = false;
            Key = KeyCode.None;
        }

        public bool holding
        {
            get
            {
                if (isNone)
                {
                    return false;
                }

                var value = true;
                if (Ctrl)
                {
                    value = value && CtrlIsHolding;
                }

                if (Win)
                {
                    value = value && WinIsHolding;
                }

                if (Alt)
                {
                    value = value && AltIsHolding;
                }

                if (Shift)
                {
                    value = value && ShiftIsHolding;
                }

                if (Key != KeyCode.None)
                {
                    value = value && Input.GetKey(Key);
                }

                return value;
            }
        }

        public override string ToString()
        {
            if (isNone)
            {
                return Enum.GetName(typeof(KeyCode), KeyCode.None);
            }

            var sb = new StringBuilder();
            if (Ctrl)
            {
                sb.Append("Ctrl+");
            }

            if (Win)
            {
                sb.Append("Win+");
            }

            if (Alt)
            {
                sb.Append("Alt+");
            }

            if (Shift)
            {
                sb.Append("Shift+");
            }

            if (Key == KeyCode.None)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            else
            {
                sb.Append(Enum.GetName(typeof(KeyCode), Key));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parse CompoundKey from string. Keys should separated by '+' and any token except Ctrl, Win, Alt, Shift
        /// should be the name of KeyCode
        /// </summary>
        /// <param name="str">the string to parse</param>
        /// <exception cref="ArgumentNullException">argument is null</exception>
        /// <exception cref="ArgumentException">argument is ill formed</exception>
        /// <returns></returns>
        public static CompoundKey FromString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException();
            }

            var v = new CompoundKey();
            var keys = str.Split('+').Select(k => k.Trim());
            foreach (var key in keys)
            {
                switch (key)
                {
                    case "Ctrl":
                        v.Ctrl = true;
                        break;
                    case "Win":
                        v.Win = true;
                        break;
                    case "Alt":
                        v.Alt = true;
                        break;
                    case "Shift":
                        v.Shift = true;
                        break;
                    default:
                        v.Key = (KeyCode)Enum.Parse(typeof(KeyCode), key);
                        break;
                }
            }

            return v;
        }

        public override bool Equals(object obj)
        {
            return obj is CompoundKey other
                   && Key == other.Key
                   && Ctrl == other.Ctrl
                   && Win == other.Win
                   && Alt == other.Alt
                   && Shift == other.Shift;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var x = 0U;
                x += (uint)Key;
                x *= 2654435789U;
                x += Ctrl ? 1U : 0U;
                x *= 2654435789U;
                x += Win ? 1U : 0U;
                x *= 2654435789U;
                x += Alt ? 1U : 0U;
                x *= 2654435789U;
                x += Shift ? 1U : 0U;
                x *= 2654435789U;
                return (int)x;
            }
        }
    }
}