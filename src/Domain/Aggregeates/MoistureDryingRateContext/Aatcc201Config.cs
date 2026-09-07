using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext
{
    /// <summary>
    /// AATCC 201 加热板干燥速率测试仪校准参数聚合根（轻量单实体，全表只存一行配置）。
    /// 来源：旧软件 mdb 201config（现场偏置/斜坡平缓判定）+ 201testerconfig（设定温度/PID）。
    /// 用途：
    ///   1) 遥测显示叠加 —— 设备原始值 + 偏置 = 真实显示值（温度/风速）；
    ///   2) 干燥终点判定 —— slope_point/flat_point/slope_dg_no/slope_continue_* 驱动平台检测；
    ///   3) 设备下发 —— set_temp/temp_board1_x/temp_board2_x/p/i/d 经串口写进加热控制器。
    /// 该聚合是本次集成的唯一持久化对象：测试记录不落结构化库（决策14），报告存文件。
    /// </summary>
    public sealed class Aatcc201Config : AggregateRoot<Aatcc201ConfigId, Guid>
    {
        /// <summary>设备编号</summary>
        public string MachineNo { get; private set; } = string.Empty;

        /// <summary>表面温度1偏置(℃)——遥测原始值 + 偏置 = 显示值</summary>
        public decimal TempHw1 { get; private set; }

        /// <summary>表面温度2偏置(℃)</summary>
        public decimal TempHw2 { get; private set; }

        /// <summary>加热板温度1偏置(℃)</summary>
        public decimal TempBoard1 { get; private set; }

        /// <summary>加热板温度2偏置(℃)</summary>
        public decimal TempBoard2 { get; private set; }

        /// <summary>风速1偏置(m/s)</summary>
        public decimal Wind1 { get; private set; }

        /// <summary>风速2偏置(m/s)</summary>
        public decimal Wind2 { get; private set; }

        /// <summary>斜坡段判定点数(干燥终点检测用)</summary>
        public int SlopePoint { get; private set; }

        /// <summary>平缓段判定点数(干燥终点检测用)</summary>
        public int FlatPoint { get; private set; }

        /// <summary>斜坡判定序号</summary>
        public int SlopeDgNo { get; private set; }

        /// <summary>斜坡持续点数</summary>
        public int SlopeContinueNo { get; private set; }

        /// <summary>斜坡持续温差(℃)</summary>
        public decimal SlopeContinueTemp { get; private set; }

        /// <summary>设定温度(℃)——设备下发</summary>
        public decimal SetTemp { get; private set; }

        /// <summary>加热板1修正(℃)——设备下发</summary>
        public decimal TempBoard1X { get; private set; }

        /// <summary>加热板2修正(℃)——设备下发</summary>
        public decimal TempBoard2X { get; private set; }

        /// <summary>PID 比例系数——设备下发</summary>
        public decimal P { get; private set; }

        /// <summary>PID 积分系数——设备下发</summary>
        public decimal I { get; private set; }

        /// <summary>PID 微分系数——设备下发</summary>
        public decimal D { get; private set; }

        /// <summary>最近更新时间</summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>最近更新人</summary>
        public string? UpdatedBy { get; private set; }

        private Aatcc201Config() { }

        /// <summary>
        /// 工厂：创建新配置（首次入库）。Create 会重置 UpdatedAt；读库回来的历史值应走 Reconstitute 原样还原。
        /// </summary>
        public static Aatcc201Config Create(
            Aatcc201ConfigId id, string machineNo,
            decimal tempHw1, decimal tempHw2, decimal tempBoard1, decimal tempBoard2,
            decimal wind1, decimal wind2,
            int slopePoint, int flatPoint, int slopeDgNo, int slopeContinueNo, decimal slopeContinueTemp,
            decimal setTemp, decimal tempBoard1X, decimal tempBoard2X, decimal p, decimal i, decimal d,
            string? updatedBy)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return new Aatcc201Config
            {
                Id = id, MachineNo = machineNo,
                TempHw1 = tempHw1, TempHw2 = tempHw2, TempBoard1 = tempBoard1, TempBoard2 = tempBoard2,
                Wind1 = wind1, Wind2 = wind2,
                SlopePoint = slopePoint, FlatPoint = flatPoint, SlopeDgNo = slopeDgNo,
                SlopeContinueNo = slopeContinueNo, SlopeContinueTemp = slopeContinueTemp,
                SetTemp = setTemp, TempBoard1X = tempBoard1X, TempBoard2X = tempBoard2X,
                P = p, I = i, D = d,
                UpdatedAt = DateTime.Now, UpdatedBy = updatedBy
            };
        }

        /// <summary>
        /// 从数据库重建（持久化实体 → 领域聚合根）。保留库里的 UpdatedAt，不做任何重置。
        /// </summary>
        public static Aatcc201Config Reconstitute(
            Aatcc201ConfigId id, string machineNo,
            decimal tempHw1, decimal tempHw2, decimal tempBoard1, decimal tempBoard2,
            decimal wind1, decimal wind2,
            int slopePoint, int flatPoint, int slopeDgNo, int slopeContinueNo, decimal slopeContinueTemp,
            decimal setTemp, decimal tempBoard1X, decimal tempBoard2X, decimal p, decimal i, decimal d,
            DateTime updatedAt, string? updatedBy)
        {
            return new Aatcc201Config
            {
                Id = id, MachineNo = machineNo,
                TempHw1 = tempHw1, TempHw2 = tempHw2, TempBoard1 = tempBoard1, TempBoard2 = tempBoard2,
                Wind1 = wind1, Wind2 = wind2,
                SlopePoint = slopePoint, FlatPoint = flatPoint, SlopeDgNo = slopeDgNo,
                SlopeContinueNo = slopeContinueNo, SlopeContinueTemp = slopeContinueTemp,
                SetTemp = setTemp, TempBoard1X = tempBoard1X, TempBoard2X = tempBoard2X,
                P = p, I = i, D = d,
                UpdatedAt = updatedAt, UpdatedBy = updatedBy
            };
        }

        /// <summary>
        /// 全量更新校准参数（操作员保存）：覆盖所有可调字段，刷新 UpdatedAt/UpdatedBy。
        /// 直接修改当前聚合实例状态，由仓储 Upsert 时把新值写回持久化实体。
        /// </summary>
        public void UpdateAll(
            string machineNo,
            decimal tempHw1, decimal tempHw2, decimal tempBoard1, decimal tempBoard2,
            decimal wind1, decimal wind2,
            int slopePoint, int flatPoint, int slopeDgNo, int slopeContinueNo, decimal slopeContinueTemp,
            decimal setTemp, decimal tempBoard1X, decimal tempBoard2X, decimal p, decimal i, decimal d,
            string? updatedBy)
        {
            MachineNo = machineNo;
            TempHw1 = tempHw1; TempHw2 = tempHw2; TempBoard1 = tempBoard1; TempBoard2 = tempBoard2;
            Wind1 = wind1; Wind2 = wind2;
            SlopePoint = slopePoint; FlatPoint = flatPoint; SlopeDgNo = slopeDgNo;
            SlopeContinueNo = slopeContinueNo; SlopeContinueTemp = slopeContinueTemp;
            SetTemp = setTemp; TempBoard1X = tempBoard1X; TempBoard2X = tempBoard2X;
            P = p; I = i; D = d;
            UpdatedAt = DateTime.Now; UpdatedBy = updatedBy;
        }
    }
}
