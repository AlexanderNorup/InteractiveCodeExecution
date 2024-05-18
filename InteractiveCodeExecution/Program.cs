using Docker.DotNet;
using InteractiveCodeExecution.ExecutorEntities;
using InteractiveCodeExecution.Hubs;
using InteractiveCodeExecution.Services;
using MessagePack;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace InteractiveCodeExecution
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSwaggerGen();
            builder.Services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                opts.JsonSerializerOptions.PropertyNamingPolicy = null; // Ensures the JSON objects are named as our fields (like with messagepack)
            });
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR()
                 .AddMessagePackProtocol(options =>
                 {
                     options.SerializerOptions = MessagePackSerializerOptions.Standard
                         .WithSecurity(MessagePackSecurity.UntrustedData);
                 });

            var config = new DockerClientConfiguration();
            builder.Services.AddSingleton(config);

            builder.Services.AddSingleton<RequestThrottler>();
            builder.Services.Configure<DockerConfiguration>(builder.Configuration.GetSection("InteractiveCodeExecution"));
            builder.Services.AddSingleton<IExecutorAssignmentProvider, PoCAssignmentProvider>();
            builder.Services.AddSingleton<IExecutorAssignmentSubmissionHandler, PoCAssignmentSubmissionHandler>();
            builder.Services.AddSingleton<IExecutorController, DockerController>();

            builder.Services.AddSingleton<VNCHelper>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.MapHub<ExecutorHub>("/executorHub");
            app.MapHub<VncHub>("/vncHub");

            app.MapControllers();

            app.Run();
        }
    }
}
