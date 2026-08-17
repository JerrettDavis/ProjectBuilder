using ProjectBuilder.Application.Modeling;
using ProjectBuilder.Application.Projects.CreateProject;
using ProjectBuilder.Application.Validation.GetProjectFindings;

namespace ProjectBuilder.Application.Tests.Validation;

public sealed class GetProjectFindingsHandlerTests
{
    [Test]
    public void Incomplete_project_produces_stably_ordered_explainable_findings()
    {
        var model = EmptyModel();

        var first = GetProjectFindingsHandler.Evaluate(model);
        var second = GetProjectFindingsHandler.Evaluate(model);

        Assert.Multiple(() =>
        {
            Assert.That(first.Findings.Select(item => item.Code), Is.EqualTo([
                "PB-CONTEXT-001", "PB-OUTCOME-001", "PB-NARR-001", "PB-STATE-011"]));
            Assert.That(first.Findings.Select(item => item.Status), Is.All.EqualTo("Open"));
            Assert.That(first.Findings[0].RepairPath, Does.EndWith("/actors/new"));
            Assert.That(first.Findings, Is.EqualTo(second.Findings));
            Assert.That(first.EvidenceRequirements, Is.EqualTo(second.EvidenceRequirements));
        });
    }

    [Test]
    public void Same_model_has_different_discovery_and_implementation_ready_requirements_without_changing_facts()
    {
        var model = EmptyModel() with
        {
            Actors = [new("actor-1", "Contributor", "person", "Defines the model", [], [], [], [], "known")],
            Outcomes = [new("outcome-1", "Repository is verifiable", "The repository can be verified.", ["Verification passes"], "actor-1", "Contributor", "known")]
        };

        var discovery = GetProjectFindingsHandler.Evaluate(model, "discovery");
        var implementationReady = GetProjectFindingsHandler.Evaluate(model, "implementation-ready");

        Assert.Multiple(() =>
        {
            Assert.That(discovery.Revision, Is.EqualTo(implementationReady.Revision));
            Assert.That(discovery.Profile.Id, Is.EqualTo("discovery"));
            Assert.That(implementationReady.Profile.Id, Is.EqualTo("implementation-ready"));
            Assert.That(discovery.Coverage.Single(item => item.Id == "state").Required, Is.False);
            Assert.That(implementationReady.Coverage.Single(item => item.Id == "state").Required, Is.True);
            Assert.That(discovery.Findings.Single(item => item.Code == "PB-STATE-011").Severity, Is.EqualTo("Info"));
            Assert.That(implementationReady.Findings.Single(item => item.Code == "PB-STATE-011").Severity, Is.EqualTo("Error"));
            Assert.That(discovery.Predicates.Single(item => item.Code == "profile.state").Satisfied, Is.True);
            Assert.That(implementationReady.Predicates.Single(item => item.Code == "profile.state").Satisfied, Is.False);
            Assert.That(implementationReady.Predicates.Single(item => item.Code == "profile.paths").Satisfied, Is.False);
            Assert.That(implementationReady.Predicates.Single(item => item.Code == "profile.evidence").Satisfied, Is.False);
            Assert.That(implementationReady.Coverage.Single(item => item.Id == "paths").Status, Is.EqualTo("Gap"));
            Assert.That(implementationReady.Coverage.Single(item => item.Id == "evidence").Status, Is.EqualTo("Gap"));
        });
    }

    [Test]
    public void Unlinked_result_and_invariant_proof_are_reported_without_inventing_evidence_or_path_state()
    {
        var model = EmptyModel() with
        {
            Actors = [new("actor-1", "Clerk", "person", "Operates the POS", [], [], [], [], "known")],
            StateLogic = [new(
                "state-1", "Transaction", "Domain", ["Status"], ["Open", "Completed"], "Clerk",
                "fact-1", "Transaction status", "TransactionStatus", "Transaction aggregate", "Transitioned",
                "rule-1", "Product may be added", "Eligibility", "Transaction must be open.",
                "Completed transaction rejects lines", "A completed transaction cannot accept a line.",
                "A completed transaction accepts a line.", ["Property test"],
                [new("result-1", "Closed", "Conflict", "The request is rejected.")],
                "transition-1", "Add product", "Status is Open", "Clerk submits product", "Status remains Open")]
        };

        var result = GetProjectFindingsHandler.Evaluate(model);

        Assert.Multiple(() =>
        {
            Assert.That(result.Findings.Any(item => item.Code == "PB-PATH-008" && item.ScopeId == "result-1"), Is.True);
            Assert.That(result.Findings.Any(item => item.Code == "PB-EVID-001" && !item.RepairAvailable), Is.True);
            Assert.That(result.EvidenceRequirements.Single().Status, Is.EqualTo("Required"));
        });
    }

    [Test]
    public void Governed_disposition_is_projected_without_hiding_the_unsatisfied_finding()
    {
        var model = EmptyModel() with
        {
            GapDispositions = [new(
                "disposition-1", "implementation-ready", "PB-STATE-011", "project-1", "Deferred",
                "State semantics move to the next bounded slice.", "Implementation remains blocked.",
                "actor-1", "Reviewer", "2026-09-30", "C11", "2026-08-16T00:00:00Z", "local-reviewer")]
        };

        var result = GetProjectFindingsHandler.Evaluate(model, "implementation-ready");
        var finding = result.Findings.Single(item => item.Code == "PB-STATE-011");

        Assert.Multiple(() =>
        {
            Assert.That(finding.Status, Is.EqualTo("Deferred"));
            Assert.That(finding.AuthorityName, Is.EqualTo("Reviewer"));
            Assert.That(finding.TargetMilestone, Is.EqualTo("C11"));
            Assert.That(result.Predicates.Single(item => item.Code == "profile.state").Satisfied, Is.False);
        });
    }

    private static ProjectModelOverview EmptyModel() => new(
        new ProjectOverview("project-1", "workspace-1", "Incomplete", "Expose gaps", "A reviewable model", 1,
            "Create incomplete model", "2026-08-16T00:00:00Z"),
        [], [], [], [], [], [], []);
}
