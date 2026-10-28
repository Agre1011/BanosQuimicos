using BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.App.Plataforma;

public class AlmacenSesionSeguro : IAlmacenSesion
{
    private const string Clave = "banosquimicos.token";
    private const string Restaurar = "banosquimicos.restaurar";
    public Task<string?> LeerAsync() => Preferences.Default.Get(Restaurar, false)
        ? SecureStorage.Default.GetAsync(Clave) : Task.FromResult<string?>(null);
    public async Task GuardarAsync(string token)
    {
        Preferences.Default.Set(Restaurar, false);
        await SecureStorage.Default.SetAsync(Clave, token);
        Preferences.Default.Set(Restaurar, true);
    }
    public Task BorrarAsync()
    {
        // Este indicador no contiene secretos e impide restaurar un token si la limpieza falla.
        Preferences.Default.Set(Restaurar, false);
        SecureStorage.Default.Remove(Clave);
        return Task.CompletedTask;
    }
}
