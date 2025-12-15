using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class PrimarkParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        {
            ["Colour Fastness to Washing"]=(p,param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null,p.SpecialCareInstruction, null, null, null, null, null, param),
            ["Absorbency of Textiles"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Colour Fastness to Hot Pressing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, p.Iron),
            ["Dimensional and Bra Wire Casing Stability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Martindale Pilling"]= (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, param),
            ["Print / Motif / Flock Durability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null,null, null, p.DryProcedure,null, null, null, p.AfterWash, null),
            ["Print Durability"]= (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, p.DryProcedure, null, null, null, p.AfterWash, null),
            ["Shower Resistant Claims Spray Rating"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Spirality"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Stability to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Waterproof Claims Hydrostatic Head"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Dimensional Stability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ["Stability to Dry Cleaning"] = (p, param) => new(p.ContactItem!, p.ReportNumber,null, null, null, null, null, null, null, p.Sensitive, null, p.AfterWash, null),
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Accelerotor"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Bursting Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Chlorinated Water"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Dry Cleaning"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Light"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Water"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Martindale Abrasion"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Nap Stability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Residual Elongation"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Residual Elongation SHAPEWEAR"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Tear Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Tensile Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Slippage"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Unrecovered Elongation"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Elastic Extension and Modulus Test"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Vertical Wicking of Textiles"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Back Pocket Application Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Belt Loop Application Strength"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Non Chlorine Bleach"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Colour Fastness to Chlorine Bleach"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
            ["Quick Dry"]= (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, param),
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
