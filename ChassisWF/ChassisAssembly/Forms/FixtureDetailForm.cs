using System;
using System.Drawing;
using System.Windows.Forms;
using ChassisAssembly.Controls;
using ChassisAssembly.Models;

namespace ChassisAssembly.Forms
{
    /// <summary>
    /// 工装详情 Modal 弹窗
    /// 点击二维图工装后弹出,用户看完手动关闭
    /// 内部复用 FixtureDetailPanel 控件,只是外面包一层 Form
    /// </summary>
    public class FixtureDetailForm : Form
    {
        private readonly FixtureDetailPanel _panel;

        public event EventHandler<FixturePoint> MeasureRequested;
        public event EventHandler<FixturePoint> PositionRequested;

        public FixtureDetailForm()
        {
            Text = "工装详情";
            Size = new Size(360, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppColors.BgSecondary;
            ForeColor = AppColors.TextPrimary;
            Font = new Font("微软雅黑", 9F);
            ShowInTaskbar = false;

            _panel = new FixtureDetailPanel
            {
                Dock = DockStyle.Fill
            };
            _panel.CloseRequested += (s, e) => Close();
            _panel.MeasureRequested += (s, fx) => MeasureRequested?.Invoke(this, fx);
            _panel.PositionRequested += (s, fx) => PositionRequested?.Invoke(this, fx);
            Controls.Add(_panel);
        }

        /// <summary>更新绑定的工装</summary>
        public void LoadFixture(FixturePoint fx)
        {
            if (fx == null) { Close(); return; }
            Text = $"工装详情 - {fx.Name}";
            _panel.LoadFixture(fx);
        }
    }
}
