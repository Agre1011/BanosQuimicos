using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Modelos;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BanosQuimicos.Api.Seguridad;

public class EmisorTokens(IOptions<OpcionesJwt> opciones)
{
    public LoginRespuesta Crear(Usuario usuario)
    {
        var config = opciones.Value;
        var ahora = DateTimeOffset.UtcNow;
        var expira = ahora.AddMinutes(config.MinutosVigencia);
        // El token identifica al usuario; permisos y estado se consultan en la base en cada petición.
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(config.Emisor, config.Audiencia, claims,
            ahora.UtcDateTime, expira.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(config.ClaveBase64)),
                SecurityAlgorithms.HmacSha256));
        return new LoginRespuesta(new JwtSecurityTokenHandler().WriteToken(token), expira,
            usuario.EstadoCuenta, usuario.EstadoCuenta == "Pendiente" ? Politicas.AvisoEspera : null);
    }
}
