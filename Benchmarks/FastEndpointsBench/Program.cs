using FastEndpoints;
using FEBench;

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Services.AddFastEndpoints();
builder.Services.AddScoped<ScopedValidator>();

var app = builder.Build();
app.UseFastEndpoints();
app.Urls.Add("http://0.0.0.0:5000");
app.Run();

namespace FEBench
{
	public partial class Program { }
}