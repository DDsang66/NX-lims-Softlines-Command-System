using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetService:IScopedDependency
    {
        private readonly ICheckListRepository _checkListRepository;
        private readonly IConditionPoolRepository _conditionPoolRepository;
        private readonly IDataSheetModelGenerator _dataSheetModelGenerator;
        private readonly DataSheetFillingEngine _dataSheetFillingEngine;
        private readonly IDataSheetBatchRepository _dataSheetBatchRepository;
        private readonly IDataSheetRepository _dataSheetRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DataSheetService(
            IUnitOfWork unitOfWork,
            ICheckListRepository checkListRepository,
            IConditionPoolRepository conditionPoolRepository,
            DataSheetFillingEngine dataSheetFillingEngine,
            IDataSheetRepository dataSheetRepository,
            IDataSheetModelGenerator dataSheetModelGenerator,
            IDataSheetBatchRepository dataSheetBatchRepository)
        {
            _unitOfWork = unitOfWork;
            _checkListRepository = checkListRepository;
            _conditionPoolRepository = conditionPoolRepository;
            _dataSheetFillingEngine = dataSheetFillingEngine;
            _dataSheetRepository = dataSheetRepository;
            _dataSheetModelGenerator = dataSheetModelGenerator;
            _dataSheetBatchRepository = dataSheetBatchRepository;
        }

        /// <summary>
        /// 彻底跑通新代码前先保留
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Result> GenerateTaskStart1(DataSheetGenerateDto dto,CancellationToken ct) 
        {
            //拿到checklistid

            var checklist = await  _checkListRepository.GetByIdAsync(new CheckListId(dto.CheckListId),ct);

            if (checklist.Status != CheckListStatus.InProgress)
                throw new ArgumentException("CheckList is not completed yet");

            var pools =  await  _conditionPoolRepository.GetByCheckListIdAsync(new CheckListId(dto.CheckListId),ct);

            //幂等验证，如果已经有checklist的批次正在生成或者说该批次的ds已经生成过了，就不再重复生成;

            foreach (var item in checklist.Items) 
            {
                //按照每个项目创建测试ds任务

                //模板查询器 (领域服务) 查询到对应模板
                var contactTemplateUrl = "DocxModel/Common_WET/WET_Dimensional_Change_Wasing.docx";

                //var ds = DataSheet.Create(
                //    checklist.Id,
                //    null,
                //    contactTemplateUrl,
                //    DataSheetStatus.Pending,
                //    checklist.OderId!,
                //    item.TestItemId!);

                // await _dataSheetRepository.AddAsync(ds,ct);
            }

            //暂时：手动触发processHub监听，实时将生成进度推送到前端

            await _unitOfWork.SaveChangesAsync(ct);

            //以下代码交由eventHandler处理，现在为了验证流程可行性先显示写在这里

            foreach (var item in checklist.Items) 
            {
                //开始调用DataSheetGenerator生成model，在应用层进行用例编排
                var models = await _dataSheetModelGenerator.GenerateAsync(pools.ToList(), item,ct);

                //调用模板引擎将model数据填充进入到模板中，生成最终的ds文件
               _dataSheetFillingEngine.FillDataSheet(models.First());
            }

            return Result.Ok();
        }

        /// <summary>
        /// 生成任务启动
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Result<GenerateDataSheetTaskResult>> GenerateTaskStart(DataSheetGenerateDto dto, CancellationToken ct)
        {
            var checkListId = new CheckListId(dto.CheckListId);

            // 1. 校验 checklist
            var checklist = await _checkListRepository.GetByIdAsync(checkListId, ct)
                ?? throw new ArgumentException("CheckList not found");

            if (checklist.Status != CheckListStatus.InProgress)
                throw new ArgumentException("CheckList is not completed yet");

            // 2. 幂等：是否已有未失败的批次
            if (!dto.ForceRegenerate)
            {
                var existingBatch = await _dataSheetBatchRepository
                    .GetActiveByCheckListIdAsync(checkListId, ct);
                if (existingBatch != null)
                {
                    return Result<GenerateDataSheetTaskResult>.Ok(new GenerateDataSheetTaskResult(
                        existingBatch.Id.Value,
                        checklist.Id.Value,
                        existingBatch.Total));
                }
            }

            // 3. 模板查询（领域服务）—— 先固定，后续替换成真正的查询
            var contactTemplateUrl = "DocxModel/Common_WET/WET_Dimensional_Change_Wasing.docx";

            // 4. 创建批次
            var batch = DataSheetBatch.Create(
                checkListId,
                checklist.OderId!,
                checklist.Items.Count,
                contactTemplateUrl);

            await _dataSheetBatchRepository.AddAsync(batch, ct);

            // 5. 按项目拆 N 个初始占位 PENDING datasheet
            foreach (var item in checklist.Items)
            {
                var ds = DataSheet.Create(
                    checklist.Id,
                    null,
                    contactTemplateUrl,
                    DataSheetStatus.Pending,
                    checklist.OderId!,
                    item.TestItemId!,
                    batch.Id,
                    0);

                await _dataSheetRepository.AddAsync(ds, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // ★ 不再在这里生成、不再推送
            return Result<GenerateDataSheetTaskResult>.Ok(new GenerateDataSheetTaskResult(
                batch.Id.Value,
                checklist.Id.Value,
                checklist.Items.Count));
        }
    }
}
