namespace Post34.Models;

public class ProjectPermission
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public bool CanAccess { get; set; }
}
