using AutoMapper;
using Eleon.Modding;
using EmpyrionModWebHost.Configuration;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace EmpyrionModWebHost.Controllers
{

    public class UserManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public void CreateAndUpdateDatabase()
        {
            using var DB = new UserContext();
            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
        }
        public IEnumerable<User> GetAll()
        {
            using var DB = new UserContext();
            return DB.Users;
        }

        public User GetById(int id)
        {
            using var DB = new UserContext();
            return DB.Users.Find(id);
        }

        public User GetBySteamId(string aSteamId)
        {
            using var DB = new UserContext();
            return DB.Users.FirstOrDefault(U => U.InGameSteamId == aSteamId);
        }

    }

    [ApiController]
    [Authorize(Roles = nameof(Role.InGameAdmin))]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService UserService { get; }
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            UserService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody]UserDto userDto)
        {
            var user = UserService.Authenticate(userDto.Username, userDto.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info (without password) and token to store client side
            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                Token = tokenString
            });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody]UserDto userDto)
        {
            // map dto to entity
            var user = _mapper.Map<User>(userDto);

            try
            {
                if(UserService.CurrentUser != null) user.Role = (Role)Math.Max((int)user.Role, (int)UserService.CurrentUser.Role);
                UserService.Create(user, userDto.Password);
                return Ok();
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetAll()
        {
            var users = UserService.GetAll();
            var userDtos = _mapper.Map<IList<UserDto>>(users);
            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = UserService.GetById(id);
            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody]UserDto userDto)
        {
            // map dto to entity and set id
            var user = _mapper.Map<User>(userDto);

            try
            {
                if (user.Role < UserService.CurrentUser.Role) throw new AccessViolationException("User permission to high");
                UserService.Update(user, userDto.Password);
                return Ok();
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var user = UserService.GetById(id);
            if (UserService.CurrentUser.Role != Role.ServerAdmin && user.Role <= UserService.CurrentUser.Role) throw new AccessViolationException("User permission to high");
            UserService.Delete(id);
            return Ok();
        }
    }
}
