using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ChassisAssembly.Models;

namespace ChassisAssembly.Controls
{
    /// <summary>
    /// 车体底架二维俯视图控件
    /// 核心职责:
    /// 1. 自绘底架轮廓 + 纵梁 + 横梁
    /// 2. 自绘所有工装点位(可点击)
    /// 3. 工装旁显示 Z / 力 数据框
    /// 4. 显示 2 台跟踪仪 LT_1 / LT_2 站位
    /// 5. 处理鼠标点击 → 触发 FixtureClicked 事件
    /// </summary>
    public class ChassisTopDownView : DoubleBufferedPanel
    {
        // ==================== 画布逻辑坐标系 ====================
        // 工装 CanvasX/CanvasY 是按 1000x260 的逻辑坐标布置的,渲染时按比例缩放到实际 panel 尺寸
        private const double LOGICAL_W = 1000;
        private const double LOGICAL_H = 260;

        // ==================== 外部数据绑定 ====================
        private List<FixturePoint> _fixtures = new List<FixturePoint>();
        public List<FixturePoint> Fixtures
        {
            get => _fixtures;
            set { _fixtures = value ?? new List<FixturePoint>(); Invalidate(); }
        }

        /// <summary>
        /// 当前步骤涉及的工装类型集合 (由外部根据 ProcessStep 设置)
        /// 为 null 或空则所有工装正常显示; 非空时只有这些类型亮显,其他变灰
        /// </summary>
        private HashSet<FixtureType> _highlightedTypes = null;
        public HashSet<FixtureType> HighlightedTypes
        {
            get => _highlightedTypes;
            set { _highlightedTypes = value; Invalidate(); }
        }

        public FixturePoint SelectedFixture { get; private set; }

        /// <summary>工装被点击时触发</summary>
        public event EventHandler<FixturePoint> FixtureClicked;

        // ==================== 画刷/画笔缓存 ====================
        private readonly Font _fontSmall = new Font("微软雅黑", 8F);
        private readonly Font _fontTiny  = new Font("微软雅黑", 7F);
        private readonly Font _fontDataMono = new Font("Consolas", 8F, FontStyle.Bold);
        private readonly Font _fontTitle = new Font("微软雅黑", 10F, FontStyle.Bold);
        private readonly Font _fontTracker = new Font("微软雅黑", 9F, FontStyle.Bold);

        // 状态色 - 与新版深色工业风配色一致
        private static readonly Color ClrBg         = AppColors.BgPrimary;
        private static readonly Color ClrChassis    = AppColors.ChassisOutline;    // 底架金色
        private static readonly Color ClrChassisFill = AppColors.ChassisFill;
        private static readonly Color ClrTracker    = AppColors.Warning;
        private static readonly Color ClrQualified  = AppColors.Success;
        private static readonly Color ClrMeasuring  = AppColors.Warning;
        private static readonly Color ClrOutOfTol   = AppColors.Danger;
        private static readonly Color ClrNotMeas    = AppColors.TextMuted;
        private static readonly Color ClrAccent     = AppColors.Primary;
        private static readonly Color ClrText       = AppColors.TextPrimary;
        private static readonly Color ClrTextSub    = AppColors.TextSecondary;

        public ChassisTopDownView()
        {
            BackColor = ClrBg;
            MouseClick += OnMouseClicked;
            MouseMove += OnMouseMoved;
            MouseWheel += OnMouseWheel;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;

            // 允许接收鼠标滚轮焦点
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
        }

        // ==================== 逻辑坐标 → 屏幕坐标映射 ====================
        // ==================== 缩放与平移 ====================
        private float _zoom = 1.0f;
        private float _panX = 0f;  // 屏幕坐标平移
        private float _panY = 0f;
        private Point _dragStart;
        private bool _isDragging;

        private float MapX(double cx) => (float)(cx / LOGICAL_W * Width * _zoom + _panX);
        private float MapY(double cy) => (float)(cy / LOGICAL_H * Height * _zoom + _panY);

        // 逆映射:屏幕 → 逻辑
        private double UnmapX(float sx) => (sx - _panX) / (_zoom * Width) * LOGICAL_W;
        private double UnmapY(float sy) => (sy - _panY) / (_zoom * Height) * LOGICAL_H;

