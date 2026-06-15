using Mapster;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class BuildAnalysisMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // DTO -> Entity (保存到数据库)
            config.NewConfig<BuildAnalysisDto, FiberAnalysis>()
                .Map(dest => dest.ReportNumber, src => src.ReportNumber)
                .Map(dest => dest.Method, src => string.Join(",", src.Method))  // List<string> -> 逗号分隔字符串
                .Map(dest => dest.Buyer, src => src.Buyer)
                .Map(dest => dest.Type, src => GetAnalysisType(src))  // 根据数据判断类型：0=单组分, 1=多组分
                .Map(dest => dest.FiberAnalysis1, src => SerializeBuildAnalysisToJson(src))  // JSON 存 text 字段
                .Map(dest => dest.Remark, src => SerializeRemarksToJson(src));  // 其他备注字段 JSON 序列化

            // Entity -> DTO (从数据库读取)
            config.NewConfig<FiberAnalysis, BuildAnalysisDto>()
                .Map(dest => dest.ReportNumber, src => src.ReportNumber ?? string.Empty)
                .Map(dest => dest.Method, src => ParseMethods(src.Method))
                .Map(dest => dest.Buyer, src => src.Buyer ?? string.Empty)
                .Map(dest => dest.MultipleBuildAnalysis, src => DeserializeMultipleAnalysis(src.FiberAnalysis1))
                .Map(dest => dest.SingleBuildAnalysis, src => DeserializeSingleAnalysis(src.FiberAnalysis1))
                .Map(dest => dest.RecommendedLabel, src => DeserializeRecommendedLabel(src.Remark))
                .Map(dest => dest.ResultRemark, src => GetRemarkValue(src.Remark, "ResultRemark"))
                .Map(dest => dest.LabelRemark, src => GetRemarkValue(src.Remark, "LabelRemark"))
                .Map(dest => dest.JudgmentLabelRemark, src => GetRemarkValue(src.Remark, "JudgmentLabelRemark"))
                .Map(dest => dest.LanguageLabelRemark, src => GetRemarkValue(src.Remark, "LanguageLabelRemark"))
                .Map(dest => dest.DurabilityLabel, src => GetRemarkValue(src.Remark, "DurabilityLabel"))
                .Map(dest => dest.OtherLabel, src => GetRemarkValue(src.Remark, "OtherLabel"))
                .Map(dest => dest.Comprehensive, src => GetRemarkValue(src.Remark, "Comprehensive"))
                .Map(dest => dest.VerifyResult, src => GetRemarkValue(src.Remark, "VerifyResult"))
                .Map(dest => dest.FinalResult, src => GetRemarkValue(src.Remark, "FinalResult"));
        }

        // ========== 序列化方法 ==========

        /// <summary>
        /// 将 MultipleBuildAnalysis 和 SingleBuildAnalysis 序列化为 JSON 存入 FiberAnalysis1(text)
        /// </summary>
        private string SerializeBuildAnalysisToJson(BuildAnalysisDto dto)
        {
            if (dto == null) return string.Empty;

            // 判断是单组分还是多组分，只序列化有数据的那部分
            var hasMultiple = dto.MultipleBuildAnalysis?.fiberSplittingList?.Any() == true
                           || dto.MultipleBuildAnalysis?.fiberDissolvedList?.Any() == true;
            var hasSingle = dto.SingleBuildAnalysis?.SingleFiberRows?.Any() == true;

            var data = new
            {
                Type = hasMultiple ? "Multiple" : (hasSingle ? "Single" : "None"),
                Multiple = hasMultiple ? dto.MultipleBuildAnalysis : null,
                Single = hasSingle ? dto.SingleBuildAnalysis : null
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false  // text 类型节省空间，不格式化
            });
        }

        /// <summary>
        /// 将 Remark 相关字段序列化为 JSON 存入 Remark(text)
        /// </summary>
        private string SerializeRemarksToJson(BuildAnalysisDto dto)
        {
            if (dto == null) return string.Empty;

            var remarks = new
            {
                dto.RecommendedLabel,
                dto.ResultRemark,
                dto.LabelRemark,
                dto.JudgmentLabelRemark,
                dto.LanguageLabelRemark,
                dto.DurabilityLabel,
                dto.OtherLabel,
                dto.Comprehensive,
                dto.VerifyResult,
                dto.FinalResult
            };

            return JsonSerializer.Serialize(remarks, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        // ========== 反序列化方法 ==========

        /// <summary>
        /// 从 FiberAnalysis1 反序列化 MultipleAnalysis
        /// </summary>
        private MultipleAnalysis DeserializeMultipleAnalysis(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new MultipleAnalysis();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 检查类型标记
                if (root.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "Single")
                {
                    return new MultipleAnalysis();  // 单组分数据，返回空多组分
                }

                if (root.TryGetProperty("multiple", out var multipleProp))
                {
                    return JsonSerializer.Deserialize<MultipleAnalysis>(
                        multipleProp.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new MultipleAnalysis();
                }

                return new MultipleAnalysis();
            }
            catch
            {
                return new MultipleAnalysis();
            }
        }

        /// <summary>
        /// 从 FiberAnalysis1 反序列化 SingleAnalysis
        /// </summary>
        private SingleAnalysis DeserializeSingleAnalysis(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new SingleAnalysis();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 检查类型标记
                if (root.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "Multiple")
                {
                    return new SingleAnalysis();  // 多组分数据，返回空单组分
                }

                if (root.TryGetProperty("single", out var singleProp))
                {
                    return JsonSerializer.Deserialize<SingleAnalysis>(
                        singleProp.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new SingleAnalysis();
                }

                return new SingleAnalysis();
            }
            catch
            {
                return new SingleAnalysis();
            }
        }

        /// <summary>
        /// 从 Remark JSON 反序列化 RecommendedLabel
        /// </summary>
        private List<string> DeserializeRecommendedLabel(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("recommendedLabel", out var prop))
                {
                    return JsonSerializer.Deserialize<List<string>>(prop.GetRawText()) ?? new List<string>();
                }
            }
            catch { }

            return new List<string>();
        }

        /// <summary>
        /// 从 Remark JSON 获取指定字段值
        /// </summary>
        private string GetRemarkValue(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var camelCaseName = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

                if (doc.RootElement.TryGetProperty(camelCaseName, out var prop))
                {
                    return prop.GetString() ?? string.Empty;
                }
            }
            catch { }

            return string.Empty;
        }

        // ========== 辅助方法 ==========

        /// <summary>
        /// 判断分析类型：0=单组分, 1=多组分
        /// </summary>
        private byte? GetAnalysisType(BuildAnalysisDto dto)
        {
            var hasMultiple = dto.MultipleBuildAnalysis?.fiberSplittingList?.Any() == true
                           || dto.MultipleBuildAnalysis?.fiberDissolvedList?.Any() == true;

            if (hasMultiple) return 1;

            var hasSingle = dto.SingleBuildAnalysis?.SingleFiberRows?.Any() == true;
            if (hasSingle) return 0;

            return null;
        }

        /// <summary>
        /// 将逗号分隔的方法字符串解析为 List
        /// </summary>
        private List<string> ParseMethods(string method)
        {
            if (string.IsNullOrWhiteSpace(method)) return new List<string>();
            return method.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .ToList();
        }
    }
}

