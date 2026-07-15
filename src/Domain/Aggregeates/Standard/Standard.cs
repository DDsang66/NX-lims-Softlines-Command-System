using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard
{
    public sealed class Standard : AggregateRoot
    {
        public StandardId IdStandard { get; private set; } = null!;

        public string StandardCode { get; private set; } = null!;

        public string? StandardCodeNameEn { get; private set; } = null!;

        public string? StandardCodeNameChn { get; private set; } = null!;

        public Status Status { get; private set; } = Status.Draft; 

        public StandardFamilyId? StandardFamilyCode { get; private set; } = null!;
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
            StandardId id,
            string standardCode,
            StandardFamilyId? familyCode,
            string? nameEn,
            string? nameChn,
            Status status)
        {
            if (string.IsNullOrWhiteSpace(standardCode))
                throw new ArgumentException("Standard code is required");

            var standard = new Standard
            {
                IdStandard = id,
                StandardCode = standardCode,
                StandardFamilyCode = familyCode,
                StandardCodeNameEn = nameEn,
                StandardCodeNameChn = nameChn,
                Status = Status.Draft,
            };

            //standard.AddDomainEvent(new StandardCreatedEvent(id, standardCode));

            return standard;
        }

        /// <summary>
        /// 从数据库重建 Standard（仓储层使用，不校验业务规则）
        /// </summary>
        internal static Standard Reconstitute(
            StandardId idStandard,
            string standardCode,
            string? standardCodeNameEn,
            string? standardCodeNameChn,
            Status status,
            StandardFamilyId? standardFamilyCode)
        {
            return new Standard
            {
                IdStandard = idStandard,
                StandardCode = standardCode,
                StandardCodeNameEn = standardCodeNameEn,
                StandardCodeNameChn = standardCodeNameChn,
                Status = status,
                StandardFamilyCode = standardFamilyCode
            };
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
            StandardFamilyId? familyCode,
            string? nameEn,
            string? nameChn)
        {
            //当前只做最简校验，后续可以根据业务规则添加更多校验逻辑，例如：标准编码格式、测试组合法性、名称长度等
            if (!string.IsNullOrWhiteSpace(standardCode))
                this.StandardCode = standardCode;

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
        /// 绑定标准到标准族
        /// </summary>
        public void BindToStandardFamily(StandardFamilyId id) 
        {
            // 1. 参数校验
            if (id == null)
                throw new Exception("标准族ID不能为空");

            // 2. 业务规则校验：一个标准不能同时属于多个标准族
            if (this.StandardFamilyCode != null && this.StandardFamilyCode.Value != id.Value)
                throw new Exception($"该标准已绑定到标准族 {this.StandardFamilyCode}，不能重复绑定到其他标准族");

            // 3. 业务规则校验：只有特定状态的标准才能被绑定
            if (this.Status != Status.Draft && this.Status != Status.Pending)
                throw new Exception($"只有“草稿”或“待审核”状态的标准才能被绑定到标准族，当前状态为：{this.Status}");

            // 4. 业务规则校验：如果标准族ID相同，则无需重复操作
            if (this.StandardFamilyCode?.Value == id.Value)
                return; // 或者可以抛出异常，取决于业务需求

            // 5. 执行状态变更
            this.StandardFamilyCode = id;
            // 6. 可以在这里触发领域事件，通知外部系统
            // AddDomainEvent(new StandardBoundToFamilyEvent(this.Id, id));

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
        public void Draft() => this.Status = Status.Draft;

        /// <summary>
        /// 将当前标准设置为“已废弃”状态
        /// </summary>
        public void Deprecated() => this.Status = Status.Deprecated;

        /// <summary>
        /// 将当前标准设置为“被替代”状态
        /// </summary>
        public void Superseded() => this.Status = Status.Superseded;

        /// <summary>
        /// 将当前标准设置为“待审核”状态
        /// </summary>
        public void Pending() => this.Status = Status.Pending;

    }
}
