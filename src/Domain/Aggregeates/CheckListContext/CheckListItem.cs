using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public class CheckListItem : Entity
    {
        /// <summary>
        /// 测试项标识
        /// 已继承实体基类，无需重复定义
        /// </summary>
        //public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 测试项目ID
        /// </summary>
        public TestItemId? TestItemId { get; set; }

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string? BuyerModifiedTestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<StandardId?> StandardIds { get; set; } = Enumerable.Empty<StandardId>();

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string? BuyerModifiedTextMethodId { get; set; } = string.Empty;

        /// <summary>
        /// 测试小组
        /// </summary>
        public TestGroup TestGroup { get; set; } = new();

        ///// <summary>
        ///// 参数集
        ///// </summary>
        //public ParamSet? Param { get; set; } = new();

        /// <summary>
        /// 样品列表
        /// </summary>
        public List<string> Samples { get; set; } = new();

        /// <summary>
        /// 测点参数集字典（每个测点对应一个参数集）
        /// Key: 测点标识（可以是字符串形式的测点ID或其他唯一标识）
        /// Value: 对应的参数集
        /// </summary>
        public IReadOnlyDictionary<string, ParamSet?> TestPointParams { get; set; } =
            new Dictionary<string, ParamSet?>();

        /// <summary>
        /// 买家限值
        /// </summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>
        /// 项目状态
        /// </summary>
        public CheckListStatus Status { get; set; } = CheckListStatus.Created;

        /// <summary>
        /// 测试清单ID
        /// </summary>
        public CheckListId CheckListId { get; set; } = new CheckListId(Guid.NewGuid());

        /// <summary>
        /// 重建
        /// </summary>
        /// <param name="id"></param>
        /// <param name="checkListId"></param>
        /// <param name="testItemId"></param>
        /// <param name="standardIds"></param>
        /// <param name="buyerModifiedTestItemId"></param>
        /// <param name="buyerModifiedTextMethodId"></param>
        /// <param name="testGroup"></param>
        /// <param name="testPointParams"></param>
        /// <param name="samples"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static CheckListItem Reconstitute(
            Guid id,
            CheckListId checkListId,
            TestItemId testItemId,
            List<StandardId> standardIds,
            string buyerModifiedTestItemId,
            string buyerModifiedTextMethodId,
            TestGroup testGroup,
            IReadOnlyDictionary<string, ParamSet?> testPointParams,
            List<string> samples,
            CheckListStatus status,
            string requirement)
        {
            return new CheckListItem
            {
                Id = id,
                CheckListId = checkListId,
                TestItemId = testItemId,
                StandardIds = standardIds,
                BuyerModifiedTestItemId = buyerModifiedTestItemId,
                BuyerModifiedTextMethodId = buyerModifiedTextMethodId,
                TestGroup = testGroup,
                TestPointParams = testPointParams,
                Samples = samples,
                Status = status,
                Requirement = requirement
            };
        }

        /// <summary>
        /// 添加或更新测点参数集
        /// </summary>
        /// <param name="testPointId">测点标识</param>
        /// <param name="paramSet">参数集</param>
        public void AddOrUpdateTestPointParam(string testPointId, ParamSet paramSet)
        {
            if (string.IsNullOrWhiteSpace(testPointId))
            {
                throw new ArgumentException("测点ID不能为空", nameof(testPointId));
            }

            if (paramSet == null)
            {
                throw new ArgumentNullException(nameof(paramSet));
            }

            var dictionary = (Dictionary<string, ParamSet>)TestPointParams;
            dictionary[testPointId] = paramSet;
            TestPointParams = dictionary;
        }

        /// <summary>
        /// 移除测点参数集
        /// </summary>
        /// <param name="testPointId">测点标识</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveTestPointParam(string testPointId)
        {
            if (string.IsNullOrWhiteSpace(testPointId))
            {
                throw new ArgumentException("测点ID不能为空", nameof(testPointId));
            }

            var dictionary = (Dictionary<string, ParamSet>)TestPointParams;
            var removed = dictionary.Remove(testPointId);
            if (removed)
            {
                TestPointParams = dictionary;
            }
            return removed;
        }

        /// <summary>
        /// 获取测点参数集
        /// </summary>
        /// <param name="testPointId">测点标识</param>
        /// <returns>参数集，如果不存在则返回null</returns>
        public ParamSet? GetTestPointParam(string testPointId)
        {
            if (string.IsNullOrWhiteSpace(testPointId))
            {
                throw new ArgumentException("测点ID不能为空", nameof(testPointId));
            }

            return TestPointParams.TryGetValue(testPointId, out var paramSet) ? paramSet : null;
        }
    }
}
