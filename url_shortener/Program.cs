using System;
using System.Security.Cryptography.X509Certificates;

namespace UrlShortenerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //netsh http add urlacl url = http://IP:port/ user=DOMAIN\user
            HTTPServer server = new HTTPServer("+:8080");

        }
    }
}

