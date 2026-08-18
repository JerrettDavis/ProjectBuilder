using System.Reflection;
using System.Xml.Linq;

namespace ProjectBuilder.Architecture.Tests;

public sealed class RepositoryArchitectureTests
{
    private static readonly string[] ForbiddenNamespaceSegments = ["Services", "Managers", "Helpers", "Utils"];
    private static readonly string[] RepositoryProjectAreas = ["src", "tests"];

    private static readonly IReadOnlyDictionary<string, string[]> AllowedReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ProjectBuilder.Domain"] = [],
            ["ProjectBuilder.Contracts"] = [],
            ["ProjectBuilder.Application"] = ["ProjectBuilder.Contracts", "ProjectBuilder.Domain"],
            ["ProjectBuilder.Infrastructure"] = ["ProjectBuilder.Application", "ProjectBuilder.Domain"],
            ["ProjectBuilder.Projections"] = ["ProjectBuilder.Contracts"],
            ["ProjectBuilder.ServiceDefaults"] = [],
            ["ProjectBuilder.Web.Client"] = ["ProjectBuilder.Contracts"],
            ["ProjectBuilder.Web"] = ["ProjectBuilder.Application", "ProjectBuilder.Contracts", "ProjectBuilder.Infrastructure", "ProjectBuilder.Projections", "ProjectBuilder.ServiceDefaults", "ProjectBuilder.Web.Client"],
            ["ProjectBuilder.AppHost"] = ["ProjectBuilder.Web"]
        };

    [Test]
    public void Source_project_reference_graph_matches_the_approved_direction()
    {
        var root = FindRepositoryRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);
        var actual = projects.ToDictionary(
            path => Path.GetFileNameWithoutExtension(path),
            ReadProjectReferences,
            StringComparer.Ordinal);

        var violations = DependencyRules.FindViolations(actual, AllowedReferences);

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Domain_references_only_approved_BCL_assemblies()
    {
        var references = Assembly.Load("ProjectBuilder.Domain")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.That(references.Any(reference => reference.StartsWith("ProjectBuilder.", StringComparison.Ordinal)), Is.False);
        Assert.That(references.Any(reference => reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)), Is.False);
        Assert.That(references.Any(reference => reference.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)), Is.False);
        Assert.That(references.Any(reference => reference.StartsWith("Npgsql", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void Provider_packages_do_not_leak_outside_infrastructure_and_apphost()
    {
        var root = FindRepositoryRoot();
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var project in projects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            var packages = XDocument.Load(project)
                .Descendants("PackageReference")
                .Select(element => (string?)element.Attribute("Include") ?? string.Empty);

            foreach (var package in packages.Where(package =>
                         package.Contains("EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                         || package.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(projectName, "ProjectBuilder.Infrastructure", StringComparison.Ordinal))
                {
                    violations.Add($"{projectName} contains provider package {package}.");
                }
            }
        }

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Package_versions_exist_only_in_the_central_package_file()
    {
        var root = FindRepositoryRoot();
        var projects = RepositoryProjectAreas
            .SelectMany(area => Directory.GetFiles(Path.Combine(root, area), "*.csproj", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        var versionedReferences = projects
            .SelectMany(project => XDocument.Load(project)
                .Descendants("PackageReference")
                .Where(element => element.Attribute("Version") is not null)
                .Select(element => $"{project}: {element}"))
            .ToArray();

        Assert.That(versionedReferences, Is.Empty, string.Join(Environment.NewLine, versionedReferences));
    }

    [Test]
    public void Generic_dumping_ground_directories_do_not_exist()
    {
        var root = FindRepositoryRoot();
        var violations = Directory.GetDirectories(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => ForbiddenNamespaceSegments.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .ToArray();

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Public_surface_uses_owned_namespaces_and_avoids_dumping_ground_names()
    {
        var assemblies = new[]
        {
            "ProjectBuilder.Domain",
            "ProjectBuilder.Contracts",
            "ProjectBuilder.Application",
            "ProjectBuilder.Infrastructure",
            "ProjectBuilder.Projections"
        };

        var violations = assemblies
            .SelectMany(name => Assembly.Load(name).ExportedTypes)
            .Where(type => type.Namespace is null
                || !type.Namespace.StartsWith("ProjectBuilder.", StringComparison.Ordinal)
                || ForbiddenNamespaceSegments.Any(segment =>
                    type.Namespace.Split('.').Contains(segment, StringComparer.Ordinal)))
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void Intentional_domain_to_infrastructure_violation_is_caught()
    {
        var invalidGraph = AllowedReferences.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
        invalidGraph["ProjectBuilder.Domain"] = ["ProjectBuilder.Infrastructure"];

        var violations = DependencyRules.FindViolations(invalidGraph, AllowedReferences);

        Assert.That(violations, Does.Contain(
            "ProjectBuilder.Domain has forbidden reference ProjectBuilder.Infrastructure."));
    }

    private static string[] ReadProjectReferences(string projectPath) =>
        XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFileNameWithoutExtension(path!.Replace('\\', '/')))
            .Order(StringComparer.Ordinal)
            .ToArray()!;

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ProjectBuilder.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}

internal static class DependencyRules
{
    public static string[] FindViolations(
        IReadOnlyDictionary<string, string[]> actual,
        IReadOnlyDictionary<string, string[]> allowed)
    {
        var violations = new List<string>();

        foreach (var expectedProject in allowed.Keys.Except(actual.Keys, StringComparer.Ordinal))
        {
            violations.Add($"Missing source project {expectedProject}.");
        }

        foreach (var (project, references) in actual)
        {
            if (!allowed.TryGetValue(project, out var approved))
            {
                violations.Add($"Unapproved source project {project}.");
                continue;
            }

            foreach (var reference in references.Except(approved, StringComparer.Ordinal))
            {
                violations.Add($"{project} has forbidden reference {reference}.");
            }

            foreach (var missing in approved.Except(references, StringComparer.Ordinal))
            {
                violations.Add($"{project} is missing required reference {missing}.");
            }
        }

        return violations.Order(StringComparer.Ordinal).ToArray();
    }
}
