using System.Collections.Generic;
using System.Linq;

namespace ChassisAssembly.Models
{
    /// <summary>
    /// 单个子阶段的定义
    /// </summary>
    public class SubStageDef
    {
        public string Title { get; set; } = "";        // "① 压紧装置定位就位"
        public string Description { get; set; } = "";  // 卡片描述
        public string[] ButtonLabels { get; set; } = new string[0];  // 按钮标签 (1~2 个)
        public string ActionKind { get; set; } = "";   // 按钮触发的动作种类 (供 Control 层映射)
        public bool Skipped { get; set; } = false;     // 该子阶段是否跳过
    }

    /// <summary>
    /// 工艺步骤静态定义 — 通用模型,兼容 3 个通用工序 + 9 个装配工序
    /// </summary>
    public class AssemblyStepDef
    {
        public ProcessStep Step { get; set; }
        public StepCategory Category { get; set; } = StepCategory.Assembly;
        public string Number { get; set; } = "";      // "4.1" 或 "公共1"
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";        // emoji 图标
        public string Description { get; set; } = "";
        public List<FixtureType> InvolvedFixtureTypes { get; set; } = new List<FixtureType>();

        /// <summary>工艺尺寸要求(装配步骤来自 HXD1C 组焊指导书)</summary>
        public string TechRequirements { get; set; } = "";

        /// <summary>4 个子阶段 (Slot1/Slot2/Slot3/Slot4)</summary>
        public SubStageDef[] SubStages { get; set; } = new SubStageDef[4];
    }

