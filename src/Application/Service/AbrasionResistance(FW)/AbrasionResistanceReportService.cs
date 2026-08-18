using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_.NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;

namespace NX_lims_Softlines_Command_System.src.Application.Service.AbrasionResistance_FW_
{
    /// <summary>
    /// 耐磨测试报告生成 — 用 Abrasion Resistance-Rotating Drum Method.docx 模板填充数据。
    /// 业务计算(密度/体积损失/磨耗指数)在本层, OpenXml 填充委托给 IAbrasionResistanceDocxEngine。
    /// </summary>
    public class AbrasionResistanceReportService :IAbrasionResistanceReportService, IScopedDependency
    {
        private readonly IAbrasionResistanceDocxEngine _engine;
        private readonly IFileStorageService _fileStorage;

        public AbrasionResistanceReportService(IAbrasionResistanceDocxEngine engine, IFileStorageService fileStorage)
        {
            _engine = engine;
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// 生成报告
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Result<DocxUrlResponseDto> Generate(BuildReportDto dto)
        {
            // 1. 基础校验
            if (dto == null)
                return Result<DocxUrlResponseDto>.Fail("请求数据为空");
            if (string.IsNullOrWhiteSpace(dto.ReportNo))
                return Result<DocxUrlResponseDto>.Fail("报告号不能为空");
            if (dto.AbrasionSpecimens == null || dto.AbrasionSpecimens.Count == 0)
                return Result<DocxUrlResponseDto>.Fail("磨耗试样数据不能为空");

            // 2. 计算测试样品密度 (Specimen A/B)
            var testDensities = dto.TestDensities?.Select(t => new
            {
                t.Specimen,
                t.M1,
                t.M2,
                Density = CalculateDensity(t.M1, t.M2)
            }).ToList() ?? new();

            var testDensityA = testDensities.FirstOrDefault(t => t.Specimen == "A")?.Density;
            var testDensityB = testDensities.FirstOrDefault(t => t.Specimen == "B")?.Density;
            var testDensityAvg = Average(testDensityA, testDensityB);

            // 3. 计算参照化合物密度 (Specimen A/B)
            var refDensities = dto.RefDensities?.Select(r => new
            {
                r.Specimen,
                r.M1,
                r.M2,
                Density = CalculateDensity(r.M1, r.M2)
            }).ToList() ?? new();

            var refDensityA = refDensities.FirstOrDefault(r => r.Specimen == "A")?.Density;
            var refDensityB = refDensities.FirstOrDefault(r => r.Specimen == "B")?.Density;
            var refDensityAvg = Average(refDensityA, refDensityB);

            // 4. 计算磨耗数据 (3个试样)
            var specimenResults = dto.AbrasionSpecimens
                .Select(s =>
                {
                    var massLoss = s.BeforeWeight.HasValue && s.AfterWeight.HasValue
                        ? s.BeforeWeight - s.AfterWeight
                        : (decimal?)null;

                    // 体积损失 - 使用新公式
                    var volLoss = CalculateVolLoss(
                        s.BeforeWeight, s.AfterWeight,
                        testDensityAvg,           // 试样密度均值 (g/cm³)
                        dto.M1Constant,
                        dto.M2Constant,
                        dto.AbrasionDistance);

                    // 磨耗指数 - 使用新公式
                    var arIndex = CalculateARIndex(
                        s.BeforeWeight, s.AfterWeight,
                        testDensityAvg,           // 试样密度均值 (g/cm³)
                        dto.M1Constant,
                        dto.M2Constant);

                    return new SpecimenResult
                    {
                        SpecimenNumber = s.SpecimenNumber,
                        BeforeWeight = s.BeforeWeight,
                        AfterWeight = s.AfterWeight,
                        MassLoss = massLoss,
                        TestDensity = testDensityAvg,
                        M1Const = dto.M1Constant,
                        M2Const = dto.M2Constant,
                        VolLoss = volLoss,
                        ARIndex = arIndex,
                        // ... 其他字段
                    };
                })
                .ToList();

            // 5. 构建填充模型
            var model = new AbrasionResistanceReportFillModel
            {
                // ==================== 报告头 ====================
                ReportNo = dto.ReportNo,
                Method = dto.Standard + " " + dto.MethodCategory,
                DateIn = dto.GeneratedAt.ToString("yyyy-MM-dd"),
                DateOut = dto.GeneratedAt.ToString("yyyy-MM-dd"),
                SampleRef = dto.Sample,
                SampleDescription = dto.Sample,
                AbrasionDistance = dto.AbrasionDistance,
                Condition = dto.Condition,
                TestAtmosphere = "23 ± 2°C / 50 ± 2% RH",
                CleanMethod = dto.CleanMethod,

                // ==================== 表头结果行 ====================
                SampleResult = dto.Sample,
                ResultDensity = testDensityAvg,
                ResultVolLoss = specimenResults.Average(s => s.VolLoss),
                ResultARIndex = specimenResults.Average(s => s.ARIndex),
                Requirement = dto.Requirement,
                Conclusion = GetConclusion(specimenResults, dto.Requirement),

                // ==================== 测试样品密度 (含公式) ====================
                TestSpecimenA = "A",
                TestM1_A = dto.TestDensities?.FirstOrDefault(t => t.Specimen == "A")?.M1,
                TestM2_A = dto.TestDensities?.FirstOrDefault(t => t.Specimen == "A")?.M2,
                TestDensityA = testDensityA,
                TestDensityA_Formula = BuildDensityFormula(
        dto.TestDensities?.FirstOrDefault(t => t.Specimen == "A")?.M1,
        dto.TestDensities?.FirstOrDefault(t => t.Specimen == "A")?.M2,
        testDensityA),

                TestSpecimenB = "B",
                TestM1_B = dto.TestDensities?.FirstOrDefault(t => t.Specimen == "B")?.M1,
                TestM2_B = dto.TestDensities?.FirstOrDefault(t => t.Specimen == "B")?.M2,
                TestDensityB = testDensityB,
                TestDensityB_Formula = BuildDensityFormula(
        dto.TestDensities?.FirstOrDefault(t => t.Specimen == "B")?.M1,
        dto.TestDensities?.FirstOrDefault(t => t.Specimen == "B")?.M2,
        testDensityB),

                // ==================== 体积损失 (每个specimen独立) ====================
                // Specimen 1
                Specimen1Number = 1,
                Specimen1_BeforeWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 1)?.BeforeWeight,
                Specimen1_AfterWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 1)?.AfterWeight,
                Specimen1_MassLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.MassLoss,
                Specimen1_VolLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.VolLoss,
                Specimen1_VolLoss_Formula = BuildVolLossFormula(
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 1)?.BeforeWeight,
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 1)?.AfterWeight,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                   GetAbrasionDistanceFactor(dto.AbrasionDistance),
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.VolLoss),

