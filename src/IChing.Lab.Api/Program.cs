using IChing.Lab.Inference;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var modelPath = builder.Configuration["Inference:ModelPath"] ?? "./models/qwen3-0.6b-genai";
builder.Services.AddSingleton(sp =>
    new ChartInterpretationService(modelPath, sp.GetRequiredService<ILogger<ChartInterpretationService>>()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program;
