using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Hardware.RFID;

/// <summary>
/// RFID 读写器业务服务 — 封装 UHFReader288 设备的连接和盘点操作
///
/// 生命周期: Singleton（应用启动时构造一次，关闭时 Dispose）
/// 自动注册: 实现 ISingletonDependency，由 Scrutor 自动扫描注册
///
/// 调用链: RfidController → RfidService.ScanOnce() → RWDev.Inventory_G2 → UHFReader288.dll
/// </summary>
public class RfidService : ISingletonDependency, IDisposable
{
    /// <summary>读写器地址（由 OpenUSBPort 返回）</summary>
    private byte _comAddr = 0xFF;

    /// <summary>端口句柄（后续所有操作都需要传）</summary>
    private int _portHandle = -1;

    /// <summary>是否成功连接设备</summary>
    private readonly bool _connected;

    private readonly ILogger<RfidService> _logger;

    /// <summary>
    /// 构造时自动打开 USB 连接。失败不抛异常，ScanOnce 会返回 null
    /// </summary>
    public RfidService(ILogger<RfidService> logger)
    {
        _logger = logger;

        try
        {
            // 先尝试 USB 方式（CP210x 驱动）
            int ret = RWDev.OpenUSBPort(ref _comAddr, ref _portHandle);
            if (ret != 0)
            {
                // USB 失败则尝试自动搜索 COM 口（CH343 等串口驱动）
                _logger.LogWarning("RFID OpenUSBPort 失败(返回码={Ret}), 尝试 AutoOpenComPort...", ret);
                int port = 0;
                ret = RWDev.AutoOpenComPort(ref port, ref _comAddr, 5, ref _portHandle); // Baud=5=115200
                _logger.LogInformation("RFID AutoOpenComPort 结果: 返回码={Ret}, Port={Port}, ComAddr={ComAddr}", ret, port, _comAddr);
            }
            _connected = ret == 0;
            if (_connected)
                _logger.LogInformation("RFID 设备连接成功, ComAddr={ComAddr}, PortHandle={PortHandle}", _comAddr, _portHandle);
            else
                _logger.LogWarning("RFID 设备连接失败, 返回码={Ret}", ret);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RFID 设备连接异常（驱动未安装或设备未插入）");
            _connected = false;
        }
    }

    /// <summary>
    /// 单次盘点 — 启动一次持续读取，1 秒后停止，返回第一个标签的 EPC
    ///
    /// 使用 StartInventory + 回调线程（与测试软件相同方式），而非直接 Inventory_G2
    ///
    /// 返回值: 第一个标签的 EPC 十六进制字符串（如 "E2003412B0..."），无标签返回 null
    /// </summary>
    public string? ScanOnce()
    {
        if (!_connected)
        {
            _logger.LogDebug("RFID ScanOnce 跳过: 设备未连接");
            return null;
        }

        string? epcResult = null;
        var gotTag = new ManualResetEventSlim(false);

        try
        {
            // 启动后台持续扫描（SDK 回调线程会反复调用此 delegate）
            int ret = RWDev.StartInventory(ref _comAddr, Target: 0, tag =>
            {
                if (tag.UID != null && tag.UID.Length > 0)
                {
                    epcResult = tag.UID;
                    _logger.LogInformation("RFID 扫到标签, EPC={EPC}, ANT={Ant}, RSSI={RSSI}", tag.UID, tag.ANT, tag.RSSI);
                    gotTag.Set(); // 拿到了，通知主线程
                }
            }, _portHandle);

            if (ret != 0)
            {
                _logger.LogWarning("RFID StartInventory 失败, 返回码={Ret}", ret);
                return null;
            }

            // 等待最多 3 秒，拿到标签就返回
            if (!gotTag.Wait(TimeSpan.FromSeconds(3)))
            {
                _logger.LogDebug("RFID 扫描超时（3 秒内未检测到标签）");
            }

            // 停止后台扫描
            RWDev.StopInventory(ref _comAddr, _portHandle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RFID ScanOnce 异常");
            try { RWDev.StopInventory(ref _comAddr, _portHandle); } catch { }
        }

        return epcResult;
    }

    /// <summary>
    /// 释放 USB 连接
    /// </summary>
    public void Dispose()
    {
        if (_connected)
        {
            try { RWDev.CloseUSBPort(_portHandle); } catch { }
        }
    }
}
