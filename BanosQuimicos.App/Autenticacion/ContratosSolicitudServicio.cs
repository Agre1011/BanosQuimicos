using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.App.Autenticacion;

public class DatosSolicitudServicio
{
    public Guid ClaveEnvio { get; set; } = Guid.NewGuid();
    private string descripcion = "";
    [Required(ErrorMessage = "Describí el reclamo."), StringLength(2000, ErrorMessage = "El reclamo no puede superar 2000 caracteres.")]
    public string Descripcion { get => descripcion; set => descripcion = value?.Trim() ?? ""; }
    [JsonIgnore] public bool Intentado { get; set; }
}