    /// <summary>
    /// 全部工艺步骤定义表 - 3 公共 + 9 装配
    /// </summary>
    public static class AssemblyProcessDef
    {
        public static readonly List<AssemblyStepDef> AllSteps = new List<AssemblyStepDef>
        {
            // ============================================================
            // 通用公共工序 (所有车型相同)
            // ============================================================
            new AssemblyStepDef
            {
                Step = ProcessStep.TrackerSetup,
                Category = StepCategory.Common,
                Number = "公共", Icon = "🛰",
                Name = "跟踪仪联合建站",
                Description = "两台激光跟踪仪 LT_1/LT_2 分别部署于车体两侧,形成对 20m 超长工件的接力式覆盖",
                TechRequirements =
                    "• 布设: LT_1 / LT_2 分别部署于车体左右两侧\n" +
                    "• 纵向站位依据车型长度选择,确保 20m 级工件全覆盖\n" +
                    "• 每台跟踪仪首次上电需完成硬件初始化 (转动镜头找机械零点,约 15~30s)\n" +
                    "• 人工引光/寻球验证测头响应正常",
                SubStages = new[]
                {
                    new SubStageDef { Title = "① 连接 LT_1 (左站)", Description = "通过网络连接左侧跟踪仪, IP 默认 192.168.1.101",
                        ButtonLabels = new[] { "▶ 连接 LT_1" }, ActionKind = "ConnectLT1" },
                    new SubStageDef { Title = "② 连接 LT_2 (右站)", Description = "通过网络连接右侧跟踪仪, IP 默认 192.168.1.102",
                        ButtonLabels = new[] { "▶ 连接 LT_2" }, ActionKind = "ConnectLT2" },
                    new SubStageDef { Title = "③ 站位响应验证", Description = "下发目标坐标驱动跟踪仪寻球,人工验证测头转动正常",
                        ButtonLabels = new[] { "▶ 打开寻球对话框" }, ActionKind = "StationSetup" },
                    new SubStageDef { Title = "④ 完成本工序", Description = "确认两台跟踪仪均响应正常,进入测量场构建",
                        ButtonLabels = new[] { "✓ 完成并进入下一步" }, ActionKind = "Finish" }
                }
            },

            new AssemblyStepDef
            {
                Step = ProcessStep.MeasureFieldBuild,
                Category = StepCategory.Common,
                Number = "公共", Icon = "📐",
                Name = "测量场构建",
                Description = "采集工装四周的公共基准点,通过 USMN 平差计算两台跟踪仪的空间转换矩阵,统一至车体全局坐标系",
                TechRequirements =
                    "• 公共基准点 (ECP) 数量 ≥ 6 个, 分布于工装四周\n" +
                    "• 使用 USMN / Bundle Adjustment 算法平差\n" +
                    "• 组网精度要求 ≤ 0.2mm\n" +
                    "• 完成后将变换矩阵写入系统,后续测量均在全局坐标系下",
                SubStages = new[]
                {
                    new SubStageDef { Title = "① 采集公共基准点", Description = "依次用 LT_1、LT_2 采集 ≥6 个公共基准点 (ECP)",
                        ButtonLabels = new[] { "▶ 采集基准点" }, ActionKind = "CollectECP" },
                    new SubStageDef { Title = "② USMN 平差", Description = "执行 USMN/Bundle Adjustment 平差,计算两台跟踪仪的变换矩阵",
                        ButtonLabels = new[] { "▶ 执行平差", "矩阵标定..." }, ActionKind = "USMNAdjust" },
                    new SubStageDef { Title = "③ 精度验证", Description = "验证组网精度 ≤ 0.2mm,不达标需重新采点",
                        ButtonLabels = new[] { "▶ 验证精度" }, ActionKind = "VerifyPrecision" },
                    new SubStageDef { Title = "④ 完成本工序", Description = "测量场构建完成,进入基准建立",
                        ButtonLabels = new[] { "✓ 完成并进入下一步" }, ActionKind = "Finish" }
                }
            },

            new AssemblyStepDef
            {
                Step = ProcessStep.DatumEstablish,
                Category = StepCategory.Common,
                Number = "公共", Icon = "📏",
                Name = "基准建立",
                Description = "建立车体全局坐标系的三大基准: 纵向X / 横向Y / 水平Z,后续所有位姿偏差都以此基准计算",
                TechRequirements =
                    "• 纵向基准 (X): 底架纵向中心线\n" +
                    "• 横向基准 (Y): 底架横向中心线 (通常以枕梁中心为参考)\n" +
                    "• 水平基准 (Z): 底架工作平面\n" +
                    "• 三大基准建立后,后续装配工序的偏差都以此为参考",
                InvolvedFixtureTypes = { FixtureType.LongitudinalDatum, FixtureType.LateralDatum },
                SubStages = new[]
                {
                    new SubStageDef { Title = "① 纵向基准 (X向)", Description = "测量并拟合底架纵向中心线",
                        ButtonLabels = new[] { "▶ 建立 X 基准" }, ActionKind = "DatumX" },
                    new SubStageDef { Title = "② 横向基准 (Y向)", Description = "测量并拟合底架横向中心线",
                        ButtonLabels = new[] { "▶ 建立 Y 基准" }, ActionKind = "DatumY" },
                    new SubStageDef { Title = "③ 水平基准 (Z向)", Description = "测量并拟合底架工作平面",
                        ButtonLabels = new[] { "▶ 建立 Z 基准" }, ActionKind = "DatumZ" },
                    new SubStageDef { Title = "④ 完成本工序", Description = "三大基准已建立,开始装配工序 4.1",
                        ButtonLabels = new[] { "✓ 完成并进入装配" }, ActionKind = "Finish" }
                }
            },

            // ----- 工装定位 (新增,所有车型共用) -----
            new AssemblyStepDef
            {
                Step = ProcessStep.FixturePositioning,
                Category = StepCategory.Common,
                Number = "公共", Icon = "🔧",
                Name = "工装定位",
                Description = "将所有压紧装置 / 工装定位至本车型理论位置 (粗定位→测量→对比→精定位 闭环)",
                TechRequirements =
                    "• 所有工装必须在装配开始前定位到位,精度 ≤ ±0.5mm\n" +
                    "• 流程: 粗定位 (PLC 下发理论坐标,工装大致到位)\n" +
                    "        → 激光跟踪仪逐点测量实际位置\n" +
                    "        → 软件比对理论值,计算偏差\n" +
                    "        → 自动下发精调指令,直至所有工装均合格\n" +
                    "• 涉及工装类型: 枕梁压紧 / 变压器梁压紧 / 牵引梁压紧 / 中心纵梁工装 / 边梁压紧 / 隔墙梁工装\n" +
                    "• 完成后所有工装位置锁定,后续装配以此为基准",
                InvolvedFixtureTypes =
                {
                    FixtureType.BolsterPress, FixtureType.TransformerBeamPress,
                    FixtureType.TractionLongPress, FixtureType.TractionLatPress,
                    FixtureType.SideBeamPress, FixtureType.CenterBeamFixture,
                    FixtureType.PartitionBeamFixture
                },
                SubStages = new[]
                {
                    new SubStageDef { Title = "① 粗定位", Description = "PLC 下发各工装理论坐标,所有压紧装置自动移动到大致位置",
                        ButtonLabels = new[] { "▶ 开始粗定位" }, ActionKind = "RoughPosition" },
                    new SubStageDef { Title = "② 跟踪仪测量实际位置", Description = "逐工装打靶球,测量当前实际位姿",
                        ButtonLabels = new[] { "▶ 开始测量" }, ActionKind = "MeasureFixtures" },
                    new SubStageDef { Title = "③ 与理论对比", Description = "软件自动计算所有工装的 ΔX/ΔY/ΔZ 偏差,标识超差工装",
                        ButtonLabels = new[] { "▶ 计算偏差" }, ActionKind = "CompareTheoretical" },
                    new SubStageDef { Title = "④ 精定位", Description = "对超差工装下发修正指令,迭代直至全部合格 (≤±0.5mm)",
                        ButtonLabels = new[] { "▶ 开始精定位", "■ 紧急停止" }, ActionKind = "FinePosition" }
                }
            },

            // ============================================================
            // 装配工序 (车型特定)
            // ============================================================
            BuildAssembly(ProcessStep.BolsterAssembly, "4.1", "枕梁装配",
                "将枕梁吊装到枕梁压紧装置上,由装置自动调姿到理论位置",
                new[] { FixtureType.BolsterPress },
                "• 吊装: 4t×3m 双肢横钳吊具,相对枕梁中心斜对称夹住下盖板两端\n" +
                "• 贴合度: 枕梁上平面与工装基准 ≤ 2mm\n" +
                "• 装配定位: 枕梁横向中心至底架总组焊工装横向中心 = 6048 (=6040+8) ±1mm\n" +
                "• 压紧: 每个枕梁用 4 件顶紧装置顶紧前后两面,限制纵向移动",
                isAssembly: true),

            BuildAssembly(ProcessStep.TransformerBeamAssembly, "4.2", "变压器梁装配",
                "将变压器梁吊装到变压器梁压紧装置上,自动调姿",
                new[] { FixtureType.TransformerBeamPress },
                "• 吊装: 3t×5m 四肢链式捆绑吊具,捆绑于箱型梁变围板孔中靠近纵向中心位置\n" +
                "• 贴合度: 变压器梁上平面与工装基准 ≤ 2mm\n" +
                "• 装配定位: 变压器梁横向中心与底架总组焊工装横向中心偏差 ≤ 1mm\n" +
                "• 压紧: 4 件顶紧装置顶紧前后两面,限制纵向移动",
                isAssembly: true),

            BuildAssembly(ProcessStep.TractionBeamAssembly, "4.3", "牵引梁装配",
                "将牵引梁吊装到牵引梁纵向/横向压紧装置上,自动调姿",
                new[] { FixtureType.TractionLongPress, FixtureType.TractionLatPress },
                "• 吊装: 5t×2.5m 双肢链条卸扣式吊具,锁住牵引梁下盖板工艺吊耳\n" +
                "• 贴合度: 前端牵引梁上平面与工装基准 ≤ 2mm\n" +
                "• 装配调整: 牵引梁前端端部靠紧底架工装前端定位装置\n" +
                "• 牵引梁前端端部至枕梁横向中心距离 ≥ 理论值 + 150mm (防止边梁干涉)\n" +
                "• 压紧: 各 2 件顶紧装置顶紧牵引梁后端",
                isAssembly: true),

            BuildAssembly(ProcessStep.CenterBeamAssembly, "4.4", "中心纵梁装配",
                "将中心纵梁吊装到中心纵梁工装上,自动调姿",
                new[] { FixtureType.CenterBeamFixture },
                "• 按图纸及流程,确认不同位置的中心纵梁结构及方向\n" +
                "• 吊装: 4t×4m 双肢链条可调捆绑索具\n" +
                "• 吊具两肢捆绑于中心纵梁中心两侧,间距 ≥ 梁体长度的一半 且 夹角 ≤ 60°",
                isAssembly: true),

            BuildAssembly(ProcessStep.BeamsCenterAlign, "4.5", "各大梁纵向中心对齐",
                "测量各大梁已装姿态,计算偏差,下发指令做协调调整",
                new[] { FixtureType.BolsterPress, FixtureType.TransformerBeamPress,
                        FixtureType.TractionLongPress, FixtureType.CenterBeamFixture },
                "• 在底架总组焊工装上拉纵向中心线 (粉线)\n" +
                "• 牵引梁、枕梁、变压器梁的纵向中心线与底架纵向中心线偏差 ≤ 2mm\n" +
                "• 两枕梁对角线偏差 ≤ 2mm\n" +
                "• 定位无误后,使用工装压紧装置压紧牵引梁、枕梁、变压器梁",
                isAssembly: false),

            BuildAssembly(ProcessStep.SideBeamAssembly, "4.6", "边梁装配",
                "将两侧边梁吊装到边梁压紧装置上,自动调姿",
                new[] { FixtureType.SideBeamPress },
                "• 组装枕梁上盖板与边梁梁体对接焊缝根部的焊接垫板\n" +
                "• 吊装: 5t×6m 双肢链式捆绑吊具,两肢间距 ≥ 4m 且 夹角 ≤ 60°\n" +
                "• 利用液压装置将边梁横向向内推至合适位置\n" +
                "• 边梁中心线与底架总组焊工装横向中心线偏差 ≤ 3mm",
                isAssembly: true),

            BuildAssembly(ProcessStep.TractionBeamPositioning, "4.7", "牵引梁定位",
                "对 4.3 已装的牵引梁做最终定位锁紧",
                new[] { FixtureType.TractionLongPress, FixtureType.TractionLatPress },
                "• 胎膜两端滑动平台纵向向内推进前、后端牵引梁至预设位置\n" +
                "• 牵引梁前端端部距枕梁横向中心 = 4911 (=4903+8) ±1mm\n" +
                "• 调整中心纵梁位置,保证两端对接焊缝间隙均匀且 ≥ 2mm\n" +
                "• 中心纵梁对接错位 ≤ 1.6mm, 每段区域内平面度 ≤ 2mm",
                isAssembly: true),

            BuildAssembly(ProcessStep.ChassisWidthAdjust, "4.8", "底架宽度调整",
                "测量两侧边梁间距,下发指令调整底架整体宽度",
                new[] { FixtureType.SideBeamPress },
                "• 变压器梁位置底架上下宽度 = 3098 (=3094+4) ±2mm\n" +
                "  接平变压器梁上下盖板与边梁,错位 ≤ 1mm\n" +
                "• 枕梁位置底架上下宽度 = 3098 (=3094+4) ±2mm\n" +
                "  接平枕梁上盖板与边梁,错位 ≤ 1mm\n" +
                "  枕梁两端二系簧座安装面高度差 ≤ 2mm\n" +
                "• 牵引梁位置底架上下宽度 = 3104 (=3100+4) (-2,0) mm\n" +
                "  接平牵引梁侧立板与边梁侧立板,错位 ≤ 1mm",
                isAssembly: false),

            BuildAssembly(ProcessStep.PartitionBeamAssembly, "4.9", "隔墙梁装配",
                "将隔墙梁吊装到隔墙梁工装上,自动调姿",
                new[] { FixtureType.PartitionBeamFixture },
                "• 隔墙梁端部至枕梁横向中心尺寸 = 2139 (=2135+4) ±2mm\n" +
                "• 隔墙梁位置底架上下外宽尺寸 = 3106 (=3100+6) ±2mm\n" +
                "• 定位无误后,将隔墙梁与边梁、中心纵梁之间施加定位焊",
                isAssembly: true),
        };

