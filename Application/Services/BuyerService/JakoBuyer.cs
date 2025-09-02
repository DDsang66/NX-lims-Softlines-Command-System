using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Infrastructure.Services;

namespace NX_lims_Softlines_Command_System.Application.Services.BuyerService
{
    public class JakoBuyer : IBuyer
    {
        private readonly JakoService _service;

        public JakoBuyer(JakoService service)
        {
            _service = service;
        }

        public async Task<object?> ShowItem([FromBody] RequiredInfoDto infoDto)
        {
            try
            {
                var groupedCheckLists = await _service.ShowItemAsync(infoDto);
                return groupedCheckLists;
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Console.WriteLine($"Error in ShowItem: {ex.Message}");
                // 返回一个空列表或抛出自定义异常
                return null;
            }
        }
        public Task<object?> ShowParameter([FromBody] RequiredInfoDto infoDto)
        {
            return _service.ShowParameterAsync(infoDto);
        }
    }
}
