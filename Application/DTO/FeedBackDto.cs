namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class FeedBackDto
    {
        public string? Type { get; set; }
        public string? FeedbackDetail { get; set; }
        public string? UserId { get; set; }
    }


    public class FeedBackDtoResponse
    {
        public string? Type { get; set; }//反馈类型
        public string? FeedbackDetail { get; set; }//反馈详情
        public string? Applicant { get; set; }//申请人
        public string? Status { get; set; }//处理状态
        public DateTimeOffset? UpdateTime { get; set; }//更新时间
    }
}
