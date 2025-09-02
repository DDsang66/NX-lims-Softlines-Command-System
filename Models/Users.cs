using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models
{
    public partial class Users
    {
        [Key]
        public long Id { get; set; }

        public string? UserName { get; set; }

        public string? PassWord { get; set; }

        public string? Name { get; set; }

        public string? CVV { get; set; }
    }
}