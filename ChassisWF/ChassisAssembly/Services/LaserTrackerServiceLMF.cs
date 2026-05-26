// ======================================================================
// Leica LMF SDK 实现 - 完全参照老代码 LaserTrackerService.cs 的逻辑
// 当前用 #if false 注释掉,正式对接时取消注释并添加 LMF.Tracker 引用
// ======================================================================
#if LMF_SDK_AVAILABLE

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using LMF.Tracker;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 徕卡 LMF SDK 实现 (老代码搬运)
    /// 后续如果换成中图 GTS3300,另起一个 ZTS3300TrackerService 即可
    /// </summary>
    public class LaserTrackerServiceLMF : ILaserTrackerService
    {
        private static readonly Dictionary<string, object> _trackers = new Dictionary<string, object>();

        public async Task<bool> ConnectAsync(string ipAddress)
        {
            try
            {
                var connectTask = Task.Run(() =>
                {
                    try
                    {
                        var connection = new LMF.Tracker.Connection();
                        var tracker = connection.Connect(ipAddress);
                        if (tracker != null)
                        {
                            _trackers[ipAddress] = tracker;
                            return true;
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("底层通讯报错: " + ex.Message);
                        return false;
                    }
                });

                // 20 秒宽容度 - 冷启动友好
                if (await Task.WhenAny(connectTask, Task.Delay(20000)) == connectTask)
                    return await connectTask;

                MessageBox.Show("首次连接耗时过长!请检查仪器是否开机或拔插网线重试。",
                    "连接超时", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect(string ipAddress)
        {
            if (_trackers.TryGetValue(ipAddress, out var tracker))
            {
                try { ((dynamic)tracker).Disconnect(); } catch { }
                _trackers.Remove(ipAddress);
            }
        }

        public async Task<bool> InitializeAsync(string ipAddress)
        {
            if (!_trackers.ContainsKey(ipAddress)) return false;
            return await Task.Run(() =>
            {
                try
                {
                    var tracker = (LMF.Tracker.Tracker)_trackers[ipAddress];
                    tracker.Initialize();
                    return true;
                }
                catch { return false; }
            });
        }

        public async Task<MeasureResult> MeasureSinglePointAsync(string ipAddress)
        {
            if (!_trackers.ContainsKey(ipAddress))
                return MeasureResult.Fail("未连接");

            return await Task.Run(() =>
            {
                var tracker = (LMF.Tracker.Tracker)_trackers[ipAddress];
                try
                {
                    dynamic result = tracker.Measurement.MeasureStationary();
                    double x, y, z;
                    try
                    {
                        // 属性命名 1
                        x = result.Position.X * 1000.0;
                        y = result.Position.Y * 1000.0;
                        z = result.Position.Z * 1000.0;
                    }
                    catch
                    {
                        // 属性命名 2 (兼容不同 SDK 版本)
                        x = result.Coordinate.X * 1000.0;
                        y = result.Coordinate.Y * 1000.0;
                        z = result.Coordinate.Z * 1000.0;
                    }
                    return MeasureResult.Success(x, y, z);
                }
                catch (Exception ex)
                {
                    string errMsg = ex.ToString();
                    // 精准捕获 200004 未初始化异常
                    if (errMsg.Contains("200004") || errMsg.Contains("initialized"))
                    {
                        return MeasureResult.Fail("200004:未初始化,请先执行硬件初始化");
                    }
                    return MeasureResult.Fail(ex.Message);
                }
            });
        }

        public async Task<bool> SearchReflectorAsync(string ipAddress)
        {
            if (!_trackers.ContainsKey(ipAddress)) return false;
            return await Task.Run(() =>
            {
                try
                {
                    var tracker = (LMF.Tracker.Tracker)_trackers[ipAddress];
                    tracker.TargetSearch.Start();
                    return true;
                }
                catch { return false; }
            });
        }

        public bool IsConnected(string ipAddress) => _trackers.ContainsKey(ipAddress);
    }
}
#endif
