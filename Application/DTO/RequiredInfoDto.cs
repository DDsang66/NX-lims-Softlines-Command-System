using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class RequiredInfoDto
    {
        public string? buyer { get; set; }
        public string? reportNumber { get; set; }
        public string? reviewer { get; set; }
        public string? menuName { get; set; }
        public string? remark { get; set; }
        public string? extraItem { get; set; }
        public string? washingProcedure { get; set; }
        public string? dryProcedure { get; set; }
        public string? dcProcedure { get; set; }
        public string? sci { get; set; }
        public string? ironProcedure { get; set; }
        public string? ironMethod { get; set; }
        public string? bleachProcedure { get; set; }
        public string? detergent{ get; set; }
        public List<Items>? items { get; set; }
        public List<string>? afterWash { get; set; }
        public List<FiberDto>? fiberComposition { get; set; }
        public List<FiberInfoNew>? fiberCompositionSingle { get; set; }
        public string? additionalRequire { get; set; }
        public string? sampleDescription { get; set; }
        public List<SampleDescObject>? sampleDescripBoundSingle { get; set; }
        public List<SeamDescObject>? SeamParameter { get; set; }
    }
    public class SampleDescObject
    {
        [JsonConverter(typeof(StringOrArrayConverter))]
        public string? sample { get; set; }
        public List<DescObject>? description { get; set; }
    }
    public class DescObject
    {
        public string? propertyName { get; set; }
        public string? value { get; set; }
    }

    public class Items
    {
        public string? itemName { get; set; }
        public string? standards { get; set; }
        [JsonConverter(typeof(StringOrArrayConverter))]
        public string? samples { get; set; }
    }
    public class FiberDto
    {
        public string? Composition { get; set; }
        public int Rate { get; set; }
    }


    public class FiberInfoNew
    {
        [JsonConverter(typeof(StringOrArrayConverter))]
        public string? Sample { get; set; }
        public List<FiberDto>? Composition { get; set; }

    }
}
