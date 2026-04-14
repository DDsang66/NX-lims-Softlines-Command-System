using Scrutor;
using System.Reflection;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject
{
    /// <summary>
    /// 依赖注入扩展类
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddAutoRegister(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            var assembliesToScan = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetCallingAssembly() };

            services.Scan(scan => scan
                .FromAssemblies(assembliesToScan)

                // ========== Transient ==========
                .AddClasses(classes => classes.AssignableTo<ITransientDependency>())
                .AsSelfWithInterfaces()  // 关键：同时注册自身和实现的接口
                .WithTransientLifetime()

                // ========== Scoped ==========
                .AddClasses(classes => classes.AssignableTo<IScopedDependency>())
                .AsSelfWithInterfaces()  // 关键：同时注册自身和实现的接口
                .WithScopedLifetime()

                // ========== Singleton ==========
                .AddClasses(classes => classes.AssignableTo<ISingletonDependency>())
                .AsSelfWithInterfaces()  // 关键：同时注册自身和实现的接口
                .WithSingletonLifetime());

            return services;
        }
    }
}
