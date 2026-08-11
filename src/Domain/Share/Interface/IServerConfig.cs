using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    public interface IServerConfig:IScopedDependency
    {
        /// <summary>
        /// 获取当前服务的基础访问地址 (例如: http://localhost:5000 或 http://192.168.1.5:80)
        /// </summary>
        string GetBaseUrl();
    }
}
