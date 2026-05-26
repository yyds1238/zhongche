# 车体底架自动化拼装系统 - 上位机软件 (WinForms 版)

基于 **C# / .NET Framework 4.8 / WinForms** 架构,完全沿用老程序 (`ForcePositionControlSystem`) 的设计思路。

## 架构模块依赖关系

```
┌─────────────────────────────────────────────────────────────┐
│  Program.cs  →  StartupForm  →  MainForm                    │
└────────────────────────────┬────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
    ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
    │ Chassis      │ │ General      │ │ Quality      │
    │ Assembly     │ │ Measure      │ │ Analysis     │
    │ Control      │ │ Control      │ │ (占位)       │
    └──────┬───────┘ └──────┬───────┘ └──────────────┘
           │                │
           ├── ChassisTopDownView (自绘二维俯视图)
           ├── StationSetupForm (站位寻球弹窗)
           └── MatrixCalibrationForm (4x4 矩阵标定弹窗)

        ▼ 所有控件共享下面的服务和数据层

    ┌─────────────────────────────────────────────────┐
    │  AppState (单例) - 全局状态/连接/日志             │
    └─────────────────────────────────────────────────┘
              │
    ┌─────────┼─────────┐──────────┐───────────┐
    ▼         ▼         ▼          ▼           ▼
  Project   Tracker    CAS     ForceData    Geometry
  Service   Service   Manager  Processor    Fitter
            (接口)    (TCP流) (矩阵+平滑)  (圆/直线拟合)
             │
    ┌────────┼───────┐
    ▼        ▼       ▼
   Mock    LMF     GTS3300
  (默认)  (老代码)  (未来)
```

## 一、目录结构


```
ChassisAssembly/
├── Program.cs                          # 程序入口
├── ChassisAssembly.csproj              # 工程文件
│
├── Models/                             # 数据模型
│   ├── DataStructures.cs               # Point3D / Line3D / Circle3D / 枚举集合
│   ├── FixturePoint.cs                 # 工装点位 (二维图上可点击对象)
│   └── ProjectFile.cs                  # 项目文件结构
│
├── Config/                             # 车型配置
│   └── HXD1CConfig.cs                  # HXD1C 24 个工装点位布局
│
├── Services/                           # 服务层
│   ├── AppState.cs                     # 全局状态单例
│   ├── ProjectService.cs               # 项目新建/打开/保存
│   ├── ILaserTrackerService.cs         # 跟踪仪抽象接口
│   ├── MockTrackerService.cs           # Mock 实现(离线调试用)
│   ├── LaserTrackerServiceLMF.cs       # 徕卡 LMF SDK 实现(老代码搬运,需 DLL)
│   ├── CASCommunicationManager.cs      # CAS 力位协同 TCP 通讯(老代码搬运)
│   └── ForceDataProcessor.cs           # 力位数据后处理(矩阵变换+滑动平滑)
│
├── Algorithms/                         # 算法层
│   └── GeometryFitter.cs               # 最小二乘圆拟合 + 直线拟合
│
├── Controls/                           # UI 控件
│   ├── DoubleBufferedPanel.cs          # 双缓冲 Panel
│   ├── ChassisTopDownView.cs           # ★ 核心二维俯视图控件
│   ├── ChassisAssemblyControl.cs       # ★ 车体底架拼装主控件
│   └── GeneralMeasureControl.cs        # ★ 通用测量常驻控件
│
└── Forms/                              # 窗口
    ├── StartupForm.cs                  # 启动窗口(新建/打开项目)
    ├── MainForm.cs                     # 主窗口(外壳+导航)
    ├── StationSetupForm.cs             # 站位响应测试弹窗(老代码搬运)
    └── MatrixCalibrationForm.cs        # 4x4 矩阵标定弹窗(老代码搬运)
```

## 二、运行

1. 确保已安装 **.NET Framework 4.8** 及 **Visual Studio 2019+**
2. 打开 `ChassisAssembly.sln`,按 F5 运行
3. 启动后先新建一个 HXD1C 项目,进入主界面

## 三、启动流程

