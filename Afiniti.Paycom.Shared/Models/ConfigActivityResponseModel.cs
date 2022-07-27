using System;

namespace Afiniti.Paycom.Shared.Models
{
    public class ConfigActivityResponseModel
    {
        public int ResponseStatus { get; set; }  
        public string ResponseDescription { get; set; }
    }

    public class ConfigurationActivityModel
    {
        public int ActivityId { get; set; }
        public Guid ActivityKey { get; set; }
        public Guid ConfigUrlkey { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        public string ActivityDetail { get; set; }

    }
}
