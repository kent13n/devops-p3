namespace DataShare.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public ICollection<FileTag> FileTags { get; set; } = [];
}
