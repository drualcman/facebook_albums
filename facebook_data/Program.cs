using facebook_albums.UI;

class Program
{
    static void Main()
    {
        var ui = new ConsoleUI();
        ui.ShowMessage("=== EXTRACTOR DE ÁLBUMES DE FACEBOOK (PARALELO + ROBUSTO) ===\n");

        string albumFolder = ui.PromptValidFolder("Pega la ruta de la carpeta con los archivos .html (ej: ...\\album): ");
        string mediaFolder = Path.Combine(Path.GetDirectoryName(albumFolder)!, "media");

        if (!Directory.Exists(mediaFolder))
        {
            ui.ShowError($"¡ERROR! No se encontró la carpeta de imágenes:\n   {mediaFolder}");
            ui.Pause();
            return;
        }

        var htmlFiles = Directory.GetFiles(albumFolder, "*.html", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Contains("your_facebook_activity", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (!htmlFiles.Any())
        {
            ui.ShowMessage("No se encontraron archivos .html.");
            ui.Pause();
            return;
        }

        ui.ShowMessage($"\nEncontrados {htmlFiles.Length} álbumes. Procesando en paralelo...\n");
        ui.ShowSeparator();

        // --- INICIAR EXTRACTOR ---
        var extractor = new FacebookAlbumExtractor();
        var animator = new ProgressAnimator();
        int progressLine = Console.CursorTop;
        Console.WriteLine(); // Línea para spinner
        animator.Start(progressLine, htmlFiles.Length, () => (extractor.Processed, extractor.Images));

        // --- PROCESAR ---
        var (results, processed, images, errors) = extractor.ExtractAlbums(htmlFiles, albumFolder, mediaFolder);

        // --- DETENER ANIMACIÓN ---
        animator.Stop();

        // --- MOSTRAR RESULTADOS ---
        var presenter = new ResultPresenter();
        presenter.Show(results, processed, images, errors);

        ui.Pause();
    }
}