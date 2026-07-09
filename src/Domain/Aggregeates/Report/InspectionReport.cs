using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Report.Enums;
using System.Security.Cryptography.Xml;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Report
{
    public sealed class InspectionReport: IAggregateRoot
    {
        public Guid ReportId { get; private set; }  // 标识

        public string ReportNum { get; private set; } = string.Empty; // 报告流水号
        public ReportStatus Status { get; private set; } // Draft → Review → Approved
        public string SavedAddress { get; private set; } = string.Empty; // 报告存储地址

        //public List<ReportVersion> Versions { get; private set; }//版本号

        /// <summary>
        /// 工厂方法
        /// 实例化报告实体
        /// </summary>
        public InspectionReport Create(Guid reportId) 
        {
            //调用IReportReposity获取报告信息

            return new InspectionReport();
        }

        /// <summary>
        /// 创建报告
        /// </summary>
        /// <returns></returns>
        public async Task GenerateReport() 
        {
            /* 创建报告逻辑 */

            //验证所有小组的测试是否完成，数据是否完整

            //获取当前测试单号的所有信息

            //通知模板引擎进行创建

            //领域事件，通知其他模块报告创建
        }

        public void SubmitForReview()
        { 
            /* 状态机转换 */ 
            Status = ReportStatus.Review;
        }
        public void Approve(Signature signature) 
        {
            /* 审批逻辑 */ 
            Status = ReportStatus.Approved;

            //领域事件，通知其他模块报告出具
        }
    }
}
