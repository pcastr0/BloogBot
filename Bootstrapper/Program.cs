using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static Bootstrapper.WinImports;

namespace Bootstrapper
{
    class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("=== Bootstrapper Starting ===");
                
                var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine($"Current folder: {currentFolder}");
                
                var bootstrapperSettingsFilePath = Path.Combine(currentFolder, "bootstrapperSettings.json");
                Console.WriteLine($"Settings file path: {bootstrapperSettingsFilePath}");
                
                if (!File.Exists(bootstrapperSettingsFilePath))
                {
                    throw new FileNotFoundException($"Bootstrapper settings file not found: {bootstrapperSettingsFilePath}");
                }
                
                var bootstrapperSettings = JsonConvert.DeserializeObject<BootstrapperSettings>(File.ReadAllText(bootstrapperSettingsFilePath));
                Console.WriteLine($"Path to WoW: {bootstrapperSettings.PathToWoW}");
                
                if (!File.Exists(bootstrapperSettings.PathToWoW))
                {
                    throw new FileNotFoundException($"WoW executable not found: {bootstrapperSettings.PathToWoW}");
                }
                
                var startupInfo = new STARTUPINFO();
                startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);

                Console.WriteLine("Attempting to create WoW process...");
                Marshal.GetLastWin32Error(); // Clear last error
                
                // run BloogBot.exe in a new process
                bool success = CreateProcess(                                                                          
                    bootstrapperSettings.PathToWoW,
                    null,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    ProcessCreationFlag.CREATE_DEFAULT_ERROR_MODE,
                    IntPtr.Zero,
                    null, 
                    ref startupInfo,
                    out PROCESS_INFORMATION processInfo);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to create WoW process. Error code: {error} - {GetErrorMessage(error)}");
                }
                
                Console.WriteLine($"WoW process created successfully. PID: {processInfo.dwProcessId}");

                // this seems to help prevent timing issues
                Thread.Sleep(1000);

                Console.WriteLine("Getting process handle...");
                Process wowProcess;
                try
                {
                    wowProcess = Process.GetProcessById((int)processInfo.dwProcessId);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException($"Failed to get WoW process by ID {processInfo.dwProcessId}. Process may have exited.", ex);
                }
                
                var processHandle = wowProcess.Handle;
                Console.WriteLine("Process handle obtained successfully.");

                // resolve by file path to Loader.dll relative to our current working directory
                var loaderPath = Path.Combine(currentFolder, "Loader.dll");
                Console.WriteLine($"Loader.dll path: {loaderPath}");
                
                if (!File.Exists(loaderPath))
                {
                    throw new FileNotFoundException($"Loader.dll not found: {loaderPath}");
                }

                Console.WriteLine("Allocating memory in target process...");
                Marshal.GetLastWin32Error(); // Clear last error
                
                // allocate enough memory to hold the full file path to Loader.dll within the BloogBot process
                var loaderPathPtr = VirtualAllocEx(
                    processHandle, 
                    (IntPtr)0, 
                    loaderPath.Length * 2, // Unicode requires 2 bytes per character
                    MemoryAllocationType.MEM_COMMIT, 
                    MemoryProtectionType.PAGE_EXECUTE_READWRITE);

                if (loaderPathPtr == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to allocate memory for Loader.dll. Error code: {error} - {GetErrorMessage(error)}");
                }
                
                Console.WriteLine($"Memory allocated successfully at address: 0x{loaderPathPtr:X8}");

                // this seems to help prevent timing issues
                Thread.Sleep(500);

                Console.WriteLine("Writing Loader.dll path to target process memory...");
                Marshal.GetLastWin32Error(); // Clear last error
                
                // write the file path to Loader.dll to the WoW process's memory
                var bytes = Encoding.Unicode.GetBytes(loaderPath);
                var bytesWritten = 0;
                bool writeSuccess = WriteProcessMemory(processHandle, loaderPathPtr, bytes, bytes.Length, ref bytesWritten);
                
                if (!writeSuccess || bytesWritten == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to write Loader.dll into the WoW.exe process. Error code: {error} - {GetErrorMessage(error)}. Bytes written: {bytesWritten}");
                }
                
                Console.WriteLine($"Successfully wrote {bytesWritten} bytes to target process.");

                // this seems to help prevent timing issues
                Thread.Sleep(1000);

                Console.WriteLine("Getting LoadLibraryW address...");
                Marshal.GetLastWin32Error(); // Clear last error
                
                // search current process's for the memory address of the LoadLibraryW function within the kernel32.dll module
                var kernel32Handle = GetModuleHandle("kernel32.dll");
                if (kernel32Handle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to get kernel32.dll handle. Error code: {error} - {GetErrorMessage(error)}");
                }
                
                var loaderDllPointer = GetProcAddress(kernel32Handle, "LoadLibraryW");
                if (loaderDllPointer == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to get LoadLibraryW address. Error code: {error} - {GetErrorMessage(error)}");
                }
                
                Console.WriteLine($"LoadLibraryW address: 0x{loaderDllPointer:X8}");

                // this seems to help prevent timing issues
                Thread.Sleep(1000);

                Console.WriteLine("Creating remote thread...");
                Marshal.GetLastWin32Error(); // Clear last error
                
                // create a new thread with the execution starting at the LoadLibraryW function, 
                // with the path to our Loader.dll passed as a parameter
                var remoteThread = CreateRemoteThread(processHandle, (IntPtr)null, (IntPtr)0, loaderDllPointer, loaderPathPtr, 0, (IntPtr)null);
                
                if (remoteThread == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to create remote thread to start execution of Loader.dll in the WoW.exe process. Error code: {error} - {GetErrorMessage(error)}");
                }
                
                Console.WriteLine("Remote thread created successfully.");

                // this seems to help prevent timing issues
                Thread.Sleep(1000);

                Console.WriteLine("Cleaning up allocated memory...");
                bool freeSuccess = VirtualFreeEx(processHandle, loaderPathPtr, 0, MemoryFreeType.MEM_RELEASE);
                if (!freeSuccess)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine($"Warning: Failed to free allocated memory. Error code: {error} - {GetErrorMessage(error)}");
                }
                else
                {
                    Console.WriteLine("Memory freed successfully.");
                }
                
                Console.WriteLine("=== Bootstrapper Completed Successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== BOOTSTRAPPER ERROR ===");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("========================");
                
                // Keep console open for debugging
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
        
        static string GetErrorMessage(int errorCode)
        {
            return new System.ComponentModel.Win32Exception(errorCode).Message;
        }
    }
}
