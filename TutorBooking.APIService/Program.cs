using TutorBooking.APIService;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    
    // Điều chỉnh kích thước thread pool để tối ưu hoá hiệu suất trên Heroku
    // - SetMinThreads: Đặt số thread tối thiểu = số CPU * 2 (worker) và số CPU * 1 (I/O)
    // - SetMaxThreads: Giới hạn thread tối đa = số CPU * 8 (worker) và số CPU * 4 (I/O)
    // Mục đích: Tránh việc tạo quá nhiều thread gây tốn tài nguyên trên môi trường Heroku với tài nguyên hạn chế
    ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount);
    ThreadPool.SetMaxThreads(Environment.ProcessorCount * 8, Environment.ProcessorCount * 4);
}

#region Config Builder
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
    
// Thêm cấu hình logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning); // Chỉ log Warning trở lên trong production
#endregion

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

app.Run();