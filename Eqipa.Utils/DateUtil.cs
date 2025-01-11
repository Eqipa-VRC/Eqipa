namespace Eqipa.Util;

internal static class DateUtil
{
  public static long ToUnixTime(this DateTime dateTime) => ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
}