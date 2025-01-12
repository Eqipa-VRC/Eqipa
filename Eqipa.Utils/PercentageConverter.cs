namespace Eqipa.Util;

public static class PercentageConverter
{
  /// <summary>
  /// Converts a value within a specified range to a percentage.
  /// </summary>
  public static double Convert(double value, double min, double max)
  {
    value = Math.Max(min, Math.Min(value, max));

    return ((value - min) / (max - min)) * 100;
  }
}