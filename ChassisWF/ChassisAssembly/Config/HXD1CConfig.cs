using System.Collections.Generic;
using ChassisAssembly.Models;

namespace ChassisAssembly.Config
{
    /// <summary>
    /// HXD1C 车型工装布局配置
    /// 画布坐标系: X [0,1000] 纵向, Y [0,260] 横向
    /// 真实坐标系: X 纵向 0~20000mm, Y 横向 -1550~1550mm, Z 高度 850mm
    ///
    /// 布局依据方案文档 5.5.1:
    /// - 枕梁压紧装置 16 组(分布在枕梁、变压器梁、后牵引梁两端)
    /// - 牵引梁纵向定位 2 个
    /// - 牵引梁横向定位 2 个
    /// - 纵向基准 2 处
    /// - 横向基准 4 处(多点控制)
    /// - 边梁压紧 若干组(沿边梁两侧分布)
    /// - 贯穿梁支撑 若干个
    /// </summary>
    public static class HXD1CConfig
    {
        public static string DisplayName => "HXD1C 重载机车底架";
        public static double LengthMm => 20000;
        public static double WidthMm => 3100;

        public static List<FixturePoint> BuildFixtures()
        {
            var list = new List<FixturePoint>();

            // ========== 纵向基准 (X向) - 2 处 ==========
            list.Add(Make("LD_01", "纵向基准 #1", FixtureType.LongitudinalDatum,
                70, 130, 500, 0, 850, hasForce: false, tracker: "LT_1"));
            list.Add(Make("LD_02", "纵向基准 #2", FixtureType.LongitudinalDatum,
                930, 130, 19500, 0, 850, hasForce: false, tracker: "LT_2"));

            // ========== 横向基准 (Y向) - 4 处 ==========
            list.Add(Make("TD_01", "横向基准 #1", FixtureType.LateralDatum,
                220, 130, 4000, 0, 850, hasForce: false, tracker: "LT_1"));
            list.Add(Make("TD_02", "横向基准 #2", FixtureType.LateralDatum,
                400, 130, 8000, 0, 850, hasForce: false, tracker: "LT_1"));
            list.Add(Make("TD_03", "横向基准 #3", FixtureType.LateralDatum,
                600, 130, 12000, 0, 850, hasForce: false, tracker: "LT_2"));
            list.Add(Make("TD_04", "横向基准 #4", FixtureType.LateralDatum,
                780, 130, 16000, 0, 850, hasForce: false, tracker: "LT_2"));

            // ========== 枕梁压紧装置 - 4 组 (两端枕梁位置) ==========
            list.Add(Make("BP_01", "枕梁压紧 #1", FixtureType.BolsterPress,
                200, 100, 3500, -1400, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("BP_02", "枕梁压紧 #2", FixtureType.BolsterPress,
                200, 160, 3500, 1400, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("BP_03", "枕梁压紧 #3", FixtureType.BolsterPress,
                800, 100, 16500, -1400, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("BP_04", "枕梁压紧 #4", FixtureType.BolsterPress,
                800, 160, 16500, 1400, 850, hasForce: true, tracker: "LT_2"));

            // ========== 牵引梁压紧 - 2 个纵向 + 2 个横向 ==========
            list.Add(Make("TR_L1", "牵引梁纵向 #1", FixtureType.TractionLongPress,
                450, 130, 9000, 0, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("TR_L2", "牵引梁纵向 #2", FixtureType.TractionLongPress,
                550, 130, 11000, 0, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("TR_T1", "牵引梁横向 #1", FixtureType.TractionLatPress,
                460, 100, 9200, -1000, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("TR_T2", "牵引梁横向 #2", FixtureType.TractionLatPress,
                540, 160, 10800, 1000, 850, hasForce: true, tracker: "LT_2"));

            // ========== 贯穿梁支撑座 - 2 个 ==========
            list.Add(Make("PS_01", "贯穿梁支撑 #1", FixtureType.PiercingSupport,
                340, 130, 6500, 0, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("PS_02", "贯穿梁支撑 #2", FixtureType.PiercingSupport,
                660, 130, 13500, 0, 850, hasForce: true, tracker: "LT_2"));

            // ========== 边梁压紧装置 - 10 组 (沿边梁两侧分布) ==========
            list.Add(Make("SB_01", "边梁压紧 #1", FixtureType.SideBeamPress,
                150, 80, 2500, -1550, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("SB_02", "边梁压紧 #2", FixtureType.SideBeamPress,
                150, 180, 2500, 1550, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("SB_03", "边梁压紧 #3", FixtureType.SideBeamPress,
                300, 80, 5500, -1550, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("SB_04", "边梁压紧 #4", FixtureType.SideBeamPress,
                300, 180, 5500, 1550, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("SB_05", "边梁压紧 #5", FixtureType.SideBeamPress,
                500, 80, 10000, -1550, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("SB_06", "边梁压紧 #6", FixtureType.SideBeamPress,
                500, 180, 10000, 1550, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("SB_07", "边梁压紧 #7", FixtureType.SideBeamPress,
                700, 80, 14500, -1550, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("SB_08", "边梁压紧 #8", FixtureType.SideBeamPress,
                700, 180, 14500, 1550, 850, hasForce: true, tracker: "LT_2"));
            list.Add(Make("SB_09", "边梁压紧 #9", FixtureType.SideBeamPress,
                850, 80, 17500, -1550, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("SB_10", "边梁压紧 #10", FixtureType.SideBeamPress,
                850, 180, 17500, 1550, 850, hasForce: true, tracker: "LT_2"));

            // ========== 变压器梁压紧装置 - 2 组 (位置待设计确认,暂放车体中后段) ==========
            list.Add(Make("TBP_01", "变压器梁压紧 #1", FixtureType.TransformerBeamPress,
                650, 100, 13000, -1400, 850, hasForce: true, tracker: "LT_1"));
            list.Add(Make("TBP_02", "变压器梁压紧 #2", FixtureType.TransformerBeamPress,
                650, 160, 13000, 1400, 850, hasForce: true, tracker: "LT_2"));

            // ========== 中心纵梁工装 - 2 个 (位置待设计确认,暂放车体中心轴线) ==========
            list.Add(Make("CB_01", "中心纵梁工装 #1", FixtureType.CenterBeamFixture,
                380, 130, 7500, 0, 850, hasForce: false, tracker: "LT_1"));
            list.Add(Make("CB_02", "中心纵梁工装 #2", FixtureType.CenterBeamFixture,
                620, 130, 12500, 0, 850, hasForce: false, tracker: "LT_2"));

            // ========== 隔墙梁工装 - 2 个 (位置待设计确认,暂放 1/4 和 3/4 位置) ==========
            list.Add(Make("PB_01", "隔墙梁工装 #1", FixtureType.PartitionBeamFixture,
                260, 130, 5000, 0, 850, hasForce: false, tracker: "LT_1"));
            list.Add(Make("PB_02", "隔墙梁工装 #2", FixtureType.PartitionBeamFixture,
                740, 130, 15000, 0, 850, hasForce: false, tracker: "LT_2"));

            return list;
        }

        private static FixturePoint Make(string id, string name, FixtureType t,
            double cx, double cy, double tx, double ty, double tz, bool hasForce, string tracker)
        {
            return new FixturePoint
            {
                Id = id,
                Name = name,
                FixtureType = t,
                CanvasX = cx,
                CanvasY = cy,
                TheoreticalX = tx,
                TheoreticalY = ty,
                TheoreticalZ = tz,
                HasForceSensor = hasForce,
                AssignedTracker = tracker,
                Status = PointStatus.NotMeasured
            };
        }
    }
}
