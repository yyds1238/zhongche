using System;
using ChassisAssembly.Config;
using ChassisAssembly.Models;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 项目服务 - 新建/打开/保存 .cap 项目文件
    /// </summary>
    public class ProjectService
    {
        public ProjectFile CreateNew(string name, VehicleType vehicleType,
            string batchNumber = "", string op = "")
        {
            var project = new ProjectFile
            {
                ProjectName = name,
                VehicleType = vehicleType,
                BatchNumber = batchNumber,
                Operator = op,
                CreatedAt = DateTime.Now
            };

            // 根据车型加载工装布局 (目前只实现 HXD1C)
            switch (vehicleType)
            {
                case VehicleType.HXD1C:
                    project.Fixtures = HXD1CConfig.BuildFixtures();
                    break;
                default:
                    project.Fixtures = HXD1CConfig.BuildFixtures(); // 其他车型先用 HXD1C 占位
                    break;
            }

            return project;
        }

        public ProjectFile Open(string filePath)
        {
            // TODO: 反序列化 .cap 文件 (JSON/XML)
            return null;
        }

        public void Save(ProjectFile project, string filePath = null)
        {
            if (!string.IsNullOrEmpty(filePath)) project.FilePath = filePath;
            project.LastSavedAt = DateTime.Now;
            project.IsDirty = false;
            // TODO: 序列化到文件
        }

        public static string GetVehicleDisplayName(VehicleType type)
        {
            switch (type)
            {
                case VehicleType.HXD1C:         return "HXD1C 重载机车";
                case VehicleType.HXD1GaoYuan:   return "HXD1 高原机车";
                case VehicleType.FXD1:          return "FXD1 机车";
                case VehicleType.QingDao8:      return "青岛 8 车型";
                case VehicleType.WuKeChe:       return "乌客车";
                case VehicleType.GaoYuanDongJi: return "高原动集";
                case VehicleType.FuXingBaZhou:  return "复兴八轴";
                case VehicleType.WuChe:         return "乌车";
                default: return type.ToString();
            }
        }
    }
}
