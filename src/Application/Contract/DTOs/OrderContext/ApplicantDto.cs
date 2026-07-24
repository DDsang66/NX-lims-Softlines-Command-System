namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 申请人信息 — 前端申请延期时提交的数据
/// </summary>
public class ApplicantDto
{
    /// <summary>申请人姓名</summary>
    public string? Applicant { get; set; }

    /// <summary>延期原因</summary>
    public string? Reason { get; set; }

    /// <summary>报告编号</summary>
    public string? ReportNumber { get; set; }

    /// <summary>测试组</summary>
    public string? TestGroup { get; set; }

    /// <summary>要求完成日期</summary>
    public DateTimeOffset? ReportDueDate { get; set; }

    /// <summary>快递类型</summary>
    public string? Express { get; set; }

    /// <summary>测点数量</summary>
    public int? TestSampleNum { get; set; }
}
