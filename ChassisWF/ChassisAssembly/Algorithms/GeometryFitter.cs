using System;
using System.Collections.Generic;
using System.Linq;
using ChassisAssembly.Models;

namespace ChassisAssembly.Algorithms
{
    /// <summary>
    /// 几何拟合算法 — 从老代码 GeneratorAlignmentControl.FitCircle 抽出来
    /// 包括:
    /// - 最小二乘圆拟合 (侧视图中心轴计算用)
    /// - 最小二乘直线拟合 (工艺基准轴线建立用)
    /// - 平面拟合 (水平基准建立用)
    /// </summary>
    public static class GeometryFitter
    {
        /// <summary>
        /// 最小二乘圆拟合 - 在 Y-Z 平面上(忽略 X)
        /// 返回圆心 (CenterY, CenterZ) 和半径
        /// </summary>
        public static Circle3D FitCircle(List<Point3D> points)
        {
            if (points == null || points.Count < 3)
                return new Circle3D { IsValid = false };

            double sumY = 0, sumZ = 0, sumY2 = 0, sumZ2 = 0, sumYZ = 0;
            double sumY2_Z2_Y = 0, sumY2_Z2_Z = 0, sumY2_Z2 = 0;
            int n = points.Count;

            foreach (var p in points)
            {
                double y = p.Y, z = p.Z;
                double y2 = y * y, z2 = z * z;
                sumY += y; sumZ += z;
                sumY2 += y2; sumZ2 += z2; sumYZ += y * z;
                sumY2_Z2_Y += (y2 + z2) * y;
                sumY2_Z2_Z += (y2 + z2) * z;
                sumY2_Z2 += (y2 + z2);
            }

            double D = sumY2 * (sumZ2 * n - sumZ * sumZ)
                     - sumYZ * (sumYZ * n - sumZ * sumY)
                     + sumY * (sumYZ * sumZ - sumZ2 * sumY);
            if (Math.Abs(D) < 1e-9) return new Circle3D { IsValid = false };

            double Da = sumY2_Z2_Y * (sumZ2 * n - sumZ * sumZ)
                      - sumYZ * (sumY2_Z2_Z * n - sumZ * sumY2_Z2)
                      + sumY * (sumY2_Z2_Z * sumZ - sumZ2 * sumY2_Z2);
            double Db = sumY2 * (sumY2_Z2_Z * n - sumZ * sumY2_Z2)
                      - sumY2_Z2_Y * (sumYZ * n - sumZ * sumY)
                      + sumY * (sumYZ * sumY2_Z2 - sumY2_Z2_Z * sumY);
            double Dc = sumY2 * (sumZ2 * sumY2_Z2 - sumY2_Z2_Z * sumZ)
                      - sumYZ * (sumYZ * sumY2_Z2 - sumY2_Z2_Z * sumY)
                      + sumY2_Z2_Y * (sumYZ * sumZ - sumZ2 * sumY);

            double a = Da / D, b = Db / D, c = Dc / D;

            return new Circle3D
            {
                CenterY = a / 2.0,
                CenterZ = b / 2.0,
                Radius = Math.Sqrt(c / D + (a / 2.0) * (a / 2.0) + (b / 2.0) * (b / 2.0)),
                IsValid = true
            };
        }

        /// <summary>
        /// 最小二乘直线拟合 - 用于工艺基准轴线建立
        /// 以 X 轴为参数,返回起点和终点
        /// </summary>
        public static Line3D FitLine3D(List<Point3D> points)
        {
            if (points == null || points.Count < 2)
                return new Line3D { IsValid = false };

            // 按 X 排序
            var sorted = points.OrderBy(p => p.X).ToList();

            // 计算均值
            double mx = sorted.Average(p => p.X);
            double my = sorted.Average(p => p.Y);
            double mz = sorted.Average(p => p.Z);

            // Y 对 X 的斜率
            double sumXY = 0, sumXX = 0;
            foreach (var p in sorted)
            {
                double dx = p.X - mx;
                sumXY += dx * (p.Y - my);
                sumXX += dx * dx;
            }
            double ky = sumXX > 1e-9 ? sumXY / sumXX : 0;

            // Z 对 X 的斜率
            double sumXZ = 0;
            foreach (var p in sorted)
            {
                double dx = p.X - mx;
                sumXZ += dx * (p.Z - mz);
            }
            double kz = sumXX > 1e-9 ? sumXZ / sumXX : 0;

            double x0 = sorted.First().X, xN = sorted.Last().X;
            return new Line3D
            {
                Start = new Point3D("S", x0, my + ky * (x0 - mx), mz + kz * (x0 - mx)),
                End   = new Point3D("E", xN, my + ky * (xN - mx), mz + kz * (xN - mx)),
                IsValid = true
            };
        }
    }
}
