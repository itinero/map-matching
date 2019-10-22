using System;
using System.IO;
using Itinero;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.MapMatching.Test.Functional;
using Newtonsoft.Json;
using Itinero.MapMatching.Test.Functional.Domain;
using Serilog;
using Serilog.Formatting.Json;

namespace Itinero.MapMatching.Test.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Fatal()
#if DEBUG
                .MinimumLevel.Fatal()
#endif
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // Link logging to OsmSharp.
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (level == OsmSharp.Logging.TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug($"[{o}] {level} - {message}");
                }
                else if (level == OsmSharp.Logging.TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information($"[{o}] {level} - {message}");
                }
                else if (level == OsmSharp.Logging.TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning($"[{o}] {level} - {message}");
                }
                else if (level == OsmSharp.Logging.TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal($"[{o}] {level} - {message}");
                }
                else if (level == OsmSharp.Logging.TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error($"[{o}] {level} - {message}");
                }
                else
                {
                    Log.Debug($"[{o}] {level} - {message}");
                }
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (level == Itinero.Logging.TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug($"[{o}] {level} - {message}");
                }
                else if (level == Itinero.Logging.TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information($"[{o}] {level} - {message}");
                }
                else if (level == Itinero.Logging.TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning($"[{o}] {level} - {message}");
                }
                else if (level == Itinero.Logging.TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal($"[{o}] {level} - {message}");
                }
                else if (level == Itinero.Logging.TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error($"[{o}] {level} - {message}");
                }
                else
                {
                    Log.Debug($"[{o}] {level} - {message}");
                }
            };

            // all tests.
            var tests = new[]
            {
//                Path.Combine("data", "bicycle", "test1.json"),
//                Path.Combine("data", "bicycle", "test2.json"),
//                Path.Combine("data", "bicycle", "test3.json"),
//                Path.Combine("data", "bicycle", "test4.json"),
//                Path.Combine("data", "bicycle", "test5.json"),
                Path.Combine("data", "bicycle", "test6.json"),
//                Path.Combine("data", "car", "test1.json"),
//                Path.Combine("data", "car", "test2.json"),
//                Path.Combine("data", "car", "test3.json"),
//                Path.Combine("data", "car", "test4.json"),
//                Path.Combine("data", "car", "test5.json")
            };

            // run all for them.
            foreach (var test in tests)
            {
                // read event template.
                var testData = JsonConvert.DeserializeObject<TestData>(
                    File.ReadAllText(test));

                Console.Write($"Running {test}");

                // run test
                var result = testData.Run();
                if (!result.success)
                {
#if DEBUG
                    Console.WriteLine($"...FAIL: {result.message}");
                    continue;
#endif
                    throw new Exception($"Test failed: {result.message}");
                }
                else
                {
                    Console.WriteLine("...OK");
                }
            }
        }
    }
}
