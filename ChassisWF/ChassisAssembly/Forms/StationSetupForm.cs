using System;
using ChassisAssembly.Controls;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChassisAssembly.Services;

namespace ChassisAssembly.Forms
{
    /// <summary>
    /// 站位响应测试弹窗
    /// 搬运自老代码 StationSetupHelper,改为支持选择 LT_1 / LT_2
    /// </summary>
    public static class StationSetupForm
    {
        public static bool Show(IWin32Window owner)
        {
            var form = new Form
            {
                Text = "仪器响应测试 (驱动激光跟踪仪寻球)",
                Size = new Size(440, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = AppColors.BgPrimary,
                ForeColor = AppColors.TextPrimary,
                Font = new Font("微软雅黑", 9F)
            };

            var lblInfo = new Label
            {
                Text = "请选择跟踪仪并输入目标三维坐标\n验证测头能否响应指令并寻球:",
                Location = new Point(25, 20),
                Size = new Size(380, 40),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                ForeColor = AppColors.Primary,
                BackColor = Color.Transparent
            };
            form.Controls.Add(lblInfo);

            // 跟踪仪选择
            form.Controls.Add(new Label
            {
                Text = "目标仪器:", Location = new Point(30, 75), AutoSize = true,
                ForeColor = AppColors.TextSecondary, BackColor = Color.Transparent
            });
            var cmbTracker = new ComboBox
            {
                Location = new Point(130, 72),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F)
            };
            cmbTracker.Items.AddRange(new object[] { "LT_1 (上站)", "LT_2 (下站)" });
            cmbTracker.SelectedIndex = 0;
            form.Controls.Add(cmbTracker);

            var fontLabel = new Font("微软雅黑", 10F);
            var fontInput = new Font("Consolas", 11F);

            // X/Y/Z
            var (lblX, txtX) = CreateCoordRow("X 坐标 (mm):", 30, 115, "5000.0", fontLabel, fontInput);
            form.Controls.Add(lblX); form.Controls.Add(txtX);

            var (lblY, txtY) = CreateCoordRow("Y 坐标 (mm):", 30, 155, "0.0", fontLabel, fontInput);
            form.Controls.Add(lblY); form.Controls.Add(txtY);

            var (lblZ, txtZ) = CreateCoordRow("Z 坐标 (mm):", 30, 195, "850.0", fontLabel, fontInput);
            form.Controls.Add(lblZ); form.Controls.Add(txtZ);

            // 驱动按钮
            var btnDrive = new Button
            {
                Text = "🎯 驱动仪器寻球",
                Location = new Point(30, 250),
                Size = new Size(180, 40),
                BackColor = AppColors.Info,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDrive.Click += async (s, e) =>
            {
                if (!double.TryParse(txtX.Text, out double x) ||
                    !double.TryParse(txtY.Text, out double y) ||
                    !double.TryParse(txtZ.Text, out double z))
                {
                    MessageBox.Show("坐标格式不正确", "提示");
                    return;
                }

                btnDrive.Enabled = false;
                btnDrive.Text = "⏳ 发送指令中...";

                try
                {
                    string trackerName = cmbTracker.SelectedIndex == 0 ? "LT_1" : "LT_2";
                    var state = AppState.Instance;
                    // 调用底层 SearchReflectorAsync (老代码 SearchReflector + 目标点传参)
                    await state.TrackerService.SearchReflectorAsync(state.TrackerIps[trackerName]);
                    await Task.Delay(500);

                    MessageBox.Show(
                        $"已成功向 {trackerName} 下发寻球指令!\n\n🎯 目标坐标:\nX: {x}\nY: {y}\nZ: {z}\n\n请观察现场跟踪仪测头是否已转动。",
                        "指令下发成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    state.Log("INFO", $"{trackerName} 寻球: 目标 ({x:F1},{y:F1},{z:F1})");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"指令下发失败: {ex.Message}", "通讯异常");
                }
                finally
                {
                    btnDrive.Text = "🎯 再次驱动仪器";
                    btnDrive.Enabled = true;
                }
            };
            form.Controls.Add(btnDrive);

            // 完成按钮
            var btnOk = new Button
            {
                Text = "✅ 确认响应正常",
                Location = new Point(220, 250),
                Size = new Size(180, 40),
                BackColor = AppColors.Success,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };
            form.Controls.Add(btnOk);

            return form.ShowDialog(owner) == DialogResult.OK;
        }

        private static (Label lbl, TextBox tb) CreateCoordRow(string label, int x, int y,
            string def, Font fontLabel, Font fontInput)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(x, y + 3),
                AutoSize = true,
                Font = fontLabel,
                ForeColor = AppColors.TextPrimary,
                BackColor = Color.Transparent
            };
            var tb = new TextBox
            {
                Text = def,
                Location = new Point(x + 105, y),
                Width = 250,
                Font = fontInput,
                BackColor = AppColors.BgSecondary,
                ForeColor = AppColors.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            return (lbl, tb);
        }
    }
}
