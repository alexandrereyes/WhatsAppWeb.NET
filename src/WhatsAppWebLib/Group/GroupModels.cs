namespace WhatsAppWebLib.Group;

public class GroupMetadata
{
    public string? Owner { get; set; }
    public long? Creation { get; set; }
    public string? Description { get; set; }
    public List<GroupParticipant>? Participants { get; set; }
}

public class GroupParticipant
{
    public string? Id { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
}
