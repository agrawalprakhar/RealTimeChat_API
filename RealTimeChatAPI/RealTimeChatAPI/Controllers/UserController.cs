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

        // Signup Method
        // Description: This HTTP POST method handles user registration. It expects a JSON object containing user registration data.
        // If the request data is valid and the user is successfully registered, it returns a success response with the registered user's information.
        // If the email provided in the registration request is already registered, it returns a conflict response.
        // If there are any validation errors or registration fails, it returns a bad request response with an error message.
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

        // Login Method
        // Description: This HTTP POST method handles user login. It expects a JSON object containing login credentials (email and password).
        // If the provided credentials are valid, it returns a success response with the user's profile information and an authentication token.
        // If the provided credentials are incorrect or if the user is not found, it returns an unauthorized response.
        // If the request data is invalid, it returns a bad request response with an error message.
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

        // Social Login Method
        // Description: This HTTP POST method handles social login with a Google access token. It expects a JSON object containing the access token.
        // The method verifies the Google token, creates a new user if not found, generates an authentication token, and returns the user's profile
        // information and the authentication token. If the token validation fails, it returns a null response.
        // This method is accessible at the `/api/LoginWithGoogle` route.
        [HttpPost("/api/LoginWithGoogle")]
        public async Task<IActionResult> SocialLogin(tokenRequest token)
        {
            var user = await _userRepo.VerifyGoogleTokenAsync(token.TokenId);
            return Ok(user);
        }

        // Get Users Method
        // Description: This HTTP GET method retrieves a list of users from the repository. It ensures that only authenticated users can access the data. 
        // The method filters out the current user from the list to prevent self-display of user information.
        // This method is accessible via a GET request to the corresponding route.
        [HttpGet("/api/users")]
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

        // Get User by ID Method
        // Description: This HTTP GET method retrieves a specific user by their unique identifier from the repository. 
        // The method ensures that the returned user's information is only accessible to authorized users.
        // This method is accessible via a GET request to the corresponding route with the user's ID as a parameter.
        [HttpGet("/api/users/{Id}")]
        public async Task<ActionResult<User>> GetUser(string Id)
        {
            var user = await _userRepo.GetUserAsync(Id);
            if (user == null)
            {
                return NotFound("User not found");
            }
            return Ok(user);
        }


        // Update User Status Method
        // Description: This HTTP PUT method allows users to update their status message. It receives a user ID and a new status message 
        // in the request body and updates the user's status in the repository. The updated status message is then broadcasted to all 
        // connected clients using SignalR.
        // This method is accessible via a PUT request to the corresponding route with the user's ID as a parameter.
        [HttpPut("/api/users/{Id}")]
        public async Task<IActionResult> UpdateStatus(string Id, [FromBody]  StatusMessage statusMessage)
        {
            try
            {
            await _userRepo.UpdateStatusAsync(Id, statusMessage.Content);
            await _chatHub.Clients.All.SendAsync("ReceiveStatusUpdate", Id, statusMessage.Content);

                return Ok(new
                {
                   statusMessage = statusMessage.Content,
                });
            }
            catch (Exception ex)
             {
                return BadRequest($"Error updating status: {ex.Message}");
            }
        }
    }
}
