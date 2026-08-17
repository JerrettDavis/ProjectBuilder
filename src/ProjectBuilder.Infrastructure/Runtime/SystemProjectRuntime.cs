using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Infrastructure.Runtime;

internal sealed class SystemProjectIdentitySource : IProjectIdentitySource, IModelIdentitySource
{
    public ProjectId NextProjectId() =>
        ((SemanticResult<ProjectId>.Accepted)ProjectId.Create(Guid.CreateVersion7())).Value;

    public ElementId NextElementId() =>
        ((SemanticResult<ElementId>.Accepted)ElementId.Create(Guid.CreateVersion7())).Value;

    public RelationId NextRelationId() =>
        ((SemanticResult<RelationId>.Accepted)RelationId.Create(Guid.CreateVersion7())).Value;
}

internal sealed class SystemApplicationClock : IApplicationClock
{
    public UtcTimestamp GetCurrentTimestamp()
    {
        var now = DateTimeOffset.UtcNow;
        var postgresTicks = now.UtcTicks - (now.UtcTicks % 10);
        return UtcTimestamp.Create(new DateTimeOffset(postgresTicks, TimeSpan.Zero));
    }
}
