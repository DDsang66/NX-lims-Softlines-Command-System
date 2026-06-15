using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public record ParamResponseDto(
        string ItemName,
        string? Standard,
        List<SampleParam> Param
        );

    public class SampleParam 
    {
        [JsonConverter(typeof(StringOrArrayConverter))]
        public string? Sample { get; set; }
        public object? NormalParam { get; set; }//里应该是json格式
        public object? WetParam { get; set; }

    }
}
