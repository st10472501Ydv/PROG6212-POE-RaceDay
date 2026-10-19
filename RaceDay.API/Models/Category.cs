namespace RaceDay.API.Models;

// Age or distance groups inside one event (e.g. "21.1km Half Marathon", "Under 20").
public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = "";
    public int? MaxParticipants { get; set; }   // null or 0 = no limit
    public decimal EntryFee { get; set; }

    public Event? Event { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
