using System.Xml.Serialization;

namespace GpsTracerRelay;

public class Track
{
    [XmlAttribute]
    public double Lat { get; set; }
    [XmlAttribute]
    public double Lon { get; set; }
    [XmlAttribute]
    public double Alt { get; set; }
    [XmlAttribute]
    public int Orientation { get; set; }
    [XmlAttribute]
    public float  Speed { get; set; }
}