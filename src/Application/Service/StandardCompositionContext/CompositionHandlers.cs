using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NX_lims_Softlines_Command_System.src.Application.Attributes;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardCompositionContext
{
    /// <summary>
    /// 标准成分计算处理器
    /// </summary>
    public class CompositionHandlers : ISingletonDependency
    {
        /// <summary>
        /// 计算合成纤维含量
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [FieldHandler("SyntheticFiberContent")]
        public object HandleSyntheticFiberContent(object input, Dictionary<string, object> context = null)
        {
            if (input is not Dictionary<string, object> dict)
                return 0;
            if (!dict.TryGetValue("composition", out var compositionObj))
                return 0;
            if (compositionObj is not System.Text.Json.JsonElement jsonElement)
                return 0;
            if (jsonElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return 0;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var compositions = System.Text.Json.JsonSerializer.Deserialize<List<CompositionCalculateDto>>(jsonElement.GetRawText(), options);

            return compositions?
                .Where(c => c.SecondaryClassificationEn == "Synthetic fibre")
                .Sum(c => c.Rate) ?? 0;
        }

        /// <summary>
        /// 计算纤维素纤维的含量
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [FieldHandler("CelluloseFiberContent")]
        public object HandleCelluloseFiberContent(object input, Dictionary<string, object> context = null)
        {
            if (input is not Dictionary<string, object> dict)
                return 0;
            if (!dict.TryGetValue("composition", out var compositionObj))
                return 0;
            if (compositionObj is not System.Text.Json.JsonElement jsonElement)
                return 0;
            if (jsonElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return 0;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var compositions = System.Text.Json.JsonSerializer.Deserialize<List<CompositionCalculateDto>>(jsonElement.GetRawText(), options);

            return compositions?
                    .Where(c => c.SecondaryClassificationEn == "Vegetable fibre" || c.TertiaryClassificationEn == "Regenerated cellulose fibre")
                    .Sum(c => c.Rate) ?? 0;

        }

        /// <summary>
        /// 计算最大成分类型
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [FieldHandler("MaxCompositionType")]
        public object HandleMaxCompositionType(object input, Dictionary<string, object> context = null)
        {
            if (input is not Dictionary<string, object> dict)
                return 0;
            if (!dict.TryGetValue("composition", out var compositionObj))
                return 0;
            if (compositionObj is not System.Text.Json.JsonElement jsonElement)
                return 0;
            if (jsonElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return 0;

            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            var compositions = System.Text.Json.JsonSerializer.Deserialize<List<CompositionCalculateDto>>(jsonElement.GetRawText(), options);

            var maxComposition = compositions.OrderByDescending(c => c.Rate).FirstOrDefault();

            return maxComposition?.SecondaryClassificationEn!;
        }
    }
}
