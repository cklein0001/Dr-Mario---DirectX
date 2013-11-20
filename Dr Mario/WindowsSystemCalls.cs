// System
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace WindowsFramework
{
    public class WindowsAPI
    {
        #region Windows Native Methods

        [System.Security.SuppressUnmanagedCodeSecurityAttribute] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool PeekMessage(out Message msg, IntPtr hWnd, UInt32 msgFilterMin, UInt32 msgFilterMax, UInt32 flags);

        [System.Security.SuppressUnmanagedCodeSecurityAttribute] // We won't use this maliciously
        [DllImport("kernel32.dll")]
        public static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);

        [System.Security.SuppressUnmanagedCodeSecurityAttribute] // We won't use this maliciously
        [DllImport("kernel32.dll")]
        public static extern bool QueryPerformanceCounter(ref long PerformanceCount);

        // Interop to call get device caps
        [System.Security.SuppressUnmanagedCodeSecurityAttribute] // We won't use this maliciously
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int cap);
        #endregion
    }
}