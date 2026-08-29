using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TenantVault.Attributes;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevelopmentController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet("jwt")]
        [AllowAnonymous]
        [Development]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<string> IssueJwt([FromQuery] string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return BadRequest("tenantId query parameter is required.");
            }

            var signingKey = _configuration["Jwt:Key"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("tenant_id", tenantId),
                new Claim(JwtRegisteredClaimNames.Sub, "test-user")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
