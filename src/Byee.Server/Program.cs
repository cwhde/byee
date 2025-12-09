using Byee.Server.Configuration;
using Byee.Server.Services;
using Byee.Server.Services.Implementations;
using Byee.Server.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddEnvironmentVariables("BYEE_");

// Bind options
builder.Services.Configure<ByeeOptions>(options =>
{
    var config = builder.Configuration;
    
    options.PublicUrl = config["PUBLIC_URL"] ?? config["PublicUrl"] ?? 
        throw new InvalidOperationException("BYEE_PUBLIC_URL environment variable is required");
    
    options.StoragePath = config["STORAGE_PATH"] ?? config["StoragePath"] ?? "./data";
    
    if (long.TryParse(config["MAX_FILE_SIZE"] ?? config["MaxFileSize"], out var maxSize))
        options.MaxFileSize = maxSize;
    
    if (int.TryParse(config["ID_WORD_COUNT"] ?? config["IdWordCount"], out var wordCount))
        options.IdWordCount = wordCount;
});

// Services
builder.Services.AddSingleton<IIdGeneratorService, WordListIdGeneratorService>();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IScriptTemplateService, ScriptTemplateService>();
builder.Services.AddScoped<IFileClaimService, FileClaimService>();

// Background services
builder.Services.AddHostedService<CleanupBackgroundService>();

// Controllers
builder.Services.AddControllers();

// Configure Kestrel for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null; // No limit
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Validate configuration on startup
var byeeOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ByeeOptions>>().Value;
if (string.IsNullOrWhiteSpace(byeeOptions.PublicUrl))
{
    throw new InvalidOperationException("BYEE_PUBLIC_URL environment variable must be set");
}

app.Logger.LogInformation("Byee server starting with public URL: {PublicUrl}", byeeOptions.PublicUrl);

app.UseRouting();
app.MapControllers();

app.Run();
