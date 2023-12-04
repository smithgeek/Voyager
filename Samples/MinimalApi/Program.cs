using Voyager;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddVoyager();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(config =>
{
	config.AddVoyager();
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapVoyager();
app.MapSwagger();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();