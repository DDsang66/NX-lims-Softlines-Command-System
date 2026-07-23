using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    public class ConditionEnricher: IConditionEnricher
    {
        public ConditionPool Enrich(IDictionary<string, object?> rawData) 
        {
            //首先需要比对Structure和Formula中的ConditionFiled

            //获取比对得到的标签，根据标签跳转至对应方法

            //实现思路参考：

            /*
             1. 定义标签（Attribute）
            [AttributeUsage(AttributeTargets.Method)]
public class AutoExecuteAttribute : Attribute
{
    public string Tag { get; }
    
    public AutoExecuteAttribute(string tag)
    {
        Tag = tag;
    }
}

            2.标记方法

            public class MyService
{
    [AutoExecute("tag1")]  // 贴标签
    public void MethodA()
    {
        Console.WriteLine("MethodA executed");
    }

    [AutoExecute("tag2")]
    public void MethodB()
    {
        Console.WriteLine("MethodB executed");
    }

    public void MethodC()  // 无标签
    {
        Console.WriteLine("MethodC executed");
    }
}
            3.扫描并执行
            public class TagExecutor
           {
                public void ExecuteByTag(object target, string tag)
                {
                    var methods = target.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.GetCustomAttribute<AutoExecuteAttribute>()?.Tag == tag);

                    foreach (var method in methods)
                    {
                        method.Invoke(target, null);
                     }
                 }
           }


             */

            //ConditionPool的Conditions字段添加新行




            return null;
        }
    }
}
