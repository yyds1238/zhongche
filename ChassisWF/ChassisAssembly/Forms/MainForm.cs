using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChassisAssembly.Controls;
using ChassisAssembly.Models;
using ChassisAssembly.Services;

namespace ChassisAssembly.Forms
{
    /// <summary>
    /// 主窗口 (2026.04.27 重构 - 取消通用测量, 改为单列嵌套展开式导航)
    /// 布局:
    ///   顶部栏 (菜单+项目信息+设备状态)
    ///   左侧导航 (180px 宽,嵌套展开)
    ///   主工作区 (Fill,直接显示当前步骤详情)
    ///   底部状态栏
    /// </summary>
    public class MainForm : Form
    {
        private readonly ProjectFile _project;

        // 顶部
        private Label _lblProjectName;
        private Label _lblVehicleType;
        private FlowLayoutPanel _pnlBadges;
        private Dictionary<string, Label> _badges = new Dictionary<string, Label>();

        // 左侧导航 (单列嵌套式)
        private Panel _pnlNav;
        private Dictionary<ProcessStep, Button> _stepButtons = new Dictionary<ProcessStep, Button>();
        private Button _hdrCommon;   // "通用公共工序" 父节点
        private Button _hdrVehicle;  // "车型装配工序" 父节点
        private bool _commonExpanded = true;
        private bool _vehicleExpanded = true;

        // 主工作区 - 一个 ChassisAssemblyControl 即可,通过 SwitchToStep 切换
        private Panel _pnlWorkspace;
        private ChassisAssemblyControl _ctrlMain;

        // 底部
        private Label _lblStatusReady;

        public MainForm(ProjectFile project)
        {
            _project = project;
            AppState.Instance.CurrentProject = project;
            InitializeComponent();
            AppState.Instance.TrackerStatusChanged += (s, name) => RefreshTrackerBadges();
        }

        private void InitializeComponent()
        {
            Text = $"车体底架自动化拼装系统 v1.0 - {_project.ProjectName}";
            Size = new Size(1600, 900);
            MinimumSize = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = AppColors.BgPrimary;
            ForeColor = AppColors.TextPrimary;
            Font = new Font("微软雅黑", 9F);

            InitStatusBar();
            InitTopBar();
            InitNavPanel();
            InitWorkspace();

            // 加载到默认步骤(第一个 Active 步骤)
            _ctrlMain.LoadProject(_project);
            HighlightCurrentStep();
        }

        // ============================================================
        // 底部状态栏
        // ============================================================
        private void InitStatusBar()
        {
            var pnlStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = AppColors.BgSecondary
            };
            Controls.Add(pnlStatus);
            pnlStatus.Controls.Add(new Panel
            {
                Dock = DockStyle.Top, Height = 1, BackColor = AppColors.Border
            });

            _lblStatusReady = new Label
            {
                Text = "● 就绪",
                Location = new Point(14, 5),
                AutoSize = true,
                ForeColor = AppColors.Success,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F)
            };
            pnlStatus.Controls.Add(_lblStatusReady);

