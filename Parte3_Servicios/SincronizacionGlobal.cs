// ============================================================================
// SINCRONIZACIÓN GLOBAL DE INTERFAZ (PC WINDOWS + CELULAR ANDROID + WEB)
// Archivo: Parte3_Servicios/SincronizacionGlobal.cs
// ============================================================================
// Permite que cualquier modificación en interfaz_global.json publicada por
// el Programador desde el .zip se refleje automáticamente en todas las
// computadoras Windows y celulares Android conectados.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FundacionUnifranz.Services
{
    public sealed class ConfigInterfazGlobal
    {
        public string NombreInstitucion { get; set; } = "FUNDACIÓN UNIFRANZ";
        public string TituloSistema { get; set; } = "Sistema de Gestión";
        public string SubtituloLogin { get; set; } = "Transformando comunidades con educación e impacto social";
        public string MensajeBienvenida { get; set; } = "Bienvenido";
        public string AvisoGlobal { get; set; } = string.Empty;
        public string ColorPrimario { get; set; } = "#007B8F";
        public string ColorSidebar { get; set; } = "#064A57";
        public string ColorAcento { get; set; } = "#E65A28";
        public string ColorFondo { get; set; } = "#F8FAFC";
        public string UrlServidorSync { get; set; } = "https://ais-pre-4uc43x4xbcve4awtqb7o2a-185409884174.us-west2.run.app";
        public int Version { get; set; } = 1;
    }

    public static class SincronizacionGlobal
    {
        public static ConfigInterfazGlobal Actual { get; private set; } = new ConfigInterfazGlobal();

        public static string RutaCacheLocal
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FundacionUNIFRANZ"
                );
                try { Directory.CreateDirectory(dir); } catch { }
                return Path.Combine(dir, "interfaz_global.json");
            }
        }

        public static string RutaCarpetaApp =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "interfaz_global.json");

        /// <summary>
        /// Carga la configuración global desde el disco local y consulta el servidor
        /// de sincronización en línea para recibir cambios publicados desde el .zip.
        /// Retorna true si cambió la versión o algún valor visual.
        /// </summary>
        public static bool SincronizarAhora()
        {
            string firmaAnterior = Firma(Actual);

            // 1. Leer archivo junto al ejecutable (empaquetado por el instalador)
            AplicarDesdeArchivo(RutaCarpetaApp);

            // 2. Leer override local (%LocalAppData%\FundacionUNIFRANZ\interfaz_global.json)
            AplicarDesdeArchivo(RutaCacheLocal);

            // 3. Consultar servidor central en vivo (actualiza PCs y celulares al mismo tiempo)
            if (!string.IsNullOrWhiteSpace(Actual.UrlServidorSync))
            {
                try
                {
                    ServicePointManager.SecurityProtocol =
                        (SecurityProtocolType)3072 | SecurityProtocolType.Tls12;
                    string endpoint = Actual.UrlServidorSync.TrimEnd('/') + "/api/sync/config";
                    var req = (HttpWebRequest)WebRequest.Create(endpoint);
                    req.Method = "GET";
                    req.Timeout = 3500;
                    req.ReadWriteTimeout = 3500;
                    req.UserAgent = "FundacionUNIFRANZ-Desktop/3.2";

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    using (var stream = resp.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        if (!string.IsNullOrWhiteSpace(json) && json.Contains("nombreInstitucion"))
                        {
                            AplicarDesdeJson(json);
                            try { File.WriteAllText(RutaCacheLocal, json, Encoding.UTF8); } catch { }
                        }
                    }
                }
                catch
                {
                    // Sin conexión o servidor dormido: continúa con la configuración local sin errores.
                }
            }

            return !string.Equals(firmaAnterior, Firma(Actual), StringComparison.Ordinal);
        }

        private static void AplicarDesdeArchivo(string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                {
                    string json = File.ReadAllText(ruta, Encoding.UTF8);
                    AplicarDesdeJson(json);
                }
            }
            catch { }
        }

        private static void AplicarDesdeJson(string json)
        {
            string s;
            if (TryGetJsonString(json, "nombreInstitucion", out s) && !string.IsNullOrWhiteSpace(s))
                Actual.NombreInstitucion = s;
            if (TryGetJsonString(json, "tituloSistema", out s) && !string.IsNullOrWhiteSpace(s))
                Actual.TituloSistema = s;
            if (TryGetJsonString(json, "subtituloLogin", out s) && !string.IsNullOrWhiteSpace(s))
                Actual.SubtituloLogin = s;
            if (TryGetJsonString(json, "mensajeBienvenida", out s) && !string.IsNullOrWhiteSpace(s))
                Actual.MensajeBienvenida = s;
            if (TryGetJsonString(json, "avisoGlobal", out s))
                Actual.AvisoGlobal = s ?? string.Empty;
            if (TryGetJsonString(json, "colorPrimario", out s) && EsHexValido(s))
                Actual.ColorPrimario = s;
            if (TryGetJsonString(json, "colorSidebar", out s) && EsHexValido(s))
                Actual.ColorSidebar = s;
            if (TryGetJsonString(json, "colorAcento", out s) && EsHexValido(s))
                Actual.ColorAcento = s;
            if (TryGetJsonString(json, "colorFondo", out s) && EsHexValido(s))
                Actual.ColorFondo = s;
            if (TryGetJsonString(json, "urlServidorSync", out s) &&
                (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                Actual.UrlServidorSync = s;

            var vm = Regex.Match(json, "\"version\"\\s*:\\s*(\\d+)");
            if (vm.Success && int.TryParse(vm.Groups[1].Value, out int ver))
                Actual.Version = ver;
        }

        private static bool TryGetJsonString(string json, string key, out string value)
        {
            value = string.Empty;
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            if (!m.Success) return false;
            value = Regex.Unescape(m.Groups[1].Value);
            return true;
        }

        private static bool EsHexValido(string hex) =>
            !string.IsNullOrWhiteSpace(hex) && Regex.IsMatch(hex.Trim(), "^#[0-9a-fA-F]{6}$");

        public static Color ParsearColor(string hex, Color fallback)
        {
            try
            {
                if (EsHexValido(hex)) return ColorTranslator.FromHtml(hex.Trim());
            }
            catch { }
            return fallback;
        }

        private static string Firma(ConfigInterfazGlobal c) =>
            $"{c.NombreInstitucion}|{c.TituloSistema}|{c.SubtituloLogin}|{c.MensajeBienvenida}|{c.AvisoGlobal}|{c.ColorPrimario}|{c.ColorSidebar}|{c.ColorAcento}|{c.ColorFondo}|{c.Version}";
    }
}
