using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Service;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;
using System.Reflection.Emit;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service.CheckListContext
{
    public class CheckListAppService:IScopedDependency,ICheckListAppService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICheckListRepository _checkListRepository;
        private readonly IFileStorageService _fileStorage;
        private readonly IConditionPoolRepository  _conditionPoolRepository;
        private readonly IParamGenerationUseCaseService _paramGenerationUseCaseService;
        private readonly CheckListAdapter _checkListAdapter;

        public CheckListAppService(
            IUnitOfWork unitOfWork, 
            ICheckListRepository checkListRepository,
            CheckListAdapter checkListAdapter,
            IFileStorageService fileStorage,
            IConditionPoolRepository conditionPoolRepository,
            IParamGenerationUseCaseService paramGenerationUseCaseService)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _checkListAdapter = checkListAdapter;
            _checkListRepository = checkListRepository;
            _conditionPoolRepository = conditionPoolRepository;
            _paramGenerationUseCaseService = paramGenerationUseCaseService;
        }

        /// <summary>
        /// 添加测试清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<Guid>> AddCheckList(AddCheckListDto dto,CancellationToken ct) 
        {
            try {
                var checkList = dto.Adapt<CheckList>();//已在Mapping调用工厂方法统一创建

                Console.WriteLine(checkList);

                await _checkListRepository.AddAsync(checkList, ct);

                await _unitOfWork.SaveChangesAsync(ct);

                return Result<Guid>.Ok(checkList.Id.Value);
            }
            catch(Exception ex) 
            {
                return Result<Guid>.Fail(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// 更新测试清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<Guid>> UpdateCheckList(UpdateCheckListDto dto, CancellationToken ct)
        {
            try 
            {
                var checkList = await _checkListRepository.GetByIdAsync(new CheckListId(dto.Id), ct);

                checkList.Update();

                await _checkListRepository.UpdateAsync(checkList, ct);

                await _unitOfWork.SaveChangesAsync(ct);
                 
                return Result<Guid>.Ok(checkList.Id.Value);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Fail(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// 获取测试清单
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<CheckListResponseDto>> GetCheckListAsync(Guid checkListId, CancellationToken ct)
        {
            var checkList = await _checkListRepository.GetByIdAsync(new CheckListId(checkListId), ct);
            if (checkList == null)
                return Result<CheckListResponseDto>.Fail("未能找到测试清单");

            var checkListDto = checkList.Adapt<CheckListResponseDto>();

            var items = new List<CheckListResponseItemDto>();

            foreach (var item in checkListDto.Items)
            {
                var newItem = item with { };  // 复制
                if (!string.IsNullOrWhiteSpace(newItem.Parameter))
                {
                    // 一次性处理提取和清理
                    var (cuttingMethod, cleanedParameter) = ProcessParameterCutting(newItem.Parameter);

                    if (!string.IsNullOrEmpty(cuttingMethod))
                    {
                        newItem.CuttingMethod = cuttingMethod;
                    }

                    // 无论是否提取到了 cutting，都将清理过（移除了指定字段）的 Parameter 赋回去
                    newItem.Parameter = cleanedParameter;
                }
                items.Add(newItem);
            }
            checkListDto.Items = items;

            return Result<CheckListResponseDto>.Ok(checkListDto);
        }

        /// <summary>
        /// 生成纸质版测试清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<DocxUrlResponseDto>> GenerateCheckListAsync(CheckListGenerateDto dto, CancellationToken ct) 
        {
            var checklist = await _checkListRepository.GetByIdAsync(new CheckListId(dto.CheckListId), ct);

            //根据checklistId生成barcode
            using var barcodeBitmap = BarcodeGenerator.GenerateBarcode(checklist.Id, 200, 40);

            //生成copy文件
            string docxFileName = $"{checklist.OderId}_{DateTime.Now:yyMMddHHmmss}_CheckList.docx";

            string targetDocxPath = _fileStorage.CopyTemplate(
                Path.Combine("DocxModel","Checklist.docx"),
                Path.Combine("DocxModel", "SaveDocx"),
                docxFileName);

            try 
            {
                //调用ChecklistAdapter
                await _checkListAdapter.FillCheckListAsync(targetDocxPath, dto, barcodeBitmap);

                // 2. 将填充好的 docx 转换为 pdf
                string pdfFileName = Path.ChangeExtension(docxFileName, ".pdf");
                string targetPdfPath = Path.ChangeExtension(targetDocxPath, ".pdf");

                PDFConverter.ConvertDocxToPdf(targetDocxPath, targetPdfPath);

                //生成成功后改变checklist状态为InProgress，保存至数据库
                checklist.ChangeInProcess();

                await _checkListRepository.UpdateAsync(checklist, ct);

                //返回生成的文件的Url，后续可能要把checklist的url保存进入数据库留档

                await _unitOfWork.SaveChangesAsync(ct);

                //return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto
                //{
                //    fileKey = fileName,
                //    fileName = fileName,
                //    downloadUrl = $"/api/Review/checklist-{fileName}/download"
                //});

                return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto
                {
                    fileKey = pdfFileName,
                    fileName = pdfFileName,
                    downloadUrl = $"/api/Review/checklist-{pdfFileName}/download"
                });
            }
            catch (Exception ex) 
            {
                return Result<DocxUrlResponseDto>.Fail(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// 计算参数
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> CalculateParamAsync(Guid id, CancellationToken ct) 
        {
            var checkListId = new CheckListId(id);

            var checkList = await _checkListRepository.GetByIdAsync(checkListId, ct);

            var checkListItems = checkList.GetTestItem(); // 通过聚合根获取内部实体
            if (checkListItems == null)
                return Result.Fail("未能找到测试项目");

            // 2. 获取与该检查项关联的所有条件池（假设已经分组完毕）
            var existingPools = await _conditionPoolRepository.GetByCheckListIdAsync(checkListId, ct);

            //对existingPools中的FiberCondition进行预拓展，如果没有Fiber Condition的Key可以直接跳过

            // 3. 为每个测试项生成参数
            foreach (var item in checkListItems)
            {
                // 创建新的参数字典
                var TestPointParams = new Dictionary<string, ParamSet?>();

                // 遍历每个测点
                foreach (var testPoint in item.Samples)
                {
                    // 找到该测点对应的条件池
                    var pool = existingPools.FirstOrDefault(p => p.TestPoints.Contains(testPoint));

                    // 使用单个条件池生成参数
                    var result = await _paramGenerationUseCaseService.GenerateForCheckListItemAsync( item, pool, ct);

                    if (!result.IsSuccess)
                        return Result.Fail($"生成测试项 {item.Id} 的测点 {testPoint} 参数时发生错误: {result.Error}");
                   
                    // 将生成的参数添加到新字典中
                    TestPointParams.Add(testPoint, result.Value);
                }

                // 更新测试项的参数
                item.TestPointParams = TestPointParams;
            }

            // 4. 保存更改
            checkList.Update();

            await _checkListRepository.UpdateAsync(checkList, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }


        /// <summary>
        /// 提取 CuttingMethod 并从 Parameter 中移除 SpecimenArea 和 SpecimenNum
        /// </summary>
        /// <param name="parameter">原始 Parameter JSON 字符串</param>
        /// <returns>Item1: CuttingMethod字符串 (可能为null), Item2: 清理后的 Parameter JSON 字符串</returns>
        private static (string? CuttingMethod, string? CleanedParameter) ProcessParameterCutting(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return (null, parameter);

            try
            {
                var root = JsonSerializer.Deserialize<JsonElement>(parameter);
                var parts = new List<string>();

                // 创建一个新的 Dictionary 来重建清理后的 JSON 结构
                var newRootDict = new Dictionary<string, JsonElement>();

                foreach (var tp in root.EnumerateObject())
                {
                    if (tp.Value.ValueKind != JsonValueKind.Object)
                    {
                        // 非 Object 节点直接保留
                        newRootDict[tp.Name] = tp.Value;
                        continue;
                    }

                    // 获取 values 对象
                    if (!tp.Value.TryGetProperty("values", out JsonElement values)
                        || values.ValueKind != JsonValueKind.Object)
                    {
                        // 没有 values 节点或 values 非 Object，直接保留原样
                        newRootDict[tp.Name] = tp.Value;
                        continue;
                    }

                    // --- 开始提取逻辑 ---
                    string? specimenArea = null;
                    if (values.TryGetProperty("SpecimenArea", out JsonElement area))
                    {
                        specimenArea = area.ValueKind == JsonValueKind.String
                            ? area.GetString()
                            : area.ToString();
                    }

                    string? specimenNum = null;
                    if (values.TryGetProperty("SpecimenNum", out JsonElement num))
                    {
                        specimenNum = num.ValueKind == JsonValueKind.String
                            ? num.GetString()
                            : num.ToString();
                    }

                    if (!string.IsNullOrEmpty(specimenArea) || !string.IsNullOrEmpty(specimenNum))
                    {
                        var groupParts = new List<string>();
                        if (!string.IsNullOrEmpty(specimenArea))
                            groupParts.Add($"SpecimenArea={specimenArea}");
                        if (!string.IsNullOrEmpty(specimenNum))
                            groupParts.Add($"SpecimenNum={specimenNum}");

                        parts.Add(string.Join(", ", groupParts));
                    }

                    // --- 开始移除逻辑 (重建 values 对象) ---
                    var newValuesDict = new Dictionary<string, JsonElement>();
                    foreach (var valProp in values.EnumerateObject())
                    {
                        // 过滤掉 SpecimenArea 和 SpecimenNum
                        if (valProp.NameEquals("SpecimenArea") || valProp.NameEquals("SpecimenNum"))
                        {
                            continue;
                        }
                        newValuesDict[valProp.Name] = valProp.Value;
                    }

                    // --- 重建 tp 对象 ---
                    var newTpDict = new Dictionary<string, JsonElement>();
                    foreach (var tpProp in tp.Value.EnumerateObject())
                    {
                        if (tpProp.NameEquals("values"))
                        {
                            // 将重建的 values 字典序列化为 JsonElement 放入
                            using var doc = JsonSerializer.SerializeToDocument(newValuesDict);
                            newTpDict["values"] = doc.RootElement.Clone();
                        }
                        else
                        {
                            newTpDict[tpProp.Name] = tpProp.Value;
                        }
                    }

                    // 将重建的 tp 放入新的根字典
                    using var tpDoc = JsonSerializer.SerializeToDocument(newTpDict);
                    newRootDict[tp.Name] = tpDoc.RootElement.Clone();
                }

                // 重建最终的 JSON 字符串
                var cleanedParameter = JsonSerializer.Serialize(newRootDict);
                var cuttingMethod = parts.Count > 0 ? string.Join("; ", parts) : null;

                return (cuttingMethod, cleanedParameter);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse parameter: {ex.Message}");
                return (null, parameter); // 发生异常时，返回原始 parameter，避免数据丢失
            }
        }

    }
}
