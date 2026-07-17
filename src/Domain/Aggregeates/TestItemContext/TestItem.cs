using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext
{
    public sealed class TestItem : AggregateRoot<TestItemId, string>
    {
        /// <summary>
        /// TestItemId
        /// </summary>
        //public TestItemId Id { get; private set; }

        ///<summary>
        /// 英文名称
        ///</summary>
        public string NameEN { get; private set; } = string.Empty;

        /// <summary>
        /// 中文名称
        /// </summary>
        public string NameChn { get; private set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// 是否在能力范围内
        /// </summary>
        public bool IsFeasible { get; private set; }

        /// <summary>
        /// 测试项目级别的参数要求定义
        /// </summary>
        public IReadOnlyCollection<ParamRequireDefinition> ParamRequireDefinition { get; private set; } =  new List<ParamRequireDefinition>();

        /// <summary>
        /// 状态
        /// </summary>
        public Status Status { get; private set; }

        /// <summary>
        /// 工厂
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nameEN"></param>
        /// <param name="nameChn"></param>
        /// <param name="description"></param>
        /// <param name="isFeasible"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TestItem Create(
            TestItemId id,
            string nameEN, 
            string nameChn, 
            string description,
            bool isFeasible,
            Status status)
        {
            //validate
            if (id == null) 
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(nameEN))
                throw new ArgumentNullException("NameEN cannot be null or empty.", nameof(nameEN));
            if (string.IsNullOrEmpty(nameChn))
                throw new ArgumentNullException("NameChn cannot be null or empty.", nameof(nameChn));
            if (string.IsNullOrEmpty(description))
                throw new ArgumentNullException("Description cannot be null or empty.", nameof(description));

            var testItem = new TestItem
            {
                Id = id,
                NameEN = nameEN,
                NameChn = nameChn,
                Description = description,
                IsFeasible = isFeasible,
                Status = status
            };
            return testItem;

        }
    }
}
