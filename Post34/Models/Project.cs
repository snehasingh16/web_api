namespace Post34.Models;
using Post34.DTOs;

public class Project
{
    public int Id { get; set; }  // ✅ Primary key
    public int project_id { get; set; } = 0;
    public string project_name { get; set; } = string.Empty;
    public List<ServiceItem> used_services_list { get; set; } = new();
}
