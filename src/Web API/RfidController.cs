using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Infrastructure.Hardware.RFID;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers;

/// <summary>
/// RFID 电子标签 API — 前端进单/实验室页面扫码时调用
///
/// 前端流程: 点击输入框旁的"扫码"按钮 → GET /api/rfid/scan → 拿到 UID → 填入输入框
/// 调用链:   Controller → RfidService.ScanOnce() → RWDev.Inventory_G2 → UHFReader288.dll
/// </summary>
[ApiController]
[Route("api/rfid")]
public class RfidController : Controller
{
    private readonly RfidService _rfid;

    /// <summary>
    /// RfidService 是 Singleton，由 DI 自动注入
    /// </summary>
    public RfidController(RfidService rfid) => _rfid = rfid;

    /// <summary>
    /// 单次盘点 — 触发一次标签扫描，返回范围内的第一个标签
    ///
    /// 请求: GET /api/rfid/scan
    ///
    /// 成功: { success: true,  data: "E2003412B0..." }  ← EPC 十六进制字符串
    /// 失败: { success: false, message: "No tag detected" }（无标签或设备未连接）
    /// </summary>
    [HttpGet("scan")]
    public IActionResult Scan()
    {
        var uid = _rfid.ScanOnce();
        if (uid == null)
            return Ok(new { success = false, message = "No tag detected" });
        return Ok(new { success = true, data = uid });
    }
}
