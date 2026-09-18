using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
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
    /// NF5022 单样品蒸发曲线 PNG —— 每个参与工位(样品)独立一张。
    /// X = 时间(min) = 点序 × 采样间隔；Y = 蒸发量(mg)，从 0 起。图内自带头"样品N 蒸发曲线"，
    /// 报告服务逐参与工位调用、把各图按序给引擎追加到文档末尾。空曲线 → null。
    /// resultPoint（终止点数, 即该工位报告里的 ResultPoint）给定且有效时，额外叠一条
    /// 【干燥速率斜率线】—— 干燥段最小二乘拟合直线 + 线尾速率值(g/h)，见 BuildSlopeLine；
    /// 拟合退化就只画曲线。传 0（默认）＝ 不画线。
    /// </summary>
    public static byte[]? RenderNf5022StationChart(
        int station, IReadOnlyList<double>? curveMg, int spaceTimeMin, int resultPoint = 0)
    {
        if (curveMg is not { Count: > 0 }) return null;
        var series = new List<LineSeries>
        {
            new($"工位{station}",
                curveMg.Select((v, i) => (X: (double)(i * spaceTimeMin), Y: v)).ToList())
        };
        var slope = BuildSlopeLine(curveMg, spaceTimeMin, resultPoint);
        return RenderLineChart($"样品{station} 蒸发曲线", "时间(min)", "蒸发量(mg)", series, yFromZero: true,
            slope is null ? null : new[] { slope });
    }

    /// <summary>
    /// 干燥速率【斜率线】—— 由曲线干燥段（首点到终止点）的最小二乘拟合直线构造:
    /// 图坐标两端点（X=分钟, Y=mg）+ 线尾数值签（g/h, 与报告表格同口径同精度）。
    /// 这条线不是示意线: 它的斜率与报告里填的干燥速率同源同值, 画出来就是那个数字的可视化。
    /// 配色走 SlopeLineColor（注解色, 不与曲线同色 —— 同色时中段两条线叠在一起看不出来）。
    /// resultPoint 不足 2 / 间隔非正 / 回归退化 → null（只画曲线, 不画线）。
    /// </summary>
    private static AuxLine? BuildSlopeLine(IReadOnlyList<double> curveMg, int spaceTimeMin, int resultPoint)
    {
        if (spaceTimeMin <= 0 || resultPoint < 2) return null;
        if (Nf5022Formulas.RegressionFitMgPerHour(curveMg, resultPoint, spaceTimeMin) is not { } fit) return null;

        double xEndMin = (resultPoint - 1) * (double)spaceTimeMin;   // 干燥段末点(分钟)
        // 拟合的 x 单位是【小时】, 图上 X 轴是分钟 → 两端点都要先 ÷60 再代进直线方程。
        // 线不过原点（截距一般不为 0, 正负看曲线形状），如实画: 起点略偏 0 才是这条拟合线的真样子。
        // 越出绘图区的部分由 ClipSegment 裁掉, 不硬拉回原点（硬拉就等于改了斜率）。
        double y0 = fit.Intercept;
        double y1 = fit.Intercept + fit.Slope * (xEndMin / 60.0);
        string label = (fit.Slope / 1000.0).ToString("F3", CultureInfo.InvariantCulture) + " g/h";
        return new AuxLine(0, y0, xEndMin, y1, SlopeLineColor, 2, DashStyle.Dash, label);
    }

    /// <summary>
    /// AATCC 201 表面温度曲线 PNG：最多 3 次测试(测试1/2/3)表面温度曲线同图。
    /// X = 采样点号（第 k 点 = 第 k 帧）—— 与原软件屏幕上那张图同口径（MainForm.cs:3162 轴名"点数"、
    ///     3065 AddXY 用点号）；不用 FrameTimeSec：前端采集非均匀（后台标签页定时器被节流），
    ///     按墙钟秒画会把同一段曲线拉成不同形状，而结果表的起点/终点本来就是点号。
    /// Y = 温度(℃) = SurfaceTemp01 ÷ 100（已叠偏置）。
    /// 图例带测试序号 → 测试3 复用工位时(如 测试3·工位1)也不与首次测试的曲线重名/覆盖。
    /// slopePoint/flatPoint 均大于 0 时，逐参与测试叠 draw_two 的两条延长线（点状虚线，同测试配色）
    /// 与终点竖线（实线，贯穿绘图区全高；斜坡虚线同样沿原斜率接到上沿，两条线一样高）
    /// —— 两条延长线的几何由 BuildDrawLines 从同一份构造给出，
    /// 两条虚线就是算终点用的那两条，但几何交点比 EndPoint 早几个点（draw_two 早停口径），
    /// 终点竖线因此画在 EndPoint 上、与交汇处有轻微错位 —— 这是口径决定的，不是作图错位。传 0（默认）＝ 只画曲线。
    /// </summary>
    public static byte[]? RenderAatcc201TemperatureChart(
        Aatcc201ComputeResultDto result, int slopePoint = 0, int flatPoint = 0)
    {
        var stations = result.Stations
            .Select((s, i) => (s, i))
            .Where(x => x.s.Participated && x.s.SurfaceTempSeries is { Count: > 0 })
            .ToList();
        if (stations.Count == 0) return null;

        var series = new List<LineSeries>(stations.Count);
        var aux = new List<AuxLine>();
        foreach (var (s, i) in stations)
        {
            var pts = s.SurfaceTempSeries!;
            series.Add(new LineSeries(
                $"测试{i + 1} · 工位{s.Station}",
                pts.Select((p, k) => (X: (double)(k + 1), Y: p.SurfaceTemp01 / 100.0)).ToList()));

            if (slopePoint <= 0 || flatPoint <= 0)
                continue;
            var draw = Aatcc201CalculationService.BuildDrawLines(
                pts.Select(p => p.SurfaceTemp01).ToList(), slopePoint, flatPoint, s.SlopeMaxPoint, s.FlatMinPoint);
            if (draw is null)
                continue;

            var color = StationColors[i % StationColors.Length];
            const float auxWidth = 1f;
            // 斜坡线带 ToTop: 沿线自身方向接到绘图区上沿（常规温区上沿就是 38℃ 那条刻度线），
            // 与终点竖线一样画满全高 —— 否则它到 37℃ 截断点就停，比竖线短一截，看着像没画完。
            // 只延长作图: SlopeB 仍是 37℃ 截断点。那是 BuildDrawArrays 喂给 draw_two 的数组右边界,
            // 动了它会连带改终点搜索结果（也就是改速率）—— 与"把线画长一点"是两回事。
            aux.Add(new AuxLine(draw.SlopeA.X, draw.SlopeA.Y / 100.0, draw.SlopeB.X, draw.SlopeB.Y / 100.0,
                color, auxWidth, DashStyle.Dot, Label: null, ToTop: true));
            aux.Add(new AuxLine(draw.FlatA.X, draw.FlatA.Y / 100.0, draw.FlatB.X, draw.FlatB.Y / 100.0,
                color, auxWidth, DashStyle.Dot));
            if (s.EndPoint > 0)
            {
                // 终点竖线：真正的竖线（原软件是 (终点−1,0)→(终点,37) 的近似竖线, 一帧横向只有像素级宽度），
                // 贯穿绘图区全高 —— 上端收在顶边框、下端从窗口下沿外裁到边框。
                // Y1 那个 37℃ 只在 ToTop 关掉时才会生效, 留着是记录原软件那个标记的高度。
                aux.Add(new AuxLine(s.EndPoint, 0, s.EndPoint,
                    Aatcc201CalculationService.SlopeLineCeiling01 / 100.0,
                    color, auxWidth, DashStyle.Solid, Label: null, ToTop: true));
            }
        }

        // 有辅助线时把固定上界抬进窗口：斜坡线延伸到 37℃ 截断，窗口上沿不到 37 就看不见它的尽头
        double? yMaxHint = aux.Count > 0 ? Aatcc201CalculationService.SlopeLineCeiling01 / 100.0 : null;
        return RenderLineChart("AATCC 201 表面温度曲线", "点数", "温度(℃)", series, yFromZero: false,
            aux.Count > 0 ? aux : null, yMaxHint);
    }

    /// <summary>单条曲线：标签 + 有序点集(X, Y)。</summary>
    private sealed record LineSeries(string Label, IReadOnlyList<(double X, double Y)> Points);

    /// <summary>
    /// 图上辅助线（数据坐标两端点）—— NF5022 的干燥速率斜率线与 AATCC 的两条延长线/终点竖线共用。
    /// Style 区分实线/虚线；Label 非空时在线尾挂一个数值签（目前只有 NF5022 那条用）。
    /// ToTop：末端不取 Y1，而是一路画到绘图区顶边框（沿线段自身方向外推，见 ExtendToTop + 裁剪收口）。
    /// 用于两条"画满全高"的线：终点竖线与 AATCC 斜坡线。好处是不随 Y 轴刻度上界（30/38 这类取整值）
    /// 而变短留空 —— 温区不同导致上界变化时，这两条线始终顶到边框。
    /// </summary>
    private sealed record AuxLine(
        double X0, double Y0, double X1, double Y1,
        Color Color, float Width, DashStyle Style, string? Label = null, bool ToTop = false);

    /// <summary>工位配色（前三色够 AATCC 3 次测试，全色板够 NF5022 6 工位）。</summary>
    private static readonly Color[] StationColors =
    {
        Color.FromArgb(230, 25, 75),    // 红
        Color.FromArgb(60, 180, 75),    // 绿
        Color.FromArgb(67, 99, 216),    // 蓝
        Color.FromArgb(245, 130, 49),   // 橙
        Color.FromArgb(145, 30, 180),   // 紫
        Color.FromArgb(66, 212, 244),   // 青
    };

    /// <summary>
    /// 斜率线配色 —— 刻意【不】取工位色（原先同色, 中段两条线叠在一起看不清）。
    /// 拟合线在干燥段几乎压在蒸发曲线上, 同色时只有首尾偏离处才看得出有两条线, 中段就等于没画。
    /// 用近黑作"注解色": 与它自己的数值签(黑字)同色, 一眼看出线签是一体的;
    /// 且不与任何工位色(红/绿/蓝/橙/紫/青)撞色, 灰度打印时(红≈中灰)也与曲线拉得开。
    /// </summary>
    private static readonly Color SlopeLineColor = Color.FromArgb(40, 40, 40);

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
    /// auxLines：叠加的辅助线（斜率线/延长线/终点竖线），不参与坐标范围计算；
    /// yMaxHint：固定的 Y 上界抬升（让已知的线端落在窗口内，如 AATCC 的 37℃ 截断线）。
    /// </summary>
    private static byte[]? RenderLineChart(
        string title, string xTitle, string yTitle,
        IReadOnlyList<LineSeries> series, bool yFromZero,
        IReadOnlyList<AuxLine>? auxLines = null, double? yMaxHint = null)
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
        if (yMaxHint is { } hint && yMax < hint) yMax = hint; // 已知线端（如 37℃ 截断）抬进窗口
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

        // 数据坐标 → 屏幕坐标（刻度/曲线/斜率线共用同一套映射，三者才会严格对齐）
        PointF Map(double x, double y) => new(
            MarginLeft + (float)((x - xNiceMin) / xSpan * plotWidth),
            MarginTop + plotHeight - (float)((y - yNiceMin) / ySpan * plotHeight));

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
                    pts.Add(Map(x, y));
                }
                screenPts[si] = pts;
                if (pts.Count >= 2)
                    g.DrawLines(linePen, pts.ToArray());
                else if (pts.Count == 1)
                    g.FillEllipse(new SolidBrush(StationColors[si % StationColors.Length]),
                        pts[0].X - 5, pts[0].Y - 5, 10, 10);
            }

            // ── 辅助线（NF5022 的干燥速率斜率线 / AATCC 的两条延长线 + 终点竖线）──
            // NF5022 那条是 2px 虚线 + 注解色（SlopeLineColor, 不用工位色）：
            // 虚线=与原数据的实线区分开, 异色=拟合线中段压在曲线上时两条都还看得见
            // （同色时只有首尾偏离处能看出是两条线, 中段视觉上合成一条）。
            // 只覆盖拟合域（首点→终止点），不往后延伸
            // —— 延伸会让人误读成"平台段仍按该速率失水"。线尾另挂数值签（见下）。
            // 两端都可能越出绘图区，统一先 ClipSegment 裁到窗口内再画；整条在外就跳过。
            (PointF At, AuxLine Line)? labeledEnd = null;
            if (auxLines is not null)
            {
                foreach (var line in auxLines)
                {
                    // ToTop 的末端外推到窗口上方一整段: 裁剪再把线收口在顶边框上，等于"画到顶"。
                    // 外推沿线段自身方向（竖线外推后 X 不变，斜线保住原斜率），不直接用 double.MaxValue
                    // —— 差值参与参数化裁剪运算, 保持同量级更稳妥。
                    var end = line.ToTop
                        ? ExtendToTop(line.X0, line.Y0, line.X1, line.Y1, yNiceMax + ySpan)
                        : (line.X1, line.Y1);
                    if (ClipSegment((line.X0, line.Y0), end,
                            xNiceMin, xNiceMax, yNiceMin, yNiceMax) is not { } seg)
                        continue;

                    var p0 = Map(seg.A.X, seg.A.Y);
                    var p1 = Map(seg.B.X, seg.B.Y);
                    using var pen = new Pen(line.Color, line.Width) { DashStyle = line.Style };
                    g.DrawLine(pen, p0, p1);
                    if (line.Label is not null && labeledEnd is null)
                        labeledEnd = (p1, line);   // 数值签只挂第一条带签的线
                }
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
            var placedChips = new List<RectangleF>();
            foreach (var c in chips.OrderBy(c => c.TopY))
            {
                float top = Math.Max(c.TopY, prevBottom + 6);
                top = Math.Min(Math.Max(top, MarginTop + 2), MarginTop + plotHeight - c.Bh);
                prevBottom = top + c.Bh;

                // 默认放终点右侧; 出画布右缘则挪到终点左侧(白底压自己线尾也无妨)
                float x = c.End.X + chipGap - chipPadX;
                if (x + c.Bw > Width - 6) x = c.End.X - chipGap - c.Bw;
                x = Math.Max(x, MarginLeft + 4);
                placedChips.Add(new RectangleF(x, top, c.Bw, c.Bh));

                using var bgBrush = new SolidBrush(Color.FromArgb(248, 255, 255, 255));
                using var bgPath = RoundedRect(x, top, c.Bw, c.Bh, 7);
                g.FillPath(bgBrush, bgPath);
                g.DrawString(c.Label, labelFont, Brushes.Black, x + chipPadX, top + chipPadY);
            }

            // ── 斜率线数值签（干燥速率 g/h）──
            // 挂在虚线线尾。终止点常贴近曲线末端 → 这个签与上面「工位N」签几乎必定撞上，
            // 故撞一次就往下让一格（白底签压住虚线尾也无妨，两个签都读得清）。
            if (labeledEnd is { } lab)
            {
                var endPt = lab.At;
                var tsz = g.MeasureString(lab.Line.Label!, labelFont);
                float bw = tsz.Width + chipPadX * 2;
                float bh = tsz.Height + chipPadY * 2;
                float x = endPt.X + chipGap - chipPadX;
                if (x + bw > Width - 6) x = endPt.X - chipGap - bw;
                x = Math.Max(x, MarginLeft + 4);

                float top = endPt.Y - bh / 2;
                foreach (var r in placedChips)
                    if (new RectangleF(x, top, bw, bh).IntersectsWith(r)) top = r.Bottom + 6;
                top = Math.Min(Math.Max(top, MarginTop + 2), MarginTop + plotHeight - bh);

                using var bgBrush = new SolidBrush(Color.FromArgb(248, 255, 255, 255));
                using var bgPath = RoundedRect(x, top, bw, bh, 7);
                g.FillPath(bgBrush, bgPath);
                g.DrawString(lab.Line.Label!, labelFont, Brushes.Black, x + chipPadX, top + chipPadY);
            }
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// 把线段末端 (X1,Y1) 沿【自身方向】外推到高度 targetY：方向向量整体乘一个正参数，斜率逐字不变，只变长。
    /// 终点竖线（末端朝上、X0 与 X1 相同）与 AATCC 斜坡线（斜着往上）共用 —— 竖线外推后 X 不变，
    /// 与"只把 Y1 抬到窗外"的老写法等价；斜线若也照老写法只抬 Y1 就会把斜率掰直，所以必须走这里。
    /// 末端已不低于 targetY，或方向不是向上（水平线/下行线）→ 原样返回，不外推也不反推。
    /// </summary>
    private static (double X, double Y) ExtendToTop(double x0, double y0, double x1, double y1, double targetY)
    {
        double dy = y1 - y0;
        if (dy <= 0 || y1 >= targetY) return (x1, y1);
        double t = (targetY - y1) / dy;              // 沿方向的参数增量, 恒为正
        return (x1 + (x1 - x0) * t, targetY);
    }

    /// <summary>
    /// 把线段裁到窗口内（Liang-Barsky 参数化裁剪），返回裁剪后的两端点；整条线都在窗口外 → null。
    /// 斜率线是【拟合】线，两端都可能越出绘图区，不裁就会画到坐标轴外面 ——
    /// 左端: 最小二乘直线的截距一般不为 0（正负都有可能，取决于曲线是鼓是凹），
    ///       为负时 x=0 处的拟合值落到 0 以下，而蒸发量轴的窗口下沿是 0；
    /// 右端: 拟合域末点若抖动到数据顶部之上，也会冒出去。
    /// xLo/xHi/yLo/yHi 传的是绘图区在【数据坐标】下的范围（与 Map 的换算同一套边界）。
    /// </summary>
    private static ((double X, double Y) A, (double X, double Y) B)? ClipSegment(
        (double X, double Y) a, (double X, double Y) b,
        double xLo, double xHi, double yLo, double yHi)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double t0 = 0, t1 = 1;
        if (!ClipEdge(-dx, a.X - xLo, ref t0, ref t1)) return null;
        if (!ClipEdge(dx, xHi - a.X, ref t0, ref t1)) return null;
        if (!ClipEdge(-dy, a.Y - yLo, ref t0, ref t1)) return null;
        if (!ClipEdge(dy, yHi - a.Y, ref t0, ref t1)) return null;
        return ((a.X + t0 * dx, a.Y + t0 * dy), (a.X + t1 * dx, a.Y + t1 * dy));
    }

    /// <summary>Liang-Barsky 单边界测试：p 为方向分量, q 为到边界的距离; 收窄参数区间 [t0,t1], 全在外侧返回 false。</summary>
    private static bool ClipEdge(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-12) return q >= 0;        // 平行于该边界: 只判是不是在内侧
        double r = q / p;
        if (p < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
        else { if (r < t0) return false; if (r < t1) t1 = r; }
        return true;
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
