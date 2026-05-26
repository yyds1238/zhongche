using System.Drawing;

namespace ChassisAssembly.Controls
{
    /// <summary>
    /// 全局统一配色方案 - 白底工业风
    /// 所有控件优先从这里取色,避免散落的 Color.FromArgb(...)
    /// </summary>
    public static class AppColors
    {
        // ========== 背景 ==========
        public static readonly Color BgPrimary   = Color.White;                    // 主背景(白)
        public static readonly Color BgSecondary = Color.FromArgb(247, 248, 250);  // 次级背景(浅灰)
        public static readonly Color BgCard      = Color.White;                    // 卡片背景
        public static readonly Color BgDarkAccent = Color.FromArgb(237, 240, 245); // 悬浮/选中时的底色

        // ========== 边框 / 分隔 ==========
        public static readonly Color Border     = Color.FromArgb(222, 226, 230);
        public static readonly Color BorderSoft = Color.FromArgb(237, 240, 245);

        // ========== 文字 ==========
        public static readonly Color TextPrimary   = Color.FromArgb(33, 37, 41);
        public static readonly Color TextSecondary = Color.FromArgb(108, 117, 125);
        public static readonly Color TextMuted     = Color.FromArgb(173, 181, 189);
        public static readonly Color TextOnAccent  = Color.White;

        // ========== 品牌/主色 ==========
        public static readonly Color Primary        = Color.FromArgb(13, 110, 253);   // 蓝
        public static readonly Color PrimaryHover   = Color.FromArgb(10, 88, 202);
        public static readonly Color PrimarySubtle  = Color.FromArgb(230, 240, 255);

        // ========== 状态色 ==========
        public static readonly Color Success     = Color.FromArgb(25, 135, 84);    // 绿
        public static readonly Color SuccessBg   = Color.FromArgb(230, 245, 236);
        public static readonly Color Warning     = Color.FromArgb(255, 152, 0);    // 橙
        public static readonly Color WarningBg   = Color.FromArgb(255, 244, 229);
        public static readonly Color Danger      = Color.FromArgb(220, 53, 69);    // 红
        public static readonly Color DangerBg    = Color.FromArgb(253, 237, 239);
        public static readonly Color Info        = Color.FromArgb(13, 202, 240);   // 青
        public static readonly Color Neutral     = Color.FromArgb(108, 117, 125);

        // ========== 工件/底架 ==========
        public static readonly Color ChassisOutline = Color.FromArgb(120, 144, 156);
        public static readonly Color ChassisFill    = Color.FromArgb(245, 247, 250);
        public static readonly Color CenterLine     = Color.FromArgb(200, 210, 220);

        // ========== 左侧导航 ==========
        public static readonly Color NavBg          = Color.FromArgb(248, 249, 250);
        public static readonly Color NavItemBg      = Color.White;
        public static readonly Color NavItemActive  = Color.FromArgb(13, 110, 253);
        public static readonly Color NavItemHover   = Color.FromArgb(237, 240, 245);
        public static readonly Color GroupHeader    = Color.FromArgb(108, 117, 125);
    }
}
