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


     

        // GET: api/Users
        [HttpGet("/api/users")]
        [Authorize]
        public async Task<ActionResult<List<User>>> GetUser()
        {
            var currentUser = HttpContext.User;
            var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = currentUser.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = currentUser.FindFirst(ClaimTypes.Email)?.Value;
            await Console.Out.WriteLineAsync(userId);

            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "Unauthorized access" });
            }
            var users = _userRepo.GetAll();

            return Ok(users);
        }


        // POST: api/register
        [HttpPost("/api/register")]
        public async Task<IActionResult> Signup([FromBody]User model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request data." });
            }

            // Check if a user with the provided email already exists
            var existingUser = _userRepo.Get(u => u.Email == model.Email);
            if (existingUser != null)
            {
                return Conflict(new { error = "Email is already registered." });
            }

            var result = await _userRepo.SignupAsync(model);

            if (result.Succeeded) {
                return Ok(result);
            }

            return BadRequest(new { });

        
          
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
            var user =  _userRepo.Get(u => u.Email == loginData.Email);

            if (user == null )
            {
                return Unauthorized(new { error = "Login Failed Due to Wrong Credential" });
            }

            var result = await _userRepo.LoginAsync(loginData);

            if(string.IsNullOrEmpty(result))
            {
                return Unauthorized();
            }

            return Ok(result);
        }
     
    }
}
