using System.Collections.Generic;
using Post34.DTOs;

namespace Post34.Helpers;

public class SeedSettings
{
    public List<SeedProject> Projects { get; set; } = new();
    public List<SeedPermission> Permissions { get; set; } = new();
}

public class SeedProject
{
    public string Name { get; set; } = string.Empty;
    public int project_id { get; set; } = 0;
    public List<ServiceItem> used_services_list { get; set; } = new();
}

public class SeedPermission
{
    public string Username { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public bool CanAccess { get; set; }
}
