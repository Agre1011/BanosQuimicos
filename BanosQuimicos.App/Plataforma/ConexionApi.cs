namespace BanosQuimicos.App.Plataforma;

public static class ConexionApi
{
    // Cambiar a false y recompilar para probar la API local; no depende de Debug/Release.
    private static readonly bool UsarHosting = true;

    public static HttpClient CrearCliente()
    {
        var direccion = "https://celeris.runasp.net/";
        if (!UsarHosting)
        {
            direccion = "https://localhost:7292/";
#if DEBUG && ANDROID
            // Solo Debug local: loopback del teléfono redirigido a la PC por USB.
            direccion = "http://127.0.0.1:5148/";
#endif
        }
        // No seguir redirecciones: una respuesta 3xx no debe reenviar credenciales fuera de la API.
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { BaseAddress = new Uri(direccion), Timeout = TimeSpan.FromSeconds(20) };
    }
}
