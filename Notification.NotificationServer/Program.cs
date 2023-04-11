using Notification.NotificationServer.Backplane.RabbitMq.Extensions;
using System.Reflection;

namespace Notification.NotificationServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            // Add services to the container.
            //builder.Services.AddLogging
            builder.Services.AddControllers();
            builder.Services.AddSignalR()
                            .AddRabbitMqBackplane<NotificationHub>(builder.Configuration.GetSection("RabbitMqBackplane"));

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseAuthorization();

            app.MapHub<NotificationHub>($"/{nameof(NotificationHub)}");
            app.Logger.LogInformation($"Version {Assembly.GetExecutingAssembly().GetName().Version}");
            app.Logger.LogInformation($"Listening @ /{nameof(NotificationHub)}");

            app.MapControllers();

            app.Run();
        }
    }
}