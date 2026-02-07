using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TranslateAPI.AppHost.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
