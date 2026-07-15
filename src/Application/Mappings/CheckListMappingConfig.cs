using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class CheckListMappingConfig : IRegister
    {

        public void Register(TypeAdapterConfig config)
        {
            //dto=>entity
            config.NewConfig<AddCheckListDto, CheckList>()
                .MapWith(src => CheckList.Create(
                    src.SourceId.Select(i => i.Adapt<OrderId>()).ToList(),
                     src.Items.Select(i => i.Adapt<CheckListItem>()).ToList(),
                    src.Remark
                    ));
        }
    }
}
