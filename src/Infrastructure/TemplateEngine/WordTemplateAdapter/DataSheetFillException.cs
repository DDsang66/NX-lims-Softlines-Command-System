namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// 数据单填充（含 area 扩容）过程中的业务异常。
    /// 与普通异常的区别在于显式携带「是否可重试」标记，供后台 Worker 决定任务是否允许再次进入队列：
    /// 模板结构不合法属于不可重试，文件被占用/IO 抖动属于可重试。
    /// </summary>
    public sealed class DataSheetFillException : Exception
    {
        /// <summary>
        /// 是否允许重试。
        /// true 表示瞬时性故障（IO、文件占用等），重跑有机会成功；
        /// false 表示模板或数据本身有问题，重跑必然再次失败。
        /// </summary>
        public bool Retryable { get; }

        /// <summary>
        /// 构造填充异常。
        /// </summary>
        /// <param name="message">面向日志与排错的错误描述，应包含模板/报告号等定位信息。</param>
        /// <param name="retryable">是否允许重试。</param>
        /// <param name="inner">原始异常（可选），保留堆栈用于排查。</param>
        public DataSheetFillException(string message, bool retryable, Exception? inner = null)
            : base(message, inner)
        {
            Retryable = retryable;
        }
    }
}
