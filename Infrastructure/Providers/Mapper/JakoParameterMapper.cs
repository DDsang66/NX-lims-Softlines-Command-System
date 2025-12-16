using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class JakoParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, null, null, null, null, null),
            ["CF to Hot Pressing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, null),
            ["Appearance"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, p.Iron),
            ["DS to Dry-clean"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Print Durability For JAKO"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, null, null, p.DryProcedure, null, null, null, null, null),
            ["Heat Press Test For JAKO"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, null, null, null, null, null, null, null, null),
            ["Snagging Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Abrasion Resistance"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Light"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Slippage"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Bursting Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Tensile Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Recovery"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Air Permeability"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Water Repellency-Spray Test"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["Spirality/Skewing"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
            ["CF to Sublimation in Storage"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, "48h"),
            ["CF to Chlorinated Water"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
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
