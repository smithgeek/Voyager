using Voyager;
using Voyager.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddVoyager();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
	config.DocumentFilter<VoyagerOpenApiDocumentFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapVoyager();
app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();