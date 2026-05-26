using System.Threading.Tasks;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 激光跟踪仪接口
    /// 实现类有:
    /// 1. LeicaLMFTrackerService  - 徕卡 LMF SDK (从老代码 LaserTrackerService.cs 搬)
    /// 2. ZTS3300TrackerService   - 国产中图 GTS3300 (方案文档选型的设备)
    /// 3. MockTrackerService      - Mock 实现,用于离线调试
    /// </summary>
    public interface ILaserTrackerService
    {
        /// <summary>连接跟踪仪 (20s 超时)</summary>
        Task<bool> ConnectAsync(string ipAddress);

        /// <summary>断开</summary>
        void Disconnect(string ipAddress);

        /// <summary>硬件初始化(冷启动需 15~30s,自动转动镜头找零点)</summary>
        Task<bool> InitializeAsync(string ipAddress);

        /// <summary>单点测量(返回 mm 单位)</summary>
        Task<MeasureResult> MeasureSinglePointAsync(string ipAddress);

        /// <summary>引光/寻球(电机螺旋扫描)</summary>
        Task<bool> SearchReflectorAsync(string ipAddress);

        /// <summary>检查连接状态</summary>
        bool IsConnected(string ipAddress);
    }

    public class MeasureResult
    {
        public bool IsSuccess { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string ErrorMessage { get; set; } = "";

        public static MeasureResult Fail(string err) =>
            new MeasureResult { IsSuccess = false, ErrorMessage = err };

        public static MeasureResult Success(double x, double y, double z) =>
            new MeasureResult { IsSuccess = true, X = x, Y = y, Z = z };
    }
}
