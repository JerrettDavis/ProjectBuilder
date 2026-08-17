using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Tests.Modeling.Primitives;

public sealed class TextPrimitiveTests
{
    [Test]
    public void Element_name_trims_only_outer_whitespace()
    {
        var result = ElementName.Create("  Order  Fulfilment  ");

        Assert.That(Accepted(result).Value, Is.EqualTo("Order  Fulfilment"));
    }

    [Test]
    public void Element_name_preserves_case_and_unicode_normalization()
    {
        const string decomposed = "Cafe\u0301 DOMAIN";

        var result = ElementName.Create(decomposed);

        Assert.That(Accepted(result).Value, Is.EqualTo(decomposed));
    }

    [Test]
    public void Element_name_length_is_measured_in_unicode_code_points()
    {
        var accepted = ElementName.Create(string.Concat(Enumerable.Repeat("😀", ElementName.MaxLength)));
        var rejected = ElementName.Create(string.Concat(Enumerable.Repeat("😀", ElementName.MaxLength + 1)));

        Assert.That(accepted, Is.TypeOf<SemanticResult<ElementName>.Accepted>());
        AssertRejected(rejected, "name.too_long");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Element_name_rejects_missing_content(string? value)
    {
        AssertRejected(ElementName.Create(value), "name.required");
    }

    [Test]
    public void Description_preserves_empty_and_outer_whitespace_as_semantic_content()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Accepted(Description.Create(string.Empty)), Is.EqualTo(Description.Empty));
            Assert.That(Accepted(Description.Create("  context  ")).Value, Is.EqualTo("  context  "));
        });
    }

    [Test]
    public void Description_enforces_the_schema_boundary_in_code_points()
    {
        Assert.That(
            Description.Create(new string('x', Description.MaxLength)),
            Is.TypeOf<SemanticResult<Description>.Accepted>());
        AssertRejected(
            Description.Create(new string('x', Description.MaxLength + 1)),
            "description.too_long");
    }

    [Test]
    public void Change_reason_trims_outer_whitespace_and_preserves_internal_content()
    {
        var result = ChangeReason.Create("  Added  reviewer evidence  ");

        Assert.That(Accepted(result).Value, Is.EqualTo("Added  reviewer evidence"));
    }

    [Test]
    public void Change_reason_enforces_minimum_and_maximum_boundaries()
    {
        Assert.Multiple(() =>
        {
            AssertRejected(ChangeReason.Create("ab"), "reason.too_short");
            Assert.That(ChangeReason.Create("abc"), Is.TypeOf<SemanticResult<ChangeReason>.Accepted>());
            Assert.That(
                ChangeReason.Create(new string('x', ChangeReason.MaxLength)),
                Is.TypeOf<SemanticResult<ChangeReason>.Accepted>());
            AssertRejected(
                ChangeReason.Create(new string('x', ChangeReason.MaxLength + 1)),
                "reason.too_long");
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
