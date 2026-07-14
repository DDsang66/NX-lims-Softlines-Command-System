using AutoMapper;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NX_lims_Softlines_Command_System.Application;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.UserService;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.FeedBackRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.OrderRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.RenderRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Order;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.src.Domain;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil;
using OfficeOpenXml;
using System.Reflection;
using System.Text;

namespace NX_lims_Softlines_Command_System
{
    public class Program
    {
        public static void Main(string[] args)
       {
            var builder = WebApplication.CreateBuilder(args);
            var jwt = builder.Configuration.GetSection("Jwt");

            builder.Services.AddAutoRegister(
                ApplicationAssemblyMarker.Assembly,
                InfrastructureAssemblyMarker.Assembly,
                DomainAssemblyMarker.Assembly);

            TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

            builder.Services.AddMapster();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                typeof(ApplicationAssemblyMarker).Assembly,
                typeof(InfrastructureAssemblyMarker).Assembly));

            builder.Services.AddHostedService<EventPublisherBackgroundService>();

            // Add services to the container.
            var licenseType = builder.Configuration.GetValue<string>("EPPlus:License");
            ExcelPackage.License.SetNonCommercialPersonal("GuangXv Chen");
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddScoped<IBuyerFactory, BuyerFactory>();
            builder.Services.AddScoped<FiberContentHelper>();
            builder.Services.AddScoped<IPrintExcelStrategyFactory, PrintExcelStrategyFactory>();
            builder.Services.AddScoped<ExcelHelper>();

            builder.Services.AddScoped<FeedBackService>();
            builder.Services.AddScoped<FeedBackRepo>();
            builder.Services.AddScoped<OrderRepo>();
            builder.Services.AddScoped<OrderQueryProvider>();
            builder.Services.AddScoped<OrderReportingQueryProvider>();
            builder.Services.AddSingleton<JwtService>();
            builder.Services.AddScoped<RenderService>();
            builder.Services.AddScoped<RenderRepos>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwt["Issuer"],
                        ValidAudience = jwt["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!))
                    };
                });

            var strategyTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IPrintExcelStrategy).IsAssignableFrom(t));

            foreach (var impl in strategyTypes)
                builder.Services.AddScoped(impl);


            builder.Services.AddCors(options =>
            {
                options.AddPolicy("VueDev", policy =>
                {
                    policy.WithOrigins("http://localhost:5173",
                                       "http://localhost:82",
                                       "http://localhost:81",
                                       "http://192.168.3.6:82",
                                       "http://192.168.3.6:81",
                                       "http://192.168.3.6:5051",
                                        "http://192.168.76.8:5173",
                                       "https://TheProductionDomain.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // �� JWT/ Cookie �ɱ���
                });
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<LabDbContextSec>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("NX-limsLabCommandSys")));

            builder.Services.AddDbContext<dbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("NX-lims")));

            var app = builder.Build();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "swagger";   // Ĭ�Ͼ��� swagger
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseExceptionHandler(builder => builder.Run(async context =>
            {
                var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (ex is OperationCanceledException)
                {
                    context.Response.StatusCode = 499;
                    await context.Response.WriteAsync("Client closed request");
                    return;
                }
            }));


            app.UseRouting();

            app.UseHttpsRedirection();

            app.UseCors("VueDev");

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
