using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Net.Sockets;
using System.Net;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Configuration
{
    public class ServerConfig : IServerConfig, IScopedDependency
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ServerConfig(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取基础ip地址
        /// </summary>
        /// <returns></returns>
        public string GetBaseUrl()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null) return "http://localhost"; // Fallback

            var request = context.Request;

            // 逻辑 1: 优先使用请求中的 Scheme (http/https) 和 Host
            // 如果服务在反向代理（如 Nginx）后面，这通常是最准确的，
            // 前提是 Nginx 正确转发了 X-Forwarded-Proto 和 X-Forwarded-Host 头。
            var scheme = request.Scheme;
            var host = request.Host;

            // 如果 Host 是 localhost 或 IP，且想强制返回内网 IP (根据你之前的代码逻辑)
            // 注意：这种硬编码 IP 的逻辑在 Docker/K8s 环境下通常是不需要的，
            // 但如果确实需要本机 IP，保留这部分逻辑。
            if (host.Host == "localhost" || IsLocalIpAddress(host.Host))
            {
                var localIP = GetLocalIPAddress();
                // 如果端口存在，则拼接端口
                var port = host.Port.HasValue ? $":{host.Port.Value}" : "";
                return $"{scheme}://{localIP}{port}";
            }

            // 默认情况：直接返回请求的 Host
            return $"{scheme}://{host}";

        }

        /// <summary>
        /// 判断是否是本地ip地址
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private bool IsLocalIpAddress(string host)
        {
            try
            {
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (IPAddress hostIP in hostIPs)
                {
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    if (localIPs.Contains(hostIP)) return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 获取ip网关地址
        /// </summary>
        /// <returns></returns>
        private string GetLocalIPAddress()
        {
            // 这是之前的逻辑
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
