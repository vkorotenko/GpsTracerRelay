using System.Xml.Serialization;

namespace GpsTracerRelay
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {

            if (args.Length == 4)
            {
                await GenerateTracks(args[1], args[2] , args[3]);
                return;
            }

            if (args.Length != 2)
            {
                Usage();
                return;
            }
            var ser = new XmlSerializer(typeof(Settings));
            await using var stream = new FileStream(args[0], FileMode.Open);
            var setting = ser.Deserialize(stream) as Settings;
            if (setting == null)
            {
                Console.WriteLine("Couldn't read setting.xml");
                return;
            }

            var tracks = GetTracks(args[1]);
            var topin = new TopinImplementation(setting.Server, setting.Port, setting.Imei);
            var t = await topin.Login();
            foreach (var point in tracks.Points)
            {
                Console.WriteLine($"Tick: {point}");
                await topin.Heartbeet();
                await topin.GpsPosition(point);
                await topin.GpsPositionOffline(point);
                Thread.Sleep(setting.Interval * 1000);
            }
        }

        private static async Task GenerateTracks(string source, string dist, string interval)
        {
            var ser = new XmlSerializer(typeof(Tracks));
            await using var stream = new FileStream(source, FileMode.Open);
            var tracks = ser.Deserialize(stream) as Tracks;

        }

        private static Tracks GetTracks(string file)
        {
            var ser = new XmlSerializer(typeof(Tracks));
            using var stream = new FileStream(file, FileMode.Open);
            var data = ser.Deserialize(stream) as Tracks;
            return data;
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: GpsTracerRelay config file tracks file");
            Console.WriteLine("example: GpsTracerRelay settings.xml track.xml");
            Console.WriteLine("Or usage: GpsTracerRelay -g source_file dist_file interval in second");
            Console.WriteLine("Example: GpsTracerRelay -g src.xml dst.xml 30");
        }
    }
}
