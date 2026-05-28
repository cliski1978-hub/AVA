using AVA.Vault.Core;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Create Vault configuration
var vaultConfig = new VaultInstanceConfig(
    vaultId: "vault-api-node",
    storagePath: "vault_api.db",
    displayName: "AVA Vault API Node");

// Register Vault module
builder.Services.AddVaultModule(vaultConfig);

// Add controllers, Swagger, and logging
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AVA Vault API", Version = "v1" });
});

var app = builder.Build();

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
