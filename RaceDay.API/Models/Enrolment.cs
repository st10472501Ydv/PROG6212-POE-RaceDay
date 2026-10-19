namespace RaceDay.API.Models;

// Links a participant to the category they entered. One row per entry.
public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrolmentDate { get; set; }

    public User? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}
