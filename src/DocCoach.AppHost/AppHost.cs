var builder = DistributedApplication.CreateBuilder(args);

// Note: The web app connects to Azurite directly via UseDevelopmentStorage=true
// No need to add Azurite as an Aspire resource since it's managed externally

// Add the web application (connects to Azurite directly via connection string)
var web = builder.AddProject<Projects.DocCoach_Web>("web")
    .WithExternalHttpEndpoints();

// Add dev tunnel for remote testing - exposes the web app publicly
// The tunnel URL will be shown in the Aspire dashboard
builder.AddDevTunnel("tunnel")
    .WithReference(web)
    .WithAnonymousAccess();

builder.Build().Run();
