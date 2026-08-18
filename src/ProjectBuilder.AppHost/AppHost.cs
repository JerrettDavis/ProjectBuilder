var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var projectBuilderDatabase = postgres.AddDatabase("projectbuilder");

builder.AddProject<Projects.ProjectBuilder_Web>("web")
    .WithReference(projectBuilderDatabase)
    .WaitFor(projectBuilderDatabase)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
