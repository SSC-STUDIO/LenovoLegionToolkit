using System;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System.Management;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

internal static class SensorReadingHelper
{
    public static int NormalizePowerReadingToWatts(object? value)
    {
        if (!TryConvertToDouble(value, out var raw) || raw <= 0)
            return -1;

        // Windows power providers most commonly expose mW. A few counters expose W.
        var watts = raw > 1000 ? raw / 1000d : raw;
        if (watts <= 0 || watts > 1000)
            return -1;

        return (int)Math.Round(watts, MidpointRounding.AwayFromZero);
    }

    public static int ConvertAcpiTenthsKelvinToCelsius(object? value)
    {
        if (!TryConvertToDouble(value, out var tenthsKelvin) || tenthsKelvin <= 0)
            return -1;

        var celsius = tenthsKelvin / 10d - 273.15d;
        if (celsius < -50 || celsius > 150)
            return -1;

        return (int)Math.Round(celsius, MidpointRounding.AwayFromZero);
    }

    public static async Task<int> GetCpuWattageFromWmiAsync()
    {
        var result = await TryReadPowerMeterAsync($"SELECT * FROM Win32_PowerMeter WHERE Name LIKE '%CPU%' OR Name LIKE '%Processor%'").ConfigureAwait(false);
        if (result >= 0)
            return result;

        return await TryReadPowerMeterAsync($"SELECT * FROM Win32_PowerMeter").ConfigureAwait(false);
    }

    public static async Task<int> GetCpuTemperatureFromAcpiAsync()
    {
        try
        {
            var temperatures = await WMI.ReadAsync("root\\WMI",
                $"SELECT * FROM MSAcpi_ThermalZoneTemperature",
                pdc => ConvertAcpiTenthsKelvinToCelsius(pdc["CurrentTemperature"]?.Value)).ConfigureAwait(false);

            return temperatures.Where(t => t > 0).DefaultIfEmpty(-1).Max();
        }
        catch (ManagementException)
        {
            return -1;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    private static async Task<int> TryReadPowerMeterAsync(FormattableString query)
    {
        try
        {
            var readings = await WMI.ReadAsync("root\\CIMV2\\power",
                query,
                pdc => NormalizePowerReadingToWatts(
                    pdc["Power"]?.Value
                    ?? pdc["CurrentPower"]?.Value
                    ?? pdc["PowerReading"]?.Value)).ConfigureAwait(false);

            return readings.Where(w => w > 0).DefaultIfEmpty(-1).Max();
        }
        catch (ManagementException)
        {
            return -1;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        if (value is null)
            return false;

        try
        {
            result = value switch
            {
                string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0,
                IConvertible convertible => convertible.ToDouble(CultureInfo.InvariantCulture),
                _ => 0
            };

            return result > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
