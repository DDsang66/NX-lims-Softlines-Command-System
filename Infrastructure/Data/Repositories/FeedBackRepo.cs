using ClosedXML.Excel;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Interfaces.Controllers;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    public class FeedBackRepo
    {
        private readonly LabDbContextSec _db;

        public FeedBackRepo(LabDbContextSec db)
        {
            _db = db;
        }

        public async Task<string?> Post(FeedBackDto input)
        {
            if (input == null) return "Fail";
            var feedback = new Feeback
            {
                Status = 0,
                Message = input.Message,
                Type = 1,
                CreateTime = DateTime.Now,
                Assignee = input.Assignee,
            };
            return "Success";
        }

        public async Task<object?> Get()
        {
            var feedbacks = _db.Feedbacks.Select(f => f.Status == 0).ToArray();
            return feedbacks;
        }

    }
}
