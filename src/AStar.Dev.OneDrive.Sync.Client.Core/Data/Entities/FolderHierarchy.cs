namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

public class FolderHierarchy
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    
    public FolderHierarchy? Parent { get; set; }

    public int Depth { get; set; }

    public string? FullPath { get; set; }

    public ICollection<FolderHierarchy> Children { get; set; } = [];
}
