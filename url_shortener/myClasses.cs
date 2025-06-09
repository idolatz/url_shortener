using System;
using Microsoft.Data.Sqlite;

public class DbHandler
{
    private SqliteConnection sqlconnection;
    public DbHandler()
    {
        // Initialize the SQLite database connection and create the table if it doesn't exist
        this.sqlconnection = new SqliteConnection("Data Source=url_shortener.db");
        this.sqlconnection.Open();
        var command = this.sqlconnection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Urls (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OriginalUrl TEXT NOT NULL,
                Code TEXT NOT NULL UNIQUE
            );
        ";
        command.ExecuteNonQuery();
    }

    public void InsertUrl(string originalUrl, string code)
    {
        // string code : the shortened URL code like "https://myserver.com/CODE"
        // string originalUrl : the original URL to be shortened like "https://example.com/some/long/url"
        var command = sqlconnection.CreateCommand();
        command.CommandText = "INSERT INTO Urls (OriginalUrl, Code) VALUES ($originalUrl, $code)";
        command.Parameters.AddWithValue("$originalUrl", originalUrl);
        command.Parameters.AddWithValue("$code", code);
        command.ExecuteNonQuery();
    }
    
    public bool CheckCodeExists(string code)
    {
        // Check if the code already exists in the database
        // part of collision handling
        var command = sqlconnection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Urls WHERE Code = $code";
        command.Parameters.AddWithValue("$code", code);
        try
        {
            long count = (long)command.ExecuteScalar();
            return count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public bool CheckOriginalUrlExists(string UrlToCheck)
    {
        // Check if the original URL exists
        // part of collision handling
        var command = sqlconnection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Urls WHERE OriginalUrl = $originalUrl";
        command.Parameters.AddWithValue("$originalUrl", UrlToCheck);
        try
        {
            long count = (long)command.ExecuteScalar();
            return count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public string GetCodeFromUrl(string Url)
    {
        var command = sqlconnection.CreateCommand();
        command.CommandText = """SELECT Code FROM Urls WHERE OriginalUrl = "$url" """;
        command.Parameters.AddWithValue("$url", Url);
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                return reader.GetString(reader.GetOrdinal("Code"));
            }
            else
            {
                return "Exists"; // Or throw an exception, or handle as needed
            }
        }
    }

}

public class UrlMgmt
{
    public string serverUrl;
    private DbHandler db;
    public UrlMgmt(string serverUrl)  
    {
        // initialize the UrlMgmt Object
        this.db = new DbHandler();
        this.serverUrl = serverUrl;
    }
    public string CreateShortenUrl(string originalUrl)
    {
        // Generate a unique code for the shortened URL
        string code = GenerateShortCode(originalUrl);
        //build the shorten url
        string shortenUrl = "http://" + this.serverUrl + "/" + code;
        return shortenUrl;
    }



    private string GenerateShortCode(string url)
    {
        // this function return random code after check that it doesnt exists in the db
        string code = getRandomString();
        bool isCodeExists = this.db.CheckCodeExists(code);
        bool isUrlExists = this.db.CheckOriginalUrlExists(url);
        Console.WriteLine("isUrlExists=" + isUrlExists);
        if (isUrlExists)
        {
             return db.GetCodeFromUrl(code);
        }

        // Ensure the generated code is unique
        while (isCodeExists)
        {
            code = getRandomString();
            isCodeExists = this.db.CheckCodeExists(code);
        }
        // Insert the original URL and the shortened URL into the db
        db.InsertUrl(url, code);
        return code;
    }

    private string getRandomString()
    {
        // this function create rundom string 
        int length = 6; // Length of the random string
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Random random = new Random();
        char[] stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}