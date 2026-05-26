using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// Mock 实现 - 没有真实跟踪仪时用于调试
    /// </summary>
    public class MockTrackerService : ILaserTrackerService
    {
        private readonly HashSet<string> _connected = new HashSet<string>();
        private readonly Random _random = new Random();

        public async Task<bool> ConnectAsync(string ipAddress)
        {
            await Task.Delay(500); // 模拟握手
            _connected.Add(ipAddress);
            return true;
        }

        public void Disconnect(string ipAddress)
        {
            _connected.Remove(ipAddress);
        }

        public async Task<bool> InitializeAsync(string ipAddress)
        {
            if (!_connected.Contains(ipAddress)) return false;
            await Task.Delay(1500); // 模拟硬件初始化
            return true;
        }

        public async Task<MeasureResult> MeasureSinglePointAsync(string ipAddress)
        {
            if (!_connected.Contains(ipAddress))
                return MeasureResult.Fail("未连接");

            await Task.Delay(300); // 模拟测量延迟

            // 返回一个基础位置 + 小扰动
            double x = 5000 + (_random.NextDouble() - 0.5) * 2.0;
            double y = (_random.NextDouble() - 0.5) * 2.0;
            double z = 850 + (_random.NextDouble() - 0.5) * 1.5;

            return MeasureResult.Success(x, y, z);
        }

        public async Task<bool> SearchReflectorAsync(string ipAddress)
        {
            if (!_connected.Contains(ipAddress)) return false;
            await Task.Delay(800); // 模拟螺旋扫描
            return true;
        }

        public bool IsConnected(string ipAddress) => _connected.Contains(ipAddress);
    }
}
