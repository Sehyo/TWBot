using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

namespace TW_Bot
{
    class SendMessage
    {
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        public static void SendKeystroke(IntPtr hWnd)
        {
            const uint WM_KEYDOWN = 0x100;
            const uint WM_KEYUP = 0x0101;

            PostMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.D2), IntPtr.Zero);
            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.D2), IntPtr.Zero);
            //IntPtr edit = P.MainWindowHandle;
            //PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.Control), IntPtr.Zero);
            //PostMessage(edit, WM_KEYDOWN, (IntPtr)(Keys.A), IntPtr.Zero);
            //PostMessage(edit, WM_KEYUP, (IntPtr)(Keys.Control), IntPtr.Zero);
        }

    }
}
