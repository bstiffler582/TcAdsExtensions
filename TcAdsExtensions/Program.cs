using System;
using TcAdsExtensions.ADS;

namespace TcAdsExtensions
{
    class Program
    {
        static void Main(string[] args)
        {
            // Example client
            using (var connection = new AdsConnection("127.0.0.1.1.1:851"))
            {
                if (connection.IsConnected)
                {
                    Console.WriteLine("Connection Established");

                    try
                    {
                        // read structure data
                        var readData = connection.ReadStructSymbol<TestStruct>("GVL.PlcStruct");

                        // modify data
                        readData.iVal = readData.iVal + 1;
                        readData.fVal = readData.fVal + 1.1f;
                        readData.sVal = readData.sVal + '1';

                        // write back
                        connection.WriteStructSymbol("GVL.PlcStruct", readData);

                        // global event handler example
                        connection.OnSymbolValueChanged += Connection_OnSymbolValueChanged;
                        connection.SubscribeOnValueChange("GVL.iEventData1");
                        connection.SubscribeOnValueChange("GVL.fEventData2");

                        // custom event handler example
                        connection.SubscribeOnValueChange("GVL.iCustomEvent", Connection_CustomValueChanged);

                        // suspend to test events
                        Console.ReadLine();
                    }
                    catch (Exception Ex)
                    {
                        Console.WriteLine(Ex.Message);
                        Console.ReadLine();
                    }

                }
                else
                {
                    Console.WriteLine("Connection Failed. Check AmsNetId and Target status.");
                    Console.ReadLine();
                }
            }
        }

        // called onchange of any subscribed symbol
        private static void Connection_OnSymbolValueChanged(object sender, object e)
        {
            string symbolPath = (string)sender;
            object val;

            switch (symbolPath)
            {
                case "GVL.iEventData1":
                    val = (int)e;
                    break;
                case "GVL.fEventData2":
                    val = (float)e;
                    break;
                default:
                    return;
            }

            Console.WriteLine("Global OnChange raised: ");
            Console.WriteLine($"{symbolPath} vale = {val.ToString()}.");
            
        }

        // called onchange for specified symbol
        private static void Connection_CustomValueChanged(object Value)
        {
            int val = (int)Value;
            Console.WriteLine("Custom OnChange raised: ");
            Console.WriteLine($"Custom symbol val = {val.ToString()}.");
        }

        /// <summary>
        /// Class definition to match PLC struct
        /// </summary>
        class TestStruct
        {
            public int iVal { get; set; }
            public float fVal { get; set; }
            public string sVal { get; set; }
        }
    }
}
