namespace TurnTrackerAspNetCore.Entities
{
    public class Participant
    {
        public long TaskId { get; set; }
        public TrackedTask Task { get; set; }
        
        public string UserId { get; set; }
        public User User { get; set; }

        public int Offset { get; set; }
    }
}
