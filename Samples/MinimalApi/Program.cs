using Voyager;
using Voyager.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddVoyager(config =>
{
	config.AddFluentValidation();
	config.AddValidot();
});
builder.Services.AddSingleton<Service>();
builder.Services.AddOpenApi(c => c.AddVoyager());

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapVoyager();
app.MapOpenApi();
app.Run();

namespace MinimalApi
{
	public partial class Program { }
}