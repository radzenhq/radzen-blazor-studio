using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMBlazorServerRBS.Models.RadzenCRM
{
    public partial class Opportunity
    {
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}