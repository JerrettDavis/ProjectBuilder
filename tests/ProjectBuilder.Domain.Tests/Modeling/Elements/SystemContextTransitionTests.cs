using ProjectBuilder.Domain.Modeling.Elements;
using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Transitions;
using ProjectBuilder.Domain.Projects;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class SystemContextTransitionTests
{
    [Test]
    public void Context_packet_commits_five_typed_definitions_atomically()
    {
        var result = SystemContextTransition.Define(Project(), RevisionValue(4),
            new(Element(1), Element(2), Element(3), Element(4), Element(5)), Draft(), 40,
            ChangeSet(5), Reason(), Now, "architect");
        var accepted = (DefineSystemContextTransitionResult.Accepted)result;
        Assert.Multiple(() =>
        {
            Assert.That(accepted.Project.Revision.Value, Is.EqualTo(5));
            Assert.That(accepted.Definitions.Elements, Has.Length.EqualTo(5));
            Assert.That(accepted.Definitions.Interface.ParticipantIds, Does.Contain(accepted.Definitions.ExternalSystem.Id));
            Assert.That(accepted.Definitions.Boundary.Kinds, Is.EqualTo([BoundaryKind.Ownership, BoundaryKind.Trust]));
            Assert.That(accepted.ChangeSet.Operations, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void Known_external_authority_cannot_silently_equal_owned_authority()
    {
        var draft = Draft() with { ExternalSystemOwnerId = Element(90) };
        var result = SystemContextTransition.Define(Project(), RevisionValue(4),
            new(Element(1), Element(2), Element(3), Element(4), Element(5)), draft, 40,
            ChangeSet(5), Reason(), Now, "architect");
        Assert.That(((DefineSystemContextTransitionResult.Invalid)result).Errors.Select(error => error.Code), Does.Contain("PB-SYS-002"));
    }

    private static SystemContextDraft Draft() => new(Name("Project Builder"), DescriptionValue("Modeling studio"), Element(90), [Term("Preserve semantic truth")],
        Name("PostgreSQL"), DescriptionValue("Persistence authority"), Element(91), [Term("Store durable records")], KnowledgeStatus.Known,
        Name("Project API"), DescriptionValue("Stable boundary"), InterfaceKind.Http, [Element(90)], [Term("Commit a change")], [Term("Committed revision")], [Term("Keyboard equivalent")],
        Name("Persistence boundary"), DescriptionValue("Owned to external crossing"), [BoundaryKind.Ownership, BoundaryKind.Trust], [Element(90), Element(91)], KnowledgeStatus.Known, null,
        Name("Persistence contract"), DescriptionValue("Governs movement"), ContractKind.Api, Term("1"), Element(90), Statement("Unknown"), Statement("Breaking changes require a version change"), Term("Typed change set"), Term("Committed revision"), Term("Internal metadata"), KnowledgeStatus.Known);
    private static ProjectDefinition Project() { var project = ProjectDefinition.Create(ProjectIdValue(), Accepted(WorkspaceId.Parse("0198ad00-0000-7000-8000-000000000700")), Name("Project Builder"), Accepted(ProjectPurpose.Create("Model meaning.")), Accepted(IntendedOutcome.Create("A contributor verifies the repository.")), ChangeSet(1), Reason(), Now, "architect"); return ProjectDefinition.Restore(project.Id, project.WorkspaceId, project.Name, project.Purpose, project.IntendedOutcome, RevisionValue(4), project.Creation); }
    private static readonly UtcTimestamp Now = UtcTimestamp.Create(new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero));
    private static ProjectId ProjectIdValue() => Accepted(ProjectId.Parse("0198ad00-0000-7000-8100-000000000001")); private static ElementId Element(int n) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8500-{n:X12}")); private static ChangeSetId ChangeSet(int n) => Accepted(ChangeSetId.Parse($"0198ad00-0000-7000-9500-{n:X12}")); private static Revision RevisionValue(long n) => Accepted(Revision.Create(n)); private static ChangeReason Reason() => Accepted(ChangeReason.Create("Define explicit context.")); private static ElementName Name(string v) => Accepted(ElementName.Create(v)); private static Description DescriptionValue(string v) => Accepted(Description.Create(v)); private static LogicTerm Term(string v) => Accepted(LogicTerm.Create(v)); private static LogicStatement Statement(string v) => Accepted(LogicStatement.Create(v)); private static T Accepted<T>(SemanticResult<T> result) where T : notnull => ((SemanticResult<T>.Accepted)result).Value;
}
