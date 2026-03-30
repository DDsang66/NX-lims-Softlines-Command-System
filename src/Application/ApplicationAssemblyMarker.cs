namespace NX_lims_Softlines_Command_System.Application
{
    /// <summary>
    /// Application 层程序集标记类
    /// 用于依赖注入时定位本程序集
    /// </summary>
    public static class ApplicationAssemblyMarker
    {
        /// <summary>
        /// 获取 Application 层程序集
        /// </summary>
        public static System.Reflection.Assembly Assembly =>
            typeof(ApplicationAssemblyMarker).Assembly;
    }
}
