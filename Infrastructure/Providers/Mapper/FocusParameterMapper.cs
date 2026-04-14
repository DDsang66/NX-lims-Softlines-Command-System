using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class FocusParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, param),
            ["DS to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, param),
            ["Appearance"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, param),
            ["DS to Dry-clean"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Abrasion Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Water Resistance-Hydrostatic Pressure"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Light"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Slippage"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Tear Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Tensile Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Recovery"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Attachment Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Water Repellency-Spray Test"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Air Permeability"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Quick Dry"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Zipper Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Dimensional Stability to Ironing"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        };

        public static ParamDto Map(string itemName, WetParameterIso p, string param = null)
        {
            if (Mappings.TryGetValue(itemName, out var mapping))
            {
                return mapping(p, param);
            }

            // 默认映射
            return new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, null);
        }
    }
}
