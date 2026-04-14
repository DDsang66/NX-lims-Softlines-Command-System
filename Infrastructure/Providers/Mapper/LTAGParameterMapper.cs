using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class LTAGParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterAatcc, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, null),
            ["CF to Light"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Dye Transfer in Storage"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Abrasion Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Recovery"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, param),
            ["Water Repellency-Spray Test"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, param),
            ["Drying Rate of Fabrics"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Absorbency"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, param),
            ["Water Resistance-Rain Test"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Air Permeability"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, param),
            ["DS to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, p.Iron),
            ["Appearance"]= (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, p.Iron),
            ["DS to Dry-clean"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ["Spirality/Skewing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, p.Iron),
        };

        public static ParamDto Map(string itemName, WetParameterAatcc p, string param = null)
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
