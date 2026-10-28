namespace QueueCare.Models
{
    public class ReceptionDashboardViewModel
    {
        public int WaitingCount { get; set; }
        public int ServedTodayCount { get; set; }
        public int NoShowCount { get; set; }

        public List<QueueItemViewModel> Queue { get; set; } = new();
    }

    public class QueueItemViewModel
    {
        public int QueueEntryId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = "Waiting";
    }
}