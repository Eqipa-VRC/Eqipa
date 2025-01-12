using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Eqipa.Util;

internal static class Debugger
{
  [DllImport("DbgHelp.dll")]
  private static extern bool MiniDumpWriteDump(
      IntPtr hProcess,
      uint processId,
      IntPtr hFile,
      MINIDUMP_TYPE dumpType,
      ref MINIDUMP_EXCEPTION_INFORMATION exceptionParam,
      IntPtr userStreamParam,
      IntPtr callbackParam);

  [Flags]
  private enum MINIDUMP_TYPE : uint
  {
    MiniDumpNormal = 0x00000000,
    MiniDumpWithDataSegs = 0x00000001,
    MiniDumpWithFullMemory = 0x00000002,
    MiniDumpWithHandleData = 0x00000004,
    MiniDumpFilterMemory = 0x00000008,
    MiniDumpScanMemory = 0x00000010,
    MiniDumpWithUnloadedModules = 0x00000020,
    MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
    MiniDumpFilterModulePaths = 0x00000080,
    MiniDumpWithProcessThreadData = 0x00000100,
    MiniDumpWithPrivateReadWriteMemory = 0x00000200,
    MiniDumpWithoutOptionalData = 0x00000400,
    MiniDumpWithFullMemoryInfo = 0x00000800,
    MiniDumpWithThreadInfo = 0x00001000,
    MiniDumpWithCodeSegs = 0x00002000,
    MiniDumpWithoutAuxiliaryState = 0x00004000,
    MiniDumpWithFullAuxiliaryState = 0x00008000,
    MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
    MiniDumpIgnoreInaccessibleMemory = 0x00020000,
    MiniDumpWithTokenInformation = 0x00040000,
    MiniDumpWithModuleHeaders = 0x00080000,
    MiniDumpFilterTriage = 0x00100000,
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  private struct MINIDUMP_EXCEPTION_INFORMATION
  {
    public uint ThreadId;
    public IntPtr ExceptionPointers;
    public bool ClientPointers;
  }

  public static void CreateMiniDump(string dumpFilePath)
  {
    using (FileStream fs = new FileStream(dumpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
    {
      Process currentProcess = Process.GetCurrentProcess();
      IntPtr hProcess = currentProcess.Handle;
      uint processId = (uint)currentProcess.Id;
      MINIDUMP_EXCEPTION_INFORMATION exceptionInfo = new MINIDUMP_EXCEPTION_INFORMATION();

      MiniDumpWriteDump(
          hProcess,
          processId,
          fs.SafeFileHandle.DangerousGetHandle(),
          MINIDUMP_TYPE.MiniDumpWithFullMemory,
          ref exceptionInfo,
          IntPtr.Zero,
          IntPtr.Zero);
    }
  }

  public static void SetupExceptionHandling()
  {
    AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
    {
      var exception = args.ExceptionObject as Exception;
      string logFilePath = $"crashes/crash_{DateTime.Now:yyyyMMdd_HHmmss}.log";
      string dumpFilePath = $"crashes/crash_dump_{DateTime.Now:yyyyMMdd_HHmmss}.dmp";

      LogException(exception!, logFilePath);
      CreateMiniDump(dumpFilePath);
    };
  }

  private static void LogException(Exception exception, string logFilePath)
  {
    using (StreamWriter writer = new StreamWriter(logFilePath))
    {
      writer.WriteLine("Exception occurred: {0}", DateTime.Now);
      writer.WriteLine("Exception message: {0}", exception.Message);
      writer.WriteLine("Stack trace: {0}", exception.StackTrace);

      if (exception.InnerException != null)
      {
        writer.WriteLine("Inner exception message: {0}", exception.InnerException.Message);
        writer.WriteLine("Inner exception stack trace: {0}", exception.InnerException.StackTrace);
      }
    }
  }
}
