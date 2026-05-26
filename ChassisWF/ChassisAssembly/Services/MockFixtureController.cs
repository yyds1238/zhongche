using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChassisAssembly.Models;

namespace ChassisAssembly.Services
{
    /// <summary>
    /// 压紧装置控制器 Mock 实现
    /// 模拟 PLC 行为: 异步等待、进度回报、故障注入、协同运动模拟
    /// </summary>
    public class MockFixtureController : IFixtureController
    {
        private readonly Random _rand = new Random();

        public bool IsReady => true;
        public string ProtocolName => "Mock (离线演示)";

        public event EventHandler<ControllerStatusEventArgs> StatusChanged;

        // ============================================================
        // 新版协同接口
        // ============================================================

        public async Task<ValidationResult> ValidatePlanAsync(CoordinationPlan plan)
        {
            await Task.Delay(100);
            var result = new ValidationResult { IsValid = true };

            if (plan.Targets == null || plan.Targets.Count == 0)
            {
                result.Errors.Add("规划中没有任何定位器目标");
                result.IsValid = false;
                return result;
            }

            if (plan.MaxContactForce > 5000)
                result.Warnings.Add($"最大接触力 {plan.MaxContactForce}N 超出推荐值 2500N");

            if (plan.SpeedOverride > 80)
                result.Warnings.Add($"速度倍率 {plan.SpeedOverride}% 较高, 建议 ≤30% 执行精调");

            // 模拟干涉检查 (Mock 随机)
            if (plan.Targets.Count > 6 && _rand.NextDouble() < 0.05)
                result.Warnings.Add("多个定位器空间靠近, 建议分批执行以避免干涉");

            if (plan.Mode == CoordinationMode.HybridPositionForce && plan.Impedance == null)
                result.Errors.Add("混合位置/力控模式需要阻抗参数, 但未提供");

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public async Task<CoordinationResult> CoordinatedMoveAsync(
            CoordinationPlan plan,
            IProgress<CoordinationProgress> progress = null,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new CoordinationResult();
            AppState.Instance.Log("INFO",
                $"[Mock] 协同运动开始: {plan.Name}  模式={plan.Mode}  目标数={plan.Targets.Count}");

            // 仿真多阶段: 规划 → 同步运动 → 到位确认 → 保压
            var phases = new[] { "运动规划", "同步运动", "到位确认", "保压稳定" };
            int totalSteps = phases.Length * 10;
            int stepCount = 0;

            foreach (var phase in phases)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ct.IsCancellationRequested)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "用户取消";
                        return result;
                    }

                    await Task.Delay(30, ct);
                    stepCount++;

