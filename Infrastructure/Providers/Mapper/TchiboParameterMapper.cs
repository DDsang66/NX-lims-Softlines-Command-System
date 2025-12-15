using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class TchiboParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p,param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, p.Iron),
            ["DS to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, p.Program, p.AfterWash, p.Detergent),
            ["DS to Dry-clean"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null,param),
            ["Air Permeability"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Absorbency"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Abrasion Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Snagging Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Water Resistance-Hydrostatic Pressure"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Recovery"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Slippage"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Sublimation in Storage"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, "48h"),
            ["CF to Hot Pressing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null,p.Iron),
            ["CF to Saliva"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Sweat"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Light"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Water"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Appearance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param)
        };

        public static ParamDto Map(string itemName, WetParameterIso p, string param = null)
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
