using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class InputManager
    {
        private static Boolean _altPressed = false;
        private List<short> _alreadyDown = new List<short>();
        public struct INPUT
        {
            internal uint type;
            internal InputUnion U;
            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal uint dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal short wVk;
            internal short wScan;
            internal uint dwFlags;
            internal int time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            internal int uMsg;
            internal short wParamL;
            internal short wParamH;
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        private enum MouseMessages
        {
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_LBUTTONDBLCLK = 0x203,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_RBUTTONDBLCLK = 0x206,
            WM_MBUTTONDOWN = 0x207,
            WM_MBUTTONUP = 0x208,
            WM_MBUTTONDBLCLK = 0x209,
            WM_MOUSEWHEEL = 0x020A,
            WM_XBUTTONDOWN = 0x20B,
            WM_XBUTTONUP = 0x20C,
            WM_XBUTTONDBLCLK = 0x20D,
            WM_MOUSEHWHEEL = 0x20E
        }

        private enum MouseEvents
        {
            MOUSEEVENTF_ABSOLUTE = 0x8000,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_HWHEEL = 0x01000
        }

        public void ProcessInputMouse(byte[] data)
        {
            int x = BitConverter.ToInt32(data, sizeof(bool));
            int y = BitConverter.ToInt32(data, sizeof(bool) + sizeof(Int32));
            int wParam = BitConverter.ToInt32(data, sizeof(bool) + 2 * sizeof(Int32));
            short mouseData = BitConverter.ToInt16(data, sizeof(bool) + 3 * sizeof(Int32));
            InputManager.INPUT[] inputs = new InputManager.INPUT[1];
            inputs[0].type = 0;
            inputs[0].U.mi.dx = x;
            inputs[0].U.mi.dy = y;
            inputs[0].U.mi.time = 0;
            inputs[0].U.mi.dwFlags = (uint)(InputManager.MouseEvents.MOUSEEVENTF_ABSOLUTE); // TODO struct mouseevents
            switch (wParam)
            {
                case (int)InputManager.MouseMessages.WM_LBUTTONDOWN:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_LEFTDOWN;
                    break;
                case (int)InputManager.MouseMessages.WM_LBUTTONUP:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_LEFTUP;
                    break;
                case (int)InputManager.MouseMessages.WM_RBUTTONDOWN:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_RIGHTDOWN;
                    break;
                case (int)InputManager.MouseMessages.WM_RBUTTONUP:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_RIGHTUP;
                    break;
                case (int)InputManager.MouseMessages.WM_MOUSEMOVE:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_MOVE;
                    break;
                case (int)InputManager.MouseMessages.WM_MBUTTONDOWN:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_MIDDLEDOWN;
                    break;
                case (int)InputManager.MouseMessages.WM_MBUTTONUP:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_MIDDLEUP;
                    break;
                case (int)InputManager.MouseMessages.WM_LBUTTONDBLCLK:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_LEFTDOWN | (uint)InputManager.MouseEvents.MOUSEEVENTF_LEFTUP;
                    break;
                case (int)InputManager.MouseMessages.WM_RBUTTONDBLCLK:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_RIGHTDOWN | (uint)InputManager.MouseEvents.MOUSEEVENTF_RIGHTUP;
                    break;
                case (int)InputManager.MouseMessages.WM_MBUTTONDBLCLK:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_MIDDLEDOWN | (uint)InputManager.MouseEvents.MOUSEEVENTF_MIDDLEUP;
                    break;
                case (int)InputManager.MouseMessages.WM_MOUSEWHEEL:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_WHEEL;
                    inputs[0].U.mi.mouseData = mouseData;
                    break;
                case (int)InputManager.MouseMessages.WM_MOUSEHWHEEL:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_HWHEEL;
                    inputs[0].U.mi.mouseData = mouseData;
                    break;
                case (int)InputManager.MouseMessages.WM_XBUTTONDOWN:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_XDOWN;
                    break;
                case (int)InputManager.MouseMessages.WM_XBUTTONUP:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_XUP;
                    break;
                case (int)InputManager.MouseMessages.WM_XBUTTONDBLCLK:
                    inputs[0].U.mi.dwFlags = inputs[0].U.mi.dwFlags | (uint)InputManager.MouseEvents.MOUSEEVENTF_XDOWN | (uint)InputManager.MouseEvents.MOUSEEVENTF_XUP;
                    break;
            }
            SendInput(1, inputs, Marshal.SizeOf((object) inputs[0]));
            if (wParam == (int)InputManager.MouseMessages.WM_LBUTTONDBLCLK || wParam == (int)InputManager.MouseMessages.WM_RBUTTONDBLCLK ||
                wParam == (int)InputManager.MouseMessages.WM_MBUTTONDBLCLK || wParam == (int)InputManager.MouseMessages.WM_XBUTTONDBLCLK)
            {
                SendInput(1, inputs, Marshal.SizeOf((object) inputs[0]));    //resend the event if doubleclick
            }
        }

        public void ProcessInputKeyboard(byte[] data)
        {
            bool keyUp = BitConverter.ToBoolean(data, sizeof(bool));
            short vkCode = BitConverter.ToInt16(data, 2 * sizeof(bool));
            if (!keyUp && (vkCode == 164 || vkCode == 165))
            {
                // MessageBox.Show("alt pressed");
                _altPressed = true;
            }

            else if (keyUp && (vkCode == 164 || vkCode == 165))
            {
                _altPressed = false;
                _alreadyDown.Clear();
            }


            InputManager.INPUT[] inputs = new InputManager.INPUT[1];
            inputs[0].type = 1; // 1 = keyboard, 0 = mouse
            inputs[0].U.ki.dwFlags = keyUp ? (uint)2 : (uint)0;
            inputs[0].U.ki.wVk = vkCode;
            inputs[0].U.ki.time = 0;
            if (_altPressed && !keyUp && vkCode != 164 && vkCode != 165)
            {
                if (_alreadyDown.Contains(vkCode))
                {
                    inputs[0].U.ki.dwFlags = 2;
                    _alreadyDown.Remove(vkCode);
                }
                else
                {
                    _alreadyDown.Add(vkCode);
                }
                // MessageBox.Show("alt pressed");

            }

            //Console.WriteLine("alt NOT pressed");
            SendInput(1, inputs, Marshal.SizeOf((object) inputs[0]));
            // Console.WriteLine("babba " + vkCode);
        }
    }
}
