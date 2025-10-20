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
            return true;
        }

        public async Task<object?> Get()
        {
            var feedbacks = _db.Feedbacks.ToArray();
            return feedbacks;
        }

    }
}
