namespace RaceDay.API.Models;

// Finish time and position for one enrolment after the event.
public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "Finished";   // Finished, DNF, Disqualified

    public Enrolment? Enrolment { get; set; }
}
