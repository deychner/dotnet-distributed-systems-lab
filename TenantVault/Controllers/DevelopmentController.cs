using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TenantVault.Attributes;
using TenantVault.Security;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevelopmentController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        private const string TENANT_ID_CLAIM_TYPE = "tenant_id";
        private const string ROLE_CLAIM_TYPE = "role";

        [HttpGet("jwt")]
        [AllowAnonymous]
        [Development]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<string> IssueJwt([FromQuery] string tenantId, [FromQuery] bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return BadRequest("tenantId query parameter is required.");
            }

            var signingKey = _configuration["Jwt:Key"]!;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            Claim[] claims =
                [
                new Claim(TENANT_ID_CLAIM_TYPE, tenantId),
                new Claim(JwtRegisteredClaimNames.Sub, isAdmin ? "admin-user" : "regular-user"),
                new Claim(ROLE_CLAIM_TYPE, isAdmin ? Roles.Admin : Roles.User)
                ];

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return Ok(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
