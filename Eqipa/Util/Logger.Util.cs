using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Eqipa.Util;

public enum LogLevel
{
  Info,
  Step,
  Warn,
  Error,
  Debug
}

/// <summary>
/// Logs messages to the console
/// </summary>
internal static class Logger
{
  private const string INFO_COLOR = "\x1b[32m";
  private const string WARN_COLOR = "\x1b[33m";
  private const string ERROR_COLOR = "\x1b[31m";
  private const string DEBUG_COLOR = "\x1b[34m";
  private const string RESET_COLOR = "\x1b[0m";
  private const string NAMESPACE_COLOR = "\x1b[95m";

  #region Utils

  /// <summary>
  /// Returns the namespace of the class that called
  /// </summary>
  private static string GetNamespace(
      string namespaceName,
      string className,
      string memberName,
      string fileName,
      int sourceLineNumber)
  {
    // string classStr = className != nameof(Program) ? $"{className}" : "";
    string memberStr = memberName != ".ctor" ? $"::{CleanAsyncMethodName(memberName)}()" : string.Empty;

    string prefix = "";
    string suffix = "";
#if DEBUG
    suffix = $" ({fileName}:{sourceLineNumber})";
    prefix = $" [{namespaceName}/{className}{memberStr}]";
#endif

    return $"{NAMESPACE_COLOR}{prefix}{RESET_COLOR}{suffix}";
  }

  /// <summary>
  /// Cleans up compiler-generated async method names
  /// </summary>
  private static string CleanAsyncMethodName(string memberName)
  {
    // Remove the compiler-generated async state machine suffix
    if (memberName.Contains("d__"))
    {
      return memberName.Split("d__")[0];
    }

    // Handle MoveNext from async state machine
    if (memberName == "MoveNext")
    {
      var frame = new StackFrame(3, false); // Skip additional frames to find original method
      var method = frame.GetMethod();
      var declaringType = method?.DeclaringType;

      if (declaringType?.Name.Contains("d__") == true)
      {
        // Extract the original method name from the state machine class name
        var originalName = declaringType.Name.Split("d__")[0];
        return originalName;
      }
    }

    return memberName;
  }

  /// <summary>
  /// Returns the name of the class that called
  /// </summary>
  private static string GetClassName()
  {
    var frame = new StackFrame(2, false);
    var method = frame.GetMethod();
    var declaringType = method?.DeclaringType;

    // Handle async state machine class names
    if (declaringType?.Name.Contains("d__") == true)
    {
      // Get the containing type (original class) instead of state machine
      return declaringType.DeclaringType?.Name ?? "UnknownClass";
    }

    return declaringType?.Name ?? "UnknownClass";
  }

  #endregion

  /// <summary>
  /// Logs a message to the console
  /// </summary>
  /// <param name="level">The log level</param>
  /// <param name="message">The message to log</param>
  public static void Log(
    LogLevel level,
    string message,
    [CallerFilePath] string sourceFilePath = "",
    [CallerLineNumber] int sourceLineNumber = 0,
    [CallerMemberName] string sourceMemberName = ""
  )
  {
    var method = new StackFrame(1, true).GetMethod();
    var fileName = Path.GetFileName(sourceFilePath);
    var className = GetClassName();
    var declaringType = method?.DeclaringType;
    var namespaceName = declaringType?.Namespace ?? "UnknownNamespace";

    string levelColor = level switch
    {
      LogLevel.Info => INFO_COLOR,
      LogLevel.Step => DEBUG_COLOR,
      LogLevel.Warn => WARN_COLOR,
      LogLevel.Debug => DEBUG_COLOR,
      LogLevel.Error => ERROR_COLOR,
      _ => RESET_COLOR
    };

    string levelString = level switch
    {
      LogLevel.Info => "INFO",
      LogLevel.Step => "STEP",
      LogLevel.Warn => "WARN",
      LogLevel.Debug => "DEBUG",
      LogLevel.Error => "ERR",
      _ => "N/A"
    };

    Console.WriteLine($"{DateTime.Now} :: {levelColor}[{levelString}]{RESET_COLOR}{GetNamespace(namespaceName, className, sourceMemberName, fileName, sourceLineNumber)} {message}");
  }
}