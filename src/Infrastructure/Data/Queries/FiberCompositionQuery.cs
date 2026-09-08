using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Queries
{
    public class FiberCompositionQuery: IScopedDependency
    {
        private readonly dbContext _dbContext;
        public FiberCompositionQuery(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取纤维成分集合
        /// </summary>
        /// <returns></returns>
        public async Task<List<CompositionResponseDto>> GetFiberCompositionsAsync()
        {
            var pos = await _dbContext.Compositions.ToListAsync();

            var compositions = pos.Adapt<List<CompositionResponseDto>>();

            return compositions;
        }
    }
}
