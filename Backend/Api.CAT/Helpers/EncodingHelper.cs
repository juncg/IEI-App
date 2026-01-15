using System.Text;

namespace Backend.Api.CAT.Helpers
{
    public static class EncodingHelper
    {
        static EncodingHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Detecta la codificación de un archivo analizando los bytes iniciales
        /// </summary>
        /// <param name="file">Ruta del archivo a analizar</param>
        /// <returns>Codificación detectada del archivo</returns>
        public static Encoding DetectEncoding(string file)
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            using (var fileStream = File.OpenRead(file))
            {
                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            }

            if (bytesRead >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                return Encoding.UTF8;
            if (bytesRead >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe)
                return Encoding.Unicode;
            if (bytesRead >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff)
                return Encoding.BigEndianUnicode;

            try
            {
                var utf8 = Encoding.UTF8;
                var decoder = utf8.GetDecoder();
                char[] chars = new char[bytesRead];
                decoder.Convert(buffer, 0, bytesRead, chars, 0, chars.Length, true, out _, out int charsUsed, out _);

                string sample = new string(chars, 0, charsUsed);
                if (sample.Contains('á') || sample.Contains('é') || sample.Contains('í') ||
                    sample.Contains('ó') || sample.Contains('ú') || sample.Contains('ñ') ||
                    sample.Contains('Á') || sample.Contains('É') || sample.Contains('Í') ||
                    sample.Contains('Ó') || sample.Contains('Ú') || sample.Contains('Ñ'))
                {
                    return Encoding.UTF8;
                }
            }
            catch
            {

            }

            return Encoding.GetEncoding("Windows-1252");
        }

        /// <summary>
        /// Intenta leer un archivo usando múltiples codificaciones hasta encontrar la correcta
        /// </summary>
        /// <param name="file">Ruta del archivo a leer</param>
        /// <returns>Contenido del archivo como cadena de texto</returns>
        public static string TryReadWithEncodings(string file)
        {
            Encoding[] encodingsToTry = new[]
            {
                Encoding.UTF8,
                Encoding.GetEncoding("ISO-8859-1"),
                Encoding.GetEncoding("Windows-1252"),
                Encoding.Default
            };

            foreach (var encoding in encodingsToTry)
            {
                try
                {
                    return File.ReadAllText(file, encoding);
                }
                catch
                {
                    continue;
                }
            }

            return File.ReadAllText(file);
        }
    }
}
