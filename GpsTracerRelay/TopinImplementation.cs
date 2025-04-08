using System;
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

        public async Task GpsData(Track track)
        {
            byte[] rp = [0x01, 0x08];
            var rv = _header
                .Concat(rp)
                .Concat(_footer).ToArray();

            await _stream.WriteAsync(rv, 0, rv.Length);
            Thread.Sleep(200);
            //    7878 12 10 0a03170f3217 9c 026b3f3e 0c22ad65 1f 3460 0d0a
            // Eg.7878 12 10 0A03170F3217 9C 026B3F3E 0C22AD65 1F 3460 0D0A
            rp = [0x12, 0x10,
            0x0A,0x03,0x17, 0x0F, 0x32, 0x17,
            0x9C,
            0x02,0x6b, 0x3F, 0x3E,
            0x0C, 0x22, 0xAD, 0x65,
            0x1F,
            0x34, 0x60
            ];
            rv = _header
                .Concat(rp)
                .Concat(_footer).ToArray();

            await _stream.WriteAsync(rv, 0, rv.Length);
            Thread.Sleep(200);
            rv[3] = 0x11;
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
