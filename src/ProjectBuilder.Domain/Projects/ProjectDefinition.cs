using System.Collections.Immutable;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Domain.Projects;

public sealed record ProjectDefinition
{
    private ProjectDefinition(
        ProjectId id,
        WorkspaceId workspaceId,
        ElementName name,
        ProjectPurpose purpose,
        IntendedOutcome intendedOutcome,
        Revision revision,
        CreatedProjectChangeSet creation)
    {
        Id = id;
        WorkspaceId = workspaceId;
        Name = name;
        Purpose = purpose;
        IntendedOutcome = intendedOutcome;
        Revision = revision;
        Creation = creation;
    }

    public ProjectId Id { get; }

    public WorkspaceId WorkspaceId { get; }

    public ElementName Name { get; }

    public ProjectPurpose Purpose { get; }

    public IntendedOutcome IntendedOutcome { get; }

    public Revision Revision { get; }

    public CreatedProjectChangeSet Creation { get; }

    public static ProjectDefinition Create(
        ProjectId id,
        WorkspaceId workspaceId,
        ElementName name,
        ProjectPurpose purpose,
        IntendedOutcome intendedOutcome,
        ChangeSetId changeSetId,
        ChangeReason reason,
        UtcTimestamp occurredAt,
        string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        var creation = new CreatedProjectChangeSet(
            changeSetId,
            id,
            Revision.Initial,
            reason,
            occurredAt,
            createdBy,
            [new ProjectChangeOperation.ProjectCreated(0, id, name)]);

        return new ProjectDefinition(id, workspaceId, name, purpose, intendedOutcome, Revision.Initial, creation);
    }

    public static ProjectDefinition Restore(
        ProjectId id,
        WorkspaceId workspaceId,
        ElementName name,
        ProjectPurpose purpose,
        IntendedOutcome intendedOutcome,
        Revision revision,
        CreatedProjectChangeSet creation) =>
        new(id, workspaceId, name, purpose, intendedOutcome, revision, creation);

    internal ProjectDefinition AtRevision(Revision revision) =>
        new(Id, WorkspaceId, Name, Purpose, IntendedOutcome, revision, Creation);
}

public sealed record CreatedProjectChangeSet(
    ChangeSetId Id,
    ProjectId ProjectId,
    Revision ResultRevision,
    ChangeReason Reason,
    UtcTimestamp OccurredAt,
    string CreatedBy,
    ImmutableArray<ProjectChangeOperation> Operations);
