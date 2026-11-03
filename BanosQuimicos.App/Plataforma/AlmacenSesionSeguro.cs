using BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.App.Plataforma;

public class AlmacenSesionSeguro(HttpClient http) : IAlmacenSesion
{
    private const string Clave = "banosquimicos.token";
    private const string Restaurar = "banosquimicos.restaurar";
    private const string Servidor = "banosquimicos.servidor";
    private readonly string servidorActual = http.BaseAddress!.AbsoluteUri;
    public async Task<string?> LeerAsync()
    {
        // Una sesión antigua sin servidor asociado tampoco se envía al nuevo destino.
        if (Preferences.Default.Get(Servidor, "") != servidorActual)
        {
            await BorrarAsync();
            return null;
        }
        return Preferences.Default.Get(Restaurar, false)
            ? await SecureStorage.Default.GetAsync(Clave) : null;
    }
    public async Task GuardarAsync(string token)
    {
        Preferences.Default.Set(Restaurar, false);
        await SecureStorage.Default.SetAsync(Clave, token);
        Preferences.Default.Set(Servidor, servidorActual);
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
