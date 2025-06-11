using Microsoft.Data.Sqlite;
using System.Net;
using System.Web;

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
        command.CommandText = "SELECT * FROM Urls WHERE `OriginalUrl`=$url ";
        command.Parameters.AddWithValue("$url", Url);
        using (var reader = command.ExecuteReader())
        {
            reader.Read();
            string code = reader.GetString(reader.GetOrdinal("Code"));
            return code;
        }
    }

    public void ListAll()
    {
        var command = sqlconnection.CreateCommand();
        command.CommandText = "SELECT * FROM Urls";
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) {
                Console.WriteLine(reader.GetString(2)+"\t\t"+reader.GetString(1));
            }
        }
    }

    public string GetUrlFromCode(string code)
    {
        var command = sqlconnection.CreateCommand();
        command.CommandText = "SELECT * FROM Urls WHERE `Code`=$code ";
        command.Parameters.AddWithValue("$code", code);
        using (var reader = command.ExecuteReader())
        {
            reader.Read();
            string url = reader.GetString(reader.GetOrdinal("OriginalUrl"));
            return url;
        }
    }

    public void DeleteURL(string url)
    {
        var command = sqlconnection.CreateCommand();
        command.CommandText = "DELETE FROM Urls Where OriginalUrl=$url";
        command.Parameters.AddWithValue("$url", url);
        command.ExecuteNonQuery();
    }

}

public class UrlMgmt
{
    private DbHandler db;
    public UrlMgmt()  
    {
        // initialize the UrlMgmt Object
        this.db = new DbHandler();
    }
    public string GetCode(string originalUrl)
    {
        // Generate a unique code for the shortened URL
        string code = GenerateShortCode(originalUrl);
        //build the shorten url
        return code;
    }

    public void ListAll()
    {
        this.db.ListAll();
    }

    public void DeleteURL(string url)
    {
        if(!this.db.CheckOriginalUrlExists(url))
        {
            Console.WriteLine("URL is not exists");
            return;
        }
        this.db.DeleteURL(url);
        Console.WriteLine("DONE!");
    }

    public string GetUrlFromCode(string code)
    {
        if (!this.db.CheckCodeExists(code)) {
            Console.WriteLine("Code is not exists");
            return "***not-found***";
        }
        return this.db.GetUrlFromCode(code);
    }



    private string GenerateShortCode(string url)
    {
        // this function return random code after check that it doesnt exists in the db
        string code = getRandomString();
        bool isCodeExists = this.db.CheckCodeExists(code);
        bool isUrlExists = this.db.CheckOriginalUrlExists(url);
        if (isUrlExists)
        {
             return db.GetCodeFromUrl(url);
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


public class HTTPServer
{
    private HttpListener listener;
    private UrlMgmt um;
    private string prefix;
    public HTTPServer(string prefix)
    {
        this.listener = new HttpListener();
        this.um = new UrlMgmt();
        this.prefix = prefix;
        this.listener.Prefixes.Add($"http://{this.prefix}/");
        this.listener.Start();
        Console.WriteLine($"Listening on http://{this.prefix}/");


        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string requestedUrl = request.RawUrl;
            Console.WriteLine($"Request for {requestedUrl}");

            if (requestedUrl.StartsWith("/?url="))
            {
                string originalUrl = extractVar(requestedUrl, "url");
                string code = this.um.GetCode(originalUrl);
                string shortenUrl = $"http://{this.prefix}/{code}";

                string responseString = $"<html><body><h1>your shorten url is: <br>{shortenUrl} </h1></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }

            else if (requestedUrl.StartsWith("/?code="))
            {
                string code = extractVar(requestedUrl, "code");
                string orginalUrl = this.um.GetUrlFromCode(code);
                string responseString = $"<html><body><h1>checking code {code} <br> answer: <br> {orginalUrl}</h1></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }

            else if (requestedUrl.Length == 7)
            {
                // Redirect
                string code = requestedUrl.TrimStart('/');
                string originalUrl = this.um.GetUrlFromCode(code);
                
                response.StatusCode = (int)HttpStatusCode.Redirect; // 302

                if (originalUrl == "***not-found***")
                {
                    response.RedirectLocation = "/code-not-exists";
                    Console.WriteLine("redirected to: /code-not-exists");
                }
                else
                {
                    response.RedirectLocation = originalUrl;
                    Console.WriteLine("redirected to: " + originalUrl);
                }
                response.Close();
            }

            else if (requestedUrl == "/code-not-exists")
            {
                string responseString = "<html><body><h1>code is not exists</h1></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }

            else
            {
                Console.WriteLine("redirected to: /code-not-exists");
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.RedirectLocation = "/code-not-exists";
                response.Close();
            }
        }

    }

    private string extractVar(string url, string type)
    {
        // Create a Uri with a dummy host (required by HttpUtility)
        var uri = new Uri("http://temp" + url);

        var query = HttpUtility.ParseQueryString(uri.Query);
        string urlParam = query.Get(type);

        // Optional: remove surrounding quotes if present
        if (urlParam != null && urlParam.StartsWith("\"") && urlParam.EndsWith("\""))
        {
            urlParam = urlParam.Substring(1, urlParam.Length - 2);
        }

        return urlParam;
    }
}