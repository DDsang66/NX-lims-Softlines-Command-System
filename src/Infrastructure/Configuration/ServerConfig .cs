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

        public string GetBaseUrl()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "http://192.168.56.1:5051"; // Fallback

            var request = context.Request;

            // 使用请求的 Scheme 和 Host，支持 IP 访问
            var scheme = request.Scheme;
            var host = request.Host;

            var port = host.Port.HasValue ? $":{host.Port.Value}" : "";
            return $"{scheme}://{host.Host}{port}";
        }

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
