using AiAssistant.Application.Features.Ask;
using AiAssistant.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Application layer — MediatR scans the assembly containing AskHandler
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<AskHandler>());

// Infrastructure layer — Ollama + InMemoryKnowledgeStore
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Expose Program for integration test WebApplicationFactory
public partial class Program { }
