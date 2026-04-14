using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
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
        /// 获取工作表
        /// </summary>
        public async Task<FiberWorksheetDto?> GetByReportNumberAsync(string reportNumber)
        {
            var worksheet = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (worksheet == null) return null;

            return MapToDto(worksheet);
        }

        /// <summary>
        /// 保存工作表
        /// </summary>
        public async Task<FiberWorksheetDto> SaveAsync(FiberWorksheetCreateDto dto)
        {
            var existing = await _worksheetRepo.GetByReportNumberAsync(dto.ReportNumber);

            if (existing != null)
            {
                // 更新现有工作表
                UpdateEntityFromDto(existing, dto);
                var updated = await _worksheetRepo.UpdateAsync(existing);
                return MapToDto(updated);
            }
            else
            {
                // 创建新工作表
                var entity = MapToEntity(dto);
                var created = await _worksheetRepo.AddAsync(entity);
                return MapToDto(created);
            }
        }

        /// <summary>
        /// 删除工作表
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _worksheetRepo.DeleteAsync(id);
        }

        /// <summary>
        /// 计算并保存结果
        /// </summary>
        public async Task<FiberWorksheetDto> CalculateAndSaveAsync(FiberWorksheetCreateDto dto)
        {
            // 先保存工作表
            var savedDto = await SaveAsync(dto);

            // 执行计算
            var calcRequest = new FiberCalculationRequestDto
            {
                Standard = "ISO", // 可从 dto 获取
                Items = dto.Details.Select(d => new FiberCalculationItemDto
                {
                    Composition = d.Composition ?? string.Empty,
                    Trial1 = d.Trial1,
                    Trial2 = d.Trial2,
                    HeaderTrial1 = d.HeaderTrial1,
                    HeaderTrial2 = d.HeaderTrial2
                }).ToList()
            };

            var calcResult = await _calcService.CalculateAsync(calcRequest);

            // 更新推荐标签
            if (dto.Result != null)
            {
                dto.Result.RecommendedLabel = calcResult.RecommendedLabel;
            }

            // 再次保存
            return await SaveAsync(dto);
        }

        #region Mapping

        private FiberWorksheetDto MapToDto(FiberWorksheet entity)
        {
            return new FiberWorksheetDto
            {
                Id = entity.Id,
                ReportNumber = entity.ReportNumber,
                ComponentType = entity.ComponentType,
                TestMethod = entity.TestMethod,
                Buyer = entity.Buyer,
                Status = entity.Status,
                Technician = entity.Technician,
                Reviewer = entity.Reviewer,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Details = entity.Details?.Select(d => new FiberWorksheetDetailDto
                {
                    Id = d.Id,
                    SectionIndex = d.SectionIndex,
                    Composition = d.Composition,
                    Trial1 = d.Trial1,
                    Trial2 = d.Trial2,
                    HeaderTrial1 = d.HeaderTrial1,
                    HeaderTrial2 = d.HeaderTrial2,
                    CalculatedPercent = d.CalculatedPercent
                }).ToList() ?? new List<FiberWorksheetDetailDto>(),
                Result = entity.Result != null ? new FiberWorksheetResultDto
                {
                    Id = entity.Result.Id,
                    VerifyResult = entity.Result.VerifyResult,
                    FinalResult = entity.Result.FinalResult,
                    DurabilityLabel = entity.Result.DurabilityLabel,
                    OtherLabel = entity.Result.OtherLabel,
                    Comprehensive = entity.Result.Comprehensive,
                    RecommendedLabel = entity.Result.RecommendedLabel,
                    ResultRemark = entity.Result.ResultRemark,
                    LabelRemark = entity.Result.LabelRemark,
                    JudgmentLabelRemark = entity.Result.JudgmentLabelRemark,
                    LanguageLabelRemark = entity.Result.LanguageLabelRemark
                } : null
            };
        }

        private FiberWorksheet MapToEntity(FiberWorksheetCreateDto dto)
        {
            var entity = new FiberWorksheet
            {
                Id = Guid.NewGuid(),
                ReportNumber = dto.ReportNumber,
                ComponentType = dto.ComponentType,
                TestMethod = dto.TestMethod,
                Buyer = dto.Buyer,
                Status = "Draft",
                Technician = dto.Technician
            };

            foreach (var detailDto in dto.Details)
            {
                entity.Details.Add(new FiberWorksheetDetail
                {
                    Id = Guid.NewGuid(),
                    WorksheetId = entity.Id,
                    SectionIndex = detailDto.SectionIndex,
                    Composition = detailDto.Composition,
                    Trial1 = detailDto.Trial1,
                    Trial2 = detailDto.Trial2,
                    HeaderTrial1 = detailDto.HeaderTrial1,
                    HeaderTrial2 = detailDto.HeaderTrial2,
                    CalculatedPercent = detailDto.CalculatedPercent
                });
            }

            if (dto.Result != null)
            {
                entity.Result = new FiberWorksheetResult
                {
                    Id = Guid.NewGuid(),
                    WorksheetId = entity.Id,
                    VerifyResult = dto.Result.VerifyResult,
                    FinalResult = dto.Result.FinalResult,
                    DurabilityLabel = dto.Result.DurabilityLabel,
                    OtherLabel = dto.Result.OtherLabel,
                    Comprehensive = dto.Result.Comprehensive,
                    RecommendedLabel = dto.Result.RecommendedLabel,
                    ResultRemark = dto.Result.ResultRemark,
                    LabelRemark = dto.Result.LabelRemark,
                    JudgmentLabelRemark = dto.Result.JudgmentLabelRemark,
                    LanguageLabelRemark = dto.Result.LanguageLabelRemark
                };
            }

            return entity;
        }

        private void UpdateEntityFromDto(FiberWorksheet entity, FiberWorksheetCreateDto dto)
        {
            entity.ComponentType = dto.ComponentType;
            entity.TestMethod = dto.TestMethod;
            entity.Buyer = dto.Buyer;
            entity.Technician = dto.Technician;

            // 更新明细
            entity.Details.Clear();
            foreach (var detailDto in dto.Details)
            {
                entity.Details.Add(new FiberWorksheetDetail
                {
                    Id = Guid.NewGuid(),
                    WorksheetId = entity.Id,
                    SectionIndex = detailDto.SectionIndex,
                    Composition = detailDto.Composition,
                    Trial1 = detailDto.Trial1,
                    Trial2 = detailDto.Trial2,
                    HeaderTrial1 = detailDto.HeaderTrial1,
                    HeaderTrial2 = detailDto.HeaderTrial2,
                    CalculatedPercent = detailDto.CalculatedPercent
                });
            }

            // 更新结果
            if (dto.Result != null)
            {
                if (entity.Result == null)
                {
                    entity.Result = new FiberWorksheetResult { WorksheetId = entity.Id };
                }

                entity.Result.VerifyResult = dto.Result.VerifyResult;
                entity.Result.FinalResult = dto.Result.FinalResult;
                entity.Result.DurabilityLabel = dto.Result.DurabilityLabel;
                entity.Result.OtherLabel = dto.Result.OtherLabel;
                entity.Result.Comprehensive = dto.Result.Comprehensive;
                entity.Result.RecommendedLabel = dto.Result.RecommendedLabel;
                entity.Result.ResultRemark = dto.Result.ResultRemark;
                entity.Result.LabelRemark = dto.Result.LabelRemark;
                entity.Result.JudgmentLabelRemark = dto.Result.JudgmentLabelRemark;
                entity.Result.LanguageLabelRemark = dto.Result.LanguageLabelRemark;
            }
        }

        #endregion
    }
}
