using System;
using ChassisAssembly.Controls;
using System.Drawing;
using System.Windows.Forms;
using ChassisAssembly.Models;
using ChassisAssembly.Services;

namespace ChassisAssembly.Forms
{
    /// <summary>
    /// 启动窗口 - 打开软件首先看到的界面
    /// 功能: 新建项目(选车型) / 打开已有项目
    /// </summary>
    public class StartupForm : Form
    {
        private ComboBox _cmbVehicleType;
        private TextBox _txtProjectName;
        private TextBox _txtBatchNumber;
        private TextBox _txtOperator;

        public ProjectFile CreatedProject { get; private set; }

        public StartupForm()
        {
            InitUI();
        }

        private void InitUI()
        {
            Text = "车体底架自动化拼装系统 - 启动";
            Size = new Size(780, 520);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppColors.BgPrimary;
            ForeColor = AppColors.TextPrimary;
            Font = new Font("微软雅黑", 9F);

            // WinForms Dock 规则: 后 Add 的先 Dock 且在最内层
            // 所以要让 pnlBrand 贴左边, pnlForm 占满剩余, 必须先 Add pnlForm 再 Add pnlBrand

            var pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 40, 30, 20)
            };
            Controls.Add(pnlForm);

            // 左侧品牌区
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = AppColors.BgSecondary
            };
            Controls.Add(pnlBrand);

            var lblLogo = new Label
            {
                Text = "C",
                Font = new Font("微软雅黑", 28F, FontStyle.Bold),
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(30, 40),
                Size = new Size(60, 60)
            };
            pnlBrand.Controls.Add(lblLogo);

            var lblTitle1 = new Label
            {
                Text = "车体底架",
                Font = new Font("微软雅黑", 18F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                Location = new Point(30, 120),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblTitle1);

            var lblTitle2 = new Label
            {
                Text = "自动化拼装系统",
                Font = new Font("微软雅黑", 18F, FontStyle.Regular),
                ForeColor = AppColors.TextPrimary,
                Location = new Point(30, 150),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblTitle2);

            var lblSubtitle = new Label
            {
                Text = "Chassis Assembly System",
                Font = new Font("微软雅黑", 9F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(30, 184),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblSubtitle);

            var lineSep = new Panel
            {
                BackColor = AppColors.Border,
                Location = new Point(30, 220),
                Size = new Size(220, 1)
            };
            pnlBrand.Controls.Add(lineSep);

            var lblVersion = new Label
            {
                Text = "版本 v1.0",
                Font = new Font("微软雅黑", 8F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(30, 230),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblVersion);

            var lblCopyright = new Label
            {
                Text = "© 2026 中科工业人工智能研究院",
                Font = new Font("微软雅黑", 8F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(30, 250),
                AutoSize = true
            };
            pnlBrand.Controls.Add(lblCopyright);

            var lblHeading = new Label
            {
                Text = "开始一个新的拼装任务",
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = AppColors.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblHeading);

            // 项目名称
            AddLabel(pnlForm, "① 项目名称", 40);
            _txtProjectName = new TextBox
            {
                Text = $"HXD1C_{DateTime.Now:yyyyMMdd}_批次01",
                Location = new Point(0, 62),
                Size = new Size(440, 26),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F)
            };
            pnlForm.Controls.Add(_txtProjectName);

            // 车型 + 批次号
            AddLabel(pnlForm, "② 选择车型", 100);
            _cmbVehicleType = new ComboBox
            {
                Location = new Point(0, 122),
                Size = new Size(215, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F)
            };
            foreach (VehicleType vt in Enum.GetValues(typeof(VehicleType)))
                _cmbVehicleType.Items.Add(ProjectService.GetVehicleDisplayName(vt));
            _cmbVehicleType.SelectedIndex = 0;
            pnlForm.Controls.Add(_cmbVehicleType);

            AddLabel(pnlForm, "③ 批次号 (可选)", 100, 225);
            _txtBatchNumber = new TextBox
            {
                Location = new Point(225, 122),
                Size = new Size(215, 26),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F)
            };
            pnlForm.Controls.Add(_txtBatchNumber);

            // 操作员
            AddLabel(pnlForm, "④ 操作员 (可选)", 160);
            _txtOperator = new TextBox
            {
                Location = new Point(0, 182),
                Size = new Size(440, 26),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("微软雅黑", 10F)
            };
            pnlForm.Controls.Add(_txtOperator);

            // 创建按钮
            var btnCreate = new Button
            {
                Text = "▶ 创建项目并开始",
                Location = new Point(0, 225),
                Size = new Size(440, 40),
                BackColor = AppColors.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += OnCreateClicked;
            pnlForm.Controls.Add(btnCreate);

            // 或者 分隔
            var lblOr = new Label
            {
                Text = "或者",
                Font = new Font("微软雅黑", 9F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(0, 285),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblOr);

            var btnOpen = new Button
            {
                Text = "打开已有项目 (.cap)",
                Location = new Point(0, 310),
                Size = new Size(215, 36),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            btnOpen.FlatAppearance.BorderColor = AppColors.Border;
            btnOpen.Click += OnOpenClicked;
            pnlForm.Controls.Add(btnOpen);

            var btnDemo = new Button
            {
                Text = "查看演示项目",
                Location = new Point(225, 310),
                Size = new Size(215, 36),
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand
            };
            btnDemo.FlatAppearance.BorderColor = AppColors.Border;
            btnDemo.Click += OnDemoClicked;
            pnlForm.Controls.Add(btnDemo);

            // 底部提示
            var lblHint = new Label
            {
                Text = "提示: 项目文件将保存本次拼装的全部测量数据,可以随时继续",
                Font = new Font("微软雅黑", 8F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(0, 405),
                AutoSize = true
            };
            pnlForm.Controls.Add(lblHint);
        }

        private void AddLabel(Control parent, string text, int y, int x = 0)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("微软雅黑", 8F),
                ForeColor = AppColors.TextSecondary,
                Location = new Point(x, y),
                AutoSize = true
            };
            parent.Controls.Add(lbl);
        }

        private void OnCreateClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtProjectName.Text))
            {
                MessageBox.Show("请输入项目名称", "提示");
                return;
            }
            var svc = new ProjectService();
            CreatedProject = svc.CreateNew(
                _txtProjectName.Text.Trim(),
                (VehicleType)_cmbVehicleType.SelectedIndex,
                _txtBatchNumber.Text.Trim(),
                _txtOperator.Text.Trim());
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnOpenClicked(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Filter = "车体底架项目文件 (*.cap)|*.cap",
                Title = "打开项目文件"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // TODO: 真实项目反序列化
                    MessageBox.Show("打开项目功能待实现,暂跳转到演示项目", "提示");
                    OnDemoClicked(sender, e);
                }
            }
        }

        private void OnDemoClicked(object sender, EventArgs e)
        {
            var svc = new ProjectService();
            CreatedProject = svc.CreateNew(
                "HXD1C_演示项目",
                VehicleType.HXD1C,
                "DEMO-001",
                "演示员");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
