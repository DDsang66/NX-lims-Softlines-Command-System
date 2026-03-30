using NX_lims_Softlines_Command_System.Domain.Share.ValueObj;
using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard
{
    public class Standard : Entity, IAggregateRoot
    {
        public IdStandard IdStandard { get; private set; } = null!;

        public string StandardCode { get; private set; } = null!;

        public string? StandardCodeNameEn { get; private set; } = null!;

        public string? StandardCodeNameChn { get; private set; } = null!;

        public string TestGroup { get; private set; } = null!;

        public byte IsDisabled { get; private set; }

        public Status Status { get; private set; } = Status.Draft; 

        public StandardFamilyCode StandardFamilyCode { get; private set; } = null!;
        private Standard() { }

        /// <summary>
        /// 工厂方法用于创建标准聚合根实例，包含必要的属性和可选的参数集合
        /// </summary>
        /// <param name="id"></param>
        /// <param name="standardCode"></param>
        /// <param name="testGroup"></param>
        /// <param name="familyCode"></param>
        /// <param name="nameEn"></param>
        /// <param name="nameChn"></param>
        /// <param name="parameterSchema"></param>
        /// <param name="parameterRule"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Standard Create(
            IdStandard id,
            string standardCode,
            string testGroup,
            StandardFamilyCode familyCode,
            string? nameEn,
            string? nameChn)
        {
            if (string.IsNullOrWhiteSpace(standardCode))
                throw new ArgumentException("Standard code is required");

            if (string.IsNullOrWhiteSpace(testGroup))
                throw new ArgumentException("Test group is required");

            var standard = new Standard
            {
                IdStandard = id,
                StandardCode = standardCode,
                TestGroup = testGroup,
                StandardFamilyCode = familyCode,
                StandardCodeNameEn = nameEn,
                StandardCodeNameChn = nameChn,
                Status = Status.Draft,
                IsDisabled = 1
            };

            //standard.AddDomainEvent(new StandardCreatedEvent(id, standardCode));

            return standard;
        }

        /// <summary>
        /// 更新标准信息
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="standardCode"></param>
        /// <param name="testGroup"></param>
        /// <param name="familyCode"></param>
        /// <param name="nameEn"></param>
        /// <param name="nameChn"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public void Update(
            string? standardCode,
            string? testGroup,
            StandardFamilyCode? familyCode,
            string? nameEn,
            string? nameChn)
        {
            //当前只做最简校验，后续可以根据业务规则添加更多校验逻辑，例如：标准编码格式、测试组合法性、名称长度等
            if (!string.IsNullOrWhiteSpace(standardCode))
                this.StandardCode = standardCode;

            if (!string.IsNullOrWhiteSpace(testGroup))
                this.TestGroup = testGroup;

            if (familyCode != null)
                this.StandardFamilyCode = familyCode;

            if (!string.IsNullOrWhiteSpace(nameEn))
                this.StandardCodeNameEn = nameEn;

            if (!string.IsNullOrWhiteSpace(nameChn))
                this.StandardCodeNameChn = nameChn;

            //standard.AddDomainEvent(new StandardUpdatedEvent(standard.IdStandard, standard.StandardCode));
            // 可以在这里添加领域事件，例如StandardUpdatedEvent，来通知系统标准信息已更新（Schema、Rule等）
        }


        /// <summary>
        /// 激活标准
        /// </summary>
        /// <returns>操作结果</returns>
        public Result Activate()
        {
            // 1. 检查当前状态
            if (Status == Status.Active)
                return Result.Ok();

            //2.验证Schema是否存在
            //if (!_parameterSchema.Any())
            //    return Result.Fail("Cannot activate standard without parameter schemas");

            //3.验证Rule是否存在
            //if (!_parameterRule.Any())
            //    return Result.Fail("Cannot activate standard without parameter rules");

            //// 4. 检查所有Schema是否有效
            //var invalidSchemas = _parameterSchema.Where(s => !s.IsValid()).ToList();
            //if (invalidSchemas.Any())
            //    return Result.Fail($"Found {invalidSchemas.Count} invalid parameter schemas");

            //// 5. 检查所有Rule是否有效
            //var invalidRules = _parameterRule.Where(r => !r.IsValid()).ToList();
            //if (invalidRules.Any())
            //    return Result.Fail($"Found {invalidRules.Count} invalid parameter rules");

            //// 6. 激活标准
            //Status = Status.Active;

            //IsDisabled = 0;

            //// 7. 发布领域事件
            //AddDomainEvent(new StandardActivatedEvent(IdStandard, StandardCode));

            return Result.Ok();
        }

        /// <summary>
        /// 将当前标准回退为草稿状态，要求必须满足某些条件，
        /// 例如：没有被任何检测项目引用，或者相关的Schema/Rule都处于草稿状态等
        /// </summary>
        /// <returns></returns>
        public Result Draft() 
        {
            //逻辑

            //领域事件
            return Result.Ok();
        }

    }
}
