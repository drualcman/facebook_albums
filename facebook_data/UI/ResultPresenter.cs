namespace facebook_albums.UI;

public class ResultPresenter
{
    public void Show(ConcurrentBag<AlbumResult> results, int processed, int images, int errors)
    {
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("ÁLBUMES CON IMÁGENES:");
        foreach (var r in results.OrderBy(x => x.Title))
            Console.WriteLine($"   • {r.Title} → {r.ImageCount} imágenes");

        Console.WriteLine(new string('=', 60));
        Console.WriteLine("PROCESO COMPLETADO");
        Console.WriteLine($"Álbumes con imágenes: {results.Count}");
        Console.WriteLine($"Imágenes extraídas: {images}");
        Console.WriteLine($"Archivos HTML procesados: {processed}");
        Console.WriteLine($"Errores encontrados: {errors}");
    }
}