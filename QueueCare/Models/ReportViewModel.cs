namespace QueueCare.Models
{
    public class DailyServedCount
    {
        public DateOnly Date {  get; set; }
        public int Count { get; set; }
    }
    
    public class ReportViewModel
    {
        public List<DailyServedCount> ServedPerDay { get; set; } = new(); 
        public double AverageWaitMinutes { get; set; }
        public double NoShowRatePercent { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public int? ServiceId { get; set; }
        public List<Service> Services { get; set; } = new(); 
    }
}
    