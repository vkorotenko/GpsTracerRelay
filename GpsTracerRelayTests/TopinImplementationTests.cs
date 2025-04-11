using Microsoft.VisualStudio.TestTools.UnitTesting;
using GpsTracerRelay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace GpsTracerRelay.Tests
{
    [TestClass()]
    public class TopinImplementationTests
    {
        [TestMethod()]
        public void GetDateTimeTest()
        {
            byte[]? buff = [0x0A, 0x03, 0x17, 0x0F, 0x32, 0x17];
            var date = new DateTime(2010, 3, 23, 15, 50, 23);

            byte[]? result = TopinImplementation.GetDateTime(date);

            Assert.AreEqual(buff[0], result[0]);
            Assert.AreEqual(buff[1], result[1]);
            Assert.AreEqual(buff[2], result[2]);
            Assert.AreEqual(buff[3], result[3]);
            Assert.AreEqual(buff[4], result[4]);
            Assert.AreEqual(buff[5], result[5]);
        }

        [TestMethod()]
        public void GetCoordinateTest()
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
            22.327658
             */

            /*
             *GPS latitude and longitude: 026B3F3E, longitude and latitude each occupy 4bytes, indicating positioning data,
             * latitude and longitude conversion method is as below:
               Convert the latitude and longitude values output by the gps module into fractions in minutes,
            then multiply the converted fractions by 30000 and convert the multiplied result to hexadecimal.
                  For example, 22"32.7658', (22X60+32.7658)X30000=40582974,
            converted to hexadecimal 0x02 0x6B 0x3F 0x3E,
            22X60 is convert ° to '.

            // Lat="51°19'44.4N" Lon="37°46'06.0E"

            22.327658
            <Track Lat="51.327103" Lon="37.765279" Alt="10" Orientation="51" Speed="400" />
             */
            // 
            // Extract degrees, minutes, and seconds
            var val = 22.54609666666666667;
            
            int degrees = (int)val;
            double remaining = (val - degrees) * 60;
            var tm = (degrees * 60 + remaining) * 30000;
            var res = (int)tm;


            
            var result = TopinImplementation.GetCoordinate(22.54609666666666667);
            byte[] buff = [0x02, 0x6B, 0x3F, 0x3E];
            Assert.AreEqual(buff[0], result[0]);
            Assert.AreEqual(buff[1], result[1]);
            Assert.AreEqual(buff[2], result[2]);
            Assert.AreEqual(buff[3], result[3]);




        }


        public static string DecimalDegreesToDms(double decimalDegrees, bool isLatitude)
        {
            // Determine direction (N/S for latitude, E/W for longitude)
            char direction = ' ';
            if (isLatitude)
                direction = decimalDegrees >= 0 ? 'N' : 'S';
            else
                direction = decimalDegrees >= 0 ? 'E' : 'W';

            decimalDegrees = Math.Abs(decimalDegrees);

            // Extract degrees, minutes, and seconds
            int degrees = (int)decimalDegrees;
            double remaining = (decimalDegrees - degrees) * 60;
            int minutes = (int)remaining;
            double seconds = (remaining - minutes) * 60;

            return $"{degrees}° {minutes}' {seconds:0.#####}\" {direction}";
        }
    }
}