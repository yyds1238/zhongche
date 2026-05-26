using System;
using System.Drawing;
using System.Windows.Forms;
using ChassisAssembly.Models;
using ChassisAssembly.Services;

namespace ChassisAssembly.Controls
{
    /// <summary>
    /// 工装详情面板 - 点击二维图工装后弹出,显示详细状态
    /// 以 UserControl 形式存在,由宿主控件决定如何显示(侧滑/悬浮/嵌入)
    /// </summary>
    public class FixtureDetailPanel : UserControl
    {
        private FixturePoint _fixture;
        private Label _lblName, _lblType, _lblId, _lblTracker;
        private Label _lblTheoretical, _lblActual, _lblDelta;
        private Label _lblForce, _lblDeformation, _lblLastMeasured;
        private Label _lblStatus;
        private Panel _statusBar;
        private Button _btnMeasure, _btnPosition, _btnClose;
        private Panel _historyContainer;

        public event EventHandler CloseRequested;
        public event EventHandler<FixturePoint> MeasureRequested;
        public event EventHandler<FixturePoint> PositionRequested;

        public FixtureDetailPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Width = 320;
            BackColor = AppColors.BgSecondary;
            Padding = new Padding(0);

            // ====== 顶部标题栏 ======
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AppColors.BgSecondary,
                Padding = new Padding(14, 0, 6, 0)
            };
            Controls.Add(pnlHeader);

            _lblName = new Label
            {
                Text = "工装详情",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlHeader.Controls.Add(_lblName);

            _btnClose = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 38,
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 12F),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
            pnlHeader.Controls.Add(_btnClose);

