namespace SmartX_Part1.Models;

// Required operator overloading example.
public class PowerReading
{
    public string MeterName { get; set; }
    public int Watts { get; set; }

    public PowerReading(string meterName, int watts)
    {
        MeterName = meterName;
        Watts = watts;
    }

    public static PowerReading operator +(PowerReading left, PowerReading right)
    {
        return new PowerReading(
            $"{left.MeterName} + {right.MeterName}",
            left.Watts + right.Watts
        );
    }
}
