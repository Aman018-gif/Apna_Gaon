using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MohallaHero
{
    public enum GameKey
    {
        Up, Down, Left, Right, Run, Interact, Phone, Map, Pause, Hint, QuickSave,
        Choice1, Choice2, Choice3, Choice4
    }

    /// <summary>
    /// Input abstraction: keyboard through either the new Input System package or the legacy Input Manager
    /// (whichever the project's "Active Input Handling" enables), plus the on-screen touch controls.
    /// Read keys only through this class; new bindings go in both branches.
    /// </summary>
    public static class GameInput
    {
        /// <summary>Movement from the on-screen joystick (set by <see cref="TouchControls"/>).</summary>
        public static Vector2 VirtualMove;

        static readonly Dictionary<GameKey, int> virtualDown = new Dictionary<GameKey, int>();

        /// <summary>
        /// A touch button was pressed. It reads as "down" for exactly one frame (the next one), so every script that
        /// polls <see cref="Down"/> during that frame sees it once, whatever the script execution order.
        /// </summary>
        public static void Press(GameKey k) => virtualDown[k] = Time.frameCount + 1;

        static bool VirtualDown(GameKey k) => virtualDown.TryGetValue(k, out var f) && f == Time.frameCount;

        public static Vector2 Move()
        {
            float x = (Held(GameKey.Right) ? 1f : 0f) - (Held(GameKey.Left) ? 1f : 0f);
            float y = (Held(GameKey.Up) ? 1f : 0f) - (Held(GameKey.Down) ? 1f : 0f);
            var v = new Vector2(x, y) + VirtualMove;
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        /// <summary>True when the joystick is pushed nearly to its edge (touch "run").</summary>
        public static bool VirtualRun => VirtualMove.sqrMagnitude > 0.85f;

        public static bool Down(GameKey k) => VirtualDown(k) || KeyDown(k);

#if ENABLE_INPUT_SYSTEM
        static Key[] Keys(GameKey k)
        {
            switch (k)
            {
                case GameKey.Up: return new[] { Key.W, Key.UpArrow };
                case GameKey.Down: return new[] { Key.S, Key.DownArrow };
                case GameKey.Left: return new[] { Key.A, Key.LeftArrow };
                case GameKey.Right: return new[] { Key.D, Key.RightArrow };
                case GameKey.Run: return new[] { Key.LeftShift, Key.RightShift };
                case GameKey.Interact: return new[] { Key.E, Key.Enter, Key.Space };
                case GameKey.Phone: return new[] { Key.Tab, Key.P, Key.J };
                case GameKey.Map: return new[] { Key.M };
                case GameKey.Pause: return new[] { Key.Escape };
                case GameKey.Hint: return new[] { Key.H };
                case GameKey.QuickSave: return new[] { Key.F5 };
                case GameKey.Choice1: return new[] { Key.Digit1, Key.Numpad1 };
                case GameKey.Choice2: return new[] { Key.Digit2, Key.Numpad2 };
                case GameKey.Choice3: return new[] { Key.Digit3, Key.Numpad3 };
                case GameKey.Choice4: return new[] { Key.Digit4, Key.Numpad4 };
            }
            return new Key[0];
        }

        public static bool Held(GameKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var key in Keys(k)) if (kb[key].isPressed) return true;
            return false;
        }

        static bool KeyDown(GameKey k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var key in Keys(k)) if (kb[key].wasPressedThisFrame) return true;
            return false;
        }

        public static bool TouchAvailable => Touchscreen.current != null;
#else
        static KeyCode[] Keys(GameKey k)
        {
            switch (k)
            {
                case GameKey.Up: return new[] { KeyCode.W, KeyCode.UpArrow };
                case GameKey.Down: return new[] { KeyCode.S, KeyCode.DownArrow };
                case GameKey.Left: return new[] { KeyCode.A, KeyCode.LeftArrow };
                case GameKey.Right: return new[] { KeyCode.D, KeyCode.RightArrow };
                case GameKey.Run: return new[] { KeyCode.LeftShift, KeyCode.RightShift };
                case GameKey.Interact: return new[] { KeyCode.E, KeyCode.Return, KeyCode.Space };
                case GameKey.Phone: return new[] { KeyCode.Tab, KeyCode.P, KeyCode.J };
                case GameKey.Map: return new[] { KeyCode.M };
                case GameKey.Pause: return new[] { KeyCode.Escape };
                case GameKey.Hint: return new[] { KeyCode.H };
                case GameKey.QuickSave: return new[] { KeyCode.F5 };
                case GameKey.Choice1: return new[] { KeyCode.Alpha1, KeyCode.Keypad1 };
                case GameKey.Choice2: return new[] { KeyCode.Alpha2, KeyCode.Keypad2 };
                case GameKey.Choice3: return new[] { KeyCode.Alpha3, KeyCode.Keypad3 };
                case GameKey.Choice4: return new[] { KeyCode.Alpha4, KeyCode.Keypad4 };
            }
            return new KeyCode[0];
        }

        public static bool Held(GameKey k)
        {
            foreach (var key in Keys(k)) if (Input.GetKey(key)) return true;
            return false;
        }

        static bool KeyDown(GameKey k)
        {
            foreach (var key in Keys(k)) if (Input.GetKeyDown(key)) return true;
            return false;
        }

        public static bool TouchAvailable => Input.touchSupported;
#endif
    }
}
