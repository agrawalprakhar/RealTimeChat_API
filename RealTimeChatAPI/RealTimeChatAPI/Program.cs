using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using System.Text;

namespace RealTimeChatAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.


            builder.Services.AddControllers();
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("JWT"));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost4200", builder =>
                {
                    builder.WithOrigins("http://localhost:4200") // Allow requests from this origin
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });


            ConfigurationManager Configuration = builder.Configuration;

            //configuring db path
            builder.Services.AddDbContext<RealTimeChatContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("RealTimeChatContext")));

           builder.Services.AddIdentity<User, IdentityRole>()
             .AddEntityFrameworkStores<RealTimeChatContext>()
             .AddDefaultTokenProviders();
            builder.Services.AddScoped<IUserRepository,UserRepository>();





            // Adding Jwt Bearer
            // Add JWT authentication
            // Adding Authentication
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

            app.Run();
        }
    }
}