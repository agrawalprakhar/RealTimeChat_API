using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.CodeAnalysis.Scripting;
using NuGet.Protocol.Plugins;
using RealTimeChat.DAL.Repository.IRepository;
using RealTimeChat.Domain.Models;

using RealTimeChat.DAL.Repository;
using Azure.Core;

using Google.Apis.Auth;
using RealTimeChatAPI;
using Microsoft.Extensions.Options;
using RealTimeChatAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MinimalChatApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;
        private readonly AppSettings _appSettings;
        private readonly IHubContext<ChatHub> _chatHub;

        public UsersController(IHubContext<ChatHub> chatHub,IUserRepository db, IConfiguration configuration,IOptions<AppSettings> appSettings)
        {
            _userRepo = db;
            _configuration = configuration;
            _appSettings = appSettings.Value;
            _chatHub = chatHub;
         
        }


    


        // POST: api/register
        [HttpPost("/api/register")]
        public async Task<IActionResult> Signup(UserRegistration request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data." });
            }

            // Check if a user with the provided email already exists
            var existingUser = _userRepo.Get(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "Email is already registered." });
            }

            var (success, message, userDto) = await _userRepo.SignupAsync(request);

            if (success)
            {
                return Ok(new { Message = message, User = userDto });
            }
            else
            {
                return BadRequest(new { error = message });
            }

        }








       // POST: api/login
       [HttpPost("/api/login")]
        public async Task<ActionResult> Login([FromBody] loginRequest loginData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data." });
            }

            // Find the user by email
            var user = _userRepo.Get(u => u.Email == loginData.Email);

            if (user == null)
            {
                return Unauthorized(new { error = "Login Failed Due to Wrong Credential" });
            }

            var (success, message, response) = await _userRepo.LoginAsync(loginData);

            if (success)
            {
                await _chatHub.Clients.User(user.Id).SendAsync("SetUserIdentifier", user.Id);
                return Ok(response);
            }
            else
            {
                return Unauthorized(new { error = message });
            }
        }

        [HttpPost("/api/LoginWithGoogle")]
        public async Task<IActionResult> SocialLogin(tokenRequest token)
        {

            Console.WriteLine(token.TokenId);
            var user = await _userRepo.VerifyGoogleTokenAsync(token.TokenId);

            return Ok(user);
        }
        //public async Task<IActionResult> LoginWithGoogle([FromBody] string credential)
        //{
        //    var settings = new GoogleJsonWebSignature.ValidationSettings()
        //    {
        //        Audience = new List<string> { this._appSettings.GoogleClientId },

        //    };

        //    var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

        //    var UserList = await _userRepo.GetAllUsersAsync();

        //    var user = UserList.Where(x =>x.Name == payload.Name).FirstOrDefault();

        //    if (user != null)
        //    {
        //        return Ok(JWTGenerator(user));
        //    }
        //    else
        //    {
        //        return BadRequest();
        //    }

        //}

        //private object? JWTGenerator(RealTimeChat.Domain.Models.User user)
        //{
        //    var authClaims = new List<Claim>
        //        {
        //            new Claim(ClaimTypes.Name,user.Name),
        //            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
        //            new Claim(ClaimTypes.Email,user.Email),
        //            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        };

        //    var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));



        //    var token = new JwtSecurityToken(
        //        issuer: _configuration["JWT:ValidIssuer"],
        //        audience: _configuration["JWT:ValidAudience"],
        //        expires: DateTime.Now.AddHours(3),
        //        claims: authClaims,
        //        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        //        );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}

     

        [HttpGet]
        public async Task<ActionResult<List<RealTimeChat.Domain.Models.User>>> GetUser()
        {
            var currentUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Unauthorized access" });
            }

            var users = await _userRepo.GetAllUsersAsync();

            var filteredUsers = users
             .Where(u => u.Id != currentUserId)
             .Select(u => new
             {   
                 Id=u.Id,
                 Name = u.Name,
                 Email = u.Email
             })
             .ToList();



            return Ok(filteredUsers);
        }
    }
}
