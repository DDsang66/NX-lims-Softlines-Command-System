using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly IFiberDatabaseRepository _fiberRepo;
        private readonly FiberCalculationService _calcService;

        public FiberWorksheetService(
            IFiberWorksheetRepository worksheetRepo,
            IFiberDatabaseRepository fiberRepo,
            FiberCalculationService calcService)
        {
            _worksheetRepo = worksheetRepo;
            _fiberRepo = fiberRepo;
            _calcService = calcService;
        }

        /// <summary>
        /// 构建成分分析报告服务
        /// </summary>
        /// <returns></returns>
        public async Task<Result> BuildAnalysisAsync(BuildAnalysisDto dto) 
        {
            //数据验证
            
            //执行计算

            //执行生成word

            //执行保存

            return Result.Ok();
        }


        /// <summary>
        /// 成分逻辑计算服务
        /// </summary>
        /// <returns></returns>
        public async Task<Result> CalculateRemarkAsync(BuildAnalysisDto dto)
        {

            return Result.Ok();
        }
    }
}
