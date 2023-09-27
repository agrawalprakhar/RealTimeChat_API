using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Providers.Entities;

namespace RealTimeChat.DAL.Repository
{
    public class UserRepository : Repository<Domain.Models.User>, IUserRepository
    {
        private readonly RealTimeChatContext _db;
        private readonly UserManager<Domain.Models.User> _userManager;
        private readonly SignInManager<Domain.Models.User> _signInManager;
        private readonly IConfiguration _configuration;

        public UserRepository(RealTimeChatContext db,UserManager<Domain.Models.User> userManager, SignInManager<Domain.Models.User> signInManager, IConfiguration configuration):base(db)
        {

          _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }


        public async Task<(bool success, string message, RegistrationDto userDto)> SignupAsync(UserRegistration signupModel)
        {

            var user = new Domain.Models.User()
            {
                Name = signupModel.Name,
                Email = signupModel.Email,
                UserName = signupModel.Email,
                Password = signupModel.Password,


            };

            var result = await _userManager.CreateAsync(user, signupModel.Password);

            if (!result.Succeeded)
            {
                return (false, "Registration failed.", null);
            }
            var userDto = new RegistrationDto
            {

                Name = user.Name,
                Email = user.Email,
              
               
            };
            return (true, "Registration successful.", userDto);
        }

      

        public async Task<(bool success, string message, LoginResponse response)> LoginAsync(loginRequest loginData)
        {
           // var user = _userRepo.Get(u => u.Email == loginData.Email);
            var user =  Get(u => u.Email == loginData.Email);
            

            if (user != null && await _userManager.CheckPasswordAsync(user, loginData.Password))
            {
                var token = GenerateJwtToken(user.Id, user.Name, user.Email);
                var userProfile = new UserProfile
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email
                };

                var response = new LoginResponse
                {

                    Profile = userProfile,
                    Token = token,
                };

                return (true, "Authentication successful.", response);
            }

            return (false, "Authentication failed.", null);
           

        }

        private string GenerateJwtToken(string id, string name, string email)
        {
            var claims = new[]
            {
                    new Claim(ClaimTypes.NameIdentifier,id.ToString()),
                    new Claim(ClaimTypes.Name,name),
                    new Claim(ClaimTypes.Email,email)

                 };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), // Token expiration time
                signingCredentials: signIn);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<IEnumerable<Domain.Models.User>> GetUsers(string currentUserId)
        {
            if (currentUserId == null)
            {
                return null;
            }
            return await _db.Users.Where(u => u.Id != currentUserId).ToListAsync();
        }


    }
}
