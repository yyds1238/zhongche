using System;
using System.Collections.Generic;

namespace ChassisAssembly.Models
{
    /// <summary>
    /// 项目文件 (.cap = Chassis Assembly Project)
    /// 打开软件 → 新建/打开项目 → 所有测量数据都保存在项目内
    /// </summary>
    public class ProjectFile
    {
        public string ProjectName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public VehicleType VehicleType { get; set; } = VehicleType.HXD1C;
        public string BatchNumber { get; set; } = "";
        public string Operator { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastSavedAt { get; set; } = DateTime.Now;
        public bool IsDirty { get; set; } = false;

        /// <summary>本项目的工装列表(根据车型自动生成)</summary>
        public List<FixturePoint> Fixtures { get; set; } = new List<FixturePoint>();

        /// <summary>本项目采集的所有测量记录</summary>
        public List<MeasurementRecord> Measurements { get; set; } = new List<MeasurementRecord>();

        /// <summary>各步骤完成状态 (4 通用 + 9 装配)</summary>
        public Dictionary<ProcessStep, StepProgress> StepStates { get; set; }
            = new Dictionary<ProcessStep, StepProgress>
            {
                // 通用公共工序
                { ProcessStep.TrackerSetup,             StepProgress.Active },
                { ProcessStep.MeasureFieldBuild,        StepProgress.Locked },
                { ProcessStep.DatumEstablish,           StepProgress.Locked },
                { ProcessStep.FixturePositioning,       StepProgress.Locked },
                // 装配工序
                { ProcessStep.BolsterAssembly,          StepProgress.Locked },
                { ProcessStep.TransformerBeamAssembly,  StepProgress.Locked },
                { ProcessStep.TractionBeamAssembly,     StepProgress.Locked },
                { ProcessStep.CenterBeamAssembly,       StepProgress.Locked },
                { ProcessStep.BeamsCenterAlign,         StepProgress.Locked },
                { ProcessStep.SideBeamAssembly,         StepProgress.Locked },
                { ProcessStep.TractionBeamPositioning,  StepProgress.Locked },
                { ProcessStep.ChassisWidthAdjust,       StepProgress.Locked },
                { ProcessStep.PartitionBeamAssembly,    StepProgress.Locked }
            };

        /// <summary>当前处于哪个工序</summary>
        public ProcessStep CurrentStep { get; set; } = ProcessStep.TrackerSetup;

        /// <summary>每个步骤当前的子阶段状态 (key: "{ProcessStep}.{SubStage}")</summary>
        public Dictionary<string, SubStageState> SubStageStates { get; set; }
            = new Dictionary<string, SubStageState>();

        public SubStageState GetSubStageState(ProcessStep step, SubStage stage)
        {
            string key = $"{step}.{stage}";
            return SubStageStates.TryGetValue(key, out var v) ? v : SubStageState.Pending;
        }

        public void SetSubStageState(ProcessStep step, SubStage stage, SubStageState state)
        {
            SubStageStates[$"{step}.{stage}"] = state;
        }
    }
}
