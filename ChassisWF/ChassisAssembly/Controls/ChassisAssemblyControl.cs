using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ChassisAssembly.Models;
using ChassisAssembly.Services;

namespace ChassisAssembly.Controls
{
    /// <summary>
    /// 车体底架拼装主控件 - 按 9 步装配工艺重构
    ///
    /// 布局:
    /// ┌────┬──────────────────────────────────────────┐
    /// │步骤│ [前置检查条:跟踪仪/站位/矩阵状态]           │
    /// │导航│ ┌──────────────┬──────────────────────┐  │
    /// │200 │ │ 当前步骤      │ 二维俯视图           │  │
    /// │    │ │ 4 子阶段卡片  │ (高亮本步工装)       │  │
    /// │    │ └──────────────┴──────────────────────┘  │
    /// │    │ [操作日志]                                │
    /// └────┴──────────────────────────────────────────┘
    /// </summary>
    public class ChassisAssemblyControl : UserControl
    {
        // 数据
        private ProjectFile _project;

        // 左侧步骤导航
        private Panel _pnlStepNav;
        private readonly Dictionary<ProcessStep, Button> _stepButtons = new Dictionary<ProcessStep, Button>();

        // 顶部前置检查
        private Label _lblPreCheck;

        // 中间操作面板(显示当前步骤的子阶段卡片)
        private Panel _pnlCurrentStep;
        private Label _lblStepTitle;
        private Label _lblStepDesc;
        private Panel _pnlTechReq;      // 工艺尺寸要求折叠区
        private Label _lblTechReq;
        private Button _btnToggleReq;
        private bool _techReqVisible = false;  // 工艺要求默认收起 (2026.04.27)
        private readonly bool _hideOwnNav;     // true 表示外层(MainForm)负责导航,本控件不显示左侧步骤栏

        public event EventHandler<ProcessStep> StepChanged;
        private Panel _pnlSubStages;    // 装子阶段卡片

        // 右侧二维图
        private ChassisTopDownView _topView;

        // 底部日志
        private DataGridView _dgvLog;

        private readonly StepCategory? _filterCategory;

        public ChassisAssemblyControl() : this(null, false) { }

        public ChassisAssemblyControl(StepCategory? filterCategory, bool hideOwnNav = false)
        {
            _filterCategory = filterCategory;
            _hideOwnNav = hideOwnNav;
            InitializeComponent();
            AppState.Instance.TrackerStatusChanged += (s, e) => RefreshPreCheckBanner();
            AppState.Instance.LogAdded += (s, e) => RefreshLogGrid();
        }

        // 原来的无参构造函数保留为兼容,但委托给有参版

        private void InitializeComponent()
        {
            Size = new Size(1400, 800);
            BackColor = AppColors.BgPrimary;

            // 左侧步骤导航 - 只在 hideOwnNav=false 时创建 (单控件用); MainForm 嵌套场景下由外层负责导航
            if (!_hideOwnNav)
            {
                _pnlStepNav = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 210,
                    BackColor = AppColors.BgSecondary,
                    Padding = new Padding(0, 10, 0, 10)
                };
                Controls.Add(_pnlStepNav);
                InitStepNav();
            }

            // 主区:上部内容 + 下部日志
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.BgPrimary
            };
            Controls.Add(pnlMain);
            pnlMain.BringToFront();

