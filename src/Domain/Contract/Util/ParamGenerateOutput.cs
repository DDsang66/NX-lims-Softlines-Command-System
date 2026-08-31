using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class ParamGenerateOutput
    {
        /// <summary>
        /// 生成的参数集
        /// </summary>
        public ParamSet ParamSet { get; set; }

        /// <summary>
        /// 可用于构建成条件的参数集
        /// </summary>
        public Dictionary<string,object> NewCondition {  get; set; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="paramSet"></param>
        /// <param name="newCondition"></param>
        public ParamGenerateOutput(ParamSet paramSet, Dictionary<string, object> newCondition) 
        {
            ParamSet = paramSet;
            NewCondition = newCondition;
        }
    }
}
