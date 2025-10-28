using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

Console.WriteLine("=== EXTRACTOR DE ÁLBUMES DE FACEBOOK (PARALELO + ROBUSTO) ===\n");

// --- PEDIR CARPETA ---
string albumFolder = "";
while (string.IsNullOrWhiteSpace(albumFolder) || !Directory.Exists(albumFolder))
{
    Console.Write("Pega la ruta de la carpeta con los archivos .html (ej: ...\\album): ");
    albumFolder = Console.ReadLine()?.Trim(' ', '"');
    if (string.IsNullOrWhiteSpace(albumFolder) || !Directory.Exists(albumFolder))
        Console.WriteLine("Carpeta no válida. Inténtalo de nuevo.\n");
}

string mediaFolder = Path.Combine(Path.GetDirectoryName(albumFolder), "media");
if (!Directory.Exists(mediaFolder))
{
    Console.WriteLine($"¡ERROR! No se encontró la carpeta de imágenes:\n   {mediaFolder}");
    Console.ReadKey();
    return;
}

// --- BUSCAR HTMLs ---
var htmlFiles = Directory.GetFiles(albumFolder, "*.html", SearchOption.TopDirectoryOnly)
                         .Where(f => !Path.GetFileName(f).Contains("your_facebook_activity", StringComparison.OrdinalIgnoreCase))
                         .ToList();

if (!htmlFiles.Any())
{
    Console.WriteLine("No se encontraron archivos .html.");
    Console.ReadKey();
    return;
}

Console.WriteLine($"\nEncontrados {htmlFiles.Count} álbumes. Procesando en paralelo...\n");
Console.WriteLine(new string('=', 60));

// --- CONTADORES THREAD-SAFE ---
int totalProcesados = 0;
int totalImagenes = 0;
int totalErrores = 0;
var lockObj = new object();

// --- BOLSA CONCURRENTE PARA RESULTADOS ---
var resultados = new ConcurrentBag<(string titulo, int imagenes)>();

// --- PARALELO (con límite para evitar sobrecarga de disco) ---
Parallel.ForEach(htmlFiles, new ParallelOptions
{
    MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4) // Limitar a 4 para evitar colisiones de I/O
}, htmlPath =>
{
    try
    {
        var (titulo, imagenes) = ProcesarAlbum(htmlPath, albumFolder, mediaFolder);
        if (imagenes > 0)
        {
            resultados.Add((titulo, imagenes));
        }

        lock (lockObj)
        {
            totalProcesados++;
            totalImagenes += imagenes;
        }
    }
    catch (Exception ex)
    {
        lock (lockObj)
        {
            totalErrores++;
        }
        Console.WriteLine($"[ERROR CRÍTICO] {Path.GetFileName(htmlPath)}: {ex.Message}");
    }
});

// --- MOSTRAR RESULTADOS ORDENADOS ---
Console.WriteLine(new string('=', 60));
Console.WriteLine("ÁLBUMES CON IMÁGENES:");
foreach (var (titulo, img) in resultados.OrderBy(r => r.titulo))
{
    Console.WriteLine($"   • {titulo} → {img} imágenes");
}

Console.WriteLine(new string('=', 60));
Console.WriteLine("PROCESO COMPLETADO");
Console.WriteLine($"Álbumes con imágenes: {resultados.Count}");
Console.WriteLine($"Imágenes extraídas: {totalImagenes}");
Console.WriteLine($"Archivos HTML procesados: {totalProcesados}");
Console.WriteLine($"Errores encontrados: {totalErrores}");
Console.WriteLine("\nPresiona cualquier tecla para salir...");
Console.ReadKey();

