using System.Reflection;
using ProjectBuilder.Domain.Modeling.Primitives;

namespace ProjectBuilder.Domain.Tests.Modeling.Primitives;

public sealed class DomainPurityTests
{
    [Test]
    public void Domain_primitive_contracts_have_no_persistence_transport_or_ui_attributes()
    {
        var forbiddenPrefixes = new[]
        {
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "System.Text.Json",
        };

        var attributedMembers = typeof(ProjectId).Assembly
            .GetExportedTypes()
            .SelectMany(type => type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Cast<MemberInfo>()
                .Append(type))
            .SelectMany(member => member.CustomAttributes.Select(attribute => (member, attribute)))
            .Where(item => forbiddenPrefixes.Any(prefix =>
                item.attribute.AttributeType.Namespace?.StartsWith(prefix, StringComparison.Ordinal) == true))
            .Select(item => $"{item.member.DeclaringType?.FullName}.{item.member.Name}: {item.attribute.AttributeType.FullName}")
            .ToArray();

        Assert.That(attributedMembers, Is.Empty);
    }
}
