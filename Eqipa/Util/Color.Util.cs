namespace Eqipa.Util;

public static class ColorUtil
{
  public static int ConvertHexToDecimalColor(string? colorHex)
  {
    if (string.IsNullOrEmpty(colorHex) || !colorHex.StartsWith("#")) return 0x5865F2; // Default Discord blue
    return int.Parse(colorHex.Substring(1), System.Globalization.NumberStyles.HexNumber);
  }
}