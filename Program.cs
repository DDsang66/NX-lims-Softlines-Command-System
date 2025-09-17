using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Tools.Factory;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Reflection;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using NX_lims_Softlines_Command_System.Application.Services.OrderService;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;

namespace NX_lims_Softlines_Command_System
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var jwt = builder.Configuration.GetSection("Jwt");


            // Add services to the container.
            var licenseType = builder.Configuration.GetValue<string>("EPPlus:License");
            ExcelPackage.License.SetNonCommercialPersonal("GuangXv Chen");
            builder.Services.AddControllers();


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddScoped<IBuyerFactory, BuyerFactory>();
            builder.Services.AddScoped<FiberContentHelper>();
            builder.Services.AddScoped<IPrintExcelStrategyFactory, PrintExcelStrategyFactory>();
            builder.Services.AddScoped<ExcelHelper>();
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<OrderRepo>();

            // 扫描所有实现 IPrintExcelStrategy 的非抽象类
            builder.Services.AddSingleton<JwtService>();

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
                    policy.WithOrigins("http://192.168.235.8:5173",
                                       "http://192.168.235.52:5173",
                                       "http://192.168.235.8:82",
                                       "http://192.168.235.8:81",
                                       "https://TheProductionDomain.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // 用 JWT/ Cookie 可保留
                });
            });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            //builder.Services.AddDbContext<LabDbContext>(options =>
            //    options.UseSqlServer(builder.Configuration.GetConnectionString("LabCommandTestEntities")));

            builder.Services.AddDbContext<LabDbContextSec>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("NX-limsLabCommandSys")));



            var app = builder.Build();
            app.UseStaticFiles(); 
            // 确保静态文件中间件已启用
            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "swagger";   // 默认就是 swagger
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
                // 其他异常继续原有处理
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
