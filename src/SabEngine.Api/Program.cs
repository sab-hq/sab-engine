// SabEngine.Api — composition root and CLI/API entry point.
// See docs/SAB_Design_Document_v0.1.2.md, Section 4.1.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SabEngine.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<SabEngineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SabEngine")));

// Real DI wiring for the orchestration engine, agent, and execution
// connectors lands as part of later pre-development-checklist.md items,
// not this schema-scaffolding commit (PD-3).

using var host = builder.Build();

Console.WriteLine("sab-engine — Engine State Store connection registered. See pre-development-checklist.md for what's implemented so far.");
