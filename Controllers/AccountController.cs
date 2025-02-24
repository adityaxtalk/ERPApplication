using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ERPApplication.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ERPApplication.Helper;
using Microsoft.EntityFrameworkCore;

namespace ERPApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly JwtHelper _jwtHelper;

        public AccountController(UserManager<ApplicationUser> userManager, JwtHelper jwtHelper,RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, Role=model.Role };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                return Ok(new { message = "User Registered successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]

        public async Task<IActionResult> Login([FromBody] Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles= await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim> {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                authClaims.AddRange(userRoles.Select(role=> new Claim(ClaimTypes.Role, role)));

                var token = _jwtHelper.GenerateJwtToken(authClaims);
                var refreshToken = _jwtHelper.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                return Ok(new { Token= token, RefreshToken= refreshToken  });
            }

            return Unauthorized(new {message= "Invalid credentials"});
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u=> u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized(new { Message ="Invalid refresh token"});
            }

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id),
                new Claim("UserRole", user.Role)
            };

            claims.AddRange(roles.Select(role=> new Claim(ClaimTypes.Role, role)));
            var newToken = _jwtHelper.GenerateJwtToken(claims);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new { Token = newToken, RefreshToken = newRefreshToken });
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] Roles role)
        {
            var user = await _userManager.FindByEmailAsync(role.Email);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var result = await _userManager.AddToRoleAsync(user, role.Role);
            if (result.Succeeded)
            {
                return Ok(new { message = "Role assigned successfully" });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("add-role")]
        public async Task<IActionResult> AddRole([FromBody] string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var results = await _roleManager.CreateAsync(new IdentityRole(role));

                if (results.Succeeded)
                {
                    return Ok(new { message = "Role Added Succesfully" });
                }
                return BadRequest(results.Errors);
            }
            return BadRequest("Role already Exists");
        }
    }
}
