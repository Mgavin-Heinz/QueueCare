namespace QueueCare.Models
{
    public class NurseDashboardViewModel
    {
        // The patient currently being consulted (or null if nobody)
        public QueueItemViewModel? CurrentPatient { get; set; }

        // The patients waiting in the queue
        public List<QueueItemViewModel> UpcomingPatients { get; set; } = new();
    }

    // Temporary definition until the Reception PR merges to dev
    public class QueueItemViewModel
    {
        public int QueueEntryId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = "Waiting";
    }
}