```
Program.Main()
    │
    ▼
StartupForm (选车型+新建项目 / 打开 .cap)
    │
    ├─ 取消 → 退出
    │
    ▼ (创建 ProjectFile,含 HXD1C 的 24 个工装)
MainForm (主窗口)
    │
    ├─ 顶部菜单栏 + 项目信息 + 设备状态监控条
    ├─ 左侧模块导航 (车体底架拼装 / 通用测量 / 质量分析)
    └─ 主工作区 (按当前模块切换)
        │
        ├─ ChassisAssemblyControl (主业务)
        │   ├─ 左侧 480px 操作面板
        │   │   ├─ ① 系统连接 (LT_1 + LT_2)
        │   │   ├─ ② 站位规划 (设定站位 + 矩阵设定)
        │   │   └─ 4 个工艺步骤 Tab (工艺基准 / 粗对中 / 精对中 / 车体拼装)
        │   └─ 右侧 二维俯视图 (自绘底架 + 24 工装 + 力/Z数据框 + 跟踪仪站位)
        │
        └─ GeneralMeasureControl (常驻工具)
            ├─ 左侧 模式选择(单点/连续/协同单点/协同连续) + 开始/停止/导出
            └─ 右侧 测量数据表
```

## 四、对接真实硬件

### 方案 A: 走 CAS 力位协同系统 (老代码风格)

优点:跟踪仪数据 + 力反馈统一一条 TCP 流,延迟小。
老程序就是这么做的,车体底架项目**如果部署 CAS 盒子也走这个路径**。

```csharp
// 在 MainForm 或 Program.cs 启动时:
bool ok = await CASCommunicationManager.Instance.ConnectAsync("127.0.0.1", 3462);
// 之后 GeneralMeasureControl 和 ChassisAssemblyControl 会自动收到数据
```

CAS 帧格式 (与老代码一致):
- 帧头 `AA55`、帧尾 `CCAA`
- 每台跟踪仪 24 字节 (X/Y/Z 各 8 字节 IEEE754 float)
- 4 路力传感器 x 8 字节
- 车体底架只用前 2 台跟踪仪 (LT_1/LT_2),后 2 台数据丢弃

### 方案 B: 徕卡 LMF SDK 直连

打开 `Services/LaserTrackerServiceLMF.cs`,去掉顶部 `#if LMF_SDK_AVAILABLE` 宏保护,
在 `csproj` 中添加 `<DefineConstants>LMF_SDK_AVAILABLE;DEBUG;TRACE</DefineConstants>`,
并引用 `LMF.Tracker.dll`。然后:

```csharp
AppState.Instance.TrackerService = new LaserTrackerServiceLMF();
```

### 方案 C: 中图 GTS3300 SDK 直连

新增 `Services/GTS3300TrackerService.cs`,实现 `ILaserTrackerService` 接口即可,
其他代码不用改。方案文档最终选型的是 **GTS3300**,所以这个是**最终要落地的实现**。

## 五、关键设计决策

| 项 | 决策 | 原因 |
|---|---|---|
| UI 框架 | WinForms | 完全复用老代码,学习成本零 |
| 跟踪仪数量 | 2 台 (LT_1 / LT_2) | 车体底架产线布局 |
| 数据通道 | 支持 双模式 | CAS 模式(TCP)+ SDK 直连,通过 ILaserTrackerService 抽象 |
| 车型 | JSON 可扩展 | 当前硬编码 HXD1C,后续改 JSON 加载 |
| 二维图 | 自绘 (GDI+) | 与老代码 `PnlVisualization_Paint` 风格一致,不用 3D 引擎 |

## 六、版本路线

| 版本 | 功能 |
|---|---|
| v1.0 (当前) | UI 框架完整,HXD1C 可运行,Mock 数据可联调 |
| v1.1 | 对接徕卡/中图 SDK,打通真实测量链路 |
| v1.2 | JSON 车型配置,新增其余 7 款车型 |
| v2.0 | 对接 CAS(或 PLC) 力位协同,实现力位双闭环 |
| v2.1 | 项目文件序列化(JSON),质量分析报告生成 |
