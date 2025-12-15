using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class NextParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        {
            ["Fastness to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, param),
            ["Cross Staining to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, param),
            ["Stability to Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Print Durability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Embellishment Durability (Childrenswear)"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Embellishment Durability (General)"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Foil Durability"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Appearance Assessment after Washing"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Spray Rating"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            ["Assessment of Easy to Iron Fabrics"] = (p, param) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, p.Sensitive, null, null, null),
            ["Appearance Assessment after Dry Clean"] = (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, param),
            ["Stability to Dry Cleaning"]= (p, param) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, param),
            ["Pilling Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Swiss Pilling"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Light"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Dry Cleaning"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Cross Staining to Dry Cleaning"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Water"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Cross Staining to Water"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Chlorinated Water"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Grab Strength & Seam Slippage"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Seam Slippage of Garment Seams"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Tear Strength"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Martindale Abrasion"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Abrasion Home"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Bursting Strength"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Recovery"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Extension and Modulus"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Saliva"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Sea Water"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Fastness to Perspiration"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Air Permeability of Textile Fabrics"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Snagging Resistance"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Hydrostatic Head Test"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Moisture Management"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
            ["Accelerotor Pile Loss"] = (p, param) => new(p.ContactItem!, null, null, null, null, null, null, null, null, null, null, null, param),
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
