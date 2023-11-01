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

        // SignupAsync Method
        // Description: This asynchronous method handles user registration. It creates a new user entity based on the provided signupModel
        // and attempts to create the user using the UserManager. It returns a tuple indicating the success status, a message, and a
        // RegistrationDto object containing user information upon successful registration.
        public async Task<(bool success, string message, RegistrationDto userDto)> SignupAsync(UserRegistration signupModel)
        {

            var user = new Domain.Models.User()
            {
                Name = signupModel.Name,
                Email = signupModel.Email,
                UserName = signupModel.Email,
                Password = signupModel.Password,
                StatusMessage = "",

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
                Email = user.Email  
            };
            return (true, "Registration successful.", userDto);
        }

        // LoginAsync Method
        // Description: This asynchronous method handles user authentication. It checks the provided loginData (email and password) against
        // the existing user data. If the provided credentials are valid, it generates a JWT token, constructs a UserProfile object, and 
        // creates a LoginResponse object containing the user's profile and the generated token. It returns a tuple indicating the 
        // success status, a message, and a LoginResponse object upon successful authentication.
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


        // GenerateJwtToken Method
        // Description: This private method generates a JSON Web Token (JWT) for a user based on the provided user ID, name, and email. 
        // The JWT contains specific claims such as name, name identifier, email, and a unique identifier (Jti) for additional security.
        // The token has a specified expiration time, issuer, audience, and is signed using a symmetric security key.
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

        // GetUserAsync Method
        // Description: This asynchronous method retrieves a user from the database based on the provided user ID.
        // The method performs an asynchronous database query to find a user entity with the specified ID.
        // If a matching user is found, it is returned; otherwise, null is returned.
        public async Task<User> GetUserAsync(string Id)
        {
            return await _db.Users.FindAsync(Id);
        }

        // GetAllUsersAsync Method
        // Description: This asynchronous method retrieves all users from the database.
        // The method performs an asynchronous database query to fetch all user entities.
        // The method returns a list of user entities.
        public async Task<List<Domain.Models.User>> GetAllUsersAsync()
        {
            return (List<Domain.Models.User>)GetAll();
        }

        // UpdateStatusAsync Method
        // Description: This asynchronous method updates the status message of a user in the database.
        // The method takes a user ID and a new status message as input parameters.
        // It retrieves the user entity based on the provided ID, updates the status message, and saves the changes to the database.
        public async Task UpdateStatusAsync(string Id, string statusMessage)
        {
            var user = await _db.Users.FindAsync(Id);
            if (user != null)
            {
                user.StatusMessage = statusMessage;
                await _db.SaveChangesAsync();
            }
        }

        // VerifyGoogleTokenAsync Method
        // Description: This asynchronous method verifies a Google authentication token.
        // The method takes a Google token ID as input parameter and validates it.
        // If the token is valid, it checks if the corresponding user exists in the database. 
        // If not, it creates a new user based on the Google token information.
        // Finally, it generates an authentication token and returns a LoginResponse containing the token and user profile information.
        public async Task<LoginResponse> VerifyGoogleTokenAsync(string tokenId)
        {
            try
            {
                var validPayload = await GoogleJsonWebSignature.ValidateAsync(tokenId);
                var user = Get(u => u.Email == validPayload.Email);

                if (user == null)
                {
                    //Create a new User if not found in the repository
                    var newUser = new User
                    {
                        UserName = validPayload.GivenName,
                        Email = validPayload.Email,
                        Name = validPayload.Name,
                        Password = "Password@123",
                        StatusMessage = "",
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
                var token = GenerateJwtToken(user.Id,user.Name,user.Email); 
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
                return loginResponse;
            }
            catch (InvalidJwtException)
            {
                return null;
            }
        }

    }
}
