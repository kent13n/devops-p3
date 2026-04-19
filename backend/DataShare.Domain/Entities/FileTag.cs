namespace DataShare.Domain.Entities;

public class FileTag
{
    public Guid FileId { get; set; }
    public Guid TagId { get; set; }

    public StoredFile File { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
