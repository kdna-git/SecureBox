using System.Windows.Input;

namespace SecureBox.Components;

// Meter en el code behind
public class BreadCrumbItem
{
    public required string DisplayName { get; set; }

    // Puedes definir algún identificador o ruta:
    public string? NavigationPath { get; set; }

    // Propiedad opcional para determinar si es el último elemento (útil para ocultar el separador)
    public bool IsLast { get; set; }

    // Comando para manejar la navegación al hacer clic en un BreadCrumb
    public ICommand? NavigateCommand { get; set; }
}
