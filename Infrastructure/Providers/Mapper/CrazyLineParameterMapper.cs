using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class CrazyLineParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterAatcc, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°F", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, null),
            ["DS to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, p.Iron),
            ["DS to Dry-clean"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Snagging Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Light"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Spirality/Skewing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, p.Iron),

        };

        public static ParamDto Map(string itemName, WetParameterAatcc p, string param = null)
        {
            if (Mappings.TryGetValue(itemName, out var mapping))
            {
                return mapping(p, param);
            }

            // 默认映射
            return new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null);
        }
    }
}
