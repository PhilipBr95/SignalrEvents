using Microsoft.Extensions.DependencyInjection;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.RabbitMq.Extensions;
using Notification.NotifierLibrary;
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
            builder.Services.AddHostedService<StartupService>();
            builder.Services.AddControllers();
            builder.Services.AddSignalR()
                            .AddRabbitMqBackplane<NotificationHub, NotifierEventArgs>(builder.Configuration.GetSection("RabbitMqBackplane"));

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

    public class StartupService : IHostedService
    {
        private IServiceProvider _services;
        public StartupService(IServiceProvider services)
        {
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //Check Rabbit
            var backplane = _services.GetRequiredService<IBackplane<NotificationHub, NotifierEventArgs>>();
            var logger = _services.GetRequiredService<ILogger<StartupService>>();

            logger.LogInformation($"Forcing RabbitMq connect: {backplane.ConsumerTag}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

}