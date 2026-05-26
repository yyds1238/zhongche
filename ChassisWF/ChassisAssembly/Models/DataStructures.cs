using System;
using System.Collections.Generic;

namespace ChassisAssembly.Models
{
    // 与老程序保持兼容的基础几何结构
    public struct Point3D
    {
        public string ID;
        public double X, Y, Z;

        public Point3D(string id, double x, double y, double z)
        { ID = id; X = x; Y = y; Z = z; }
    }

    public struct Line3D
    {
        public Point3D Start;
        public Point3D End;
        public bool IsValid;
    }

    public struct Circle3D
    {
        public double CenterY;
        public double CenterZ;
        public double Radius;
        public bool IsValid;
    }

    /// <summary>
    /// 工装/压紧装置类型枚举 (对应方案文档 5.5.1 中列出的定位装置)
    /// 后续若有新类型,在此枚举末尾追加即可
    /// </summary>
    public enum FixtureType
    {
        LongitudinalDatum,    // 纵向定位基准装置
        LateralDatum,         // 横向定位基准装置
        BolsterPress,         // 枕梁压紧装置
        TransformerBeamPress, // 变压器梁压紧装置
        TractionLongPress,    // 牵引梁纵向定位压紧
        TractionLatPress,     // 牵引梁横向定位压紧
        SideBeamPress,        // 边梁压紧装置
        PiercingSupport,      // 贯穿梁定位支撑座
        CenterBeamFixture,    // 中心纵梁工装 (4.4 用,目前设计阶段,位置未定)
        PartitionBeamFixture  // 隔墙梁工装 (4.9 用,目前设计阶段,位置未定)
    }

    /// <summary>
    /// 工装点位状态 (二维图上显示用)
    /// </summary>
    public enum PointStatus
    {
        NotMeasured,    // 未测 - 灰
        Measuring,      // 测量中 - 橙
        Qualified,      // 合格 - 绿
        OutOfTolerance  // 超差 - 红
    }

    /// <summary>
    /// 工艺流程步骤
    /// - 101~104: 通用公共工序(所有车型相同)
    /// - 1~9: 装配工序(4.1~4.9, 车型特定)
    /// 数字故意不连续,方便扩展公共工序
    /// </summary>
    public enum ProcessStep
    {
        // ========== 通用公共工序 ==========
        TrackerSetup             = 101,  // 跟踪仪联合建站
        MeasureFieldBuild        = 102,  // 测量场构建 (USMN)
        DatumEstablish           = 103,  // 基准建立 (纵/横/水平基准)
        FixturePositioning       = 104,  // 工装定位 (粗定位→测量→对比→精定位)

        // ========== 装配工序 ==========
        BolsterAssembly          = 1,   // 4.1 枕梁装配
        TransformerBeamAssembly  = 2,   // 4.2 变压器梁装配
        TractionBeamAssembly     = 3,   // 4.3 牵引梁装配
        CenterBeamAssembly       = 4,   // 4.4 中心纵梁装配
        BeamsCenterAlign         = 5,   // 4.5 各大梁纵向中心对齐(只调姿)
        SideBeamAssembly         = 6,   // 4.6 边梁装配
        TractionBeamPositioning  = 7,   // 4.7 牵引梁定位
        ChassisWidthAdjust       = 8,   // 4.8 底架宽度调整(只调姿)
        PartitionBeamAssembly    = 9    // 4.9 隔墙梁装配
    }

    /// <summary>步骤分类</summary>
    public enum StepCategory
    {
        Common,    // 通用公共工序
        Assembly   // 装配工序
    }

    /// <summary>步骤完成状态</summary>
    public enum StepProgress { Locked, Active, Completed, Error }

    /// <summary>
    /// 单个装配步骤内部的 4 个子阶段(对应卡片式界面)
    /// 4.5/4.8 这种"只调姿不装配"的步骤,跳过 FixturePositioning 和 ComponentHoisting,只走后两阶段
    /// </summary>
    public enum SubStage
    {
        FixturePositioning = 0,  // ① 压紧装置定位就位
        ComponentHoisting  = 1,  // ② 构件吊装就位
        ComponentAlignment = 2,  // ③ 构件调姿(直接下发,不测靶球)
        Finish             = 3   // ④ 完成
    }

    public enum SubStageState
    {
        Pending,    // 未开始
        Active,     // 进行中
        Completed,  // 已完成
        Skipped     // 跳过(4.5/4.8 的前两阶段)
    }

    /// <summary>
    /// 通用测量模式 (与老程序一致)
    /// </summary>
    public enum MeasureMode
    {
        SinglePoint,         // 单点测量
        Continuous,          // 连续测量
        CollaborativeSingle, // 协同单点测量
        CollaborativeCont    // 协同连续测量
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus { Disconnected, Connecting, Connected, Error }

    /// <summary>
    /// 左侧一级模块 (2026.04.27 重构)
    /// 取消"通用测量"独立模块,所有工序统一在装配流程下管理
    /// </summary>
    public enum MainModule
    {
        CommonProcesses,   // 通用公共工序 (建站/测量场/基准/工装定位)
        VehicleAssembly    // 车型装配工序 (HXD1C 4.1~4.9)
    }

    /// <summary>
    /// 车型枚举 (当前只先做 HXD1C demo)
    /// </summary>
    public enum VehicleType
    {
        HXD1C,
        HXD1GaoYuan,
        FXD1,
        QingDao8,
        WuKeChe,
        GaoYuanDongJi,
        FuXingBaZhou,
        WuChe
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Level { get; set; } = "INFO";
        public string Message { get; set; } = "";
    }
}
