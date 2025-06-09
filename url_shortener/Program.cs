using System;

namespace UrlShortenerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            UrlMgmt um = new UrlMgmt("localhost");
            string shorten = um.CreateShortenUrl("https://this.is.a.test/test/page");
            Console.WriteLine(shorten);

        }
    }
}