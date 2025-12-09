using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.RenderRepos;

namespace NX_lims_Softlines_Command_System.Application.Services.UserService
{
    public class RenderService
    {
        private readonly RenderRepos _renderRepo;

        public RenderService(RenderRepos renderRepo)
        {
            _renderRepo = renderRepo;
        }
        public async Task<object?> SampleDesc(string buyername) 
        {
            if (string.IsNullOrWhiteSpace(buyername)) return null;
            var result  =await _renderRepo.RenderAsync(buyername.ToLower());
            return result;
        }
    }
}
