﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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



         Task<(bool success, string message, RegistrationDto userDto)> SignupAsync(UserRegistration signupModel);

        Task<(bool success, string message, LoginResponse response)> LoginAsync(loginRequest loginData);

        Task<IEnumerable<Domain.Models.User>> GetUsers(string currentUserId);

    }
}
