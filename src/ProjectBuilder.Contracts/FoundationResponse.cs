namespace ProjectBuilder.Contracts;

public sealed record FoundationResponse(
    string Name,
    string Purpose,
    string Version,
    string Commit,
    string ReadinessEndpoint,
    string LivenessEndpoint);
