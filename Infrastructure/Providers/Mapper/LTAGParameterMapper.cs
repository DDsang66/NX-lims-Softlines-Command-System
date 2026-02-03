using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class LTAGParameterMapper
    {
        private static readonly Dictionary<string, Func<WetParameterAatcc, string, ParamDto>> Mappings = new()
        {
            ["CF to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, param),

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
