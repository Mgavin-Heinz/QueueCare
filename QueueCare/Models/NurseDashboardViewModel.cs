namespace QueueCare.Models
{
    public class NurseDashboardViewModel
    {
        // The patient currently being consulted 
        public QueueItemViewModel? CurrentPatient { get; set; }

        // The patients waiting in the queue
        public List<QueueItemViewModel> UpcomingPatients { get; set; } = new();
    }
}