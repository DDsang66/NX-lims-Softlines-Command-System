using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext
{
    /// <summary>
    /// 可安全序列化到前端的 ParamSet DTO。
    /// 使用 JsonElement 保证任意值都能被 JSON 表示。
    /// </summary>
    public record ParamSetDto
    {
        public Dictionary<string, JsonElement> Values { get; init; } = new();
    }
}
