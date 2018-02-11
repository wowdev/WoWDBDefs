using DBDefsLib;
using System;

namespace DBDefsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new DBDReader();
            var definition = reader.Read("../../../definitions/Map.dbd");
            Console.WriteLine("Done, press enter to exit");
            Console.ReadLine();
        }
    }
}