            // 底部日志(占 160px)
            var pnlLogContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 160,
                BackColor = AppColors.BgPrimary,
                Padding = new Padding(10, 6, 10, 8),
                BorderStyle = BorderStyle.None
            };
            pnlMain.Controls.Add(pnlLogContainer);
            InitLogGrid(pnlLogContainer);

            // 顶部前置检查条:紧凑小圆点显示 (高 26px)
            var pnlPreCheck = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = AppColors.BgSecondary,
                Padding = new Padding(14, 0, 14, 0)
            };
            pnlMain.Controls.Add(pnlPreCheck);

            _lblPreCheck = new Label
            {
                Text = "前置状态:  跟踪仪 ●●   测量场 ●   基准 ●",
                Dock = DockStyle.Fill,
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlPreCheck.Controls.Add(_lblPreCheck);

            // 中间工作区:左侧当前步骤操作 + 右侧二维图
            var splitWork = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 3,
                BackColor = AppColors.Border
            };
            pnlMain.Controls.Add(splitWork);
            splitWork.BringToFront();
            splitWork.SplitterDistance = 440;

            // 左侧:当前步骤操作面板
            _pnlCurrentStep = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.BgSecondary,
                Padding = new Padding(14, 14, 14, 14),
                AutoScroll = true
            };
            splitWork.Panel1.Controls.Add(_pnlCurrentStep);
            InitCurrentStepPanel();

            // 右侧: 二维图占满
            var pnlRightHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppColors.BgPrimary
            };
            splitWork.Panel2.Controls.Add(pnlRightHost);

            _topView = new ChassisTopDownView { Dock = DockStyle.Fill };
            _topView.FixtureClicked += OnTopViewFixtureClicked;
            pnlRightHost.Controls.Add(_topView);
        }

        // Modal 工装详情弹窗 (单例,避免重复打开)
        private ChassisAssembly.Forms.FixtureDetailForm _fixtureDetailForm;

        // ============================================================
        // 工装详情弹窗 - 点击二维图工装时 Modal 弹出
        // ============================================================
        private void OnTopViewFixtureClicked(object sender, FixturePoint fx)
        {
            AppState.Instance.Log("INFO", $"选中工装: {fx.Name}");
            ShowFixtureDetail(fx);
        }

        private void ShowFixtureDetail(FixturePoint fx)
        {
            // 如果已有弹窗,关闭再重新开
            if (_fixtureDetailForm != null && !_fixtureDetailForm.IsDisposed)
            {
                _fixtureDetailForm.LoadFixture(fx);
                _fixtureDetailForm.BringToFront();
                _fixtureDetailForm.Activate();
                return;
            }

            _fixtureDetailForm = new ChassisAssembly.Forms.FixtureDetailForm();
            _fixtureDetailForm.LoadFixture(fx);
            _fixtureDetailForm.MeasureRequested += async (s, f) => await OnDetailMeasureClick(f);
            _fixtureDetailForm.PositionRequested += async (s, f) => await OnDetailPositionClick(f);
            _fixtureDetailForm.FormClosed += (s, e) => _fixtureDetailForm = null;
            _fixtureDetailForm.Show(FindForm());  // 非 Modal, 避免阻塞主界面操作
        }

        private void RefreshDetailIfOpen(FixturePoint fx)
        {
            if (_fixtureDetailForm != null && !_fixtureDetailForm.IsDisposed)
                _fixtureDetailForm.LoadFixture(fx);
        }

        private async System.Threading.Tasks.Task OnDetailMeasureClick(FixturePoint fx)
        {
            var state = AppState.Instance;
            if (state.TrackerStatuses[fx.AssignedTracker] != ConnectionStatus.Connected)
            {
                MessageBox.Show($"{fx.AssignedTracker} 未连接,请先完成通用公共工序的跟踪仪联合建站", "提示");
                return;
            }

            fx.Status = PointStatus.Measuring;
            _topView.Invalidate();
            RefreshDetailIfOpen(fx);

            var result = await state.TrackerService.MeasureSinglePointAsync(state.TrackerIps[fx.AssignedTracker]);
            if (!result.IsSuccess)
            {
                fx.Status = PointStatus.NotMeasured;
                _topView.Invalidate();
                RefreshDetailIfOpen(fx);
                MessageBox.Show($"测量失败: {result.ErrorMessage}", "错误");
                return;
            }

            var rand = new Random();
            fx.ActualX = fx.TheoreticalX + (rand.NextDouble() - 0.5) * 0.8;
            fx.ActualY = fx.TheoreticalY + (rand.NextDouble() - 0.5) * 0.8;
            fx.ActualZ = fx.TheoreticalZ + (rand.NextDouble() - 0.5) * 1.2;
            fx.PressForce = fx.HasForceSensor ? 1500 + rand.NextDouble() * 500 : 0;
            fx.Deformation = rand.NextDouble() * 50;
            fx.LastMeasuredAt = DateTime.Now;
            fx.Status = fx.DeviationNorm > 0.5 ? PointStatus.OutOfTolerance : PointStatus.Qualified;

            _topView.Invalidate();
            RefreshDetailIfOpen(fx);
            AppState.Instance.Log("INFO",
                $"测量 {fx.Name}: ΔX={fx.DeltaX:F3} ΔY={fx.DeltaY:F3} ΔZ={fx.DeltaZ:F3}");
        }

        private async System.Threading.Tasks.Task OnDetailPositionClick(FixturePoint fx)
        {
            AppState.Instance.Log("INFO", $"手动下发定位指令: {fx.Name}");
            var list = new System.Collections.Generic.List<FixturePoint> { fx };
            await AppState.Instance.FixtureController.PositionFixturesAsync(list);
            _topView.Invalidate();
            RefreshDetailIfOpen(fx);
        }

        // ============================================================
        // 左侧 9 步导航
        // ============================================================
        private void InitStepNav()
        {
            // 顶部标题(按过滤器显示)
            string titleText = "工序列表";
            if (_filterCategory == StepCategory.Common) titleText = "◆ 通用公共工序";
            else if (_filterCategory == StepCategory.Assembly) titleText = "◆ 装配工序 (HXD1C)";
            var lblTitle = new Label
            {
                Text = titleText,
                Dock = DockStyle.Top,
                Height = 30,
                ForeColor = AppColors.GroupHeader,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            };
            _pnlStepNav.Controls.Add(lblTitle);

            // 创建 9 个步骤按钮 (FlowLayoutPanel 自上而下)
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            _pnlStepNav.Controls.Add(flow);
            flow.BringToFront();

            // 根据过滤器决定显示哪些分组
            bool showCommon   = !_filterCategory.HasValue || _filterCategory.Value == StepCategory.Common;
            bool showAssembly = !_filterCategory.HasValue || _filterCategory.Value == StepCategory.Assembly;

            if (showCommon)
            {
                foreach (var def in AssemblyProcessDef.CommonSteps)
                {
                    var btn = CreateStepNavButton(def);
                    _stepButtons[def.Step] = btn;
                    flow.Controls.Add(btn);
                }
            }

            if (showCommon && showAssembly)
                flow.Controls.Add(CreateSeparator());

            if (showAssembly)
            {
                foreach (var def in AssemblyProcessDef.AssemblySteps)
                {
                    var btn = CreateStepNavButton(def);
                    _stepButtons[def.Step] = btn;
                    flow.Controls.Add(btn);
                }
            }
        }

        private Label CreateGroupHeader(string text)
        {
            return new Label
            {
                Text = text,
                Size = new Size(196, 22),
                Margin = new Padding(7, 10, 7, 2),
                Font = new Font("微软雅黑", 8F, FontStyle.Bold),
                ForeColor = AppColors.Warning,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Panel CreateSeparator()
        {
            return new Panel
            {
                Size = new Size(186, 1),
                Margin = new Padding(12, 8, 12, 4),
                BackColor = AppColors.Border
            };
        }

        private Button CreateStepNavButton(AssemblyStepDef def)
        {
            var btn = new Button
            {
                Size = new Size(196, 42),
                Margin = new Padding(7, 2, 7, 2),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(36, 0, 6, 0),
                Text = $"{def.Number} {def.Name}",
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand,
                Tag = def.Step
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) => PaintStepDot(btn, def);
            btn.Click += (s, e) =>
            {
                // 允许查看任何步骤 (包括未解锁的,方便提前了解流程)
                // 但若是 Locked 状态,后面 BuildSubStageCards 会自动把子阶段按钮禁用
                SwitchToStep(def.Step);
            };
            return btn;
        }

        /// <summary>在步骤按钮左边画状态圆点 (✓/图标或编号/灰)</summary>
        private void PaintStepDot(Button btn, AssemblyStepDef def)
        {
            if (_project == null) return;
            using (var g = btn.CreateGraphics())
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var state = _project.StepStates[def.Step];

                Color dotBg;
                string dotText;
                switch (state)
                {
                    case StepProgress.Completed:
                        dotBg = AppColors.Success;
                        dotText = "✓";
                        break;
                    case StepProgress.Active:
                        dotBg = AppColors.Primary;
                        dotText = GetDotLabel(def);
                        break;
                    default:
                        dotBg = AppColors.Border;
                        dotText = GetDotLabel(def);
                        break;
                }

                using (var br = new SolidBrush(dotBg))
                    g.FillEllipse(br, 10, 11, 20, 20);
                using (var br = new SolidBrush(Color.White))
                using (var font = new Font("微软雅黑", 8F, FontStyle.Bold))
                {
                    var size = g.MeasureString(dotText, font);
                    g.DrawString(dotText, font, br,
                        10 + (20 - size.Width) / 2,
                        11 + (20 - size.Height) / 2);
                }
            }
        }

        private string GetDotLabel(AssemblyStepDef def)
        {
            // 通用工序用 Icon,装配工序用 "4.x" 中的 x
            if (def.Category == StepCategory.Common)
                return string.IsNullOrEmpty(def.Icon) ? "·" : def.Icon.Substring(0, 1);
            var parts = def.Number.Split('.');
            return parts.Length > 1 ? parts[1] : def.Number;
        }

        // ============================================================
        // 中间当前步骤面板
        // ============================================================
        private void InitCurrentStepPanel()
        {
            // 子阶段容器放最底(Dock=Fill),其他控件按 Dock=Top 从上往下叠
            _pnlSubStages = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            _pnlCurrentStep.Controls.Add(_pnlSubStages);

            // 工艺尺寸要求折叠区 (默认收起 - 2026.04.27)
            _pnlTechReq = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,  // 默认只显示标题栏
                BackColor = AppColors.BgSecondary,
                Padding = new Padding(10, 6, 10, 6)
            };

            // 内部结构:Dock 顺序规则(后 Add 先 Dock 且在最内)
            // 最底的 Label(Fill) 必须先 Add,然后再 Add Header(Top) 才会正确叠加
            _lblTechReq = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent,
                Visible = false,  // 默认隐藏
                Text = "(切换步骤后显示)"
            };
            _pnlTechReq.Controls.Add(_lblTechReq);

            var reqHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.Transparent
            };

            var reqTitle = new Label
            {
                Text = "📐 工艺尺寸要求 (HXD1C 组焊通用作业指导书)",
                Location = new Point(0, 2),
                AutoSize = true,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = AppColors.Warning,
                BackColor = Color.Transparent
            };
            reqHeader.Controls.Add(reqTitle);

            _btnToggleReq = new Button
            {
                Text = "▶ 展开",
                Location = new Point(0, 0),
                Size = new Size(60, 22),
                BackColor = Color.Transparent,
                ForeColor = AppColors.TextSecondary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 8F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnToggleReq.FlatAppearance.BorderSize = 0;
            _btnToggleReq.Click += (s, e) => ToggleTechReq();
            reqHeader.Controls.Add(_btnToggleReq);
            // 在 Header 尺寸确定后调整按钮位置到右边
            reqHeader.Resize += (s, e) => _btnToggleReq.Location =
                new Point(reqHeader.ClientSize.Width - _btnToggleReq.Width, 0);

            _pnlTechReq.Controls.Add(reqHeader);
            _pnlCurrentStep.Controls.Add(_pnlTechReq);

            // 描述
            _lblStepDesc = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("微软雅黑", 9F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent,
                Text = ""
            };
            _pnlCurrentStep.Controls.Add(_lblStepDesc);

            // 标题
            _lblStepTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent,
                Text = "请选择装配步骤"
            };
            _pnlCurrentStep.Controls.Add(_lblStepTitle);
        }

        private void ToggleTechReq()
        {
            _techReqVisible = !_techReqVisible;
            _pnlTechReq.Height = _techReqVisible ? 140 : 28;
            _lblTechReq.Visible = _techReqVisible;
            _btnToggleReq.Text = _techReqVisible ? "▼ 收起" : "▶ 展开";
        }

        // ============================================================
        // 底部日志
        // ============================================================
        private void InitLogGrid(Panel host)
        {
            var lblTitle = new Label
            {
                Text = "📋 操作日志",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            host.Controls.Add(lblTitle);

            _dgvLog = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                GridColor = AppColors.Border,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                RowTemplate = { Height = 20 }
            };
            _dgvLog.DefaultCellStyle.BackColor = AppColors.BgSecondary;
            _dgvLog.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            _dgvLog.DefaultCellStyle.Font = new Font("Consolas", 8.5F);
            _dgvLog.DefaultCellStyle.SelectionBackColor = AppColors.Primary;
            _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", Width = 80 });
            _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Level", Width = 55 });
            _dgvLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Msg", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            host.Controls.Add(_dgvLog);
            _dgvLog.BringToFront();
        }

        private void RefreshLogGrid()
        {
            if (_dgvLog == null) return;
            if (_dgvLog.InvokeRequired)
            {
                _dgvLog.BeginInvoke(new Action(RefreshLogGrid));
                return;
            }
            _dgvLog.Rows.Clear();
            foreach (var log in AppState.Instance.GlobalLogs)
            {
                int ri = _dgvLog.Rows.Add(
                    log.Timestamp.ToString("HH:mm:ss"),
                    log.Level,
                    log.Message);
                switch (log.Level)
                {
                    case "ERROR": _dgvLog.Rows[ri].Cells[1].Style.ForeColor = AppColors.Danger; break;
                    case "WARN":  _dgvLog.Rows[ri].Cells[1].Style.ForeColor = AppColors.Warning; break;
                    default:      _dgvLog.Rows[ri].Cells[1].Style.ForeColor = AppColors.Info; break;
                }
                if (_dgvLog.Rows.Count > 200) break;
            }
        }

        // ============================================================
        // 对外 API
        // ============================================================
        public void LoadProject(ProjectFile project)
        {
            _project = project;
            _topView.Fixtures = project.Fixtures;

            // 按过滤器选择本控件要切到的步骤
            ProcessStep targetStep = project.CurrentStep;
            if (_filterCategory.HasValue)
            {
                var def = AssemblyProcessDef.Get(project.CurrentStep);
                // 如果当前项目步骤不在本控件的过滤范围内,切到本类别的第一个步骤
                if (def == null || def.Category != _filterCategory.Value)
                {
                    var list = _filterCategory.Value == StepCategory.Common
                        ? AssemblyProcessDef.CommonSteps
                        : AssemblyProcessDef.AssemblySteps;
                    if (list.Count > 0) targetStep = list[0].Step;
                }
            }

            SwitchToStep(targetStep);
            AppState.Instance.Log("INFO",
                $"已加载项目: {project.ProjectName} ({project.VehicleType})");
            RefreshPreCheckBanner();
            foreach (var btn in _stepButtons.Values) btn.Invalidate();
        }

        // ============================================================
        // 切换步骤
        // ============================================================
        public void SwitchToStep(ProcessStep step)
        {
            if (_project == null) return;
            _project.CurrentStep = step;

            var def = AssemblyProcessDef.Get(step);
            if (def == null) return;

            // 更新标题、描述
            _lblStepTitle.Text = $"{def.Number} {def.Name}";
            bool locked = _project.StepStates[step] == StepProgress.Locked;
            string lockedHint = locked
                ? "  🔒 此步骤尚未解锁 - 可查看流程,操作按钮需先完成前面的工序"
                : "";
            _lblStepDesc.Text = $"{def.Description}  ·  涉及工装: " +
                string.Join("、", def.InvolvedFixtureTypes.Select(GetFixtureTypeName))
                + lockedHint;
            _lblStepDesc.ForeColor = locked ? AppColors.Warning : AppColors.TextSecondary;
            _lblTechReq.Text = string.IsNullOrWhiteSpace(def.TechRequirements)
                ? "(本步骤暂无工艺尺寸要求记录)"
                : def.TechRequirements;

            // 通知外层(MainForm)步骤变化
            StepChanged?.Invoke(this, step);

            // 高亮步骤按钮
            foreach (var kv in _stepButtons)
            {
                if (kv.Key == step)
                {
                    kv.Value.BackColor = AppColors.PrimarySubtle;
                    kv.Value.ForeColor = AppColors.Primary;
                    kv.Value.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
                }
                else
                {
                    kv.Value.BackColor = AppColors.BgSecondary;
                    kv.Value.ForeColor = _project.StepStates[kv.Key] == StepProgress.Completed
                        ? AppColors.Success
                        : AppColors.TextSecondary;
                    kv.Value.Font = new Font("微软雅黑", 9F);
                }
                kv.Value.Invalidate();
            }

            // 二维图高亮本步涉及的工装类型
            _topView.HighlightedTypes = new HashSet<FixtureType>(def.InvolvedFixtureTypes);

            // 重建子阶段卡片
            BuildSubStageCards(def);

            AppState.Instance.Log("INFO", $"进入步骤 {def.Number} {def.Name}");
        }

        // ============================================================
        // 子阶段卡片
        // ============================================================
        private void BuildSubStageCards(AssemblyStepDef def)
        {
            _pnlSubStages.Controls.Clear();
            if (def == null || def.SubStages == null) return;

            // 同步 Skipped 状态
            for (int i = 0; i < 4; i++)
            {
                var stage = (SubStage)i;
                if (def.SubStages[i] != null && def.SubStages[i].Skipped)
                {
                    if (_project.GetSubStageState(def.Step, stage) != SubStageState.Completed)
                        _project.SetSubStageState(def.Step, stage, SubStageState.Skipped);
                }
            }

            // 确保至少一个子阶段 Active (但 Locked 步骤不激活,让用户看到"全部未解锁"状态)
            if (_project.StepStates[def.Step] != StepProgress.Locked)
                EnsureFirstActive(def);

            // 渲染每个非 Skipped 的卡片
            int y = 0;
            for (int i = 0; i < 4; i++)
            {
                var subDef = def.SubStages[i];
                if (subDef == null || subDef.Skipped) continue;

                var stage = (SubStage)i;
                var buttons = new System.Collections.Generic.List<(string, Action)>();
                if (subDef.ButtonLabels != null)
                {
                    string actionKind = subDef.ActionKind ?? "";
                    foreach (var label in subDef.ButtonLabels)
                    {
                        string btnLabel = label;
                        buttons.Add((btnLabel, () => DispatchSubStageAction(def, stage, actionKind, btnLabel)));
                    }
                }

                y = AddSubStageCard(def, stage,
                    subDef.Title, subDef.Description,
                    y, buttons.ToArray());
            }
        }

        /// <summary>根据 ActionKind 字符串分发到具体的业务方法</summary>
        private async void DispatchSubStageAction(AssemblyStepDef def, SubStage stage,
            string actionKind, string buttonLabel)
        {
            if (!CheckPreChecksForAction(def, actionKind))
                return;

            switch (actionKind)
            {
                // ---- 通用公共工序 ----
                case "ConnectLT1":
                    await ConnectOneTracker("LT_1", def, stage);
                    break;
                case "ConnectLT2":
                    await ConnectOneTracker("LT_2", def, stage);
                    break;
                case "StationSetup":
                    ChassisAssembly.Forms.StationSetupForm.Show(FindForm());
                    MarkSubStageDoneAndAdvance(def, stage);
                    break;
                case "CollectECP":
                    await MockOperation(def, stage, "采集公共基准点", "[Mock] 共采集 6 个公共基准点");
                    break;
                case "USMNAdjust":
                    if (buttonLabel.Contains("矩阵"))
                    {
                        using (var f = new ChassisAssembly.Forms.MatrixCalibrationForm())
                            f.ShowDialog(FindForm());
                    }
                    else
                    {
                        await MockOperation(def, stage, "USMN 平差", "[Mock] USMN 平差完成,组网精度 0.15mm");
                    }
                    break;
                case "VerifyPrecision":
                    await MockOperation(def, stage, "精度验证", "[Mock] 组网精度验证通过 ≤0.2mm");
                    break;
                case "DatumX":
                    await MockOperation(def, stage, "建立 X 基准", "[Mock] 纵向基准 X 已建立");
                    break;
                case "DatumY":
                    await MockOperation(def, stage, "建立 Y 基准", "[Mock] 横向基准 Y 已建立");
                    break;
                case "DatumZ":
                    await MockOperation(def, stage, "建立 Z 基准", "[Mock] 水平基准 Z 已建立");
                    break;

                // ---- 工装定位 (新增公共工序) ----
                case "RoughPosition":
                    await MockOperation(def, stage, "工装粗定位", "[Mock] PLC 已下发理论坐标,所有工装移动至大致位置");
                    break;
                case "MeasureFixtures":
                    await MockOperation(def, stage, "测量工装实际位置", "[Mock] 已逐工装测量,共 30 个点位完成采集");
                    break;
                case "CompareTheoretical":
                    await MockOperation(def, stage, "对比理论位置", "[Mock] 偏差计算完成,3 个工装超差,等待精定位");
                    break;
                case "FinePosition":
                    if (buttonLabel.Contains("停止"))
                        await AppState.Instance.FixtureController.EmergencyStopAsync();
                    else
                        await MockOperation(def, stage, "工装精定位", "[Mock] 迭代调整完成,所有工装均合格 ≤±0.5mm");
                    break;

                // ---- 装配工序 (重构后) ----
                case "MeasureMarkers":
                    await MockOperation(def, stage, "测构件标志点", "[Mock] 已采集 ≥3 个标志点,完成构件本体位姿基准建立");
                    break;
                case "HoistingDone":
                    OnHoistingDoneAction(def, stage);
                    break;
                case "AlignComponent":
                    if (buttonLabel.Contains("停止"))
                        await AppState.Instance.FixtureController.EmergencyStopAsync();
                    else
                        await OnAlignAction(def, stage);
                    break;

                // ---- 兼容旧版按钮 (装配工序原 PositionFixtures) ----
                case "PositionFixtures":
                    await OnPositionFixturesAction(def, stage);
                    break;

                // ---- 完成本工序 ----
                case "Finish":
                    OnFinishClick(def);
                    return;

                default:
                    AppState.Instance.Log("WARN", $"未识别的动作: {actionKind}");
                    break;
            }
        }

        private bool CheckPreChecksForAction(AssemblyStepDef def, string actionKind)
        {
            // 跟踪仪连接动作本身不需要前置检查
            if (actionKind == "ConnectLT1" || actionKind == "ConnectLT2" || actionKind == "Finish")
                return true;

            // 其他动作需要至少一个跟踪仪已连接
            var s = AppState.Instance;
            if (s.TrackerStatuses["LT_1"] != ConnectionStatus.Connected
             && s.TrackerStatuses["LT_2"] != ConnectionStatus.Connected)
            {
                MessageBox.Show("前置条件未满足: 请先在「跟踪仪联合建站」中连接至少一台跟踪仪", "提示");
                return false;
            }
            return true;
        }

        /// <summary>通用的 Mock 操作执行器</summary>
        private async System.Threading.Tasks.Task MockOperation(AssemblyStepDef def, SubStage stage,
            string title, string logMsg)
        {
            AppState.Instance.Log("INFO", $"[{def.Number}] 开始: {title}");
            await System.Threading.Tasks.Task.Delay(800);
            AppState.Instance.Log("INFO", logMsg);
            MarkSubStageDoneAndAdvance(def, stage);
        }

        /// <summary>连接单个跟踪仪 (用于通用公共工序 Step 101 的 SubStage)</summary>
        private async System.Threading.Tasks.Task ConnectOneTracker(string name, AssemblyStepDef def, SubStage stage)
        {
            var state = AppState.Instance;
            if (state.TrackerStatuses[name] == ConnectionStatus.Connected)
            {
                state.Log("INFO", $"{name} 已处于连接状态");
                MarkSubStageDoneAndAdvance(def, stage);
                return;
            }
            state.SetTrackerStatus(name, ConnectionStatus.Connecting);
            bool ok = await state.TrackerService.ConnectAsync(state.TrackerIps[name]);
            state.SetTrackerStatus(name, ok ? ConnectionStatus.Connected : ConnectionStatus.Error);
            state.Log(ok ? "INFO" : "ERROR", $"{name} {(ok ? "连接成功" : "连接失败")}");
            if (ok) MarkSubStageDoneAndAdvance(def, stage);
        }

        private void MarkSubStageDoneAndAdvance(AssemblyStepDef def, SubStage stage)
        {
            _project.SetSubStageState(def.Step, stage, SubStageState.Completed);
            // 激活下一个未跳过、未完成的子阶段
            for (int i = (int)stage + 1; i < 4; i++)
            {
                var next = (SubStage)i;
                if (def.SubStages[i]?.Skipped == true) continue;
                var st = _project.GetSubStageState(def.Step, next);
                if (st == SubStageState.Pending)
                {
                    _project.SetSubStageState(def.Step, next, SubStageState.Active);
                    break;
                }
            }
            BuildSubStageCards(def);
        }

        /// <summary>装配工序: 压紧装置定位就位</summary>
        private async System.Threading.Tasks.Task OnPositionFixturesAction(AssemblyStepDef def, SubStage stage)
        {
            var fixtures = _project.Fixtures
                .Where(f => def.InvolvedFixtureTypes.Contains(f.FixtureType)).ToList();
            if (fixtures.Count == 0)
            {
                MessageBox.Show($"本步骤涉及的工装尚未在车型配置中定义\n(类型: {string.Join(",", def.InvolvedFixtureTypes)})",
                    "配置缺失");
                return;
            }
            AppState.Instance.Log("INFO", $"[{def.Number}] 开始压紧装置定位,共 {fixtures.Count} 个");
            await AppState.Instance.FixtureController.PositionFixturesAsync(fixtures);
            _topView.Invalidate();
            MarkSubStageDoneAndAdvance(def, stage);
        }

        private void OnHoistingDoneAction(AssemblyStepDef def, SubStage stage)
        {
            var r = MessageBox.Show($"确认{def.Name.Replace("装配", "")}已吊装就位?", "确认", MessageBoxButtons.YesNo);
            if (r != DialogResult.Yes) return;
            AppState.Instance.Log("INFO", $"[{def.Number}] 构件吊装就位确认");
            MarkSubStageDoneAndAdvance(def, stage);
        }

        private async System.Threading.Tasks.Task OnAlignAction(AssemblyStepDef def, SubStage stage)
        {
            var fixtures = _project.Fixtures
                .Where(f => def.InvolvedFixtureTypes.Contains(f.FixtureType)).ToList();
            if (fixtures.Count == 0) return;
            AppState.Instance.Log("INFO", $"[{def.Number}] 开始构件调姿");
            await AppState.Instance.FixtureController.AlignComponentAsync(fixtures);
            _topView.Invalidate();
            MarkSubStageDoneAndAdvance(def, stage);
        }

        /// <summary>撤销已完成的子阶段 — 把 Completed → Pending, 并把其他后续已完成也重置为 Pending</summary>
        private void UndoSubStage(AssemblyStepDef def, SubStage stage)
        {
            var r = MessageBox.Show(
                $"确认撤销「{def.SubStages[(int)stage]?.Title}」?\n\n" +
                "该子阶段及其后所有已完成的子阶段都会重置为未开始状态。",
                "撤销确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;

            // 本子阶段及其后所有 Completed 的阶段都重置为 Pending
            for (int i = (int)stage; i < 4; i++)
            {
                var st = _project.GetSubStageState(def.Step, (SubStage)i);
                if (st == SubStageState.Completed || st == SubStageState.Active)
                    _project.SetSubStageState(def.Step, (SubStage)i, SubStageState.Pending);
            }

            // 如果本步骤之前被标记为 Completed,也要降级回 Active
            if (_project.StepStates[def.Step] == StepProgress.Completed)
                _project.StepStates[def.Step] = StepProgress.Active;

            // 把本步骤设为当前
            SwitchToStep(def.Step);

            AppState.Instance.Log("WARN",
                $"[{def.Number}] 已撤销到「{def.SubStages[(int)stage]?.Title}」");
            RefreshAllStepButtons();
        }

        /// <summary>确保本步骤至少有一个非跳过子阶段处于 Active (新进入步骤时调用)</summary>
        private void EnsureFirstActive(AssemblyStepDef def)
        {
            // SubStage 枚举值为 0/1/2/3,对应 4 个通用槽位
            var allStages = new[]
            {
                (SubStage)0, (SubStage)1, (SubStage)2, (SubStage)3
            };

            // 如果已经有 Active 阶段,什么都不做
            foreach (var s in allStages)
                if (_project.GetSubStageState(def.Step, s) == SubStageState.Active)
                    return;

            // 否则找第一个 Pending (非 Skipped) 阶段设为 Active
            foreach (var s in allStages)
            {
                int idx = (int)s;
                if (def.SubStages[idx] != null && def.SubStages[idx].Skipped) continue;
                var st = _project.GetSubStageState(def.Step, s);
                if (st == SubStageState.Pending)
                {
                    _project.SetSubStageState(def.Step, s, SubStageState.Active);
                    return;
                }
            }
        }

        /// <summary>返回下一个卡片的 Y 位置</summary>
        private int AddSubStageCard(AssemblyStepDef def, SubStage stage,
            string title, string desc, int topY,
            (string label, Action handler)[] buttons)
        {
            var state = _project.GetSubStageState(def.Step, stage);

            // 根据状态决定颜色
            Color borderColor;
            Color bgColor;
            switch (state)
            {
                case SubStageState.Completed:
                    borderColor = AppColors.Success;
                    bgColor = AppColors.SuccessBg;
                    break;
                case SubStageState.Active:
                    borderColor = AppColors.Primary;
                    bgColor = AppColors.PrimarySubtle;
                    break;
                default:
                    borderColor = AppColors.TextMuted;
                    bgColor = AppColors.BgSecondary;
                    break;
            }

            var card = new Panel
            {
                Location = new Point(0, topY),
                Width = _pnlSubStages.ClientSize.Width - 20,
                Height = 120,
                BackColor = bgColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(10, 8, 10, 8)
            };
            // 左侧彩色竖条
            var leftBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 3,
                BackColor = borderColor
            };
            card.Controls.Add(leftBar);

            // 标题+状态图标
            var titleText = title;
            if (state == SubStageState.Completed) titleText += "   ✓";
            var lblTitle = new Label
            {
                Text = titleText,
                Location = new Point(18, 8),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            // 撤销按钮 - 只在 Completed 状态显示
            if (state == SubStageState.Completed)
            {
                var btnUndo = new Button
                {
                    Text = "↶ 撤销",
                    Size = new Size(68, 22),
                    BackColor = Color.Transparent,
                    ForeColor = AppColors.TextSecondary,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("微软雅黑", 8F),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btnUndo.FlatAppearance.BorderColor = AppColors.Border;
                btnUndo.FlatAppearance.MouseOverBackColor = AppColors.BgDarkAccent;
                btnUndo.Location = new Point(card.Width - 92, 10);
                btnUndo.Click += (s, e) => UndoSubStage(def, stage);
                card.Controls.Add(btnUndo);
            }

            var lblDesc = new Label
            {
                Text = desc,
                Location = new Point(18, 32),
                Size = new Size(card.Width - 40, 40),
                Font = new Font("微软雅黑", 9F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblDesc);

            int bx = 18;
            // 通用公共工序不做前置检查(因为它们自己就是建立前置条件的步骤)
            bool isCommonStep = def.Category == StepCategory.Common;
            bool preChecksOk = isCommonStep || CheckPreChecks();

            // 步骤级锁定: 如果当前步骤还被锁定(前置步骤没完成),所有操作按钮都应给"禁用"提示
            bool stepLocked = _project != null && _project.StepStates[def.Step] == StepProgress.Locked;

            foreach (var (label, handler) in buttons)
            {
                bool isActionable = state == SubStageState.Active && preChecksOk && !stepLocked;

                Color btnBg;
                if (stepLocked)
                    btnBg = AppColors.BgSecondary;          // 整个步骤锁定:浅灰
                else if (state == SubStageState.Active)
                    btnBg = AppColors.Primary;              // 当前活跃:蓝色
                else
                    btnBg = AppColors.Border;               // 其他非活跃:边框灰

                Color btnFg = stepLocked ? AppColors.TextMuted : Color.White;

                var btn = new Button
                {
                    Text = label,
                    Location = new Point(bx, 78),
                    Size = new Size(130, 30),
                    BackColor = btnBg,
                    ForeColor = btnFg,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("微软雅黑", 9F),
                    Enabled = true,   // 保持可点击以响应提示
                    Cursor = isActionable ? Cursors.Hand : Cursors.No,
                    Tag = isActionable ? "actionable" : "blocked"
                };
                btn.FlatAppearance.BorderSize = 0;

                if (isActionable)
                {
                    btn.Click += (s, e) => handler?.Invoke();
                }
                else if (stepLocked)
                {
                    // 点击时提示 "请先完成前面的工序"
                    btn.Click += (s, e) =>
                    {
                        MessageBox.Show(
                            "请先完成前面的工序后再操作本步骤\n\n" +
                            "(您可以点击左侧任意步骤名称查看流程内容,但操作按钮需按顺序解锁)",
                            "步骤尚未解锁", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                }
                else if (state != SubStageState.Active)
                {
                    // 本步骤已解锁,但该子阶段不是当前活跃
                    btn.Click += (s, e) =>
                    {
                        MessageBox.Show(
                            "请按顺序完成当前高亮的子阶段",
                            "顺序提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                }
                else if (!preChecksOk)
                {
                    btn.Click += (s, e) =>
                    {
                        MessageBox.Show(
                            "前置条件未满足: 请先在「通用工序」中完成跟踪仪联合建站",
                            "前置未就绪", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    };
                }

                card.Controls.Add(btn);
                bx += 138;
            }

            _pnlSubStages.Controls.Add(card);
            return topY + card.Height + 8;
        }

        // ============================================================
        // ============================================================
        // 子阶段按钮回调 (新版统一走 DispatchSubStageAction)
        // ============================================================
        private void OnFinishClick(AssemblyStepDef def)
        {
            _project.SetSubStageState(def.Step, SubStage.Finish, SubStageState.Completed);
            _project.StepStates[def.Step] = StepProgress.Completed;

            // 解锁下一步
            var idx = AssemblyProcessDef.AllSteps.FindIndex(s => s.Step == def.Step);
            if (idx >= 0 && idx < AssemblyProcessDef.AllSteps.Count - 1)
            {
                var nextStep = AssemblyProcessDef.AllSteps[idx + 1].Step;
                if (_project.StepStates[nextStep] == StepProgress.Locked)
                    _project.StepStates[nextStep] = StepProgress.Active;

                AppState.Instance.Log("INFO", $"[{def.Number}] {def.Name} 已完成,进入下一步");
                SwitchToStep(nextStep);
            }
            else
            {
                AppState.Instance.Log("INFO", $"🎉 全部 9 步装配流程已完成!");
                MessageBox.Show("全部装配流程已完成!\n\n可在项目文件中查看完整装配记录。", "流程完成");
                BuildSubStageCards(def);
            }

            foreach (var btn in _stepButtons.Values) btn.Invalidate();
        }

        // ============================================================
        // 前置检查
        // ============================================================
        private bool CheckPreChecks()
        {
            var s = AppState.Instance;
            return s.TrackerStatuses["LT_1"] == ConnectionStatus.Connected
                && s.TrackerStatuses["LT_2"] == ConnectionStatus.Connected;
        }

        private void RefreshPreCheckBanner()
        {
            if (_lblPreCheck == null) return;
            if (_lblPreCheck.InvokeRequired)
            {
                _lblPreCheck.BeginInvoke(new Action(RefreshPreCheckBanner));
                return;
            }

            var s = AppState.Instance;

            // 跟踪仪状态: 以两台连接状态为准
            string lt1Dot = s.TrackerStatuses["LT_1"] == ConnectionStatus.Connected ? "●" : "○";
            string lt2Dot = s.TrackerStatuses["LT_2"] == ConnectionStatus.Connected ? "●" : "○";

            // 三个通用工序完成状态
            string fieldDot = "○";
            string datumDot = "○";
            if (_project != null)
            {
                fieldDot = _project.StepStates[ProcessStep.MeasureFieldBuild] == StepProgress.Completed ? "●" : "○";
                datumDot = _project.StepStates[ProcessStep.DatumEstablish] == StepProgress.Completed ? "●" : "○";
            }

            _lblPreCheck.Text = $"前置状态:  跟踪仪 LT_1 {lt1Dot}  LT_2 {lt2Dot}    测量场 {fieldDot}    基准 {datumDot}";

            // 整体颜色:全绿表示全部完成,否则灰色
            bool allReady = s.TrackerStatuses["LT_1"] == ConnectionStatus.Connected
                         && s.TrackerStatuses["LT_2"] == ConnectionStatus.Connected
                         && fieldDot == "●" && datumDot == "●";
            _lblPreCheck.ForeColor = allReady
                ? AppColors.Success
                : AppColors.TextSecondary;
        }

        /// <summary>刷新所有步骤按钮的外观(特别是 Locked → Active 解锁后)</summary>
        private void RefreshAllStepButtons()
        {
            foreach (var btn in _stepButtons.Values) btn.Invalidate();
        }



        // ============================================================
        // 辅助
        // ============================================================
        private static string GetFixtureTypeName(FixtureType t)
        {
            switch (t)
            {
                case FixtureType.BolsterPress:         return "枕梁压紧";
                case FixtureType.TransformerBeamPress: return "变压器梁压紧";
                case FixtureType.TractionLongPress:    return "牵引梁纵向";
                case FixtureType.TractionLatPress:     return "牵引梁横向";
                case FixtureType.SideBeamPress:        return "边梁压紧";
                case FixtureType.CenterBeamFixture:    return "中心纵梁工装";
                case FixtureType.PartitionBeamFixture: return "隔墙梁工装";
                case FixtureType.LongitudinalDatum:    return "纵向基准";
                case FixtureType.LateralDatum:         return "横向基准";
                case FixtureType.PiercingSupport:      return "贯穿梁支撑";
                default: return t.ToString();
            }
        }
    }
}
