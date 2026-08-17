using ProjectBuilder.Contracts;

namespace ProjectBuilder.Application.Foundation;

public static class FoundationDefinition
{
    public static FoundationResponse Describe(string version, string commit) =>
        new(
            "Project Builder",
            "A definition-first studio for turning a domain into an inspectable, testable, and eventually executable system model.",
            version,
            commit,
            "/health",
            "/alive");
}
