using System;
using System.Collections.Generic;
using ChassisAssembly.Models;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 全局应用状态 - 单例
    /// 跨窗口/跨 UserControl 共享数据
    /// </summary>
    public class AppState
    {
        private static AppState _instance;
        public static AppState Instance => _instance ?? (_instance = new AppState());

        /// <summary>当前打开的项目</summary>
        public ProjectFile CurrentProject { get; set; }

        /// <summary>跟踪仪服务 (默认 Mock,对接真机后换成 LMF / ZTS)</summary>
        public ILaserTrackerService TrackerService { get; set; } = new MockTrackerService();

        /// <summary>压紧装置控制器 (默认 Mock,PLC 协议定后替换为真实实现)</summary>
        public IFixtureController FixtureController { get; set; } = new MockFixtureController();

        /// <summary>跟踪仪连接状态 (key: "LT_1" / "LT_2")</summary>
        public Dictionary<string, ConnectionStatus> TrackerStatuses { get; }
            = new Dictionary<string, ConnectionStatus>
            {
                { "LT_1", ConnectionStatus.Disconnected },
                { "LT_2", ConnectionStatus.Disconnected }
            };

        /// <summary>跟踪仪 IP 配置</summary>
        public Dictionary<string, string> TrackerIps { get; }
            = new Dictionary<string, string>
            {
                { "LT_1", "192.168.1.101" },
                { "LT_2", "192.168.1.102" }
            };

        /// <summary>当前主模块</summary>
        public MainModule CurrentModule { get; set; } = MainModule.CommonProcesses;

        /// <summary>全局日志</summary>
        public List<LogEntry> GlobalLogs { get; } = new List<LogEntry>();

        public event EventHandler LogAdded;
        public event EventHandler<string> TrackerStatusChanged;

        public void Log(string level, string message)
        {
            var entry = new LogEntry { Timestamp = DateTime.Now, Level = level, Message = message };
            GlobalLogs.Insert(0, entry);
            while (GlobalLogs.Count > 500) GlobalLogs.RemoveAt(GlobalLogs.Count - 1);
            LogAdded?.Invoke(this, EventArgs.Empty);
        }

        public void SetTrackerStatus(string tracker, ConnectionStatus status)
        {
            TrackerStatuses[tracker] = status;
            TrackerStatusChanged?.Invoke(this, tracker);
        }
    }
}
