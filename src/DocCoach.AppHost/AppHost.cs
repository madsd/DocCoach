var builder = DistributedApplication.CreateBuilder(args);

// Note: The web app connects to Azurite directly via UseDevelopmentStorage=true
// No need to add Azurite as an Aspire resource since it's managed externally

// Add the web application (connects to Azurite directly via connection string)
builder.AddProject<Projects.DocCoach_Web>("web")
    .WithExternalHttpEndpoints();

builder.Build().Run();