// ==================== FUNCIÓN PROCESAR ÁLBUM (MEJORADA) ====================
static (string titulo, int imagenes) ProcesarAlbum(string htmlPath, string albumFolder, string mediaFolder)
{
    var htmlDoc = new HtmlDocument();
    htmlDoc.Load(htmlPath);

    var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//h1");
    string albumTitle = titleNode?.InnerText.Trim() ?? "Álbum sin título";
    albumTitle = SanitizeFileName(albumTitle); // Sanitizado MEJORADO

    var secciones = htmlDoc.DocumentNode.SelectNodes("//section[contains(@class, '_a6-g')]");

    // TU LÍNEA: ¡PERFECTA!
    if (secciones == null || !secciones.Any())
        return (albumTitle, 0);

    string albumOutputFolder = Path.Combine(albumFolder, albumTitle);

    // NUEVO: Verificar/crear carpeta con manejo de errores
    try
    {
        Directory.CreateDirectory(albumOutputFolder);

        // Verificar que se creó correctamente
        if (!Directory.Exists(albumOutputFolder))
        {
            throw new DirectoryNotFoundException($"No se pudo crear la carpeta: {albumOutputFolder}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR CARPETA] No se pudo crear carpeta para '{albumTitle}': {ex.Message}");
        return (albumTitle, 0); // Saltar álbum entero si falla la carpeta
    }

    int index = 1;
    int imagenesGuardadas = 0;
    int imagenesFallidas = 0;

    foreach (var sec in secciones)
    {
        try
        {
            var imgNode = sec.SelectSingleNode(".//img");
            var commentNode = sec.SelectSingleNode(".//div[contains(@class, '_3-95')]");

            string imgSrc = imgNode?.GetAttributeValue("src", "");
            string comment = commentNode?.InnerText.Trim();

            if (string.IsNullOrEmpty(imgSrc) || !imgSrc.Contains("your_facebook_activity"))
                continue;

            string relativePath = imgSrc.Replace("your_facebook_activity/", "");
            string fullImagePath = Path.Combine(mediaFolder, relativePath.Replace("posts/media/", ""));

            if (!File.Exists(fullImagePath))
            {
                imagenesFallidas++;
                continue;
            }

            // Sanitizar NOMBRE DE ARCHIVO también
            string fileName = !string.IsNullOrWhiteSpace(comment)
                ? SanitizeFileName(comment)
                : $"{albumTitle} - {index:D3}";

            if (string.IsNullOrEmpty(fileName))
                fileName = $"{albumTitle} - {index:D3}";

            string extension = Path.GetExtension(fullImagePath);
            string finalName = GetUniqueFileName(albumOutputFolder, fileName, extension);
            string destPath = Path.Combine(albumOutputFolder, finalName);

            // NUEVO: Manejo de errores en copia
            try
            {
                File.Copy(fullImagePath, destPath, true);
                imagenesGuardadas++;
            }
            catch (Exception copyEx)
            {
                Console.WriteLine($"[ERROR COPIA] Imagen '{finalName}' en álbum '{albumTitle}': {copyEx.Message}");
                imagenesFallidas++;
                continue; // Continúa con la siguiente imagen
            }

            index++;
        }
        catch (Exception imgEx)
        {
            imagenesFallidas++;
            Console.WriteLine($"[ERROR IMAGEN] En álbum '{albumTitle}': {imgEx.Message}");
            continue; // Continúa con la siguiente sección
        }
    }

    // Log de fallos por álbum
    if (imagenesFallidas > 0)
    {
        Console.WriteLine($"[INFO] Álbum '{albumTitle}': {imagenesGuardadas} guardadas, {imagenesFallidas} fallidas");
    }

    return (albumTitle, imagenesGuardadas);
}

// --- SANITIZE MEJORADO (maneja TODOS los inválidos + ... + espacios múltiples) ---
static string SanitizeFileName(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return "";

    // Reemplazar puntos suspensivos y otros comunes
    name = name.Replace("...", "_")
               .Replace("..", "_")
               .Replace("...", "_"); // Triple para cubrir casos

    // Eliminar/reemplazar TODOS los caracteres inválidos
    var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars())
                           .Concat(new char[] { '*', '?', '"', '<', '>', '|' }); // Extra precaución
    name = invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));

    // Limpiar espacios múltiples y trim
    name = Regex.Replace(name, @"\s+", " ").Trim();

    // Limitar longitud
    return name.Length > 180 ? name.Substring(0, 180) : name;
}

// --- NOMBRE ÚNICO (sin cambios) ---
static string GetUniqueFileName(string folder, string baseName, string extension)
{
    string name = baseName;
    string path = Path.Combine(folder, name + extension);
    int count = 1;
    while (File.Exists(path))
    {
        name = $"{baseName} ({count})";
        path = Path.Combine(folder, name + extension);
        count++;
    }
    return name + extension;
}