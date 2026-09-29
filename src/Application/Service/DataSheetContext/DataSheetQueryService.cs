using Mapster;
using Microsoft.AspNetCore.Authentication;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetQueryService:IScopedDependency
    {
        private readonly IDataSheetRepository _dataSheetRepository;

        public DataSheetQueryService(IDataSheetRepository dataSheetRepository) 
        {
            _dataSheetRepository = dataSheetRepository;
        }

        /// <summary>
        /// 根据CheckListId获取DataSheet
        /// </summary>
        /// <param name="checklistId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<DataSheetResponseDto>>> GetDataSheetByChecklistId(Guid checklistId, CancellationToken ct)
        {
            try
            {
                var dataSheetList = await _dataSheetRepository.GetByCheckListIdAsync(new CheckListId(checklistId), ct);

                var dto = dataSheetList.Adapt<List<DataSheetResponseDto>>();

                return Result<List<DataSheetResponseDto>>.Ok(dto);
            }
            catch (Exception ex) 
            {
                return Result<List<DataSheetResponseDto>>.Fail(ex.Message);
            }
        }
    }
}
 