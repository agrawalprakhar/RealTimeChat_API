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
using System.Web.Providers.Entities;
using RealTimeChat.DAL.Repository;
using Azure.Core;

namespace MinimalChatApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;

        public UsersController(IUserRepository db, IConfiguration configuration)
        {
            _userRepo = db;
            _configuration = configuration;
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
                return Ok(response);
            }
            else
            {
                return Unauthorized(new { error = message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserList()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await Console.Out.WriteLineAsync(currentUserId);

            var users = await _userRepo.GetUsers(currentUserId);

            if (users == null)
            {
                return NotFound();
            }

            var userListResponse = users.Select(u => new
            {
                id = u.Id,
                name = u.UserName,
                email = u.Email
            }).ToList();

            return Ok(userListResponse);
        }

    }
}
