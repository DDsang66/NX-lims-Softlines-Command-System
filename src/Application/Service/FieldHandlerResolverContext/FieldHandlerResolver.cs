using NX_lims_Softlines_Command_System.src.Application.Attributes;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Reflection;

namespace NX_lims_Softlines_Command_System.src.Application.Service.FieldHandlerResolverContext
{
    /// <summary>
    /// 字段处理程序解析器
    /// </summary>
    public class FieldHandlerResolver:ISingletonDependency
    {
        private readonly Dictionary<string, MethodInfo> _handlers = new();
        private readonly object _lock = new();
        private bool _initialized = false;
        private readonly Assembly[] _assemblies;

        // 修改构造函数：不依赖 DI 注入 Assembly[]
        public FieldHandlerResolver()
            : this(Assembly.GetExecutingAssembly())  // 默认使用当前程序集
        {
        }

        public FieldHandlerResolver(params Assembly[] assemblies)
        {
            _assemblies = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetExecutingAssembly() };
        }

        /// <summary>
        /// 动态调用字段处理方法
        /// </summary>
        public Result<object> Resolve(string fieldName, object input, Dictionary<string, object> context = null)
        {
            EnsureInitialized();

            if (_handlers.TryGetValue(fieldName, out var method))
            {
                return InvokeMethod(method, input, context);
            }

            // 如果没有找到，返回原值
            return Result<object>.Fail("没有找到字段处理方法");
        }

        /// <summary>
        /// 获取所有已注册的字段名
        /// </summary>
        public IEnumerable<string> GetRegisteredFieldNames()
        {
            EnsureInitialized();
            return _handlers.Keys;
        }

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                ScanAndRegisterHandlers();
                _initialized = true;
            }
        }

        /// <summary>
        /// 扫描并注册字段处理程序
        /// </summary>
        private void ScanAndRegisterHandlers()
        {
            foreach (var assembly in _assemblies)
            {
                var methods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    .Where(m => m.GetCustomAttribute<FieldHandlerAttribute>() != null);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<FieldHandlerAttribute>();
                    _handlers[attr.FieldName] = method;
                }
            }
        }

        /// <summary>
        /// 调用方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private object InvokeMethod(MethodInfo method, object input, Dictionary<string, object> context)
        {
            object instance = null;

            // 如果是实例方法，需要创建实例
            if (!method.IsStatic)
            {
                instance = Activator.CreateInstance(method.DeclaringType);
            }

            // 准备参数
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                if (i == 0 && paramType == typeof(object))
                {
                    args[i] = input;
                }
                else if (paramType == typeof(Dictionary<string, object>))
                {
                    args[i] = context ?? new Dictionary<string, object>();
                }
                else
                {
                    // 尝试转换类型
                    args[i] = Convert.ChangeType(input, paramType);
                }
            }

            return method.Invoke(instance, args);
        }
    }
}
