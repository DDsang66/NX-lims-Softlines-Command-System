using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext
{
    public class TemplateStructure:Entity
    {
        /// <summary>
        /// 测试Condition个数
        /// </summary>
        public int TestConditionCount { get;private set; }

        /// <summary>
        /// method位置个数
        /// </summary>
        public int TestMethodCount { get; private set; }

        /// <summary>
        /// 试样编号位置个数
        /// </summary>
        public int SampleDataAreaCount { get; private set; }

        /// <summary>
        /// 试验结果位置个数
        /// </summary>
        public int SampleResultAreaCount { get; private set; }

        /// <summary>
        /// 洗涤位置个数
        /// </summary>
        public int AfterWashDataCount { get; private set; }

        public TemplateId TemplateId { get; private set; }

        // EF Core 需要
        private TemplateStructure() { }

        private TemplateStructure(
            int testConditionCount,
            int testMethodCount,
            int sampleDataAreaCount,
            int sampleResultAreaCount,
            int afterWashDataCount,
            TemplateId templateId)
        {
            TestConditionCount = testConditionCount;
            TestMethodCount = testMethodCount;
            SampleDataAreaCount = sampleDataAreaCount;
            SampleResultAreaCount = sampleResultAreaCount;
            AfterWashDataCount = afterWashDataCount;
            TemplateId = templateId;
        }

        public static TemplateStructure Create(
            int testConditionCount,
            int testMethodCount,
            int sampleDataAreaCount,
            int sampleResultAreaCount,
            int afterWashDataCount,
            TemplateId templateId)
        {
            ValidateCounts(testConditionCount, testMethodCount, sampleDataAreaCount,
                           sampleResultAreaCount, afterWashDataCount);

            if (templateId == null)
                throw new ArgumentNullException(nameof(templateId), "模板ID不能为空");

            return new TemplateStructure(
                testConditionCount, testMethodCount, sampleDataAreaCount,
                sampleResultAreaCount, afterWashDataCount, templateId);
        }

        /// <summary>
        /// 更新模板结构配置
        /// </summary>
        public void Update(
            int testConditionCount,
            int testMethodCount,
            int sampleDataAreaCount,
            int sampleResultAreaCount,
            int afterWashDataCount)
        {
            ValidateCounts(testConditionCount, testMethodCount, sampleDataAreaCount,
                           sampleResultAreaCount, afterWashDataCount);

            TestConditionCount = testConditionCount;
            TestMethodCount = testMethodCount;
            SampleDataAreaCount = sampleDataAreaCount;
            SampleResultAreaCount = sampleResultAreaCount;
            AfterWashDataCount = afterWashDataCount;
        }

        private static void ValidateCounts(
            int testConditionCount,
            int testMethodCount,
            int sampleDataAreaCount,
            int sampleResultAreaCount,
            int afterWashDataCount)
        {
            if (testConditionCount < 0)
                throw new ArgumentException("测试条件数量不能为负数", nameof(testConditionCount));
            if (testMethodCount < 0)
                throw new ArgumentException("测试方法数量不能为负数", nameof(testMethodCount));
            if (sampleDataAreaCount < 0)
                throw new ArgumentException("试样数据区域数量不能为负数", nameof(sampleDataAreaCount));
            if (sampleResultAreaCount < 0)
                throw new ArgumentException("试样结果区域数量不能为负数", nameof(sampleResultAreaCount));
            if (afterWashDataCount < 0)
                throw new ArgumentException("洗涤后数据数量不能为负数", nameof(afterWashDataCount));
        }
    }
}
