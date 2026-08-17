using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Tests.Modeling.Primitives;

public sealed class IdentifierTests
{
    [Test]
    public void All_identifier_types_reject_the_empty_identifier()
    {
        AssertRejected(ProjectId.Create(Guid.Empty), "identity.empty");
        AssertRejected(WorkspaceId.Create(Guid.Empty), "identity.empty");
        AssertRejected(ElementId.Create(Guid.Empty), "identity.empty");
        AssertRejected(RelationId.Create(Guid.Empty), "identity.empty");
        AssertRejected(ChangeSetId.Create(Guid.Empty), "identity.empty");
    }

    [Test]
    public void All_identifier_types_reject_noncanonical_external_text()
    {
        const string compactGuid = "0198ad00000070008000000000000001";

        AssertRejected(ProjectId.Parse(compactGuid), "identity.format");
        AssertRejected(WorkspaceId.Parse(compactGuid), "identity.format");
        AssertRejected(ElementId.Parse(compactGuid), "identity.format");
        AssertRejected(RelationId.Parse(compactGuid), "identity.format");
        AssertRejected(ChangeSetId.Parse(compactGuid), "identity.format");
    }

    [Test]
    public void Identifier_round_trip_property_holds_for_representative_nonempty_guids()
    {
        for (var index = 1; index <= 256; index++)
        {
            var guid = Guid.ParseExact(
                $"0198ad00-0000-7000-8000-{index:X12}",
                "D");

            AssertRoundTrip(ProjectId.Create(guid), ProjectId.Parse);
            AssertRoundTrip(WorkspaceId.Create(guid), WorkspaceId.Parse);
            AssertRoundTrip(ElementId.Create(guid), ElementId.Parse);
            AssertRoundTrip(RelationId.Create(guid), RelationId.Parse);
            AssertRoundTrip(ChangeSetId.Create(guid), ChangeSetId.Parse);
        }
    }

    private static void AssertRoundTrip<T>(
        SemanticResult<T> created,
        Func<string?, SemanticResult<T>> parse)
        where T : notnull
    {
        var value = Accepted(created);
        var serialized = value.ToString();
        var reparsed = Accepted(parse(serialized));

        Assert.Multiple(() =>
        {
            Assert.That(serialized, Is.EqualTo(serialized?.ToLowerInvariant()));
            Assert.That(reparsed, Is.EqualTo(value));
        });
    }

    private static T Accepted<T>(SemanticResult<T> result)
        where T : notnull
    {
        Assert.That(result, Is.TypeOf<SemanticResult<T>.Accepted>());
        return ((SemanticResult<T>.Accepted)result).Value;
    }

    private static void AssertRejected<T>(SemanticResult<T> result, string expectedCode)
        where T : notnull
    {
        Assert.That(result, Is.TypeOf<SemanticResult<T>.Rejected>());
        Assert.That(((SemanticResult<T>.Rejected)result).Error.Code, Is.EqualTo(expectedCode));
    }
}
