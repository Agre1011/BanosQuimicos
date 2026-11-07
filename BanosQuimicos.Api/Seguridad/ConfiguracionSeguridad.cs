using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BanosQuimicos.Api.Seguridad;

public static class ConfiguracionSeguridad
{
    public static IServiceCollection AgregarSeguridad(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpcionesJwt>().Bind(configuration.GetSection("Jwt"))
            .Validate(OpcionesJwt.ClaveValida, "Configurar Jwt:ClaveBase64 fuera de Git: al menos 32 bytes aleatorios en Base64.")
            .Validate(x => x.MinutosVigencia is >= 1 and <= 60 && !string.IsNullOrWhiteSpace(x.Emisor)
                && !string.IsNullOrWhiteSpace(x.Audiencia), "Configuración JWT inválida.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        services.AddScoped<EmisorTokens>();
        services.AddScoped<PrimerAdministrador>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcionesJwt>>((bearer, jwt) =>
            {
                var config = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = config.Emisor,
                    ValidateAudience = true, ValidAudience = config.Audiencia,
                    ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.FromSeconds(15),
                    ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(config.ClaveBase64))
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (!int.TryParse(context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id))
                        { context.Fail("Identidad inválida."); return; }
                        var db = context.HttpContext.RequestServices.GetRequiredService<BanosQuimicosDbContext>();
                        var usuario = await db.Usuarios.AsNoTracking().Include(x => x.Rol)
                            .SingleOrDefaultAsync(x => x.IdUsuario == id, context.HttpContext.RequestAborted);
                        // Nunca confiar en el rol/estado de una sesión anterior.
                        if (usuario is null || usuario.EstadoCuenta is not ("Activa" or "Pendiente"))
                        { context.Fail("Cuenta no habilitada."); return; }
                        var claims = new List<Claim> {
                            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                            new(Politicas.EstadoClaim, usuario.EstadoCuenta)
                        };
                        if (usuario.EstadoCuenta == "Activa")
                            claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.NombreRol));
                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims,
                            JwtBearerDefaults.AuthenticationScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role));
                        context.HttpContext.Items[Politicas.CuentaActual] = usuario;
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            var activa = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                .RequireClaim(Politicas.EstadoClaim, "Activa").Build();
            // [Authorize] y cualquier endpoint sin política explícita exigen cuenta activa.
            options.DefaultPolicy = activa;
            options.FallbackPolicy = activa;
            options.AddPolicy(Politicas.CuentaActiva, activa);
            options.AddPolicy(Politicas.Cliente, p => p.RequireAuthenticatedUser()
                .RequireClaim(Politicas.EstadoClaim, "Activa").RequireRole("Cliente"));
            options.AddPolicy(Politicas.CuentaIdentificada, p => p.RequireAuthenticatedUser());
            options.AddPolicy(Politicas.Empleado, p => p.RequireAuthenticatedUser()
                .RequireClaim(Politicas.EstadoClaim, "Activa").RequireRole("Empleado"));
            options.AddPolicy(Politicas.Administrador, p => p.RequireAuthenticatedUser()
                .RequireClaim(Politicas.EstadoClaim, "Activa").RequireRole("Administrador"));
        });
        return services;
    }
}
