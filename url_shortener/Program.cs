using System;

namespace UrlShortenerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            UrlMgmt um = new UrlMgmt("localhost");
            //DbHandler db = new DbHandler();
            //Console.WriteLine(db.GetCodeFromUrl("https://this.is.a.test/test/page"));
            string shorten = um.GetShortenUrl("https://this.is.a.test/test/pa1ge");
            Console.WriteLine(shorten);
            string longurl = um.GetUrlFromCode("CWO99I");
            Console.WriteLine(longurl);

        }
    }
}