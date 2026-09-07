using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext.ValueObj
{
    /// <summary>
    /// AATCC 201 校准参数聚合根 Id（唯一标识，Guid 类型值对象）。
    /// 继承 AggregateRootId 获得基于 Value 的值相等比较，禁止空 Guid。
    /// </summary>
    public class Aatcc201ConfigId : AggregateRootId<Guid>
    {
        public Aatcc201ConfigId(Guid value)
            : base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Aatcc201ConfigId cannot be empty", nameof(value));
        }
    }
}
