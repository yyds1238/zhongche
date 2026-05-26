using System;
using System.Windows.Forms;
using ChassisAssembly.Forms;

namespace ChassisAssembly
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 启动窗口:选择新建或打开项目
            using (var startup = new StartupForm())
            {
                if (startup.ShowDialog() != DialogResult.OK || startup.CreatedProject == null)
                    return;

                // 进入主窗口
                var main = new MainForm(startup.CreatedProject);
                Application.Run(main);
            }
        }
    }
}
