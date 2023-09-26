using Microsoft.AspNetCore.Identity;
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
using System.Web.Providers.Entities;

namespace RealTimeChat.DAL.Repository
{
    public class UserRepository : Repository<Domain.Models.User>, IUserRepository
    {
        private readonly RealTimeChatContext _db;
        private readonly UserManager<Domain.Models.User> _userManager;
        private readonly SignInManager<loginRequest> _signInManager;
        private readonly IConfiguration _configuration;

        public UserRepository(RealTimeChatContext db,UserManager<Domain.Models.User> userManager, SignInManager<loginRequest> signInManager, IConfiguration configuration):base(db)
        {

          _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }


        public async Task<IdentityResult> SignupAsync(Domain.Models.User signupModel)
        {
            var user = new Domain.Models.User()
            {
                Name = signupModel.Name,
                Email = signupModel.Email,
                UserName = signupModel.Email,
                Password = signupModel.Password,


            };

            return await _userManager.CreateAsync(user, signupModel.Password);
        }

        public async Task<string> LoginAsync(loginRequest login)
        {
            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, false, false);

            if (!result.Succeeded)
            {
                return null;
            }
            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,login.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            var authSigninKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigninKey, SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(token);

        } 

   
    }
}
