using Google.Apis.Auth;
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

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


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
                Id=user.Id,
                Name = user.Name,
                Email = user.Email,
              
               
            };
            return (true, "Registration successful.", userDto);
        }

      

        public async Task<(bool success, string message, LoginResponse response)> LoginAsync(loginRequest loginData)
        {

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
         
            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,name),
                    new Claim(ClaimTypes.NameIdentifier,id.ToString()),
                    new Claim(ClaimTypes.Email,email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

           
           
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    

        public async Task<List<Domain.Models.User>> GetAllUsersAsync()
        {
    

            return (List<Domain.Models.User>)GetAll();
        }

        public async Task<LoginResponse> VerifyGoogleTokenAsync(string tokenId)
        {
            try
            {
                
                var validPayload = await GoogleJsonWebSignature.ValidateAsync(tokenId);
                var user = Get(u => u.Email == validPayload.Email);

                if (user == null)
                {
                    //Create a new IdentityUser if not found in the repository
                    var newUser = new User
                    {
                        UserName = validPayload.GivenName,
                        Email = validPayload.Email,
                        Name = validPayload.Name,
                        Password = "Password@123",
                    };
                    var result = await _userManager.CreateAsync(newUser);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error creating user: {error.Description}");
                        }
                        return null;
                    }

                    user = newUser;
                }

                // Generate or retrieve the authentication token
                var token = GenerateJwtToken(user.Id,user.Name,user.Email); // Replace with your token generation logic

                var userProfile = new UserProfile
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Email = user.Email
                };

                var loginResponse = new LoginResponse
                {
                    Token = token,
                    Profile = userProfile
                };

                Console.WriteLine("Login response generated successfully.");
                return loginResponse;
            }
            catch (InvalidJwtException)
            {
                // Token validation failed
                Console.WriteLine("Token validation failed.");
                return null;
            }
        }

    }
}