                    progress?.Report(new CoordinationProgress
                    {
                        PercentComplete = stepCount * 100.0 / totalSteps,
                        CurrentPhase = phase,
                        LiveFeedback = BuildLiveFeedback(plan, stepCount / (double)totalSteps)
                    });
                }
            }

            // 最终状态
            result.FinalState = BuildLiveFeedback(plan, 1.0).ToList();
            result.MaxPositionError = result.FinalState.Count > 0
                ? result.FinalState.Max(f => 0.05 + _rand.NextDouble() * 0.15)
                : 0;
            result.MaxAttitudeError = 0.02 + _rand.NextDouble() * 0.06;  // 度
            result.MaxContactForce = plan.Mode == CoordinationMode.ForceOnly
                ? 1800 + _rand.NextDouble() * 300
                : 500 + _rand.NextDouble() * 800;
            result.Duration = sw.Elapsed;
            result.IsSuccess = true;

            AppState.Instance.Log("INFO",
                $"[Mock] 协同运动完成: 最大位置偏差={result.MaxPositionError:F3}mm  " +
                $"姿态偏差={result.MaxAttitudeError:F3}°  最大接触力={result.MaxContactForce:F0}N");

            // 同步更新 AppState 里对应 FixturePoint 的数据
            UpdateFixtureStates(plan);
            return result;
        }

        public async Task<IReadOnlyList<FixtureFeedback>> ReadFeedbackAsync()
        {
            await Task.Delay(50);
            var project = AppState.Instance.CurrentProject;
            if (project == null) return new List<FixtureFeedback>();

            return project.Fixtures.Select(fx => new FixtureFeedback
            {
                FixtureId = fx.Id,
                CurrentX = fx.ActualX,
                CurrentY = fx.ActualY,
                CurrentZ = fx.ActualZ,
                CurrentForce = fx.PressForce,
                HasFault = false
            }).ToList();
        }

        public async Task EmergencyStopAsync()
        {
            AppState.Instance.Log("WARN", "[Mock] 紧急停止! 运动切断, 保压中");
            await Task.Delay(100);
            StatusChanged?.Invoke(this, new ControllerStatusEventArgs
            {
                IsReady = true,
                Message = "紧急停止已触发,需手动恢复"
            });
        }

        // ============================================================
        // 兼容旧版简单接口(内部转新接口)
        // ============================================================

        public async Task<bool> PositionFixturesAsync(List<FixturePoint> fixtures)
        {
            var plan = new CoordinationPlan
            {
                Name = "压紧装置定位",
                Mode = CoordinationMode.PositionOnly,
                SpeedOverride = 50,
                Targets = fixtures.Select(fx => new FixtureTargetPose
                {
                    FixtureId = fx.Id,
                    TargetX = fx.TheoreticalX,
                    TargetY = fx.TheoreticalY,
                    TargetZ = fx.TheoreticalZ
                }).ToList()
            };

            var r = await CoordinatedMoveAsync(plan);
            return r.IsSuccess;
        }

        public async Task<bool> AlignComponentAsync(List<FixturePoint> fixtures)
        {
            var plan = new CoordinationPlan
            {
                Name = "构件协同调姿",
                Mode = CoordinationMode.HybridPositionForce,
                SpeedOverride = 20,
                MaxContactForce = 2500,
                Impedance = new ImpedanceParams(),
                Targets = fixtures.Select(fx => new FixtureTargetPose
                {
                    FixtureId = fx.Id,
                    TargetX = fx.TheoreticalX,
                    TargetY = fx.TheoreticalY,
                    TargetZ = fx.TheoreticalZ,
                    TargetForce = fx.HasForceSensor ? 1800 : 0
                }).ToList()
            };

            var r = await CoordinatedMoveAsync(plan);
            return r.IsSuccess;
        }

        // ============================================================
        // 内部辅助
        // ============================================================

        private List<FixtureFeedback> BuildLiveFeedback(CoordinationPlan plan, double progress)
        {
            var project = AppState.Instance.CurrentProject;
            if (project == null) return new List<FixtureFeedback>();

            var list = new List<FixtureFeedback>();
            foreach (var t in plan.Targets)
            {
                var fx = project.Fixtures.Find(f => f.Id == t.FixtureId);
                if (fx == null) continue;

                // 线性插值: 从当前实测向目标点逼近
                double startX = fx.ActualX != 0 ? fx.ActualX : fx.TheoreticalX - 3;
                double startY = fx.ActualY != 0 ? fx.ActualY : fx.TheoreticalY;
                double startZ = fx.ActualZ != 0 ? fx.ActualZ : fx.TheoreticalZ;

                list.Add(new FixtureFeedback
                {
                    FixtureId = t.FixtureId,
                    CurrentX = startX + (t.TargetX - startX) * progress + (_rand.NextDouble() - 0.5) * 0.05,
                    CurrentY = startY + (t.TargetY - startY) * progress + (_rand.NextDouble() - 0.5) * 0.05,
                    CurrentZ = startZ + (t.TargetZ - startZ) * progress + (_rand.NextDouble() - 0.5) * 0.05,
                    CurrentForce = fx.HasForceSensor ? 1500 * progress + _rand.NextDouble() * 200 : 0
                });
            }
            return list;
        }

        private void UpdateFixtureStates(CoordinationPlan plan)
        {
            var project = AppState.Instance.CurrentProject;
            if (project == null) return;

            foreach (var t in plan.Targets)
            {
                var fx = project.Fixtures.Find(f => f.Id == t.FixtureId);
                if (fx == null) continue;

                fx.ActualX = t.TargetX + (_rand.NextDouble() - 0.5) * 0.15;
                fx.ActualY = t.TargetY + (_rand.NextDouble() - 0.5) * 0.15;
                fx.ActualZ = t.TargetZ + (_rand.NextDouble() - 0.5) * 0.15;
                if (fx.HasForceSensor)
                    fx.PressForce = 1500 + _rand.NextDouble() * 500;
                fx.LastMeasuredAt = DateTime.Now;
                fx.Status = fx.DeviationNorm > 0.5 ? PointStatus.OutOfTolerance : PointStatus.Qualified;
            }
        }
    }
}
