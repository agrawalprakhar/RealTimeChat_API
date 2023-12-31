using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinimalChatApplication.Middlewares;
using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using RealTimeChatAPI.Hubs;
using System.Text;

namespace RealTimeChatAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddSignalR();

            builder.Services.AddScoped<RequestLoggingMiddleware>();

            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("JWT"));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost4200", builder =>
                {
                    builder.WithOrigins("http://localhost:4200") // Allow requests from this origin
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddHttpContextAccessor();

            ConfigurationManager Configuration = builder.Configuration;
            builder.Services.AddDbContext<RealTimeChatContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("RealTimeChatContext")));

           builder.Services.AddIdentity<User, IdentityRole>()
             .AddEntityFrameworkStores<RealTimeChatContext>()
             .AddDefaultTokenProviders();

            builder.Services.AddScoped<IUserRepository,UserRepository>();

            builder.Services.AddScoped<IMessageRepository, MessageRepository>();

            builder.Services.AddScoped<ILogs, LogRepository>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
             {
                 options.SaveToken = true;
                 options.RequireHttpsMetadata = false;
                 options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidAudience = Configuration["JWT:ValidAudience"],
                     ValidIssuer = Configuration["JWT:ValidIssuer"],
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))
                 };
             });

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

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCors("AllowLocalhost4200");

            app.MapControllers();

            app.UseMiddleware<RequestLoggingMiddleware>();

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}