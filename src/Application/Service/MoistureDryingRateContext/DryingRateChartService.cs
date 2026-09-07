using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// 干燥速率报告曲线图 PNG 生成 —— System.Drawing（Windows-only；本系统部署即 Windows）。
/// 纯函数：结果 DTO → PNG 字节，无状态、无 IO、无依赖注入。
/// 报告服务在组装填充模型时调用，返回的 PNG 由 DOCX 引擎嵌入"曲线图"占位段
/// （DryingRateDocxEngine / Aatcc201DocxEngine 占位文字同为"曲线图"，图片尺寸 14cm×8cm）。
/// 无任何参与工位（无曲线数据）→ 返回 null，引擎保留占位段不插图。
/// </summary>
public static class DryingRateChartService
{
    /// <summary>
    /// NF5022 蒸发曲线 PNG：6 工位蒸发量曲线同图。
    /// X = 时间(min) = 点序 × 采样间隔（间隔 sp 单位是分钟——设备回帧 @n，原软件 time=sp×(rp−1) 即分钟；
    ///     这里不再 ÷60，避免 60× 压缩，与结果表 TimeMin 完全同语义）。首点 0。
    /// Y = 蒸发量(mg)，从 0 起（蒸发从 0 → 滴水量）。
    /// </summary>
    public static byte[]? RenderNf5022EvaporationChart(Nf5022ComputeResultDto result)
    {
        var series = result.Stations
            .Where(s => s.Participated && s.EvaporationCurveMg is { Count: > 0 })
            .Select(s => new LineSeries(
                $"工位{s.Station}",
                s.EvaporationCurveMg!.Select((v, i) => (X: (double)(i * result.SpaceTimeMin), Y: v)).ToList()))
            .ToList();

        return series.Count == 0
            ? null
            : RenderLineChart("NF5022 蒸发曲线", "时间(min)", "蒸发量(mg)", series, yFromZero: true);
    }

    /// <summary>
    /// NF5022 单样品蒸发曲线 PNG —— **每个参与工位(样品)独立一张**。
    /// X = 时间(min) = 点序 × 采样间隔；Y = 蒸发量(mg)，从 0 起。图内自带头"样品N 蒸发曲线"，
    /// 报告服务逐参与工位调用、把各图按序给引擎追加到文档末尾。空曲线 → null。
    /// </summary>
    public static byte[]? RenderNf5022StationChart(int station, IReadOnlyList<double>? curveMg, int spaceTimeMin)
    {
        if (curveMg is not { Count: > 0 }) return null;
        var series = new List<LineSeries>
        {
            new($"工位{station}",
                curveMg.Select((v, i) => (X: (double)(i * spaceTimeMin), Y: v)).ToList())
        };
        return RenderLineChart($"样品{station} 蒸发曲线", "时间(min)", "蒸发量(mg)", series, yFromZero: true);
    }

    /// <summary>
    /// AATCC 201 表面温度曲线 PNG：2 工位表面温度曲线同图。
    /// X = 帧真实到达秒（FrameTimeSec，测试开始起）；Y = 温度(℃) = SurfaceTemp01 ÷ 100（已叠偏置）。
    /// </summary>
    public static byte[]? RenderAatcc201TemperatureChart(Aatcc201ComputeResultDto result)
    {
        var series = result.Stations
            .Where(s => s.Participated && s.SurfaceTempSeries is { Count: > 0 })
            .Select(s => new LineSeries(
                $"工位{s.Station}",
                s.SurfaceTempSeries!.Select(p => (X: p.FrameTimeSec, Y: p.SurfaceTemp01 / 100.0)).ToList()))
            .ToList();

        return series.Count == 0
            ? null
            : RenderLineChart("AATCC 201 表面温度曲线", "时间(s)", "温度(℃)", series, yFromZero: false);
    }

    /// <summary>单条曲线：标签 + 有序点集(X, Y)。</summary>
    private sealed record LineSeries(string Label, IReadOnlyList<(double X, double Y)> Points);

