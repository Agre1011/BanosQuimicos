using BanosQuimicos.App.Autenticacion;
using BanosQuimicos.App.Plataforma;
using Microsoft.Extensions.Logging;

namespace BanosQuimicos.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton(_ => ConexionApi.CrearCliente());
        builder.Services.AddSingleton<ClienteAutenticacion>();
        builder.Services.AddSingleton<IAlmacenSesion, AlmacenSesionSeguro>();
        builder.Services.AddSingleton<SesionUsuario>();
#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
