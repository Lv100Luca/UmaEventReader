using System.Runtime.InteropServices;

public class OverlayForm : Form
{
    private readonly IEnumerable<Rectangle> _rectangles;

    public OverlayForm(IEnumerable<Rectangle> rects)
    {
        _rectangles = rects;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Lime;
        TransparencyKey = Color.Lime;
        WindowState = FormWindowState.Maximized;

        // Make window click-through
        int initialStyle = GetWindowLong(Handle, GwlExstyle);
        SetWindowLong(Handle, GwlExstyle, initialStyle | WsExTransparent | WsExLayered);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (Pen pen = new Pen(Color.Black, 2))
        {
            foreach (var rect in _rectangles)
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }

    // WinAPI
    const int GwlExstyle = -20;
    const int WsExTransparent = 0x20;
    const int WsExLayered = 0x80000;

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}