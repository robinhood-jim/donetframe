using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

namespace Frameset.Web.Utils
{
    public static class JwtAuthBuilderExtesnions
    {
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("JWT");
            var Issuer = section["Issuer"];
            var Secret = section["Key"];
            var Audience = section["Audience"];
            var ExpireDays = Convert.ToInt32(section["ExpireDays"], CultureInfo.InvariantCulture);

            services.AddAuthorization();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false,
                ValidateLifetime = true
            };
            services.AddSingleton(tokenValidationParameters);
            return services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = tokenValidationParameters;
                    x.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            string authorization = context.Request.Headers["Authorization"];

                            if (string.IsNullOrEmpty(authorization))
                            {
                                context.NoResult();
                            }
                            else
                            {
                                context.Token = authorization.Replace("Bearer ", string.Empty);
                            }

                            return Task.CompletedTask;
                        },
                    };

                });
        }


    }
}
