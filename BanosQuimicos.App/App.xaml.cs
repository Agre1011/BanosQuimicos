using BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.App;

public partial class App : Application
{
    private readonly SesionUsuario sesion;
    public App(SesionUsuario sesion) { this.sesion = sesion; InitializeComponent(); }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "Baños Químicos" };
        // Volver desde otra app o desbloquear el teléfono exige consultar otra vez el estado actual.
        window.Resumed += async (_, _) => await sesion.ActualizarAsync();
        return window;
    }
}
