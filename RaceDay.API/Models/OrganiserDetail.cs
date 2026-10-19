namespace RaceDay.API.Models;

// Extra club info for users with the Organiser role. PK is also the FK to Users.
public class OrganiserDetail
{
    public int OrganiserID { get; set; }
    public string? ClubName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }

    public User? User { get; set; }
}
