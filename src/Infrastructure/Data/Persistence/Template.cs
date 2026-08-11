using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

/// <summary>
/// 模板聚合根表
/// </summary>
public partial class Template
{
    /// <summary>
    /// 模板唯一标识
    /// </summary>
    public string TemplateId { get; set; } = null!;

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = null!;

    /// <summary>
    /// 模板的URL地址
    /// </summary>
    public string TemplateUrl { get; set; } = null!;

    /// <summary>
    /// 地区站点枚举 (例如: 0=NB)
    /// </summary>
    public byte Site { get; set; }

    /// <summary>
    /// 当前datasheet状态枚举 (例如: 0=Draft, 1=Published)
    /// </summary>
    public byte Status { get; set; }

    /// <summary>
    /// 当前模板版本
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 变更时间
    /// </summary>
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// 模板文件类型枚举 (0=Docx, 1=Excel)
    /// </summary>
    public byte FileType { get; set; }

    /// <summary>
    /// 业务子分类文件夹名称 (如 Common_FLAM, Common_PHY 等)
    /// </summary>
    public string BusinessCategory { get; set; } = null!;
}
