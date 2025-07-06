
namespace UrlShortenerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //netsh http add urlacl url = http://+:8080/ user=DOMAIN\user
            HTTPServer server = new HTTPServer("+:8080");

        }
    }
}

