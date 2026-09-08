namespace NX_lims_Softlines_Command_System.src.Application.Attributes
{
    /// <summary>
    /// 用途:标记方法为字段处理器，用于处理字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FieldHandlerAttribute : Attribute
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="fieldName"></param>
        public FieldHandlerAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
