// ============================================================================
// PARTE 5 (C): PUNTO DE ENTRADA CON PANTALLA DE CARGA Y RECUPERACION SQL
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

        var database = new DatabaseManager();
        var service = new GestionService(database);

        // 1. Pantalla de carga + conexion automatica silenciosa (SQL Server o motor local)
        using (var splash = new FormSplash(service))
        {
            if (splash.ShowDialog() != DialogResult.OK || !splash.InicializadoCorrectamente)
            {
                service.ActivarModoLocal();
            }
        }

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
