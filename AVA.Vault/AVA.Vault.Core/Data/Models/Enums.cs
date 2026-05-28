namespace AVA.Vault.Core.Data.Models.Enums
{
    public enum VaultRelationType
    {
        Reference,
        Response,
        Attachment,
        Semantic,
        Derived
    }

    public enum VaultEventType
    {
        NoteCreated,
        NoteUpdated,
        NoteDeleted,
        TagAssigned,
        TagRemoved,
        ProjectCreated,
        ProjectUpdated
    }
}
