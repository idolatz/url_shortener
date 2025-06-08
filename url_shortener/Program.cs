using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        // Initialize the database connection
        using (var sqlconnection = new SqliteConnection("Data Source=url_shortener.db"))
        {
            sqlconnection.Open();
            // Create the table if it doesn't exist
            var command = sqlconnection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Urls (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OriginalUrl TEXT NOT NULL,
                    ShortenedUrl TEXT NOT NULL UNIQUE
                );
            ";
            command.ExecuteNonQuery();
        }

        Console.WriteLine("Please enter a URL to shorten:");
        String url = Console.ReadLine();
        string shortUrl = ShortenUrl(url);
        Console.WriteLine($"Shortened URL: {shortUrl}");

    }
    
}