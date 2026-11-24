var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var projectBuilderDatabase = postgres.AddDatabase("projectbuilder");

builder.AddDotnetProject("web", "../ProjectBuilder.Web/ProjectBuilder.Web.csproj")
    .WithReference(projectBuilderDatabase)
    .WaitFor(projectBuilderDatabase)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
