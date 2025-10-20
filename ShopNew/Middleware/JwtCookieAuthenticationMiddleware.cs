using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ShopNew.Middleware
{
    public class JwtCookieAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtCookieAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = context.Request.Cookies["AuthToken"];
            
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(GetJwtSetting("Key")));
                    
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = GetJwtSetting("Issuer", "ShopNew"),
                        ValidAudience = GetJwtSetting("Audience", "ShopNewUsers"),
                        IssuerSigningKey = key
                    };

                    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                    context.User = principal;
                }
                catch
                {
                    // Token is invalid, clear the cookie
                    context.Response.Cookies.Delete("AuthToken");
                }
            }

            await _next(context);
        }

        private string GetJwtSetting(string name, string? defaultValue = null)
        {
            var value = _configuration[$"Jwt:{name}"] ??
                        _configuration[$"JWT_{name.ToUpperInvariant()}"] ??
                        _configuration[$"Jwt__{name}"] ??
                        defaultValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"JWT {name} is not configured. Please set Jwt:{name} in configuration or provide JWT_{name.ToUpperInvariant()} environment variable.");
            }

            return value;
        }
    }
}
