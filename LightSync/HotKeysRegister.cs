using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightSync
{
    //组合控制键
    public enum HKModifiers : byte
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8,
    }

    struct KeyAction
    {
        public Action action;
        public bool reenterable;
        public int reenterCount;
        public bool asynchronized;

        public KeyAction(bool reenterable, Action action, bool asynchronized=false)
        {
            this.action = action;
            this.reenterable = reenterable;
            this.reenterCount = 0;
            this.asynchronized = asynchronized;
        }
    }


    public class HotKeysRegister
    {
        //引入系统API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int modifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern UInt32 RegisterHotKey(IntPtr hWnd, UInt32 id, UInt32 fsModifiers, UInt32 vk); //API


        private Dictionary<int, KeyContext> keys = new Dictionary<int, KeyContext>();
        private int CurrentMaxId { get; set; } = 10;
        private IntPtr hWnd;

        public HotKeysRegister(IntPtr handle)
        {
            hWnd = handle;
        }

        public int Register(HKModifiers modifiers, int vk, Dictionary<int, Action> actions, 
                                bool reenterable=true, bool asynchronized=false)
        {
            int id = CurrentMaxId;
            keys.Add(CurrentMaxId, new KeyContext(reenterable, modifiers, vk, actions, asynchronized));
            RegisterHotKey(hWnd, CurrentMaxId++, (int)modifiers, vk);

            return id;
        }

        public int Register(HKModifiers modifiers, int vk, Action action,
                        bool reenterable = true, bool asynchronized = false)
        {
            int id = CurrentMaxId;

            keys.Add(CurrentMaxId, new KeyContext(reenterable, modifiers, vk, new Dictionary<int, Action>() { { 1, action } }, asynchronized));
            bool res = RegisterHotKey(hWnd, CurrentMaxId++, (int)modifiers, vk);

            return id;
        }

        public void Register(int id)
        {
            if (!(keys[id]?.Enabled ?? true))
            {
                RegisterHotKey(hWnd, id, (int)keys[id].modifiers, keys[id].vk);
                keys[id].Enabled = true;
            }
        }

        public void UnRegister(int id)
        {
            UnregisterHotKey(hWnd, id);
            keys[id].Enabled = false;
        }

        public void UnRegisterAll()
        {
            foreach (var key in keys)
            {
                UnregisterHotKey(hWnd, key.Key);
                keys[key.Key].Enabled = false;
            }
        }

        public bool Process(int msg, IntPtr wParam)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                KeyContext context = null;
                if (keys.TryGetValue(wParam.ToInt32(), out context))
                {
                    context.Process();
                    return true;
                }
            }
            return false;
        }



        public class KeyContext
        {
            Dictionary<int, KeyAction> actions = new Dictionary<int, KeyAction>();
            int curRepeatCount;
            public bool Enabled = true;
            public readonly HKModifiers modifiers;
            public readonly int vk;

            public KeyContext(bool reenterable, HKModifiers modifiers, int vk,
                            Dictionary<int, Action> actions, bool asynchronized = false)
            {
                this.modifiers = modifiers;
                this.vk = vk;
                this.actions = actions.ToDictionary(i => i.Key, i => new KeyAction(true, i.Value, asynchronized));
            }

            public void Process()
            {
                if (actions.Count > 0)
                {
                    curRepeatCount++;
                    if (actions.TryGetValue(curRepeatCount, out KeyAction keyAction))
                    {
                        lock (this)
                        {
                            if (keyAction.reenterCount == 0 ||
                                keyAction.reenterCount > 0 && keyAction.reenterable)
                            {
                                keyAction.reenterCount++;
                                if (keyAction.asynchronized)
                                {
                                    Task.Run(keyAction.action);
                                }
                                else
                                {
                                    keyAction.action();
                                }
                                curRepeatCount = 0;
                                keyAction.reenterCount--;
                            }
                        }
                    }

                }
            }
        }
    }
}
