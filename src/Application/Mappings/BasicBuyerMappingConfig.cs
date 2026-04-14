using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class BasicBuyerMappingConfig: IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<BasicBuyer, BuyerListDto>()
                .Map(d => d.BuyerCode, s => s.BuyerCode)
                .Map(d => d.BuyerName, s => s.BuyerName);
        }
    }
}
