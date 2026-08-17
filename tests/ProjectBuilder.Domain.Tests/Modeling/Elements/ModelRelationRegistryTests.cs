using ProjectBuilder.Domain.Modeling.Primitives;
using ProjectBuilder.Domain.Modeling.Relations;

namespace ProjectBuilder.Domain.Tests.Modeling.Elements;

public sealed class ModelRelationRegistryTests
{
    [Test]
    public void Every_relation_kind_has_one_complete_descriptor()
    {
        var descriptors = ModelRelationRegistry.All;

        Assert.Multiple(() =>
        {
            Assert.That(descriptors.Select(descriptor => descriptor.Kind), Is.EquivalentTo(Enum.GetValues<ModelRelationKind>()));
            Assert.That(descriptors.Select(descriptor => descriptor.Key), Is.Unique);
            Assert.That(descriptors, Has.All.Property(nameof(ModelRelationDescriptor.AllowedEndpoints)).Not.Empty);
        });

        var benefitsFrom = ModelRelationRegistry.Describe(ModelRelationKind.BenefitsFrom);
        Assert.Multiple(() =>
        {
            Assert.That(benefitsFrom.AllowedEndpoints, Is.EqualTo(new[] { new RelationEndpoint(ModelElementKind.Actor, ModelElementKind.Outcome) }));
            Assert.That(benefitsFrom.Direction, Is.EqualTo(RelationDirection.Directed));
            Assert.That(benefitsFrom.Cardinality, Is.EqualTo(RelationCardinality.OneToMany));
            Assert.That(benefitsFrom.IsUnique, Is.True);
            Assert.That(benefitsFrom.Ownership, Is.EqualTo(RelationOwnership.Target));
            Assert.That(benefitsFrom.DeletionBehavior, Is.EqualTo(RelationDeletionBehavior.Restrict));
            Assert.That(benefitsFrom.AllowsCycles, Is.False);
        });
    }

    [Test]
    public void Invalid_endpoint_combination_cannot_construct_a_committable_relation()
    {
        var result = Create(ModelElementKind.Outcome, ModelElementKind.Actor, Element(1), Element(2));

        Assert.That(result, Is.EqualTo(SemanticResult.Reject<ModelRelationDefinition>(
            "PB-REF-002", "Relation 'benefitsFrom' does not permit Outcome to Actor.")));
    }

    [Test]
    public void Second_beneficiary_for_the_same_outcome_cannot_construct_a_committable_relation()
    {
        var target = Element(3);
        var first = (SemanticResult<ModelRelationDefinition>.Accepted)Create(
            ModelElementKind.Actor, ModelElementKind.Outcome, Element(4), target);

        var result = Create(
            ModelElementKind.Actor, ModelElementKind.Outcome, Element(5), target, [first.Value]);

        Assert.That(result, Is.EqualTo(SemanticResult.Reject<ModelRelationDefinition>(
            "PB-REF-003", "Relation 'benefitsFrom' permits only one source for each target.")));
    }

    private static SemanticResult<ModelRelationDefinition> Create(
        ModelElementKind sourceKind,
        ModelElementKind targetKind,
        ElementId sourceId,
        ElementId targetId,
        IEnumerable<ModelRelationDefinition>? existing = null) =>
        ModelRelationRegistry.Create(
            Relation(1), Project(), ModelRelationKind.BenefitsFrom,
            sourceId, sourceKind, targetId, targetKind,
            UtcTimestamp.Create(new DateTimeOffset(2026, 8, 15, 22, 0, 0, TimeSpan.Zero)),
            "modeler", existing);

    private static ProjectId Project() => Accepted(ProjectId.Parse("0198ad00-0000-7000-8000-000000000801"));
    private static ElementId Element(int seed) => Accepted(ElementId.Parse($"0198ad00-0000-7000-8001-{seed:X12}"));
    private static RelationId Relation(int seed) => Accepted(RelationId.Parse($"0198ad00-0000-7000-8002-{seed:X12}"));
    private static T Accepted<T>(SemanticResult<T> result) where T : notnull =>
        ((SemanticResult<T>.Accepted)result).Value;
}
