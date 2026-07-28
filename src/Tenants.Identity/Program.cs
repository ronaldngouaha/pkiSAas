using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Acme.Pki.Tenants.Identity.Data;
using Acme.Pki.Tenants.Identity.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conn = builder.Configuration.GetConnectionString("Default") ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");
builder.Services.AddDbContext<TenantsIdentityDbContext>(options => options.UseSqlServer(conn));

builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddHealthChecks().AddDbContextCheck<TenantsIdentityDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();