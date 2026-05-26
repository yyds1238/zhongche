using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChassisAssembly.Models;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 压紧装置控制器接口 — 支持"力-位耦合驱动大负载高精度协同控制"
    ///
    /// 接口设计依据课题任务书(2025ZD1608301)关键技术 3:
    /// - 位置控制轴与力控制轴分布策略
    /// - 雅可比矩阵最大线性无关组选择位置控制轴
    /// - 力控制轴约束协调模型
    /// - 阻抗参数配置满足期望动力学关系
    ///
    /// 真实对接待 PLC 协议确定,目前有 Mock 实现
    /// </summary>
    public interface IFixtureController
    {
        /// <summary>
        /// 协同运动规划 — 多个定位器协同到达各自目标位姿
        /// 内部需要:
        /// - 同步各轴运动时序 (位置控制轴)
        /// - 约束接触力在阻抗参数内 (力控制轴)
        /// - 避免机械干涉 (运动学可达性检查)
        /// - 预留紧急停止点
        /// </summary>
        Task<CoordinationResult> CoordinatedMoveAsync(
            CoordinationPlan plan,
            IProgress<CoordinationProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>预检测: 规划是否可达、有无干涉风险、力约束是否满足</summary>
        Task<ValidationResult> ValidatePlanAsync(CoordinationPlan plan);

        /// <summary>只读取当前所有工装实时位姿与力反馈(闭环用)</summary>
        Task<IReadOnlyList<FixtureFeedback>> ReadFeedbackAsync();

        /// <summary>紧急停止 - 切断所有运动,保持保压</summary>
        Task EmergencyStopAsync();

        /// <summary>控制器是否就绪</summary>
        bool IsReady { get; }

        /// <summary>下位机协议名 (Mock / YingChengAC800 / Modbus-TCP 等)</summary>
        string ProtocolName { get; }

        /// <summary>控制器状态变化事件</summary>
        event EventHandler<ControllerStatusEventArgs> StatusChanged;

        // ============================================================
        // 向后兼容: 保留旧版简单接口, 内部转调新接口
        // ============================================================

        /// <summary>[兼容] 简单定位 — 内部自动构造 CoordinationPlan 后调 CoordinatedMoveAsync</summary>
        Task<bool> PositionFixturesAsync(List<FixturePoint> fixtures);

        /// <summary>[兼容] 简单调姿 — 内部自动构造 CoordinationPlan 后调 CoordinatedMoveAsync</summary>
        Task<bool> AlignComponentAsync(List<FixturePoint> fixtures);
    }

    // ============================================================
    // 数据结构
    // ============================================================

    public class CoordinationPlan
    {
        public string Name { get; set; } = "";
        public CoordinationMode Mode { get; set; } = CoordinationMode.PositionOnly;
        public List<FixtureTargetPose> Targets { get; set; } = new List<FixtureTargetPose>();
        public ImpedanceParams Impedance { get; set; } = new ImpedanceParams();
        public double MaxContactForce { get; set; } = 2500.0;
        public double SyncTolerance { get; set; } = 0.3;
        public int SpeedOverride { get; set; } = 30;
    }

    public enum CoordinationMode
    {
        PositionOnly,
        ForceOnly,
        HybridPositionForce,
        Compliant
    }

    public class FixtureTargetPose
    {
        public string FixtureId { get; set; } = "";
        public double TargetX { get; set; }
        public double TargetY { get; set; }
        public double TargetZ { get; set; }
        /// <summary>姿态角 (度), 对应 ±0.1° 课题指标</summary>
        public double TargetRx { get; set; }
        public double TargetRy { get; set; }
        public double TargetRz { get; set; }
        /// <summary>力控目标 (N)</summary>
        public double TargetForce { get; set; }
    }

    public class ImpedanceParams
    {
        public double StiffnessX { get; set; } = 1000.0;
        public double StiffnessY { get; set; } = 1000.0;
        public double StiffnessZ { get; set; } = 1500.0;
        public double DampingX { get; set; } = 50.0;
        public double DampingY { get; set; } = 50.0;
        public double DampingZ { get; set; } = 80.0;
    }

    public class CoordinationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public double MaxPositionError { get; set; }
        public double MaxAttitudeError { get; set; }
        public double MaxContactForce { get; set; }
        public List<FixtureFeedback> FinalState { get; set; } = new List<FixtureFeedback>();
    }

    public class CoordinationProgress
    {
        public double PercentComplete { get; set; }
        public string CurrentPhase { get; set; } = "";
        public IReadOnlyList<FixtureFeedback> LiveFeedback { get; set; }
    }

    public class FixtureFeedback
    {
        public string FixtureId { get; set; } = "";
        public double CurrentX { get; set; }
        public double CurrentY { get; set; }
        public double CurrentZ { get; set; }
        public double CurrentForce { get; set; }
        public bool HasFault { get; set; }
        public string FaultCode { get; set; } = "";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
    }

    public class ControllerStatusEventArgs : EventArgs
    {
        public bool IsReady { get; set; }
        public string Message { get; set; } = "";
    }
}