        /// <summary>重置缩放与平移</summary>
        public void ResetView()
        {
            _zoom = 1.0f;
            _panX = 0f;
            _panY = 0f;
            Invalidate();
        }

        // ==================== 绘制主循环 ====================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawTitle(g);
            DrawChassisOutline(g);
            DrawCenterLine(g);
            DrawTrackers(g);
            DrawFixtures(g);
            DrawLegend(g);
            DrawViewHint(g);
        }

        private void DrawViewHint(Graphics g)
        {
            string hint = $"缩放 {_zoom * 100:F0}%  |  滚轮缩放  右键拖动  F键复位";
            using (var font = new Font("微软雅黑", 8F))
            using (var brush = new SolidBrush(ClrTextSub))
            {
                var size = g.MeasureString(hint, font);
                g.DrawString(hint, font, brush, Width - size.Width - 10, 8);
            }
        }

        private void DrawTitle(Graphics g)
        {
            using (var brush = new SolidBrush(ClrTextSub))
            {
                g.DrawString("俯视图 (Top View)", _fontTitle, brush, 10, 6);
            }
        }

        private void DrawChassisOutline(Graphics g)
        {
            // 底架两端有弧度的长条形 - 依据 HXD1C 图形简化
            float left = MapX(50);
            float right = MapX(950);
            float top = MapY(100);
            float bottom = MapY(160);
            float arc = MapX(50) - MapX(0); // 两端弧

            using (var path = new GraphicsPath())
            {
                path.AddLine(left, top, right, top);
                path.AddArc(right - arc / 2, top, arc, bottom - top, -90, 180);
                path.AddLine(right, bottom, left, bottom);
                path.AddArc(left - arc / 2, top, arc, bottom - top, 90, 180);
                path.CloseFigure();

                using (var fillBrush = new SolidBrush(Color.FromArgb(130, ClrChassisFill)))
                    g.FillPath(fillBrush, path);
                using (var pen = new Pen(ClrChassis, 1.5f))
                    g.DrawPath(pen, path);
            }

            // 纵梁线 (模拟 2 条边梁)
            using (var pen = new Pen(Color.FromArgb(120, ClrChassis), 1f))
            {
                g.DrawLine(pen, MapX(60), MapY(110), MapX(940), MapY(110));
                g.DrawLine(pen, MapX(60), MapY(150), MapX(940), MapY(150));
            }
        }

