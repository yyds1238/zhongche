using System;

namespace ChassisAssembly.Models
{
    /// <summary>
    /// 底架上的一个工装/压紧装置/测点
    /// 这是二维图上可点击的对象,点击后进入测量模式
    /// </summary>
    public class FixturePoint
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public FixtureType FixtureType { get; set; }

        // ===== 二维图上的位置 (画布坐标系 0~1000 x 0~260) =====
        public double CanvasX { get; set; }
        public double CanvasY { get; set; }

        // ===== 理论位姿 (mm,车体实际坐标系) =====
        public double TheoreticalX { get; set; }
        public double TheoreticalY { get; set; }
        public double TheoreticalZ { get; set; }

        // ===== 实测位姿 (mm) =====
        public double ActualX { get; set; }
        public double ActualY { get; set; }
        public double ActualZ { get; set; }

        // ===== 偏差 =====
        public double DeltaX => ActualX - TheoreticalX;
        public double DeltaY => ActualY - TheoreticalY;
        public double DeltaZ => ActualZ - TheoreticalZ;

        /// <summary>综合偏差模长</summary>
        public double DeviationNorm => Math.Sqrt(DeltaX * DeltaX + DeltaY * DeltaY + DeltaZ * DeltaZ);

        // ===== 传感器数据 =====
        public double PressForce { get; set; } = 0;    // 压紧力 N
        public double Deformation { get; set; } = 0;   // 形变 μm
        public bool HasForceSensor { get; set; } = true; // 该工装是否带力反馈

        // ===== 状态 =====
        public PointStatus Status { get; set; } = PointStatus.NotMeasured;
        public bool IsSelected { get; set; } = false;
        public DateTime? LastMeasuredAt { get; set; }

        /// <summary>指派给哪台跟踪仪测 (LT_1 / LT_2)</summary>
        public string AssignedTracker { get; set; } = "LT_1";
    }

    /// <summary>单次测量记录</summary>
    public class MeasurementRecord
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string FixtureId { get; set; } = "";
        public string TrackerName { get; set; } = "LT_1";
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Force { get; set; }
        public double Deformation { get; set; }
        public MeasureMode Mode { get; set; }
        public ProcessStep InStep { get; set; }
    }
}