        /// <summary>
        /// 装配步骤的标准 4 子阶段构造器 (2026.04.27 重构)
        /// 工装定位已抽离为独立工序,装配内部不再做工装定位
        /// 新流程: ① 测构件标志点 → ② 吊装就位 → ③ 调姿(含测实际+对比) → ④ 完成
        /// isAssembly=false (4.5/4.8 只调姿步骤): 跳过测标志点和吊装,只走 调姿+完成
        /// </summary>
        private static AssemblyStepDef BuildAssembly(ProcessStep step, string number, string name,
            string desc, FixtureType[] fixtureTypes, string techReq, bool isAssembly)
        {
            string componentName = name.Replace("装配", "").Replace("定位", "");
            return new AssemblyStepDef
            {
                Step = step,
                Category = StepCategory.Assembly,
                Number = number,
                Name = name,
                Description = desc,
                InvolvedFixtureTypes = fixtureTypes.ToList(),
                TechRequirements = techReq,
                SubStages = new[]
                {
                    new SubStageDef
                    {
                        Title = "① 测构件标志点",
                        Description = isAssembly
                            ? $"在{componentName}上选择 ≥3 个标志点,激光跟踪仪逐点测量,作为该构件本体的位姿基准 (后续拟合理论测点用)"
                            : "",
                        ButtonLabels = isAssembly ? new[] { "▶ 开始测标志点" } : new string[0],
                        ActionKind = isAssembly ? "MeasureMarkers" : "",
                        Skipped = !isAssembly
                    },
                    new SubStageDef
                    {
                        Title = "② 构件吊装就位",
                        Description = isAssembly
                            ? $"请现场将{componentName}吊装到工装上,贴合到位后点击确认"
                            : "",
                        ButtonLabels = isAssembly ? new[] { "✓ 已吊装完成" } : new string[0],
                        ActionKind = isAssembly ? "HoistingDone" : "",
                        Skipped = !isAssembly
                    },
                    new SubStageDef
                    {
                        Title = isAssembly ? "③ 调姿 (测实际→对比→修正)" : "① 协调调姿",
                        Description = isAssembly
                            ? "测量构件当前位姿 → 与理论位置对比 → 自动下发调姿指令,迭代至合格"
                            : "测量各工装当前位姿,计算偏差,下发协调调整指令",
                        ButtonLabels = new[] { "▶ 开始调姿", "■ 紧急停止" },
                        ActionKind = "AlignComponent"
                    },
                    new SubStageDef
                    {
                        Title = isAssembly ? "④ 完成本工序" : "② 完成本工序",
                        Description = "确认本步骤装配结果,进入下一步骤",
                        ButtonLabels = new[] { "✓ 完成并进入下一步" },
                        ActionKind = "Finish"
                    }
                }
            };
        }

        public static AssemblyStepDef Get(ProcessStep step) =>
            AllSteps.Find(s => s.Step == step);

        public static List<AssemblyStepDef> CommonSteps =>
            AllSteps.Where(s => s.Category == StepCategory.Common).ToList();

        public static List<AssemblyStepDef> AssemblySteps =>
            AllSteps.Where(s => s.Category == StepCategory.Assembly).ToList();
    }
}
