using System.Windows.Forms;

namespace ChassisAssembly.Controls
{
    /// <summary>
    /// 双缓冲 Panel - 消除自绘闪烁
    /// 完全照搬老程序 GeneratorAlignmentControl.DoubleBufferedPanel
    /// </summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }
    }
}
