using System;

namespace UrlShortenerApp
{
    class Program
    {
        static void Main(string[] args)
        {   
            string serverIP = "127.0.0.1";
            UrlMgmt um = new UrlMgmt(serverIP);

            string message = """
                help            print this message
                getCode [url    create or get a shortcut
                getURL [Code]   see the url of that code
                list            list all shortcuts
                delete [url]    delete shortcut by shortcut
                """;
            if (args.Length == 0)
            {
                Console.WriteLine(message);
                return;
            }
            else if (args[0] == "getCode" && args.Length == 2)
            {
                Console.WriteLine(um.GetShortenUrl(args[1]));
            }
            else if (args[0] == "getURL" && args.Length == 2)
            {
                um.GetUrlFromCode(args[1]);
            }
            else if (args[0] == "list" && args.Length == 1)
            {
                um.ListAll();
            }
            else if (args[0] == "delete" && args.Length == 2)
            {
                um.DeleteURL(args[1]);
            }
            else
            {
                Console.WriteLine(message);
                return;
            }



        }
    }
}