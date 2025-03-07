var builder = WebApplication.CreateBuilder(args);

// Set up Startup-style configuration
var startup = new Startup(builder.Configuration);

// Configure services (equivalent to ConfigureServices in Startup.cs)
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline (equivalent to Configure in Startup.cs)
startup.Configure(app, app.Environment);

app.Run();