            // ====== 状态指示条 ======
            _statusBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = AppColors.TextMuted,
                Padding = new Padding(14, 0, 14, 0)
            };
            Controls.Add(_statusBar);

            _lblStatus = new Label
            {
                Text = "未测",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusBar.Controls.Add(_lblStatus);

            // ====== 主内容区 ======
            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.BgSecondary,
                AutoScroll = true,
                Padding = new Padding(14, 10, 14, 10)
            };
            Controls.Add(pnlContent);
            pnlContent.BringToFront();

            int y = 0;

            // --- 基本信息 ---
            y = AddSectionTitle(pnlContent, "基本信息", y);
            _lblId = AddInfoRow(pnlContent, "编号", "—", y); y += 24;
            _lblType = AddInfoRow(pnlContent, "类型", "—", y); y += 24;
            _lblTracker = AddInfoRow(pnlContent, "跟踪仪", "—", y); y += 32;

            // --- 位置信息 ---
            y = AddSectionTitle(pnlContent, "位置信息 (mm)", y);

            _lblTheoretical = new Label
            {
                Text = "理论位置  X: —   Y: —   Z: —",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Consolas", 9F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent
            };
            pnlContent.Controls.Add(_lblTheoretical);
            y += 22;

            _lblActual = new Label
            {
                Text = "实测位置  X: —   Y: —   Z: —",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Consolas", 9F),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent
            };
            pnlContent.Controls.Add(_lblActual);
            y += 22;

            _lblDelta = new Label
            {
                Text = "偏差 Δ    X: —   Y: —   Z: —",
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                ForeColor = AppColors.Warning,
                BackColor = Color.Transparent
            };
            pnlContent.Controls.Add(_lblDelta);
            y += 32;

            // --- 传感器数据 ---
            y = AddSectionTitle(pnlContent, "传感器数据", y);
            _lblForce = AddInfoRow(pnlContent, "压紧力", "—", y); y += 24;
            _lblDeformation = AddInfoRow(pnlContent, "形变", "—", y); y += 32;

            // --- 时间戳 ---
            y = AddSectionTitle(pnlContent, "时间", y);
            _lblLastMeasured = AddInfoRow(pnlContent, "最后测量", "—", y); y += 32;

            // --- 操作按钮 ---
            y = AddSectionTitle(pnlContent, "操作", y);

            _btnMeasure = new Button
            {
                Text = "▶ 单点测量",
                Location = new Point(0, y),
                Size = new Size(140, 32),
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            _btnMeasure.FlatAppearance.BorderSize = 0;
            _btnMeasure.Click += (s, e) =>
            {
                if (_fixture != null)
                    MeasureRequested?.Invoke(this, _fixture);
            };
            pnlContent.Controls.Add(_btnMeasure);

            _btnPosition = new Button
            {
                Text = "📍 下发定位",
                Location = new Point(150, y),
                Size = new Size(140, 32),
                BackColor = AppColors.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            _btnPosition.FlatAppearance.BorderSize = 0;
            _btnPosition.Click += (s, e) =>
            {
                if (_fixture != null)
                    PositionRequested?.Invoke(this, _fixture);
            };
            pnlContent.Controls.Add(_btnPosition);

            y += 44;

            // --- 测量历史 ---
            y = AddSectionTitle(pnlContent, "最近测量记录", y);
            _historyContainer = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(290, 120),
                BackColor = AppColors.BgSecondary,
                AutoScroll = true,
                BorderStyle = BorderStyle.None
            };
            pnlContent.Controls.Add(_historyContainer);
        }

        private int AddSectionTitle(Panel parent, string title, int y)
        {
            var lbl = new Label
            {
                Text = title,
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = AppColors.Primary,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lbl);
            return y + 22;
        }

        private Label AddInfoRow(Panel parent, string label, string value, int y)
        {
            var lblKey = new Label
            {
                Text = label,
                Location = new Point(0, y),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblKey);

            var lblVal = new Label
            {
                Text = value,
                Location = new Point(85, y),
                AutoSize = true,
                Font = new Font("Consolas", 9F),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblVal);
            return lblVal;
        }

        /// <summary>绑定工装,刷新所有字段</summary>
        public void LoadFixture(FixturePoint fx)
        {
            _fixture = fx;
            if (fx == null) return;

            _lblName.Text = fx.Name;
            _lblId.Text = fx.Id;
            _lblType.Text = GetFixtureTypeName(fx.FixtureType);
            _lblTracker.Text = fx.AssignedTracker;

            _lblTheoretical.Text = $"理论位置  X: {fx.TheoreticalX,8:F1}   Y: {fx.TheoreticalY,8:F1}   Z: {fx.TheoreticalZ,8:F1}";

            if (fx.Status == PointStatus.NotMeasured)
            {
                _lblActual.Text = "实测位置  X: —        Y: —        Z: —";
                _lblDelta.Text  = "偏差 Δ    X: —        Y: —        Z: —";
            }
            else
            {
                _lblActual.Text = $"实测位置  X: {fx.ActualX,8:F3}   Y: {fx.ActualY,8:F3}   Z: {fx.ActualZ,8:F3}";
                _lblDelta.Text  = $"偏差 Δ    X: {fx.DeltaX,+8:+0.000;-0.000;0.000}   Y: {fx.DeltaY,+8:+0.000;-0.000;0.000}   Z: {fx.DeltaZ,+8:+0.000;-0.000;0.000}";
            }

            _lblForce.Text = fx.HasForceSensor
                ? (fx.Status == PointStatus.NotMeasured ? "—" : $"{fx.PressForce:F0} N")
                : "(无力传感器)";
            _lblDeformation.Text = fx.HasForceSensor
                ? (fx.Status == PointStatus.NotMeasured ? "—" : $"{fx.Deformation:F1} μm")
                : "(无形变传感器)";

            _lblLastMeasured.Text = fx.LastMeasuredAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

            // 状态条
            switch (fx.Status)
            {
                case PointStatus.Qualified:
                    _statusBar.BackColor = AppColors.Success;
                    _lblStatus.Text = "● 合格";
                    break;
                case PointStatus.Measuring:
                    _statusBar.BackColor = AppColors.Warning;
                    _lblStatus.Text = "● 测量中";
                    break;
                case PointStatus.OutOfTolerance:
                    _statusBar.BackColor = AppColors.Danger;
                    _lblStatus.Text = $"● 超差  (综合偏差 {fx.DeviationNorm:F3} mm)";
                    break;
                default:
                    _statusBar.BackColor = AppColors.TextMuted;
                    _lblStatus.Text = "● 未测";
                    break;
            }

            // 定位按钮启用状态
            _btnPosition.Enabled = fx.HasForceSensor;

            RefreshHistory();
        }

        private void RefreshHistory()
        {
            _historyContainer.Controls.Clear();
            if (_fixture == null) return;

            var project = AppState.Instance.CurrentProject;
            if (project == null) return;

            int y = 4;
            int count = 0;
            for (int i = project.Measurements.Count - 1; i >= 0 && count < 8; i--)
            {
                var m = project.Measurements[i];
                if (m.FixtureId != _fixture.Id) continue;

                var row = new Label
                {
                    Location = new Point(6, y),
                    AutoSize = true,
                    Font = new Font("Consolas", 8.5F),
                    ForeColor = AppColors.TextPrimary,
                    BackColor = Color.Transparent,
                    Text = $"{m.Timestamp:HH:mm:ss}  {m.TrackerName}  X={m.X:F2} Y={m.Y:F2} Z={m.Z:F2}"
                };
                _historyContainer.Controls.Add(row);
                y += 18;
                count++;
            }

            if (count == 0)
            {
                var lbl = new Label
                {
                    Text = "(尚无测量记录)",
                    Location = new Point(6, 4),
                    AutoSize = true,
                    Font = new Font("微软雅黑", 9F, FontStyle.Italic),
                    ForeColor = AppColors.TextMuted,
                    BackColor = Color.Transparent
                };
                _historyContainer.Controls.Add(lbl);
            }
        }

        private static string GetFixtureTypeName(FixtureType t)
        {
            switch (t)
            {
                case FixtureType.BolsterPress:         return "枕梁压紧装置";
                case FixtureType.TransformerBeamPress: return "变压器梁压紧装置";
                case FixtureType.TractionLongPress:    return "牵引梁纵向压紧";
                case FixtureType.TractionLatPress:     return "牵引梁横向压紧";
                case FixtureType.SideBeamPress:        return "边梁压紧装置";
                case FixtureType.CenterBeamFixture:    return "中心纵梁工装";
                case FixtureType.PartitionBeamFixture: return "隔墙梁工装";
                case FixtureType.LongitudinalDatum:    return "纵向基准装置";
                case FixtureType.LateralDatum:         return "横向基准装置";
                case FixtureType.PiercingSupport:      return "贯穿梁支撑座";
                default: return t.ToString();
            }
        }
    }
}
