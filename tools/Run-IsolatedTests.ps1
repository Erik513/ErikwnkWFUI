# Runs "dotnet test ..." on a desktop of its own (a hidden one): the windows that the tests make live there, so they can neither take the
# focus nor minimize or cover anything on the screen of the person who works. The output goes to a file, which is printed at the end.
param(
    [Parameter(Mandatory = $true)][string]$Arguments,
    [string]$WorkingDirectory = (Get-Location).Path,
    [int]$TimeoutSeconds = 900
)

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class IsolatedDesktop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr devmode, int flags, uint access, IntPtr security);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseDesktop(IntPtr desktop);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcess(string application, string commandLine, IntPtr processSecurity, IntPtr threadSecurity, bool inherit,
        uint flags, IntPtr environment, string directory, ref STARTUPINFO startup, out PROCESS_INFORMATION process);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern bool TerminateProcess(IntPtr process, uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    public static int Run(string commandLine, string directory, int timeoutSeconds)
    {
        string name = "ErikwnkTests" + Guid.NewGuid().ToString("N").Substring(0, 8);
        IntPtr desktop = CreateDesktop(name, IntPtr.Zero, IntPtr.Zero, 0, 0x10000000, IntPtr.Zero);   // GENERIC_ALL

        if (desktop == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        try
        {
            STARTUPINFO startup = new STARTUPINFO();
            startup.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            startup.lpDesktop = "WinSta0\\" + name;
            PROCESS_INFORMATION process;

            if (!CreateProcess(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, 0x08000000 /* CREATE_NO_WINDOW */, IntPtr.Zero, directory, ref startup, out process))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                uint wait = WaitForSingleObject(process.hProcess, (uint)(timeoutSeconds * 1000));

                if (wait != 0)
                {
                    TerminateProcess(process.hProcess, 1);
                    return -1;
                }

                uint code;
                GetExitCodeProcess(process.hProcess, out code);

                return (int)code;
            }
            finally
            {
                CloseHandle(process.hThread);
                CloseHandle(process.hProcess);
            }
        }
        finally
        {
            CloseDesktop(desktop);
        }
    }
}
"@

$out = Join-Path $env:TEMP ("isolated_tests_" + [Guid]::NewGuid().ToString("N").Substring(0, 8) + ".txt")
$command = 'cmd.exe /c "dotnet test ' + $Arguments + ' > "' + $out + '" 2>&1"'
$code = [IsolatedDesktop]::Run($command, $WorkingDirectory, $TimeoutSeconds)
Get-Content $out -ErrorAction SilentlyContinue | Select-Object -Last 40
Remove-Item $out -ErrorAction SilentlyContinue
"exit code: $code"
