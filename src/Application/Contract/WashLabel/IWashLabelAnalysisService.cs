using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.WashLabel;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.WashLabel;

/// <summary>
/// 洗标识别服务接口 — 定义在 Application 层，实现在 Infrastructure 层
/// </summary>
public interface IWashLabelAnalysisService
{
    Task<AnalysisResult> AnalyzeImageAsync(byte[] imageBytes, string mediaType);
}
