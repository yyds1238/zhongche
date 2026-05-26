using System;
using ChassisAssembly.Controls;
using System.Drawing;
using System.Windows.Forms;
using ChassisAssembly.Services;

namespace ChassisAssembly.Forms
{
    /// <summary>
    /// 4x4 齐次变换矩阵标定窗口
    /// 搬运自老代码 MatrixCalibrationForm,改为 2 台跟踪仪下拉
    /// </summary>
    public class MatrixCalibrationForm : Form
    {
        private ComboBox _cmbTrackerSelect;
        private DataGridView _dgvMatrix;
        private int _currentTrackerIndex = 0;

        public MatrixCalibrationForm()
        {
            Text = "⚙️ 坐标系转换矩阵设定 (4x4 Matrix)";
            Size = new Size(540, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = AppColors.BgPrimary;
            ForeColor = AppColors.TextPrimary;
            Font = new Font("微软雅黑", 9F);

            var lblSelect = new Label
            {
                Text = "选择激光跟踪仪:",
                Location = new Point(20, 25),
                AutoSize = true,
                Font = new Font("微软雅黑", 10F),
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent
            };
            Controls.Add(lblSelect);

            _cmbTrackerSelect = new ComboBox
            {
                Location = new Point(140, 22),
                Size = new Size(180, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F)
            };
            _cmbTrackerSelect.Items.AddRange(new string[] { "LT_1 (上站)", "LT_2 (下站)" });
            _cmbTrackerSelect.SelectedIndex = 0;
            _cmbTrackerSelect.SelectedIndexChanged += (s, e) =>
            {
                _currentTrackerIndex = _cmbTrackerSelect.SelectedIndex;
                LoadMatrixToGrid();
            };
            Controls.Add(_cmbTrackerSelect);

            var lblHint = new Label
            {
                Text = "提示: 最后一行 [0,0,0,1] 为齐次坐标固定项,不可编辑",
                Location = new Point(20, 60),
                AutoSize = true,
                Font = new Font("微软雅黑", 8F),
                ForeColor = AppColors.TextSecondary,
                BackColor = Color.Transparent
            };
            Controls.Add(lblHint);

            _dgvMatrix = new DataGridView
            {
                Location = new Point(20, 85),
                Size = new Size(480, 160),
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Consolas", 10F),
                ScrollBars = ScrollBars.None,
                BackgroundColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                GridColor = AppColors.Border,
                EnableHeadersVisualStyles = false,
                BorderStyle = BorderStyle.None
            };
            _dgvMatrix.ColumnHeadersDefaultCellStyle.BackColor = AppColors.BgSecondary;
            _dgvMatrix.ColumnHeadersDefaultCellStyle.ForeColor = AppColors.TextSecondary;
            _dgvMatrix.DefaultCellStyle.BackColor = AppColors.BgSecondary;
            _dgvMatrix.DefaultCellStyle.ForeColor = AppColors.TextPrimary;
            _dgvMatrix.Columns.Add("C0", "Col 1 (X)");
            _dgvMatrix.Columns.Add("C1", "Col 2 (Y)");
            _dgvMatrix.Columns.Add("C2", "Col 3 (Z)");
            _dgvMatrix.Columns.Add("C3", "Col 4 (Trans)");
            for (int i = 0; i < 4; i++) _dgvMatrix.Rows.Add("0", "0", "0", "0");
            _dgvMatrix.Rows[3].ReadOnly = true;
            _dgvMatrix.Rows[3].DefaultCellStyle.BackColor = AppColors.BgSecondary;
            Controls.Add(_dgvMatrix);

            var btnSave = new Button
            {
                Text = "💾 保存矩阵参数",
                Location = new Point(90, 270),
                Size = new Size(160, 40),
                BackColor = AppColors.Success,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.Click += OnSave;
            Controls.Add(btnSave);

            var btnReset = new Button
            {
                Text = "🔄 恢复单位矩阵",
                Location = new Point(270, 270),
                Size = new Size(160, 40),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.Click += OnReset;
            Controls.Add(btnReset);

            LoadMatrixToGrid();
        }

        private void LoadMatrixToGrid()
        {
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    _dgvMatrix.Rows[r].Cells[c].Value =
                        ForceDataProcessor.Instance.TransformMatrices[_currentTrackerIndex, r, c].ToString("F6");
        }

        private void OnSave(object sender, EventArgs e)
        {
            try
            {
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        if (!double.TryParse(_dgvMatrix.Rows[r].Cells[c].Value?.ToString(), out double val))
                        {
                            MessageBox.Show($"第 {r + 1} 行第 {c + 1} 列输入格式错误!", "错误");
                            return;
                        }
                        ForceDataProcessor.Instance.TransformMatrices[_currentTrackerIndex, r, c] = val;
                    }
                }
                MessageBox.Show("矩阵参数已更新,后续测量将采用新矩阵", "成功");
                AppState.Instance.Log("INFO",
                    $"已更新 {_cmbTrackerSelect.SelectedItem} 的坐标变换矩阵");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败: " + ex.Message);
            }
        }

        private void OnReset(object sender, EventArgs e)
        {
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    _dgvMatrix.Rows[r].Cells[c].Value = (r == c) ? "1.000000" : "0.000000";
        }
    }
}
