﻿using AutoMapper;
using EventPlanning.Bll.Interfaces;
using EventPlanning.Data.Entities;
using EventPlanning.Server.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventPlanning.Server.Controllers
{
    [EnableCors("AllowClient")]
    [ApiController]
    [Route("[controller]")]
    public class AuthorizationController : ControllerBase
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;

        public AuthorizationController(IRepository<User> userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> LogIn([FromBody]UserLogInModel userLogIn)
        {
            var identity = await GetIdentity(userLogIn);

            if (identity == null)
            {
                return BadRequest(new { errorText = "Invalid email or password." });
            }

            var now = DateTime.UtcNow;

            var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                user_name = identity.Name,
                role = identity.RoleClaimType
            };

            return Ok(JsonConvert.SerializeObject(response));
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(UserRegisterModel userRegister)
        {
            if (await DoesUserExist(userRegister?.Email))
            {
                return BadRequest(new { errorText = "User with this email already exists." });
            }

            var user = _mapper.Map<UserRegisterModel, User>(userRegister);
            user = await _userRepository.CreateAsync(user);

            if (user == null)
            {
                return BadRequest(new { errorText = "Error while creating user." });
            }

            return await LogIn(new UserLogInModel { Email = user.Email, Password = user.Password });
        }

        private async Task<ClaimsIdentity?> GetIdentity(UserLogInModel userLogIn)
        {
            var user = await _userRepository.GetAsync(userLogIn.Email);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email ?? "Anonymus"),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, "User")
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, "User");

                return claimsIdentity;
            }

            return null;
        }

        private async Task<bool> DoesUserExist(string? email)
        {
            return await _userRepository.GetAsync(email) != null;
        }

        public static class AuthOptions
        {
            public const string ISSUER = "MyAuthServer";
            public const string AUDIENCE = "MyAuthClient";
            public const int LIFETIME = 20;
            const string KEY = "mysupersecret_secretsecretsecretkey!123";
            public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
        }
    }
}
