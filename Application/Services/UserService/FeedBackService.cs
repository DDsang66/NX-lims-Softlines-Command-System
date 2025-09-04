using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Services;

namespace NX_lims_Softlines_Command_System.Application.Services.UserService
{
    public class FeedBackService
    {
        private readonly FeedBackRepo _repo;

        public FeedBackService(FeedBackRepo repo)
        {
            _repo = repo;
        }
        public async Task<string?> Post([FromBody] FeedBackDto infoDto)
        {
            try
            {
                string? res = await _repo.Post(infoDto);
                return res;
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Console.WriteLine($"Error in ShowItem: {ex.Message}");
                // 返回一个空列表或抛出自定义异常
                return null;
            }
        }
        public async Task<object?> Get()
        {
            try 
            {
                var feedback = await _repo.Get();
                return feedback;
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Console.WriteLine($"Error in ShowItem: {ex.Message}");
                return null;
            }
        }
    }
}
