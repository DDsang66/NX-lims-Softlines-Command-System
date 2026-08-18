using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext
{
    public sealed class Buyer : AggregateRoot<BuyerId, string>
    {
        // 冗余存储便于映射与读取
        public string BuyerCode => Id.Value;

        public string BuyerName { get; private set; } = string.Empty;

        public string? Remark { get; private set; }

        /// <summary>
        /// 留样天数（可为空）
        /// </summary>
        public int? SampleStorageDate { get; private set; }

        public string? Country { get; private set; }

        /// <summary>
        /// 散客标志：true 时跳过买家覆盖逻辑
        /// </summary>
        public bool IsIndividualTraveler { get; private set; }

        // 私有构造防止外部直接 new
        private Buyer() { }

        public static Buyer Create(BuyerId id, string buyerName, string? remark = null,
            int? sampleStorageDate = null, string? country = null, bool isIndividualTraveler = false)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(buyerName))
                throw new ArgumentException("BuyerName cannot be empty", nameof(buyerName));

            return new Buyer
            {
                Id = id,
                BuyerName = buyerName.Trim(),
                Remark = remark,
                SampleStorageDate = sampleStorageDate,
                Country = country,
                IsIndividualTraveler = isIndividualTraveler
            };
        }

        /// <summary>
        /// 重建方法
        /// </summary>
        /// <param name="id"></param>
        /// <param name="buyerName"></param>
        /// <param name="remark"></param>
        /// <param name="sampleStorageDate"></param>
        /// <param name="country"></param>
        /// <param name="isIndividualTraveler"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Buyer Reconstitute(BuyerId id, string buyerName, string? remark,
            int? sampleStorageDate, string? country, bool isIndividualTraveler)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            return new Buyer
            {
                Id = id,
                BuyerName = buyerName,
                Remark = remark,
                SampleStorageDate = sampleStorageDate,
                Country = country,
                IsIndividualTraveler = isIndividualTraveler
            };
        }

        // ============ 修改方法 ============
        public void UpdateName(string buyerName)
        {
            if (string.IsNullOrWhiteSpace(buyerName))
                throw new ArgumentException("BuyerName cannot be empty", nameof(buyerName));

            BuyerName = buyerName.Trim();
        }

        public void UpdateRemark(string? remark)
        {
            Remark = remark;
        }

        public void UpdateSampleStorageDate(int? days)
        {
            if (days < 0) throw new ArgumentOutOfRangeException(nameof(days), "SampleStorageDate cannot be negative");
            SampleStorageDate = days;
        }

        public void UpdateCountry(string? country)
        {
            Country = country;
        }

        public void SetIsIndividualTraveler(bool isIndividualTraveler)
        {
            IsIndividualTraveler = isIndividualTraveler;
        }
    }
}