    /// <summary>工位配色（前两色够 AATCC 2 工位，全色板够 NF5022 6 工位）。</summary>
    private static readonly Color[] StationColors =
    {
        Color.FromArgb(230, 25, 75),    // 红
        Color.FromArgb(60, 180, 75),    // 绿
        Color.FromArgb(67, 99, 216),    // 蓝
        Color.FromArgb(245, 130, 49),   // 橙
        Color.FromArgb(145, 30, 180),   // 紫
        Color.FromArgb(66, 212, 244),   // 青
    };

    /// <summary>画布尺寸（px）。引擎按 14cm×8cm 框缩放嵌入，高分辨率保证清晰。</summary>
    private const int Width = 1400;
    private const int Height = 800;
    private const int MarginLeft = 120;
    private const int MarginRight = 50;
    private const int MarginTop = 80;
    private const int MarginBottom = 80;

    /// <summary>
    /// 通用折线图渲染。自动缩放坐标范围（X 恒从 0 起；Y 由 yFromZero 决定是否从 0 起——
    /// 蒸发曲线从 0 起、温度曲线用实际温区避免被压扁）。
    /// 白底、浅灰网格、工位彩色曲线 + 图例、微软雅黑（Windows 自带，失败回退通用无衬线）。
    /// </summary>
    private static byte[]? RenderLineChart(
        string title, string xTitle, string yTitle,
        IReadOnlyList<LineSeries> series, bool yFromZero)
    {
        int plotWidth = Width - MarginLeft - MarginRight;
        int plotHeight = Height - MarginTop - MarginBottom;

        // ── 数据范围（跳过 NaN）──
        double xMin = 0, xMax = 0, yMin = 0, yMax = 0;
        bool hasPoint = false;
        foreach (var s in series)
        {
            foreach (var (x, y) in s.Points)
            {
                if (double.IsNaN(x) || double.IsNaN(y)) continue;
                if (!hasPoint) { xMin = xMax = x; yMin = yMax = y; hasPoint = true; }
                else
                {
                    xMin = Math.Min(xMin, x); xMax = Math.Max(xMax, x);
                    yMin = Math.Min(yMin, y); yMax = Math.Max(yMax, y);
                }
            }
        }
        if (!hasPoint) return null;

        if (yFromZero) yMin = Math.Min(0, yMin);
        if (xMin > 0) xMin = 0;                            // 时间轴从 0 起
        if (yMax <= yMin) yMax = yMin + 1;                 // 零范围兜底（如全 0 曲线）
        if (xMax <= xMin) xMax = xMin + 1;
        double yRange = yMax - yMin;
        double xRange = xMax - xMin;
        yMax += yRange * 0.05;                             // 顶部留 5% 余量
        xMax += xRange * 0.05;

        // 整齐刻度（1/2/5×10ⁿ 步进）→ 刻度是 0/5/10、0/20/40 这类圆值，
        // 不做 22.4/44.8 碎值；映射与网格/标签共用同一组 nice 边界，避免文字叠字。
        var (xNiceMin, xNiceMax, xStep) = NiceRange(xMin, xMax, 6);
        var (yNiceMin, yNiceMax, yStep) = NiceRange(yMin, yMax, 5);
        double xSpan = xNiceMax - xNiceMin;
        double ySpan = yNiceMax - yNiceMin;
        int xCount = (int)Math.Round(xSpan / xStep);
        int yCount = (int)Math.Round(ySpan / yStep);

        using var bitmap = new Bitmap(Width, Height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.White);

            using var titleFont = CreateFont(34, FontStyle.Bold);
            using var axisFont = CreateFont(26, FontStyle.Regular);
            using var tickFont = CreateFont(20, FontStyle.Regular);

            // ── 网格 + 坐标轴刻度 ──
            using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1);
            using var axisPen = new Pen(Color.FromArgb(120, 120, 120), 2);
            // y 标签右对齐到绘图区左边沿前 12px —— 绝不与左侧旋转的 y 轴标题重叠；
            // x 标签居中，标签文字短（整齐刻度圆值），不再叠字。
            for (int i = 0; i <= yCount; i++)
            {
                double v = yNiceMin + yStep * i;
                float py = MarginTop + plotHeight - (float)((v - yNiceMin) / ySpan * plotHeight);
                g.DrawLine(gridPen, MarginLeft, py, MarginLeft + plotWidth, py);
                string label = FormatTick(v);
                var sz = g.MeasureString(label, tickFont);
                g.DrawString(label, tickFont, Brushes.Black, MarginLeft - 12 - sz.Width, py - sz.Height / 2);
            }
            for (int i = 0; i <= xCount; i++)
            {
                double v = xNiceMin + xStep * i;
                float px = MarginLeft + (float)((v - xNiceMin) / xSpan * plotWidth);
                g.DrawLine(gridPen, px, MarginTop, px, MarginTop + plotHeight);
                string label = FormatTick(v);
                var sz = g.MeasureString(label, tickFont);
                g.DrawString(label, tickFont, Brushes.Black, px - sz.Width / 2, MarginTop + plotHeight + 14);
            }

