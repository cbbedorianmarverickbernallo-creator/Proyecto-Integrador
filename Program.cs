// ============================================================================
// PARTE 5 (C): PUNTO DE ENTRADA Y CICLO DE SESION DE LA APLICACION
// Archivo: Program.cs
// ============================================================================

using FundacionUnifranz.Data;
using FundacionUnifranz.Services;
using FundacionUnifranz.UI;

namespace FundacionUnifranz;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1. Inicializar motor de base de datos SQLite integrada y servicios
        var database = new DatabaseManager();
        var service = new GestionService(database);
        service.Inicializar();

        // 2. Ciclo de sesion: permite cerrar sesion y volver al Login limpiamente
        while (true)
        {
            using var formLogin = new FormLogin(service);
            if (formLogin.ShowDialog() != DialogResult.OK || formLogin.UsuarioAutenticado is null)
            {
                break;
            }

            using var formPrincipal = new FormPrincipal(service, formLogin.UsuarioAutenticado);
            Application.Run(formPrincipal);

            if (!formPrincipal.SolicitoCerrarSesion)
            {
                break;
            }
        }
    }
}
