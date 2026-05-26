using System.Collections.Generic;
using System.Linq;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 力位数据后处理器 (单例) — 承担两项工作:
    /// 1. 按照 4x4 齐次变换矩阵将各跟踪仪原始坐标映射到车体全局坐标系
    /// 2. 可选的滑动窗口平滑滤波 (默认 10 帧均值)
    ///
    /// 车体底架项目改动:
    /// - 矩阵数组从 [4] 缩减为 [2] (对应 LT_1 / LT_2)
    /// - 其他逻辑完全照搬老代码
    /// </summary>
    public class ForceDataProcessor
    {
        private static ForceDataProcessor _instance;
        public static ForceDataProcessor Instance => _instance ?? (_instance = new ForceDataProcessor());

        public const int TRACKER_COUNT = 2;

        /// <summary>每台跟踪仪的 4x4 齐次变换矩阵 (由 MatrixCalibrationForm 填写)</summary>
        public double[,,] TransformMatrices { get; private set; }

        /// <summary>是否启用滑动平滑</summary>
        public bool EnableSmoothing { get; set; } = true;

        /// <summary>滑动窗口长度</summary>
        public int SmoothingWindow { get; set; } = 10;

        private readonly Queue<ForcePositionData> _buffer = new Queue<ForcePositionData>();

        private ForceDataProcessor()
        {
            TransformMatrices = new double[TRACKER_COUNT, 4, 4];
            ResetToIdentity();
        }

        public void ResetToIdentity()
        {
            for (int t = 0; t < TRACKER_COUNT; t++)
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++)
                        TransformMatrices[t, r, c] = (r == c) ? 1.0 : 0.0;
        }

        public void ClearBuffer() => _buffer.Clear();

        /// <summary>
        /// 把一帧原始数据经 矩阵变换 + 滑动平均 处理成干净数据
        /// </summary>
        public ForcePositionData ProcessRawData(ForcePositionData raw)
        {
            if (raw == null) return null;

            var transformed = ApplyMatrices(raw);

            if (!EnableSmoothing) return transformed;

            _buffer.Enqueue(transformed);
            while (_buffer.Count > SmoothingWindow) _buffer.Dequeue();

            return AverageBuffer();
        }

        private ForcePositionData ApplyMatrices(ForcePositionData raw)
        {
            var result = new ForcePositionData();
            for (int i = 0; i < TRACKER_COUNT; i++)
            {
                double x = raw.TrackerX[i];
                double y = raw.TrackerY[i];
                double z = raw.TrackerZ[i];
                // 齐次坐标乘法: [x' y' z' 1] = M * [x y z 1]
                result.TrackerX[i] = TransformMatrices[i, 0, 0] * x + TransformMatrices[i, 0, 1] * y
                                  + TransformMatrices[i, 0, 2] * z + TransformMatrices[i, 0, 3];
                result.TrackerY[i] = TransformMatrices[i, 1, 0] * x + TransformMatrices[i, 1, 1] * y
                                  + TransformMatrices[i, 1, 2] * z + TransformMatrices[i, 1, 3];
                result.TrackerZ[i] = TransformMatrices[i, 2, 0] * x + TransformMatrices[i, 2, 1] * y
                                  + TransformMatrices[i, 2, 2] * z + TransformMatrices[i, 2, 3];
            }
            // 力值不变(力不受坐标系影响)
            for (int i = 0; i < raw.SensorForces.Length; i++)
                result.SensorForces[i] = raw.SensorForces[i];
            return result;
        }

        private ForcePositionData AverageBuffer()
        {
            if (_buffer.Count == 0) return new ForcePositionData();
            var snapshot = _buffer.ToArray();
            var avg = new ForcePositionData();
            int n = snapshot.Length;
            for (int i = 0; i < TRACKER_COUNT; i++)
            {
                avg.TrackerX[i] = snapshot.Average(f => f.TrackerX[i]);
                avg.TrackerY[i] = snapshot.Average(f => f.TrackerY[i]);
                avg.TrackerZ[i] = snapshot.Average(f => f.TrackerZ[i]);
            }
            for (int i = 0; i < 4; i++)
                avg.SensorForces[i] = snapshot.Average(f => f.SensorForces[i]);
            return avg;
        }
    }
}
