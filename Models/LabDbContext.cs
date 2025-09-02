using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Models
{
    public class LabDbContext : DbContext
    {

        public LabDbContext(DbContextOptions<LabDbContext> options) : base(options) 
        {

        }

        public  DbSet<Buyer> Buyer { get; set; }
        public  DbSet<Items> Item { get; set; }
        public  DbSet<Menus> Menu { get; set; }
        public DbSet<Composition> Composition { get; set; }
        public  DbSet<Standards> Standards { get; set; }
        public  DbSet<DryProcedure> DryProcedure { get; set; }
        public  DbSet<ParameterType> ParameterType { get; set; }
        public  DbSet<Program> Program { get; set; }
        public  DbSet<SpecialCareInstruction> SpecialCareInstruction { get; set; }
        public  DbSet<WashingProcedure> WashingProcedure { get; set; }
        public  DbSet<WetParameters> WetParameters { get; set; }
        public  DbSet<Users> User { get; set; }
        public  DbSet<Parameter> Parameter { get; set; }
        public  DbSet<AdidasMtoI> AdidasMtoI { get; set; }
    }
}