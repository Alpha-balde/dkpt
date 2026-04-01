namespace Dkpt.Domain.Entities;

public class ContributionAmount
{
    public int Year { get; set; }
    public int Amount { get; set; } = 60000;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
