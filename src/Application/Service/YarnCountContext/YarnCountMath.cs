using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;

namespace NX_lims_Softlines_Command_System.src.Application.Service.YarnCountContext;

/// <summary>
/// 纱支的算式与取位 —— 全模块唯一的实现。
///
/// 为什么单独拎出来: 页面要即时显示 Average/Tex, 报告要落格, 两边必须逐位一致,
/// 否则会出现"页面上是 12.34、报告里是 12.33"这种最难解释的差异。
/// 取位常量放在 YarnCountReportRequestDto(契约层), 前端镜像同一组值; 算式只有这一份。
///
/// 舍入一律按实验室口径：常规四舍五入。
/// </summary>
public static class YarnCountMath
{
    /// <summary>可空值舍入; 空值原样返回(不把"没测"变成 0)</summary>
    public static decimal? Round(decimal? value, int decimals)
        => value.HasValue ? RoundValue(value.Value, decimals) : null;

    /// <summary>舍入到指定小数位(四舍五入)</summary>
    public static decimal RoundValue(decimal value, int decimals)
        => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    /// <summary>若干读数的算术平均并舍入; 忽略空槽位(null), 全空返回 null</summary>
    public static decimal? Average(IEnumerable<decimal?> values, int decimals)
    {
        var valid = values?.Where(v => v.HasValue).Select(v => v.Value).ToList() ?? new List<decimal>();
        if (valid.Count == 0) return null;
        return RoundValue(valid.Average(), decimals);
    }

    /// <summary>
    /// Tex = (Mass ÷ 50 × 100) ÷ (Average Length ÷ 100)   ← 模板正文里的原式
    ///     = Mass × 200 ÷ Average
    ///
    /// 传入的必须是“已舍入”的 Average 与 Mass(服务端就是这么调的), 这样报告上印的三个数自成闭环:
    /// （拿纸上印的 Average/Mass 套同一个式子, 能算出纸上印的那个 Tex）。
    /// 分母 ≤ 0 或 Mass 缺失时返回 null → 报告该格留空, 不写 0。
    /// </summary>
    public static decimal? ComputeTex(decimal? average, decimal? mass)
    {
        if (!average.HasValue || !mass.HasValue) return null;
        if (average.Value <= 0m) return null;

        return RoundValue(mass.Value * 200m / average.Value, YarnCountReportRequestDto.TexDecimals);
    }

    /// <summary>
    /// 方向汇总 Tex = 该方向各试样 Tex 的**算术平均**(各试样等权), 再舍入。
    /// 注意不是"合计质量 ÷ 平均长度"的池化口径 —— 两者在试样不等长时不等值, 本模块按等权取。
    /// 只统计真的算出 Tex 的试样(缺质量的试样不拉低汇总); 一个都没有则返回 null → 该格留空。
    /// </summary>
    public static decimal? MeanTex(IEnumerable<decimal?> texValues)
    {
        var valid = texValues?.Where(v => v.HasValue).Select(v => v.Value).ToList() ?? new List<decimal>();
        if (valid.Count == 0) return null;
        return RoundValue(valid.Average(), YarnCountReportRequestDto.TexDecimals);
    }
}
