using Microsoft.Data.Sqlite;
using System.Net;
using System.Web;

public class DbHandler
{
    private SqliteConnection sqlconnection;
    public DbHandler()
    {
        // Initialize the SQLite database connection and create the table if it doesn't exist
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "url_shortener.db");
        Console.WriteLine("db path: "+ dbPath);

        this.sqlconnection = new SqliteConnection("Data Source="+dbPath); //create the db object
        this.sqlconnection.Open(); // open the db
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

    public bool CheckUrlExists(string UrlToCheck)
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
        // get Url and return it code
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


    public string GetUrlFromCode(string code)
    {
        // get Code and return it Url
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
        bool isUrlExists = this.db.CheckUrlExists(url);
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
    private string faviconPath;
    public HTTPServer(string prefix)
    {
        this.listener = new HttpListener();
        this.um = new UrlMgmt();
        this.prefix = prefix;
        this.faviconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
        this.listener.Prefixes.Add($"http://{this.prefix}/");
        this.listener.Start();
        this.prefix = this.prefix.Replace("+", "0.0.0.0");
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
                string shortenUrl = $"http://{request.Url.Host}:{request.Url.Port}/{code}";

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
            else if (requestedUrl == "/favicon.ico")
            {
                byte[] buffer = File.ReadAllBytes(this.faviconPath);
                response.ContentType = "image/x-icon";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
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

            else if (requestedUrl == "/")
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
                string contents = File.ReadAllText(path);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(contents);
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