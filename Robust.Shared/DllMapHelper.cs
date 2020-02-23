using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared
{
    internal static class DllMapHelper
    {
        [Conditional("NETCOREAPP")]
        public static void RegisterSimpleMap(Assembly assembly, string baseName)
        {
            // On .NET Framework this doesn't need to run because:
            // On Windows, the DLL names should check out correctly to just work.
            // On Linux/macOS, Mono's DllMap handles it for us.
#if NETCOREAPP
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // DLL names should line up on Windows by default.
                // So a hook won't do anything.
                return;
            }

            NativeLibrary.SetDllImportResolver(assembly, (name, _, __) =>
            {
                if (name == $"{baseName}.dll")
                {
                    var assemblyDir = Path.GetDirectoryName(assembly.Location)!;

                    DebugTools.AssertNotNull(assemblyDir);

                    string libName;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        libName = $"lib{baseName}.so";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        libName = $"lib{baseName}.dylib";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return NativeLibrary.Load(Path.Combine(assemblyDir, libName));
                }

                return IntPtr.Zero;
            });
#endif
        }

        [Conditional("NETCOREAPP")]
        public static void RegisterExplicitMap(Assembly assembly, string baseName, string linuxName, string macName)
        {
            // On .NET Framework this doesn't need to run because:
            // On Windows, the DLL names should check out correctly to just work.
            // On Linux/macOS, Mono's DllMap handles it for us.
#if NETCOREAPP
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // DLL names should line up on Windows by default.
                // So a hook won't do anything.
                return;
            }

            NativeLibrary.SetDllImportResolver(assembly, (name, _, __) =>
            {
                if (name == baseName)
                {
                    string libName;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        libName = linuxName;
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        libName = macName;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return NativeLibrary.Load(libName);
                }

                return IntPtr.Zero;
            });
#endif
        }
    }
}
