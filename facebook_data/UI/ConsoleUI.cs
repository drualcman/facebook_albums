namespace facebook_albums.UI;

public class ConsoleUI
{
    public string PromptValidFolder(string message)
    {
        while (true)
        {
            Console.Write(message);
            var input = Console.ReadLine()?.Trim(' ', '"');
            if (string.IsNullOrWhiteSpace(input) || !Directory.Exists(input))
                Console.WriteLine("Carpeta no válida. Inténtalo de nuevo.\n");
            else
                return input;
        }
    }

    public void ShowMessage(string message) => Console.WriteLine(message);
    public void ShowError(string error) => Console.WriteLine(error);
    public void ShowSeparator() => Console.WriteLine(new string('=', 60));
    public void Pause()
    {
        Console.WriteLine("\nPresiona cualquier tecla para salir...");
        Console.ReadKey();
    }

}