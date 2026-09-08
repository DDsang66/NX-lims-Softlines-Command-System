using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Queries;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardCompositionContext
{
    public class CompositionQueryService:IScopedDependency
    {
        private readonly FiberCompositionQuery _fiberCompositionQuery;

        public CompositionQueryService(FiberCompositionQuery fiberCompositionQuery)
        {
            _fiberCompositionQuery = fiberCompositionQuery;
        }
        /// <summary>
        /// 获取纤维成分集合
        /// </summary>
        /// <returns></returns>
        public async Task<List<CompositionResponseDto>> GetFiberCompositionsAsync()
        {
            var compositions = await _fiberCompositionQuery.GetFiberCompositionsAsync();

            return compositions;
        }
    }
}
