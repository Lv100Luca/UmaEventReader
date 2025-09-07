using System.ComponentModel;
namespace UmaEventReader;

using System.Drawing;
using System.Windows.Forms;

public class SelectionForm : Form
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle SelectedRegion { get; private set; }
    private Point _startPoint;
    private Rectangle _currentRect;

    public SelectionForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.LightGray;
        this.Opacity = 0.25;
        this.TopMost = true;
        this.WindowState = FormWindowState.Maximized;
        this.DoubleBuffered = true;
        this.Cursor = Cursors.Cross;

        this.MouseDown += SelectionForm_MouseDown;
        this.MouseMove += SelectionForm_MouseMove;
        this.MouseUp += SelectionForm_MouseUp;
    }

    private void SelectionForm_MouseDown(object sender, MouseEventArgs e)
    {
        _startPoint = e.Location;
        _currentRect = new Rectangle(e.Location, new Size(0, 0));
    }

    private void SelectionForm_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        int x = Math.Min(e.X, _startPoint.X);
        int y = Math.Min(e.Y, _startPoint.Y);
        int width = Math.Abs(e.X - _startPoint.X);
        int height = Math.Abs(e.Y - _startPoint.Y);

        _currentRect = new Rectangle(x, y, width, height);
        this.Invalidate(); // triggers repaint
    }

    private void SelectionForm_MouseUp(object sender, MouseEventArgs e)
    {
        SelectedRegion = _currentRect;
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_currentRect != Rectangle.Empty)
        {
            using var pen = new Pen(Color.Red, 2);
            e.Graphics.DrawRectangle(pen, _currentRect);
        }
    }
}