using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GpsTracerRelay
{

    /// <summary>
    /// Minimum support is required to implement location functionality:
    /// 0x01 login data packet,
    /// 0x08 heartbeat data packet,
    /// 0x10 GPS online location data,
    /// 0x11 GPS offline location data,
    /// 0x13 status data packet,
    /// 0x17 offline WIFILBS location data packet,
    /// 0x30 time synchronization,
    /// 0x69 online WIFILBS data packet,
    /// 0x57 Parameter setting packet.
    /// </summary>
    public class TopinImplementation
    {
        private readonly string _server;
        private readonly int _port;
        private TcpClient _client;
        private readonly byte[] _header = [0x78, 0x78];
        private readonly byte[] _footer = [0x0D, 0x0A];
        private readonly long _imei;
        private NetworkStream _stream;
        public TopinImplementation(string server, int port, long imei)
        {
            _port = port;
            _server = server;
            _client = new();
            _imei = imei;
        }
        /// <summary>
        /// Login package 0x01
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Login()
        {
            // Start bit 2byte Reserved bit 1byte Protocol number 1byte IMEI 8byte Software version number 1byte Stop bit 2byte
            // Eg.7878 0A 01 0123456789012345 01 0D0A

            byte[] rp = [0x0A, 0x01];
            byte[] sv = [0x01];
            var imei = ToBcd(_imei);
            var rv = _header
                .Concat(rp)
                .Concat(imei)
                .Concat(sv)
                .Concat(_footer).ToArray();
            var addr = IPAddress.Parse(_server);
            var endPoint = new IPEndPoint(addr, _port);

            try
            {
                await _client.ConnectAsync(endPoint);
                _stream = _client.GetStream();
                await _stream.WriteAsync(rv, 0, rv.Length);
                var buffer = new byte[1_024];

                var received = await _stream.ReadAsync(buffer);


                /*
                 Eg.7878 01 01 0D0A successfully logged in
                   7878 01 44 0D0A login failed

                 */
                if (buffer[3] == 1)
                {
                    Console.WriteLine("logged in");
                    return true;
                }
                if (buffer[3] == 4)
                {
                    Console.WriteLine("login failed");
                }
                //var message = Encoding.UTF8.GetString(buffer, 0, received);
                //Console.WriteLine($"Message received: \"{message}\"");
            }
            catch (Exception e)
            {
                ;
            }

            return false;
        }


        /// <summary>
        /// Heartbeet 0x08
        /// </summary>
        /// <returns></returns>
        public async Task Heartbeet()
        {
            byte[] rp = [0x01, 0x08];
            var rv = _header
                .Concat(rp)
                .Concat(_footer).ToArray();

            await _stream.WriteAsync(rv, 0, rv.Length);
            Thread.Sleep(200);
        }
        /// <summary>
        /// 0x10 GPS positioning data packet
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public async Task GpsPosition(Track track)
        {
            var dateTime = GetDateTime(DateTime.UtcNow);
            byte[] body = [0x12, 0x10];
            var lat = GetCoordinate(track.Lat);
            var lon = GetCoordinate(track.Lon);
            body = body
                .Concat(dateTime)
                .Concat<byte>([0x9C])
                .Concat<byte>(lat)
                .Concat<byte>(lon)
                .Concat<byte>(GetSpeed(track))
                .Concat<byte>([0x34, 0x60])
                .ToArray();


            var package = _header
                .Concat(body)
                .Concat(_footer).ToArray();

            await _stream.WriteAsync(package, 0, package.Length);

            if (_stream.DataAvailable)
            {
                var buffer = new byte[1_024];

                var received = await _stream.ReadAsync(buffer);
                Console.WriteLine("read data");
            }
            Thread.Sleep(200);
        }

        public static byte[] GetCoordinate(double val)
        {
            /*
             *GPS latitude and longitude: 026B3F3E, longitude and latitude each occupy 4bytes, indicating positioning data,
             * latitude and longitude conversion method is as below:
               Convert the latitude and longitude values output by the gps module into fractions in minutes, 
            then multiply the converted fractions by 30000 and convert the multiplied result to hexadecimal.
                  For example, 22"32.7658', (22X60+32.7658)X30000=40582974, 
            converted to hexadecimal 0x02 0x6B 0x3F 0x3E, 
            22X60 is convert ° to '.
            
            
            
            22.327658
             */


            var nn = (int)val;
            var nnl = (int) Math.Ceiling((val- nn) * 1000000);
            var str = nnl.ToString();
            str = str.Substring(0, str.Length - 4) + "." + str.Substring(str.Length-4);
            var ci = new CultureInfo("en-US");
            var rmk = double.Parse(str,ci);
            var ds = 32.7658;
            var res = ((nn * 60) + rmk) * 30_000;

            
            var bb = BitConverter.GetBytes((int)res).Reverse().ToArray();
            var hs = Convert.ToHexString(bb);
            return bb;
        }
        private byte[] GetSpeed(Track track)
        {
            return track.Speed > 0xff ? new byte[0xff] : new byte[(byte)track.Speed];
        }
        /// <summary>
        /// Get bytes from datetime GMT0
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public static byte[] GetDateTime(DateTime now)
        {
            byte[] buff = [0x0A, 0x03, 0x17, 0x0F, 0x32, 0x17];
            buff[5] = (byte)now.Second;
            buff[4] = (byte)now.Minute;
            buff[3] = (byte)now.Hour;
            buff[2] = (byte)now.Day;
            buff[1] = (byte)now.Month;
            buff[0] = (byte)(now.Year - 2000);
            return buff;
        }
        /// <summary>
        /// 0x11 offline GPS positioning data package
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public async Task GpsPositionOffline(Track track)
        {

            byte[] rp = [0x12, 0x11,
                0x0A,0x03,0x17, 0x0F, 0x32, 0x17,
                0x9C,
                0x02,0x6b, 0x3F, 0x3E,
                0x0C, 0x22, 0xAD, 0x65,
                0x1F,
                0x34, 0x60
            ];
            var rv = _header
                .Concat(rp)
                .Concat(_footer).ToArray();

            await _stream.WriteAsync(rv, 0, rv.Length);
            if (_stream.DataAvailable)
            {
                var buffer = new byte[1_024];

                var received = await _stream.ReadAsync(buffer);
                Console.WriteLine("read data");
            }

        }
        public static byte[] ToBcd(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative");

            var digits = value.ToString();
            if (digits.Length % 2 != 0)
            {
                digits = "0" + digits; // Pad with leading zero if odd number of digits
            }

            byte[] bcd = new byte[digits.Length / 2];

            for (int i = 0; i < digits.Length; i += 2)
            {
                byte upper = (byte)(digits[i] - '0');
                byte lower = (byte)(digits[i + 1] - '0');
                bcd[i / 2] = (byte)((upper << 4) | lower);
            }

            return bcd;
        }
    }
}
