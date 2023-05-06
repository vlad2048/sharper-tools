var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/reqTest", (int val, bool crash) =>
{
	Console.WriteLine($"reqTest <- {val}");
	if (crash)
		throw new ArgumentException("Induced crash");
	return val;
});

app.Run();