            // ── 绘图区边框 ──
            g.DrawRectangle(axisPen, MarginLeft, MarginTop, plotWidth, plotHeight);

            // ── 工位曲线 (先全画完; 屏幕点留着, 末尾名称最后统一画在最上层, 压住别的线也清晰) ──
            var screenPts = new List<PointF>[series.Count];
            for (int si = 0; si < series.Count; si++)
            {
                var s = series[si];
                using var linePen = new Pen(StationColors[si % StationColors.Length], 3);
                var pts = new List<PointF>(s.Points.Count);
                foreach (var (x, y) in s.Points)
                {
                    if (double.IsNaN(x) || double.IsNaN(y)) continue;
                    pts.Add(new PointF(
                        MarginLeft + (float)((x - xNiceMin) / xSpan * plotWidth),
                        MarginTop + plotHeight - (float)((y - yNiceMin) / ySpan * plotHeight)));
                }
                screenPts[si] = pts;
                if (pts.Count >= 2)
                    g.DrawLines(linePen, pts.ToArray());
                else if (pts.Count == 1)
                    g.FillEllipse(new SolidBrush(StationColors[si % StationColors.Length]),
                        pts[0].X - 5, pts[0].Y - 5, 10, 10);
            }

            // ── 标题 / 坐标轴名 / 图例 ──
            var titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, Brushes.Black, (Width - titleSize.Width) / 2, 20);

            var xTitleSize = g.MeasureString(xTitle, axisFont);
            g.DrawString(xTitle, axisFont, Brushes.Black,
                MarginLeft + (plotWidth - xTitleSize.Width) / 2, MarginTop + plotHeight + 40);

            var yState = g.Save();
            // y 轴标题放最左独立列（x≈20），刻度标签右对齐止于 MarginLeft-12，互不重叠
            g.TranslateTransform(20, MarginTop + plotHeight / 2);
            g.RotateTransform(-90);
            var yTitleSize = g.MeasureString(yTitle, axisFont);
            g.DrawString(yTitle, axisFont, Brushes.Black, -yTitleSize.Width / 2, 0);
            g.Restore(yState);

            // ── 曲线末尾名称 (替代原右上图例, 用户拍板 2026-09-03) ──
            // 每条曲线在自己的最后一点右侧标「工位N」(黑字 + 白底圆角小签, 压住别的曲线/网格也清晰)。
            // 真实蒸发曲线多条终点常挤在右上同一带——各签若按各自末点 Y 硬放必互相重叠;
            // 故按屏幕 Y 从上到下逐条排签, 下一条撞上上一条就往下让(≥6px): 顺序保持,
            // 顶部签永远对最高终点的线, 不会认错; 终点 Y 拉得开时让位规则自然失效(签就贴在各自线端)。
            using var labelFont = CreateFont(24, FontStyle.Regular);
            const float chipPadX = 6f, chipPadY = 3f, chipGap = 12f;
            var chips = new List<(int Si, PointF End, string Label, float Bw, float Bh, float TopY)>();
            for (int si = 0; si < series.Count; si++)
            {
                var pts = screenPts[si];
                if (pts == null || pts.Count == 0) continue;
                var end = pts[pts.Count - 1];
                var tsz = g.MeasureString(series[si].Label, labelFont);
                float bw = tsz.Width + chipPadX * 2;
                float bh = tsz.Height + chipPadY * 2;
                chips.Add((si, end, series[si].Label, bw, bh, end.Y - bh / 2));
            }
            float prevBottom = float.NegativeInfinity;
            foreach (var c in chips.OrderBy(c => c.TopY))
            {
                float top = Math.Max(c.TopY, prevBottom + 6);
                top = Math.Min(Math.Max(top, MarginTop + 2), MarginTop + plotHeight - c.Bh);
                prevBottom = top + c.Bh;

                // 默认放终点右侧; 出画布右缘则挪到终点左侧(白底压自己线尾也无妨)
                float x = c.End.X + chipGap - chipPadX;
                if (x + c.Bw > Width - 6) x = c.End.X - chipGap - c.Bw;
                x = Math.Max(x, MarginLeft + 4);

                using var bgBrush = new SolidBrush(Color.FromArgb(248, 255, 255, 255));
                using var bgPath = RoundedRect(x, top, c.Bw, c.Bh, 7);
                g.FillPath(bgBrush, bgPath);
                g.DrawString(c.Label, labelFont, Brushes.Black, x + chipPadX, top + chipPadY);
            }
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// 整齐刻度范围：把 [min,max] 扩成 [⌊min/step⌋·step, ⌈max/step⌉·step]，
    /// step 取 1/2/5×10ⁿ —— 刻度自然落在 0/5/10、0/20/40 这类圆值上，
    /// 标签短、间隔均匀，不会出现 22.4/44.8 碎值挤字。
    /// </summary>
    private static (double Min, double Max, double Step) NiceRange(double min, double max, int targetTicks)
    {
        double span = max - min;
        if (span <= 0) span = 1;
        double roughStep = span / Math.Max(targetTicks, 1);
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        double norm = roughStep / magnitude;                     // 1..10
        double step = (norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10) * magnitude;
        double niceMin = Math.Floor(min / step) * step;
        double niceMax = Math.Ceiling(max / step) * step;
        if (niceMax - niceMin < step) niceMax = niceMin + step;  // 保证至少一格
        return (niceMin, niceMax, step);
    }

    /// <summary>坐标刻度文本：两位小数封顶，尾零与 0 规范化。</summary>
    private static string FormatTick(double v)
    {
        double r = Math.Round(v, 2);
        if (Math.Abs(r) < 0.005) r = 0;
        return r.ToString("0.##");
    }

    /// <summary>圆角矩形路径（GraphicsPath 无现成 API，手绘 4 段圆弧 + 4 直线闭合）。</summary>
    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        var path = new GraphicsPath();
        if (r <= 0 || r > w / 2 || r > h / 2)
        {
            path.AddRectangle(new RectangleF(x, y, w, h));
            return path;
        }
        float d = r * 2;
        path.AddArc(x, y, d, d, 180, 90);              // 左上
        path.AddArc(x + w - d, y, d, d, 270, 90);      // 右上
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90); // 右下
        path.AddArc(x, y + h - d, d, d, 90, 90);        // 左下
        path.CloseFigure();
        return path;
    }

    /// <summary>微软雅黑（Windows 自带，实验室系统必有）；构造失败回退通用无衬线，绝不因字体中断报告。</summary>
    private static Font CreateFont(float size, FontStyle style)
    {
        try { return new Font("Microsoft YaHei", size, style, GraphicsUnit.Pixel); }
        catch { return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel); }
    }
}
