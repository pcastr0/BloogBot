using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Bootstrapper
{
    /// <summary>
    /// Diagnostic tool to help identify the source of "Access is denied" errors
    /// Run this separately to check system permissions and prerequisites
    /// </summary>
    class DiagnosticTool
    {
        static void Main()
        {
            Console.WriteLine("=== BloogBot Bootstrapper Diagnostic Tool ===");
            Console.WriteLine();
            
            try
            {
                CheckAdminPrivileges();
                CheckFilePermissions();
                CheckWowExecutable();
                CheckLoaderDll();
                CheckProcessCreationPermissions();
                CheckMemoryOperations();
                
                Console.WriteLine("=== Diagnostic Complete ===");
                Console.WriteLine("If all checks pass but you still get 'Access is denied',");
                Console.WriteLine("the issue may be with antivirus software or Windows Defender.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Diagnostic failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static void CheckAdminPrivileges()
        {
            Console.WriteLine("1. Checking Administrator Privileges...");
            
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("   ✓ Running as Administrator");
            }
            else
            {
                Console.WriteLine("   ⚠ NOT running as Administrator");
                Console.WriteLine("   → Try running as Administrator to fix 'Access is denied' errors");
            }
            Console.WriteLine();
        }
        
        static void CheckFilePermissions()
        {
            Console.WriteLine("2. Checking File Permissions...");
            
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var settingsFile = Path.Combine(currentFolder, "bootstrapperSettings.json");
            var loaderDll = Path.Combine(currentFolder, "Loader.dll");
            
            CheckFileAccess(settingsFile, "bootstrapperSettings.json");
            CheckFileAccess(loaderDll, "Loader.dll");
            CheckDirectoryAccess(currentFolder, "Current Directory");
            
            Console.WriteLine();
        }
        
        static void CheckFileAccess(string filePath, string description)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    var accessControl = fileInfo.GetAccessControl();
                    Console.WriteLine($"   ✓ {description}: Readable");
                }
                else
                {
                    Console.WriteLine($"   ⚠ {description}: File not found at {filePath}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"   ✗ {description}: Access denied - {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ {description}: Error checking - {ex.Message}");
            }
        }
        
        static void CheckDirectoryAccess(string dirPath, string description)
        {
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);
                var accessControl = dirInfo.GetAccessControl();
                Console.WriteLine($"   ✓ {description}: Accessible");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"   ✗ {description}: Access denied - {dirPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ {description}: Error checking - {ex.Message}");
            }
        }
        
        static void CheckWowExecutable()
        {
            Console.WriteLine("3. Checking WoW Executable...");
            
            try
            {
                var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var settingsFile = Path.Combine(currentFolder, "bootstrapperSettings.json");
                
                if (File.Exists(settingsFile))
                {
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<BootstrapperSettings>(File.ReadAllText(settingsFile));
                    var wowPath = settings.PathToWoW;
                    
                    if (File.Exists(wowPath))
                    {
                        Console.WriteLine($"   ✓ WoW executable found: {wowPath}");
                        
                        // Check if we can get file info
                        var fileInfo = new FileInfo(wowPath);
                        Console.WriteLine($"   ✓ WoW file size: {fileInfo.Length:N0} bytes");
                        Console.WriteLine($"   ✓ WoW last modified: {fileInfo.LastWriteTime}");
                        
                        // Try to check if it's executable
                        var extension = Path.GetExtension(wowPath).ToLower();
                        if (extension == ".exe")
                        {
                            Console.WriteLine($"   ✓ WoW has .exe extension");
                        }
                        else
                        {
                            Console.WriteLine($"   ⚠ WoW extension is {extension}, expected .exe");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ✗ WoW executable not found: {wowPath}");
                        Console.WriteLine("   → Update bootstrapperSettings.json with correct WoW path");
                    }
                }
                else
                {
                    Console.WriteLine("   ⚠ bootstrapperSettings.json not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ Error checking WoW executable: {ex.Message}");
            }
            Console.WriteLine();
        }
        
        static void CheckLoaderDll()
        {
            Console.WriteLine("4. Checking Loader.dll...");
            
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var loaderPath = Path.Combine(currentFolder, "Loader.dll");
            
            if (File.Exists(loaderPath))
            {
                Console.WriteLine($"   ✓ Loader.dll found: {loaderPath}");
                
                try
                {
                    var fileInfo = new FileInfo(loaderPath);
                    Console.WriteLine($"   ✓ Loader.dll size: {fileInfo.Length:N0} bytes");
                    Console.WriteLine($"   ✓ Loader.dll last modified: {fileInfo.LastWriteTime}");
                    
                    // Check if it's a valid DLL
                    if (fileInfo.Length > 0)
                    {
                        Console.WriteLine($"   ✓ Loader.dll appears to be valid");
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠ Loader.dll is empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Error checking Loader.dll: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"   ✗ Loader.dll not found: {loaderPath}");
                Console.WriteLine("   → Build the solution to generate Loader.dll");
            }
            Console.WriteLine();
        }
        
        static void CheckProcessCreationPermissions()
        {
            Console.WriteLine("5. Checking Process Creation Permissions...");
            
            try
            {
                // Try to create a simple test process (notepad)
                var startInfo = new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process != null && !process.HasExited)
                    {
                        process.Kill();
                        Console.WriteLine("   ✓ Can create processes successfully");
                    }
                    else
                    {
                        Console.WriteLine("   ⚠ Process creation test failed");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Console.WriteLine($"   ✗ Process creation failed: {ex.Message}");
                Console.WriteLine("   → This may indicate security policy restrictions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ Process creation test error: {ex.Message}");
            }
            Console.WriteLine();
        }
        
        static void CheckMemoryOperations()
        {
            Console.WriteLine("6. Checking Memory Operation Permissions...");
            
            try
            {
                // Try to open current process with required permissions
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    var processHandle = currentProcess.Handle;
                    Console.WriteLine("   ✓ Can access current process handle");
                    
                    // Try a simple memory allocation test
                    var testPtr = WinImports.VirtualAllocEx(
                        processHandle,
                        IntPtr.Zero,
                        1024,
                        WinImports.MemoryAllocationType.MEM_COMMIT,
                        WinImports.MemoryProtectionType.PAGE_EXECUTE_READWRITE);
                    
                    if (testPtr != IntPtr.Zero)
                    {
                        WinImports.VirtualFreeEx(processHandle, testPtr, 0, WinImports.MemoryFreeType.MEM_RELEASE);
                        Console.WriteLine("   ✓ Can allocate and free memory in processes");
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        Console.WriteLine($"   ⚠ Memory allocation test failed: Error {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠ Memory operation test error: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
}