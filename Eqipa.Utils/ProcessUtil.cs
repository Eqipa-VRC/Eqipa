using System.Diagnostics;

namespace Eqipa.Util;

/// <summary>
/// Provides utility methods for working with processes.
/// </summary>
internal static class ProcessUtil
{
  /// <summary>
  /// Gets the first process with the specified name.
  /// </summary>
  /// <param name="name">The name of the process to find.</param>
  /// <returns>The first <see cref="Process"/> with the specified name, or <c>null</c> if no such process is found.</returns>
  public static Process? GetProcessByName(string name)
  {
    return Process.GetProcessesByName(name).FirstOrDefault();
  }

  /// <summary>
  /// Gets all processes with the specified name.
  /// </summary>
  /// <param name="name">The name of the processes to find.</param>
  /// <returns>An array of <see cref="Process"/> objects matching the specified name, or an empty array if no such processes are found.</returns>
  public static Process[] GetProcessesByName(string name)
  {
    return Process.GetProcessesByName(name);
  }
}
