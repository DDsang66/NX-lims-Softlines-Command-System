using ClosedXML.Excel;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Interfaces.Controllers;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    public class FeedBackRepo
    {
        private readonly LabDbContextSec _db;

        public FeedBackRepo(LabDbContextSec db)
        {
            _db = db;
        }

        public async Task<bool?> Post(FeedBackDto input)
        {
            if (input == null) return false;
            var snowflake = new SnowflakeIdGenerator();
            long snowId = snowflake.NextId();
            var user = _db.Users.FirstOrDefault(u=>u.UserId == input.UserId!);
            if (user == null) return false;
            try 
            {
                var feedback = new Feedback
                {
                    Id = snowId,
                    Status = "In Process",
                    CreateTime = DateTimeOffset.Now.ToUniversalTime().ToOffset(TimeSpan.FromHours(8)),
                    IsDone = "N",
                    Type = input.Type,
                    FeedbackDetail = input.FeedbackDetail,
                    Applicant = user.NickName
                };
                _db.Feedbacks.Add(feedback);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<FeedBackDtoResponse>>Get()
        {
            var feedbacks = _db.Feedbacks.ToArray();
            var List = new List<FeedBackDtoResponse>();
            foreach (var feedback in feedbacks) 
            {
                var ResponseList = new FeedBackDtoResponse
                {
                    Type = feedback.Type,
                    FeedbackDetail = feedback.FeedbackDetail,
                    Applicant = feedback.Applicant,
                    Status = feedback.Status,
                    UpdateTime = feedback.CreateTime
                };
                if (ResponseList != null) List.Add(ResponseList);
            }
            return List;
        }

    }
}
