using AutoMapper;

using KMG.Repository;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using KMG.Repository.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Quartz.Spi;
using System.Text;
using System.Text.Json.Serialization;
using KMS.APIService.Jobs;

namespace KMS.APIService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });


            builder.Services.AddControllers();
            builder.Services.AddScoped<SwpkoiFarmShopContext>();
            builder.Services.AddScoped<EmailService>();


            var key = Encoding.ASCII.GetBytes("xinchaocacbanminhlasang1234567890");
            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                })
             .AddGoogle(googleOptions =>
             {
                 var configuration = builder.Configuration;
                 googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
                 googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                 googleOptions.SaveTokens = true;
             });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: \"Bearer abcdef12345\""
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
            });
            builder.Services.AddScoped<UnitOfWork>();
            builder.Services.AddDbContext<SwpkoiFarmShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'MovieDBContext' not found.")));
            builder.Services.AddScoped<UnitOfWork>();
            builder.Services.AddScoped<IConsignmentService, ConsignmentService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddAutoMapper(typeof(MapperProfile));
            builder.Services.AddQuartz(q =>
            {
                // Đăng ký sử dụng DI cho Quartz.NET
                q.UseMicrosoftDependencyInjectionJobFactory();

                // Đăng ký CheckConsignmentJob
                q.AddJob<CheckConsignmentJob>(opts => opts.WithIdentity("CheckConsignmentJob"));
                q.AddTrigger(opts => opts
                    .ForJob("CheckConsignmentJob")
                    .WithIdentity("CheckConsignmentTrigger")
                    .StartNow()
                    .WithSimpleSchedule(x => x
                        //.WithIntervalInHours(1)
                        .WithIntervalInMinutes(1)
                        .RepeatForever()));
            });


            builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("AllowSpecificOrigins");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}