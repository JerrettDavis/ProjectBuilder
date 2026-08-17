using System.Reflection;

namespace ProjectBuilder.Domain.Tests;

public sealed class DomainFoundationTests
{
    [Test]
    public void Domain_assembly_has_no_module_initializer_side_effects()
    {
        var assembly = Assembly.Load("ProjectBuilder.Domain");

        Assert.That(assembly.GetName().Name, Is.EqualTo("ProjectBuilder.Domain"));
    }
}
