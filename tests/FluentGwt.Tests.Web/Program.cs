using FluentGwt.Tests.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWeb();
var app = builder.Build();
app.MapWeb();
await app.RunAsync();

public partial class Program;
