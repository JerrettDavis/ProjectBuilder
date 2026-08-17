using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Tests.Modeling.Primitives;

public sealed class RevisionAndTimestampTests
{
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(long.MinValue)]
    public void Revision_rejects_values_before_the_initial_revision(long value)
    {
        AssertRejected(Revision.Create(value), "revision.out_of_range");
    }

    [Test]
    public void Revision_round_trips_using_invariant_decimal_text()
    {
        var revision = Accepted(Revision.Create(9_223_372_036_854_775_000));

        Assert.Multiple(() =>
        {
            Assert.That(revision.ToString(), Is.EqualTo("9223372036854775000"));
            Assert.That(Accepted(Revision.Parse(revision.ToString())), Is.EqualTo(revision));
            Assert.That(Revision.Initial.Value, Is.EqualTo(1));
        });
    }

    [Test]
    public void Timestamp_normalizes_an_explicit_offset_to_utc_and_round_trips()
    {
        var timestamp = Accepted(UtcTimestamp.Parse("2026-08-15T14:30:00.0000000-05:00"));

        Assert.Multiple(() =>
        {
            Assert.That(timestamp.ToString(), Is.EqualTo("2026-08-15T19:30:00.0000000Z"));
            Assert.That(Accepted(UtcTimestamp.Parse(timestamp.ToString())), Is.EqualTo(timestamp));
            Assert.That(timestamp.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [TestCase(null)]
    [TestCase("2026-08-15T19:30:00.0000000")]
    [TestCase("2026-08-15")]
    public void Timestamp_rejects_text_without_an_explicit_offset(string? value)
    {
        AssertRejected(UtcTimestamp.Parse(value), "timestamp.format");
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
