using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// CAS 力位协同系统通讯管理器 (单例) — 搬运自老代码,仅做参数调整
    ///
    /// 车体底架项目修改:
    /// 1. 跟踪仪数从 4 台 (AT1/2/3/4) 改为 2 台 (LT_1/LT_2),但协议帧仍按老格式解析,后两通道数据丢弃
    /// 2. 力传感器数保留 4 路 (枕梁/牵引/贯穿/边梁各种压紧装置都有力反馈,具体分配见 AppState)
    /// 3. UI 限流阀 50ms 不变
    /// 4. 成功连接的 MessageBox 通知改为事件回调,减少 UI 弹窗干扰
    ///
    /// 协议帧格式 (与老代码一致):
    /// - 帧头 AA55,帧尾 CCAA
    /// - 每台跟踪仪 24 字节 (X/Y/Z 各 8 字节 IEEE754 float)
    /// - 4 个跟踪仪 x 24 = 96 字节
    /// - 4 路力传感器 x 8 = 32 字节
    /// </summary>
    public class CASCommunicationManager
    {
        private static CASCommunicationManager _instance;
        public static CASCommunicationManager Instance => _instance ?? (_instance = new CASCommunicationManager());

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private long _lastInvokeTime = 0;

        public event Action<ForcePositionData> OnForceDataReceived;
        public event Action<bool, string> OnConnectionStateChanged;
        public ForcePositionData LatestForceData { get; private set; }
        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);
                _stream = _tcpClient.GetStream();

                _cts = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cts.Token);
                OnConnectionStateChanged?.Invoke(true, $"CAS 已连接 {ip}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                OnConnectionStateChanged?.Invoke(false, $"CAS 连接失败: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            OnConnectionStateChanged?.Invoke(false, "CAS 已断开");
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            string incomplete = "";

            while (!token.IsCancellationRequested && IsConnected)
            {
                try
                {
                    int n = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (n <= 0) break;

                    string hexStr = BitConverter.ToString(buffer, 0, n).Replace("-", "");
                    hexStr = hexStr.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    incomplete += hexStr;

                    while (incomplete.Contains("AA55") && incomplete.Contains("CCAA"))
                    {
                        int hi = incomplete.IndexOf("AA55");
                        int ti = incomplete.IndexOf("CCAA", hi);
                        if (ti > hi)
                        {
                            int len = (ti + 4) - hi;
                            string frame = incomplete.Substring(hi, len);
                            ParseFrame(frame);
                            incomplete = incomplete.Substring(ti + 4);
                        }
                        else break;
                    }

                    if (incomplete.Length > 20000) incomplete = "";
                }
                catch { break; }
            }
        }

        private void ParseFrame(string frame)
        {
            if (frame.Length < 132) return;

            var data = new ForcePositionData();
            try
            {
                // 解析 4 台测量设备的 X/Y/Z (但车体底架项目只用前 2 台,后 2 台数据保留兼容协议)
                for (int i = 0; i < 4; i++)
                {
                    data.TrackerX[i] = HexToFloat(frame.Substring(4 + i * 24, 8));
                    data.TrackerY[i] = HexToFloat(frame.Substring(12 + i * 24, 8));
                    data.TrackerZ[i] = HexToFloat(frame.Substring(20 + i * 24, 8));
                    data.SensorForces[i] = HexToFloat(frame.Substring(100 + i * 8, 8));
                }

                LatestForceData = data;

                // UI 限流: 20 FPS
                long now = Environment.TickCount;
                if (now - _lastInvokeTime > 50)
                {
                    _lastInvokeTime = now;
                    Task.Run(() => OnForceDataReceived?.Invoke(data));
                }
            }
            catch { }
        }

        private static float HexToFloat(string hex)
        {
            uint num = Convert.ToUInt32(hex, 16);
            byte[] bytes = BitConverter.GetBytes(num);
            return BitConverter.ToSingle(bytes, 0);
        }
    }

    /// <summary>CAS 一帧数据</summary>
    public class ForcePositionData
    {
        public double[] TrackerX { get; set; } = new double[4];
        public double[] TrackerY { get; set; } = new double[4];
        public double[] TrackerZ { get; set; } = new double[4];
        public double[] SensorForces { get; set; } = new double[4];
    }
}
