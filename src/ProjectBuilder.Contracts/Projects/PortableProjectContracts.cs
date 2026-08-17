namespace ProjectBuilder.Contracts.Projects;

public sealed record ImportProjectRequest(string Document, string Reason);

public sealed record ImportProjectResponse(
    string ProjectId,
    string Name,
    long Revision,
    int ElementCount,
    int RelationCount,
    string ContentHash,
    string ExportUrl);

public sealed record PortableProjectFindingResponse(string Code, string Path, string Message);

public sealed record PortableProjectProblemResponse(
    string Code,
    string Title,
    IReadOnlyList<PortableProjectFindingResponse> Findings);