            var lblFile = new Label
            {
                Text = $"项目文件: {_project.ProjectName}.cap",
                Location = new Point(90, 5),
                AutoSize = true,
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F)
            };
            pnlStatus.Controls.Add(lblFile);

            var lblCopy = new Label
            {
                Text = "中科工业人工智能研究院 © 2026",
                AutoSize = true,
                ForeColor = AppColors.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlStatus.Controls.Add(lblCopy);
            pnlStatus.Resize += (s, e) =>
            {
                lblCopy.Location = new Point(pnlStatus.Width - lblCopy.Width - 14, 5);
            };
        }

        // ============================================================
        // 顶部栏
        // ============================================================
        private void InitTopBar()
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = AppColors.BgPrimary
            };
            Controls.Add(pnlTop);
            pnlTop.Controls.Add(new Panel
            {
                Dock = DockStyle.Bottom, Height = 1, BackColor = AppColors.Border
            });

            // 菜单栏
            var menu = new MenuStrip
            {
                Dock = DockStyle.Top,
                BackColor = AppColors.BgPrimary,
                ForeColor = AppColors.TextPrimary,
                Font = new Font("微软雅黑", 9F),
                RenderMode = ToolStripRenderMode.System
            };
            var miFile = new ToolStripMenuItem("文件(&F)");
            miFile.DropDownItems.Add("新建项目", null, (s, e) => MessageBox.Show("请重启软件后选择新建", "提示"));
            miFile.DropDownItems.Add("打开项目...", null, (s, e) => MessageBox.Show("项目打开功能待实现", "提示"));
            miFile.DropDownItems.Add("保存", null, OnSaveProject);
            miFile.DropDownItems.Add(new ToolStripSeparator());
            miFile.DropDownItems.Add("退出", null, (s, e) => Close());
            menu.Items.Add(miFile);

            var miHelp = new ToolStripMenuItem("帮助(&H)");
            miHelp.DropDownItems.Add("关于", null, (s, e) =>
                MessageBox.Show("车体底架自动化拼装系统 v3.2\n\n中科工业人工智能研究院\n2026", "关于"));
            menu.Items.Add(miHelp);
            pnlTop.Controls.Add(menu);

            // 项目信息行
            var lblP = new Label { Text = "当前项目:", Location = new Point(14, 40), AutoSize = true,
                ForeColor = AppColors.TextSecondary, BackColor = Color.Transparent };
            pnlTop.Controls.Add(lblP);

            _lblProjectName = new Label
            {
                Text = _project.ProjectName,
                Location = new Point(84, 38), AutoSize = true,
                ForeColor = AppColors.TextPrimary, BackColor = AppColors.BgSecondary,
                Padding = new Padding(10, 4, 10, 4)
            };
            pnlTop.Controls.Add(_lblProjectName);

            var lblV = new Label { Text = "车型:", Location = new Point(320, 40), AutoSize = true,
                ForeColor = AppColors.TextSecondary, BackColor = Color.Transparent };
            pnlTop.Controls.Add(lblV);

            _lblVehicleType = new Label
            {
                Text = ProjectService.GetVehicleDisplayName(_project.VehicleType),
                Location = new Point(360, 40), AutoSize = true,
                ForeColor = AppColors.Primary,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            pnlTop.Controls.Add(_lblVehicleType);

            // 设备监控徽章 (只 LT_1, LT_2)
            _pnlBadges = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                Location = new Point(0, 36),
                BackColor = Color.Transparent
            };
            pnlTop.Controls.Add(_pnlBadges);

            AddBadge("LT_1");
            AddBadge("LT_2");

            var lblMon = new Label
            {
                Text = "设备监控:",
                AutoSize = true,
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Margin = new Padding(4, 5, 8, 0)
            };
            _pnlBadges.Controls.Add(lblMon);

            pnlTop.Resize += (s, e) =>
            {
                _pnlBadges.Location = new Point(pnlTop.Width - _pnlBadges.Width - 14, 36);
            };
            pnlTop.PerformLayout();
            _pnlBadges.Location = new Point(pnlTop.Width - _pnlBadges.Width - 14, 36);
        }

        private void AddBadge(string name)
        {
            var lbl = new Label
            {
                Text = "○ " + name,
                AutoSize = true,
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextMuted,
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(3, 0, 3, 0),
                Font = new Font("微软雅黑", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pnlBadges.Controls.Add(lbl);
            _badges[name] = lbl;
        }

        private void RefreshTrackerBadges()
        {
            UpdateBadge("LT_1", AppState.Instance.TrackerStatuses["LT_1"]);
            UpdateBadge("LT_2", AppState.Instance.TrackerStatuses["LT_2"]);
        }

        private void UpdateBadge(string name, ConnectionStatus status)
        {
            if (!_badges.ContainsKey(name)) return;
            var lbl = _badges[name];
            if (lbl.InvokeRequired)
            {
                lbl.Invoke(new Action(() => UpdateBadge(name, status)));
                return;
            }
            switch (status)
            {
                case ConnectionStatus.Connected:
                    lbl.Text = "● " + name; lbl.ForeColor = AppColors.Success; lbl.BackColor = AppColors.SuccessBg;
                    break;
                case ConnectionStatus.Connecting:
                    lbl.Text = "● " + name; lbl.ForeColor = AppColors.Warning; lbl.BackColor = AppColors.WarningBg;
                    break;
                default:
                    lbl.Text = "○ " + name; lbl.ForeColor = AppColors.TextMuted; lbl.BackColor = AppColors.BgSecondary;
                    break;
            }
        }

        // ============================================================
        // 左侧导航 - 单列嵌套展开
        // ============================================================
        private void InitNavPanel()
        {
            _pnlNav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = AppColors.NavBg,
                AutoScroll = true
            };
            Controls.Add(_pnlNav);

            _pnlNav.Controls.Add(new Panel
            {
                Dock = DockStyle.Right, Width = 1, BackColor = AppColors.Border
            });

            BuildNavTree();
        }

        private void BuildNavTree()
        {
            _pnlNav.Controls.Clear();
            _pnlNav.Controls.Add(new Panel
            {
                Dock = DockStyle.Right, Width = 1, BackColor = AppColors.Border
            });
            _stepButtons.Clear();

            int y = 12;

            // ===== 父节点: 通用公共工序 =====
            _hdrCommon = CreateGroupHeader(
                _commonExpanded ? "▼  通用公共工序" : "▶  通用公共工序",
                y);
            _hdrCommon.Click += (s, e) =>
            {
                _commonExpanded = !_commonExpanded;
                BuildNavTree();
                HighlightCurrentStep();
            };
            _pnlNav.Controls.Add(_hdrCommon);
            y += 36;

            if (_commonExpanded)
            {
                foreach (var def in AssemblyProcessDef.CommonSteps)
                {
                    var btn = CreateStepNavItem(def, y);
                    _stepButtons[def.Step] = btn;
                    _pnlNav.Controls.Add(btn);
                    y += 38;
                }
                y += 4;
            }

            y += 4;

            // ===== 父节点: 车型装配工序 =====
            _hdrVehicle = CreateGroupHeader(
                _vehicleExpanded ? "▼  车型装配工序 (HXD1C)" : "▶  车型装配工序 (HXD1C)",
                y);
            _hdrVehicle.Click += (s, e) =>
            {
                _vehicleExpanded = !_vehicleExpanded;
                BuildNavTree();
                HighlightCurrentStep();
            };
            _pnlNav.Controls.Add(_hdrVehicle);
            y += 36;

            if (_vehicleExpanded)
            {
                foreach (var def in AssemblyProcessDef.AssemblySteps)
                {
                    var btn = CreateStepNavItem(def, y);
                    _stepButtons[def.Step] = btn;
                    _pnlNav.Controls.Add(btn);
                    y += 38;
                }
            }
        }

        private Button CreateGroupHeader(string text, int top)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(8, top),
                Size = new Size(180, 30),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Font = new Font("微软雅黑", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AppColors.BgDarkAccent;
            return btn;
        }

        private Button CreateStepNavItem(AssemblyStepDef def, int top)
        {
            string label = $"   {def.Number} {def.Name}";
            // 通用工序的 Number 是 "公共", 用图标代替显示
            if (def.Category == StepCategory.Common)
                label = $"   {def.Icon} {def.Name}";

            var btn = new Button
            {
                Text = label,
                Location = new Point(16, top),
                Size = new Size(172, 34),
                BackColor = AppColors.NavItemBg,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 6, 0),
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand,
                Tag = def.Step,
                UseCompatibleTextRendering = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = AppColors.NavItemHover;
            btn.Paint += (s, e) => PaintStepDot(btn, def, e.Graphics);
            btn.Click += (s, e) =>
            {
                _ctrlMain.SwitchToStep(def.Step);
                HighlightCurrentStep();
            };
            return btn;
        }

        private void PaintStepDot(Button btn, AssemblyStepDef def, Graphics g)
        {
            if (_project == null) return;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var state = _project.StepStates[def.Step];

            Color bg;
            switch (state)
            {
                case StepProgress.Completed: bg = AppColors.Success; break;
                case StepProgress.Active:    bg = AppColors.Primary; break;
                default:                     bg = AppColors.Border;  break;
            }
            using (var br = new SolidBrush(bg))
                g.FillEllipse(br, 6, 12, 10, 10);
        }

        private void HighlightCurrentStep()
        {
            var current = _project.CurrentStep;
            foreach (var kv in _stepButtons)
            {
                if (kv.Key == current)
                {
                    kv.Value.BackColor = AppColors.PrimarySubtle;
                    kv.Value.ForeColor = AppColors.Primary;
                    kv.Value.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                }
                else
                {
                    kv.Value.BackColor = AppColors.NavItemBg;
                    kv.Value.ForeColor = AppColors.TextPrimary;
                    kv.Value.Font = new Font("微软雅黑", 9F);
                }
                kv.Value.Invalidate();
            }
        }

        // ============================================================
        // 主工作区
        // ============================================================
        private void InitWorkspace()
        {
            _pnlWorkspace = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.BgPrimary
            };
            Controls.Add(_pnlWorkspace);
            _pnlWorkspace.BringToFront();

            // 主工作区不再做 Common/Assembly 的过滤——因为左侧导航已经把所有步骤都列出来了
            // ChassisAssemblyControl 不显示自己的左侧步骤导航,只显示中间面板+右侧二维图
            _ctrlMain = new ChassisAssemblyControl(filterCategory: null, hideOwnNav: true)
            {
                Dock = DockStyle.Fill
            };
            _ctrlMain.StepChanged += (s, step) => HighlightCurrentStep();
            _pnlWorkspace.Controls.Add(_ctrlMain);
        }

        // ============================================================
        // 命令
        // ============================================================
        private void OnSaveProject(object sender, EventArgs e)
        {
            var svc = new ProjectService();
            svc.Save(_project);
            AppState.Instance.Log("INFO", "项目已保存");
            MessageBox.Show("项目已保存", "成功");
        }
    }
}
