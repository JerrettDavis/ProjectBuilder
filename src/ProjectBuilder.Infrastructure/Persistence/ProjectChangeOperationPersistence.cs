using System.Diagnostics;
using System.Text.Json;
using ProjectBuilder.Domain.Modeling.Gaps;
using ProjectBuilder.Domain.Modeling.Relations;
using ProjectBuilder.Domain.Modeling.Traceability;
using ProjectBuilder.Domain.Modeling.Transitions;

namespace ProjectBuilder.Infrastructure.Persistence;

internal static class ProjectChangeOperationPersistence
{
    internal static void Attach(
        ProjectChangeSetRecord changeSet,
        IEnumerable<ProjectChangeOperation> operations)
    {
        foreach (var operation in operations.OrderBy(candidate => candidate.Sequence))
            changeSet.Operations.Add(Record(changeSet, operation));
    }

    private static ProjectChangeOperationRecord Record(
        ProjectChangeSetRecord changeSet,
        ProjectChangeOperation operation) => operation switch
        {
            ProjectChangeOperation.ProjectCreated created => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = created.Sequence,
                Kind = "project.created",
                SubjectKind = "project",
                Summary = $"Created project '{created.Name.Value}'.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    projectId = created.ProjectId.Value,
                    name = created.Name.Value,
                }),
            },
            ProjectChangeOperation.ElementAdded added => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = added.Sequence,
                Kind = "element.added",
                SubjectKind = ElementKind(added.ElementKind),
                ElementId = added.ElementId.Value,
                Summary = $"Added {ElementKind(added.ElementKind)} '{added.Name.Value}'.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    elementId = added.ElementId.Value,
                    elementKind = ElementKind(added.ElementKind),
                    name = added.Name.Value,
                }),
            },
            ProjectChangeOperation.ElementUpdated updated => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = updated.Sequence,
                Kind = "element.updated",
                SubjectKind = ElementKind(updated.ElementKind),
                ElementId = updated.ElementId.Value,
                Summary = $"Updated {ElementKind(updated.ElementKind)} '{updated.Name.Value}'.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    elementId = updated.ElementId.Value,
                    elementKind = ElementKind(updated.ElementKind),
                    previousName = updated.PreviousName.Value,
                    name = updated.Name.Value,
                }),
            },
            ProjectChangeOperation.RelationAdded added => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = added.Sequence,
                Kind = "relation.added",
                SubjectKind = ModelRelationRegistry.Describe(added.RelationKind).Key,
                RelationId = added.RelationId.Value,
                Summary = $"Added {ModelRelationRegistry.Describe(added.RelationKind).DisplayName} relation.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    relationId = added.RelationId.Value,
                    relationKind = ModelRelationRegistry.Describe(added.RelationKind).Key,
                    sourceElementId = added.SourceElementId.Value,
                    targetElementId = added.TargetElementId.Value,
                }),
            },
            ProjectChangeOperation.RelationUpdated updated => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = updated.Sequence,
                Kind = "relation.updated",
                SubjectKind = ModelRelationRegistry.Describe(updated.RelationKind).Key,
                RelationId = updated.RelationId.Value,
                Summary = $"Updated {ModelRelationRegistry.Describe(updated.RelationKind).DisplayName} relation.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    relationId = updated.RelationId.Value,
                    relationKind = ModelRelationRegistry.Describe(updated.RelationKind).Key,
                    previousSourceElementId = updated.PreviousSourceElementId.Value,
                    sourceElementId = updated.SourceElementId.Value,
                    targetElementId = updated.TargetElementId.Value,
                }),
            },
            ProjectChangeOperation.GapDispositionRecorded disposition => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = disposition.Sequence,
                Kind = "gap.disposition.recorded",
                SubjectKind = "gapDisposition",
                ElementId = disposition.ScopeId.Value,
                Summary = $"Recorded {Disposition(disposition.Disposition)} for {disposition.RuleCode}.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    dispositionId = disposition.DispositionId.Value,
                    scopeId = disposition.ScopeId.Value,
                    ruleCode = disposition.RuleCode,
                    disposition = disposition.Disposition.ToString(),
                }),
            },
            ProjectChangeOperation.ClaimAdded claim => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = claim.Sequence,
                Kind = "claim.added",
                SubjectKind = "claim",
                ElementId = claim.ScopeId.Value,
                Summary = $"Added {LowerFirst(claim.ClaimKind.ToString())} claim.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    claimId = claim.ClaimId.Value,
                    scopeId = claim.ScopeId.Value,
                    claimKind = LowerFirst(claim.ClaimKind.ToString())
                }),
            },
            ProjectChangeOperation.EvidenceAdded evidence => new()
            {
                ChangeSetId = changeSet.Id,
                ProjectId = changeSet.ProjectId,
                Sequence = evidence.Sequence,
                Kind = "evidence.added",
                SubjectKind = "evidence",
                Summary = $"Added {LowerFirst(evidence.EvidenceKind.ToString())} evidence.",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    evidenceId = evidence.EvidenceId.Value,
                    claimId = evidence.ClaimId.Value,
                    evidenceKind = LowerFirst(evidence.EvidenceKind.ToString())
                }),
            },
            _ => throw new UnreachableException(),
        };

    private static string Disposition(GapDispositionKind kind) => kind switch
    {
        GapDispositionKind.AcceptedRisk => "accepted risk",
        GapDispositionKind.NotApplicable => "not applicable",
        _ => kind.ToString().ToLowerInvariant(),
    };
    private static string LowerFirst(string value) => char.ToLowerInvariant(value[0]) + value[1..];

    private static string ElementKind(ModelElementKind kind) => kind switch
    {
        ModelElementKind.Actor => "actor",
        ModelElementKind.Outcome => "outcome",
        ModelElementKind.Capability => "capability",
        ModelElementKind.Episode => "episode",
        ModelElementKind.Scenario => "scenario",
        ModelElementKind.Scene => "scene",
        ModelElementKind.Interaction => "interaction",
        ModelElementKind.Intent => "intent",
        ModelElementKind.Step => "step",
        ModelElementKind.Observation => "observation",
        ModelElementKind.StateDefinition => "stateDefinition",
        ModelElementKind.FactDefinition => "factDefinition",
        ModelElementKind.RuleDefinition => "ruleDefinition",
        ModelElementKind.InvariantDefinition => "invariantDefinition",
        ModelElementKind.ResultDefinition => "resultDefinition",
        ModelElementKind.TransitionDefinition => "transitionDefinition",
        ModelElementKind.Path => "path",
        ModelElementKind.Condition => "condition",
        ModelElementKind.EffectDefinition => "effectDefinition",
        ModelElementKind.System => "system",
        ModelElementKind.Interface => "interface",
        ModelElementKind.Boundary => "boundary",
        ModelElementKind.Contract => "contract",
        _ => throw new UnreachableException(),
    };
}
