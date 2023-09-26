using Microsoft.AspNetCore.Identity;
using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository.IRepository
{
   public interface IUserRepository : IRepository<User>
    {
        Task<IdentityResult> SignupAsync(User signupModel);
        Task<string> LoginAsync(loginRequest login);
    
    }
}