        private void DrawCenterLine(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(120, ClrAccent), 0.8f))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawLine(pen, MapX(60), MapY(130), MapX(940), MapY(130));
            }
        }

        private void DrawTrackers(Graphics g)
        {
            // 激光跟踪仪布置: 在底架两面的中间 — 一上 (LT_1) 一下 (LT_2)
            // 这种布置避免激光被底架两端遮挡,全长覆盖更均匀
            float cx = MapX(500);   // 纵向中线
            float topY = MapY(40);  // 底架上方中点 (底架本体大概 100-160)
            float botY = MapY(225); // 底架下方中点

            DrawTrackerMarker(g, cx, topY, "LT_1 (上站)", true);
            DrawTrackerMarker(g, cx, botY, "LT_2 (下站)", false);
        }

        /// <summary>绘制跟踪仪标记 — isTopSide 表示在底架上方(三角形朝下) 或下方(三角形朝上)</summary>
        private void DrawTrackerMarker(Graphics g, float x, float y, string label, bool isTopSide)
        {
            PointF[] triPts;
            float labelY;
            if (isTopSide)
            {
                // 底架上方的跟踪仪,三角形尖朝下
                triPts = new PointF[]
                {
                    new PointF(x, y + 14),
                    new PointF(x - 8, y),
                    new PointF(x + 8, y)
                };
                labelY = y - 18;
            }
            else
            {
                // 底架下方的跟踪仪,三角形尖朝上
                triPts = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x - 8, y + 14),
                    new PointF(x + 8, y + 14)
                };
                labelY = y + 18;
            }

            using (var brush = new SolidBrush(ClrTracker))
                g.FillPolygon(brush, triPts);

            using (var brush = new SolidBrush(ClrTracker))
            {
                var size = g.MeasureString(label, _fontTracker);
                g.DrawString(label, _fontTracker, brush, x - size.Width / 2, labelY);
            }
        }

        private void DrawFixtures(Graphics g)
        {
            foreach (var fx in _fixtures)
            {
                DrawFixture(g, fx);
            }
        }

        private void DrawFixture(Graphics g, FixturePoint fx)
        {
            float x = MapX(fx.CanvasX);
            float y = MapY(fx.CanvasY);

            // 计算该工装是否"在当前步骤涉及列表里"
            bool isHighlighted = _highlightedTypes == null
                              || _highlightedTypes.Count == 0
                              || _highlightedTypes.Contains(fx.FixtureType);
            bool isDimmed = !isHighlighted;

            Color stateColor = GetStatusColor(fx.Status);
            if (isDimmed)
            {
                // 变暗:整体降低饱和度
                stateColor = Color.FromArgb(80, stateColor);
            }

            // 选中虚框(最底层)
            if (fx.IsSelected)
            {
                using (var pen = new Pen(ClrAccent, 1.5f) { DashStyle = DashStyle.Dash })
                    g.DrawEllipse(pen, x - 17, y - 17, 34, 34);
            }

            // 高亮的工装加个橙色动感光环
            if (isHighlighted && _highlightedTypes != null && _highlightedTypes.Count > 0)
            {
                using (var pen = new Pen(Color.FromArgb(120, ClrMeasuring), 1.2f) { DashStyle = DashStyle.Dash })
                    g.DrawEllipse(pen, x - 20, y - 20, 40, 40);
            }

            // 点位圆圈
            int bgAlpha = isDimmed ? 80 : 255;
            using (var fillBrush = new SolidBrush(Color.FromArgb(bgAlpha, 240, 244, 248)))
                g.FillEllipse(fillBrush, x - 13, y - 13, 26, 26);
            using (var pen = new Pen(stateColor, isDimmed ? 1.5f : 2.5f))
                g.DrawEllipse(pen, x - 13, y - 13, 26, 26);

            // 小白点 (中心标志)
            using (var brush = new SolidBrush(stateColor))
                g.FillEllipse(brush, x - 2.5f, y - 2.5f, 5, 5);

            // 数据框 - 只有带力传感器且高亮的工装才显示
            if (isHighlighted && fx.HasForceSensor && fx.Status != PointStatus.NotMeasured)
            {
                DrawDataBox(g, x, y - 44, fx, GetStatusColor(fx.Status));
            }

            // 名称标签
            Color labelColor = isDimmed
                ? Color.FromArgb(80, ClrTextSub)
                : (isHighlighted && _highlightedTypes != null && _highlightedTypes.Count > 0
                    ? ClrMeasuring : ClrTextSub);
            using (var brush = new SolidBrush(labelColor))
            {
                var size = g.MeasureString(fx.Name, _fontTiny);
                g.DrawString(fx.Name, _fontTiny, brush, x - size.Width / 2, y + 16);
            }
        }

        private void DrawDataBox(Graphics g, float x, float y, FixturePoint fx, Color stateColor)
        {
            const float boxW = 70, boxH = 32;
            RectangleF rect = new RectangleF(x - boxW / 2, y - boxH / 2, boxW, boxH);

            using (var brush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                g.FillRectangle(brush, rect);
            using (var pen = new Pen(AppColors.Border, 1))
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            // Z 值
            using (var brush = new SolidBrush(ClrTextSub))
                g.DrawString("Z", _fontTiny, brush, rect.X + 4, rect.Y + 2);
            using (var brush = new SolidBrush(stateColor))
            {
                string zStr = fx.Status == PointStatus.Measuring ? "..." : $"{fx.ActualZ:F1}";
                var size = g.MeasureString(zStr, _fontDataMono);
                g.DrawString(zStr, _fontDataMono, brush, rect.Right - size.Width - 3, rect.Y + 2);
            }

            // F 值
            using (var brush = new SolidBrush(ClrTextSub))
                g.DrawString("F", _fontTiny, brush, rect.X + 4, rect.Y + 16);
            using (var brush = new SolidBrush(ClrText))
            {
                string fStr = fx.Status == PointStatus.Measuring ? "测中" : $"{fx.PressForce:F0}N";
                var size = g.MeasureString(fStr, _fontDataMono);
                g.DrawString(fStr, _fontDataMono, brush, rect.Right - size.Width - 3, rect.Y + 16);
            }
        }

        private void DrawLegend(Graphics g)
        {
            int lx = Width - 180;
            int ly = Height - 32;

            using (var bgBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                g.FillRectangle(bgBrush, lx - 6, ly - 6, 175, 24);
            using (var pen = new Pen(AppColors.Border, 1))
                g.DrawRectangle(pen, lx - 6, ly - 6, 175, 24);

            var items = new (Color, string)[]
            {
                (ClrQualified, "合格"),
                (ClrMeasuring, "测中"),
                (ClrOutOfTol, "超差"),
                (ClrNotMeas, "未测")
            };

            int step = 44;
            int offsetX = 0;
            foreach (var (color, label) in items)
            {
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, lx + offsetX, ly + 1, 8, 8);
                using (var brush = new SolidBrush(ClrTextSub))
                    g.DrawString(label, _fontTiny, brush, lx + offsetX + 11, ly - 1);
                offsetX += step;
            }
        }

        private Color GetStatusColor(PointStatus s)
        {
            switch (s)
            {
                case PointStatus.Qualified:      return ClrQualified;
                case PointStatus.Measuring:      return ClrMeasuring;
                case PointStatus.OutOfTolerance: return ClrOutOfTol;
                default: return ClrNotMeas;
            }
        }

        // ==================== 鼠标交互 ====================
        private void OnMouseClicked(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            // 若刚才在拖拽,不触发 click
            if (_isDragging) return;

            // Focus 获取,滚轮才会生效
            Focus();

            var hit = HitTest(e.Location);
            if (hit == null) return;

            if (SelectedFixture != null) SelectedFixture.IsSelected = false;
            SelectedFixture = hit;
            hit.IsSelected = true;
            Invalidate();
            FixtureClicked?.Invoke(this, hit);
        }

        private void OnMouseMoved(object sender, MouseEventArgs e)
        {
            if (_isDragging && (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle))
            {
                // 右键/中键按住拖拽
                _panX += e.X - _dragStart.X;
                _panY += e.Y - _dragStart.Y;
                _dragStart = e.Location;
                Invalidate();
                return;
            }

            var hit = HitTest(e.Location);
            Cursor = hit != null ? Cursors.Hand : Cursors.Default;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // 右键/中键按下 = 准备拖拽
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                _dragStart = e.Location;
                _isDragging = true;
                Cursor = Cursors.SizeAll;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                _isDragging = false;
                Cursor = Cursors.Default;

                // 右键拖拽距离很小,当作"复位缩放"快捷键(双击右键也可)
                // 这里做保守处理:不做复位,避免误触
            }
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            // 以鼠标指针位置为锚点缩放
            float oldZoom = _zoom;
            float zoomFactor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
            float newZoom = Math.Max(0.3f, Math.Min(5.0f, _zoom * zoomFactor));
            if (Math.Abs(newZoom - oldZoom) < 1e-4) return;

            // 保持鼠标点下方的逻辑坐标不变
            // oldScreen = oldLogical * W * oldZoom + oldPan
            // newScreen = oldLogical * W * newZoom + newPan = oldScreen  (我们希望光标点不动)
            // 所以 newPan = oldScreen - oldLogical * W * newZoom
            //          = oldScreen - (oldScreen - oldPan) / oldZoom * newZoom
            //          = oldScreen * (1 - newZoom/oldZoom) + oldPan * (newZoom/oldZoom)
            float ratio = newZoom / oldZoom;
            _panX = e.X * (1 - ratio) + _panX * ratio;
            _panY = e.Y * (1 - ratio) + _panY * ratio;
            _zoom = newZoom;
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            // 让方向键和 F 键也能被控件处理
            if (keyData == Keys.F || keyData == Keys.Home) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // F 键或 Home 键 = 复位视图
            if (e.KeyCode == Keys.F || e.KeyCode == Keys.Home)
            {
                ResetView();
                e.Handled = true;
            }
        }

        private FixturePoint HitTest(Point clickPoint)
        {
            foreach (var fx in _fixtures)
            {
                float x = MapX(fx.CanvasX);
                float y = MapY(fx.CanvasY);
                double dx = clickPoint.X - x;
                double dy = clickPoint.Y - y;
                double r = 13 * _zoom; // 半径随缩放调整
                if (dx * dx + dy * dy <= r * r)
                    return fx;
            }
            return null;
        }
    }
}