                // Specimen 2
                Specimen2Number = 2,
                Specimen2_BeforeWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 2)?.BeforeWeight,
                Specimen2_AfterWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 2)?.AfterWeight,
                Specimen2_MassLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.MassLoss,
                Specimen2_VolLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.VolLoss,
                Specimen2_VolLoss_Formula = BuildVolLossFormula(
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 2)?.BeforeWeight,
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 2)?.AfterWeight,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                   GetAbrasionDistanceFactor(dto.AbrasionDistance),
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.VolLoss),

                // Specimen 3
                Specimen3Number = 3,
                Specimen3_BeforeWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 3)?.BeforeWeight,
                Specimen3_AfterWeight = dto.AbrasionSpecimens.FirstOrDefault(s => s.SpecimenNumber == 3)?.AfterWeight,
                Specimen3_MassLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.MassLoss,
                Specimen3_VolLoss = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.VolLoss,
                Specimen3_VolLoss_Formula = BuildVolLossFormula(
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 3)?.BeforeWeight,
                    dto.AbrasionSpecimens?.FirstOrDefault(s => s.SpecimenNumber == 3)?.AfterWeight,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                   GetAbrasionDistanceFactor(dto.AbrasionDistance),
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.VolLoss),

                // ==================== 磨耗指数 (每个specimen独立) ====================
                Specimen1ARIndex = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.ARIndex,
                Specimen1_ARIndex_Formula = BuildARIndexFormula(
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.BeforeWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.AfterWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.MassLoss,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 1)?.ARIndex),

                Specimen2ARIndex = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.ARIndex,
                Specimen2_ARIndex_Formula = BuildARIndexFormula(
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.BeforeWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.AfterWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.MassLoss,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 2)?.ARIndex),

                Specimen3ARIndex = specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.ARIndex,
                Specimen3_ARIndex_Formula = BuildARIndexFormula(
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.BeforeWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.AfterWeight,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.MassLoss,
                    testDensityAvg,
                    dto.M1Constant,
                    dto.M2Constant,
                    specimenResults.FirstOrDefault(s => s.SpecimenNumber == 3)?.ARIndex),
                // ==================== 底部 ====================
                Remark = dto.Operator,
                GeneratedAt = dto.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 6. 复制模板并填充 (委托给 Engine)
            string fileName = $"{dto.ReportNo}_{DateTime.Now:yyMMddHHmmss}_Abrasion_Resistance.docx";
            string targetPath = _fileStorage.CopyTemplate(
                Path.Combine("DocxModel", "Abrasion_Resistance-Rotating_Drum_Method.docx"),
                Path.Combine("DocxModel", "SaveDocx"),
                fileName);

            try
            {
                _engine.FillReport(targetPath, model);
            }
            catch (Exception ex)
            {
                return Result<DocxUrlResponseDto>.Fail("生成报告失败: " + ex.Message);
            }

            return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto
            {
                fileKey = fileName,
                fileName = fileName,
                downloadUrl = $"/api/AbrasionResistanceReport/{fileName}/download"
            });
        }

        /// <summary>密度计算: ρ = ρ_w × m1 / (m1 - m2)</summary>
        private static decimal? CalculateDensity(decimal? m1, decimal? m2)
        {
            if (!m1.HasValue || !m2.HasValue) return null;
            if (m1.Value - m2.Value == 0) return null;
            const decimal waterDensity = 1m; // 23°C 水的密度 取1(g/cm³)
            return waterDensity * m1.Value / (m1.Value - m2.Value);
        }

        /// <summary>
        /// 相对体积损失: ΔV_rel = 1000 × (W1-W2) × 400mg / [ρ(g/m³) × (M1+M2)] × 里程系数
        /// 注意: ρ 需要传入 g/m³ 单位
        /// </summary>
        /// <summary>
        /// 相对体积损失: ΔV_rel = 1000 × Δm_t × 0.4 / [ρ_t × (M1+M2)] × 里程系数
        /// </summary>
        private static decimal? CalculateVolLoss(
            decimal? beforeWeight,
            decimal? afterWeight,
            decimal? testDensity,     // g/cm³
            decimal? m1Const,
            decimal? m2Const,
            string abrasionDistance)
        {
            if (!beforeWeight.HasValue || !afterWeight.HasValue) return null;
            if (!testDensity.HasValue || testDensity.Value == 0) return null;
            if (!m1Const.HasValue || !m2Const.HasValue) return null;

            var deltaMt = beforeWeight.Value - afterWeight.Value;  // W1-W2 (g)
            if (deltaMt <= 0) return null;

            var distanceFactor = GetAbrasionDistanceFactor(abrasionDistance);

            // 400mg = 0.4g
            return 1000m * deltaMt * 400m / (testDensity.Value * (m1Const.Value + m2Const.Value)) * distanceFactor;
        }

        /// <summary>
        /// 获取磨损里程系数
        /// </summary>
        private static decimal GetAbrasionDistanceFactor(string abrasionDistance)
        {
            if (string.IsNullOrWhiteSpace(abrasionDistance))
                return 1m;

            return abrasionDistance.ToLower().Trim() switch
            {
                "full" or "全程" => 1m,
                "half" or "半程" => 2m,
                "quarter" or "1/4" or "¼" => 4m,
                _ => 1m  // 默认全程
            };
        }

        /// <summary>
        /// 磨耗指数: I_AR = (Δm_r × ρ_t) / (Δm_t × ρ_r) × 100
        /// Δm_r = (M1+M2)/2, ρ_r = 1.15 g/cm³
        /// </summary>
        private static decimal? CalculateARIndex(
            decimal? beforeWeight,
            decimal? afterWeight,
            decimal? testDensity,     // 试样密度均值 (g/cm³)
            decimal? m1Const,
            decimal? m2Const)
        {
            if (!beforeWeight.HasValue || !afterWeight.HasValue) return null;
            if (!testDensity.HasValue || testDensity.Value == 0) return null;
            if (!m1Const.HasValue || !m2Const.HasValue) return null;

            var deltaMt = beforeWeight.Value - afterWeight.Value;  // W1-W2 (g)
            if (deltaMt <= 0) return null;

            var deltaMtMg = deltaMt * 1000m;  // 转为 mg
            var deltaMr = (m1Const.Value + m2Const.Value) / 2m;   // (M1+M2)/2
            const decimal rhoR = 1.15m;  // 参考橡胶密度 (g/cm³)

            if (deltaMtMg <= 0 || rhoR <= 0) return null;

            return (deltaMr * testDensity.Value) / (deltaMtMg * rhoR) * 100m;
        }

        /// <summary>
        /// 构建体积损失公式 - 显示密度单位为 g/cm³，但计算时自动换算
        /// </summary>
        private static string BuildVolLossFormula(
            decimal? beforeWeight,
            decimal? afterWeight,
            decimal? density,      // g/cm³ 显示值
            decimal? m1Const,
            decimal? m2Const,
            decimal distanceFactor,
            decimal? result)
        {
            if (!beforeWeight.HasValue || !afterWeight.HasValue || !density.HasValue || !m1Const.HasValue || !m2Const.HasValue)
                return "";

            var massLoss = beforeWeight.Value - afterWeight.Value;
            if (massLoss <= 0) return "质量损失为0或负值";

            // 显示密度时标注 g/cm³，但说明已换算为 g/m³
            // 1000 × (W1-W2) × 0.4 / [ρ(g/cm³)×1000 × (M1+M2)] × 系数
            string formula = $"1000 × ({beforeWeight.Value:F4} - {afterWeight.Value:F4}) × 400mg / [{density.Value:F2} × ({m1Const.Value:F4} + {m2Const.Value:F4})]";

            if (distanceFactor != 1m)
            {
                formula += $" × {distanceFactor:F2}";
            }

            return formula;
        }

        /// <summary>
        /// 构建磨耗指数公式: I_AR = (Δm_r × ρ_t) / (Δm_t × ρ_r) × 100
        /// Δm_r = (M1+M2)/2, ρ_r = 1.15 g/cm³
        /// </summary>
        private static string BuildARIndexFormula(
            decimal? beforeWeight,
            decimal? afterWeight,
            decimal? massLoss,
            decimal? testDensity,
            decimal? m1Const,
            decimal? m2Const,
            decimal? result)
        {
            if (!beforeWeight.HasValue || !afterWeight.HasValue || !testDensity.HasValue)
                return "";

            if (!massLoss.HasValue || massLoss.Value <= 0) return "质量损失为0或负值";
            if (!m1Const.HasValue || !m2Const.HasValue) return "常量数据不足";

            var deltaMtMg = massLoss.Value * 1000m;                    // Δm_t (mg)
            var deltaMr = (m1Const.Value + m2Const.Value) / 2m;        // Δm_r = (M1+M2)/2
            const decimal rhoR = 1.15m;                                // ρ_r = 1.15 g/cm³

            // 构建公式: ((M1+M2)/2 × ρ_t) / (Δm_t(mg) × 1.15) × 100
            string formula = $"(({m1Const.Value:F4} + {m2Const.Value:F4}) / 2 × {testDensity.Value:F2}) / ({massLoss.Value:F4} × 1000 × 1.15) × 100";

            return formula;
        }
        /// <summary>
        /// 构建密度公式 - 单位 g/cm³
        /// </summary>
        private static string BuildDensityFormula(decimal? m1, decimal? m2, decimal? result)
        {
            if (!m1.HasValue || !m2.HasValue)
                return "";

            if (m1.Value - m2.Value == 0)
                return "分母为零，无法计算";

            // 公式: m₁ / (m₁ - m₂)  单位: g/cm³
            string formula = $"{m1.Value:F4} × 1.0g/cm³ / ({m1.Value:F4} - {m2.Value:F4})";

            return formula;
        }

        private static decimal? Average(decimal? a, decimal? b)
        {
            if (!a.HasValue && !b.HasValue) return null;
            if (!a.HasValue) return b;
            if (!b.HasValue) return a;
            return (a.Value + b.Value) / 2m;
        }

        /// <summary>
        /// 获取结论
        /// </summary>
        private static string GetConclusion(List<SpecimenResult> results, string requirement)
        {
            // 判断所有试样的体积损失是否 <= 要求值 (体积损失越小，耐磨性越好)
            if (string.IsNullOrWhiteSpace(requirement)) return "N/A";
            if (!decimal.TryParse(requirement.TrimEnd('%'), out decimal req)) return "N/A";

            // 只获取有体积损失值的试样进行判断，没有值的跳过
            var validResults = results.Where(r => r.VolLoss.HasValue).ToList();

            // 如果没有有效的试样数据，返回 N/A
            if (validResults.Count == 0) return "N/A";

            // 所有有效试样的体积损失都必须 <= 要求值
            var allPass = validResults.All(r => r.VolLoss.Value <= req);

            return allPass ? "PASS" : "FAIL";
        }

        /// <summary>
        /// 磨耗试样结果类 - 每个试样独立包含原始数据和计算过程
        /// </summary>
        private class SpecimenResult
        {
            // ==================== 原始数据 ====================
            public int SpecimenNumber { get; set; }
            public decimal? BeforeWeight { get; set; }      // W1
            public decimal? AfterWeight { get; set; }       // W2
            public decimal? MassLoss { get; set; }          // Δm = W1 - W2

            // ==================== 密度相关 ====================
            public decimal? TestDensity { get; set; }       // ρ_t (共用测试样品密度)
            public decimal? RefDensity { get; set; }        // ρ_r (共用参照化合物密度)
            public decimal? M1Const { get; set; }           // M1常量
            public decimal? M2Const { get; set; }           // M2常量

            // ==================== 计算结果 ====================
            public decimal? VolLoss { get; set; }           // 体积损失
            public decimal? ARIndex { get; set; }           // 磨耗指数

            // ==================== 公式文本（每个试样独立，变量替换为实际数值） ====================
            public string VolLossFormula { get; set; } = string.Empty;
            public string ARIndexFormula { get; set; } = string.Empty;
            public string DensityFormula { get; set; } = string.Empty;
        }
    }
}
