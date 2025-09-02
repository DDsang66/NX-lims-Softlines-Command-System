using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Models;

namespace NX_lims_Softlines_Command_System.Domain
{
    public interface IRepository
    {
        Task<List<CheckListDto>?> GetCheckListAsync(dynamic input);
        Task<T?> GetOrCreateWetParamsAsync<T>(ParamsInput input, string itemName) where T : IWetParam, new();
    }
}
