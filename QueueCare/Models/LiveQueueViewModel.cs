namespace QueueCare.Models
{
    public class LiveQueueViewModel
    {
        public int Position { get; set; }
        public int EstimatedWaitMinutes { get; set; }
        public string NowServingTicket { get; set; } = string.Empty;
        public string YourTicket { get; set; } = string.Empty;

        // 1 = Waiting, 2 = Called, 3 = In Consultation, 4 = Served
        public int CurrentStage { get; set; }
    }
}
