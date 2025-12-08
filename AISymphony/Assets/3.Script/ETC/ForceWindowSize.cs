using System;
using System.Runtime.InteropServices;

class ForceWindowed
{
    const int GWL_STYLE = -16;
    const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
    const int SWP_FRAMECHANGED = 0x0020;
    const int SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    public static void Force()
    {
        IntPtr hWnd = FindWindow(null, "AISymphony");

        // 창 모드 스타일로 강제 변경
        SetWindowLong(hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW);

        // 창 크기 강제 설정
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 3315, 1200,
            SWP_FRAMECHANGED | SWP_SHOWWINDOW);
    }
}
