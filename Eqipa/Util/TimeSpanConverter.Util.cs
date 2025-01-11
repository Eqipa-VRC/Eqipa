using System.Globalization;
using System.Text.RegularExpressions;

namespace Eqipa.Util;

public class TimeSpanConverter
{
  private const double Millisecond = 1;
  private const double Second = Millisecond * 1000;
  private const double Minute = Second * 60;
  private const double Hour = Minute * 60;
  private const double Day = Hour * 24;
  private const double Week = Day * 7;
  private const double Year = Day * 365.25;

  public static double Parse(string value)
  {
    if (string.IsNullOrWhiteSpace(value) || value.Length > 100)
    {
      throw new ArgumentException("Value must be a non-empty string with length between 1 and 99.");
    }

    var match = Regex.Match(value, @"^(?<value>-?(?:\d+)?\.?\d*)\s*(?<type>milliseconds?|msecs?|ms|seconds?|secs?|s|minutes?|mins?|m|hours?|hrs?|h|days?|d|weeks?|w|years?|yrs?|y)?$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return double.NaN;
    }

    var groups = match.Groups;
    if (!double.TryParse(groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
    {
      return double.NaN;
    }

    var type = groups["type"].Value.ToLowerInvariant();
    return type switch
    {
      "years" or "year" or "yrs" or "yr" or "y" => number * Year,
      "weeks" or "week" or "w" => number * Week,
      "days" or "day" or "d" => number * Day,
      "hours" or "hour" or "hrs" or "hr" or "h" => number * Hour,
      "minutes" or "minute" or "mins" or "min" or "m" => number * Minute,
      "seconds" or "second" or "secs" or "sec" or "s" => number * Second,
      "milliseconds" or "millisecond" or "msecs" or "msec" or "ms" or "" => number * Millisecond,
      _ => throw new ArgumentException($"The unit '{type}' is not recognized.")
    };
  }

  public static string Format(double milliseconds, bool longFormat = false)
  {
    if (double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
    {
      throw new ArgumentException("Value must be a finite number.");
    }

    return longFormat ? FormatLong(milliseconds) : FormatShort(milliseconds);
  }

  private static string FormatShort(double ms)
  {
    var absMs = Math.Abs(ms);
    if (absMs >= Day)
      return $"{Math.Round(ms / Day)}d";
    if (absMs >= Hour)
      return $"{Math.Round(ms / Hour)}h";
    if (absMs >= Minute)
      return $"{Math.Round(ms / Minute)}m";
    if (absMs >= Second)
      return $"{Math.Round(ms / Second)}s";

    return $"{ms}ms";
  }

  private static string FormatLong(double ms)
  {
    var absMs = Math.Abs(ms);
    if (absMs >= Day)
      return Plural(ms, absMs, Day, "day");
    if (absMs >= Hour)
      return Plural(ms, absMs, Hour, "hour");
    if (absMs >= Minute)
      return Plural(ms, absMs, Minute, "minute");
    if (absMs >= Second)
      return Plural(ms, absMs, Second, "second");

    return $"{ms} ms";
  }

  private static string Plural(double ms, double absMs, double unit, string singular)
  {
    var value = Math.Round(ms / unit);
    var plural = absMs >= unit * 1.5;
    return $"{value} {singular}{(plural ? "s" : "")}";
  }

  public static double ParseStrict(string value)
  {
    return Parse(value);
  }

  public static bool IsError(Exception ex)
  {
    return ex != null && ex is Exception;
  }

  // Example usage
  // public static void Main()
  // {
  //   try
  //   {
  // Parse examples
  //     Console.WriteLine(Parse("2h")); // 7200000
  //     Console.WriteLine(Parse("5.5 days")); // 475200000
  //     Console.WriteLine(Parse("invalid")); // NaN
  // Format examples
  //     Console.WriteLine(Format(7200000)); // "2h"
  //     Console.WriteLine(Format(475200000, true)); // "5 days"
  //   }
  //   catch (Exception ex)
  //   {
  //     Console.WriteLine($"Error: {ex.Message}");
  //   }
  // }
}
