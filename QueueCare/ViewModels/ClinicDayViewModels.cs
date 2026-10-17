using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QueueCare.ViewModels
{
    /// <summary>One row of the capacity list.</summary>
    public class CapacityRowViewModel
    {
        public int ClinicDayId { get; set; }
        public DateOnly Date { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceCode { get; set; } = string.Empty;
        public TimeOnly OpenTime { get; set; }
        public TimeOnly CloseTime { get; set; }
        public int Capacity { get; set; }

        /// <summary>Booked appointments (not cancelled) plus walk-ins already checked in.</summary>
        public int Used { get; set; }
    }

    /// <summary>The add / edit form for a clinic day. The rules are checked in CapacityRules.</summary>
    public class ClinicDayFormViewModel
    {
        public int ClinicDayId { get; set; }

        public int? ServiceId { get; set; }
        public DateOnly? Date { get; set; }
        public TimeOnly? OpenTime { get; set; }
        public TimeOnly? CloseTime { get; set; }
        public int? Capacity { get; set; }

        // Only used to show information on the Edit page.
        public string ServiceName { get; set; } = string.Empty;
        public int Used { get; set; }

        [ValidateNever]
        public List<SelectListItem> Services { get; set; } = new();
    }
}
