namespace RaceDay.API.Models;

// One row for every account, organiser or participant.
public class User
{
    public int UserID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";   // "Organiser" or "Participant"
    public DateTime DateJoined { get; set; }

    public OrganiserDetail? OrganiserDetail { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
