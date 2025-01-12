namespace Eqipa.Util;

public static class StringUtil
{
  public static string Replace<T>(string input, string search, T replacement)
  {
    int index = input.IndexOf(search);
    if (index < 0)
      return input;

    return input.Substring(0, index) + replacement + input.Substring(index + search.Length);
  }
}