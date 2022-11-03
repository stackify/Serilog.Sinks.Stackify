using System;
using System.Threading;
using Serilog;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging();

            for (var i = 0; i < 1000; i++)
            {
                var log = Log.ForContext("myobject", new
                {
                    key = "value",
                    iteration = i
                }, true);

                log.Information("This is a test of the {name} System", "Emergency Broadcast");

                try
                {
                    throw new ApplicationException("Test error");
                }
                catch (Exception ex)
                {
                    log.Error(ex, ex.Message);
                }

                Thread.Sleep(2000);
            }
        }

        static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .Stackify("", "", "")
                .CreateLogger();
        }
    }
}
