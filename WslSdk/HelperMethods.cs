using Microsoft.Win32;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WslSdk
{
    internal static class HelperMethods
    {
        public static Guid GetGuidFromType(Type t)
        {
            if (t == null)
                throw new ArgumentNullException("Type object cannot be null.", "t");

            object[] attributes = t.GetCustomAttributes(typeof(GuidAttribute), false);

            if (attributes.Length < 1)
                throw new ArgumentException("No GUID attribute specified on the type " + t.Name);

            GuidAttribute attr = (GuidAttribute)attributes[0];
            return new Guid(attr.Value);
        }

        /// <summary>
        /// Register the component as a local server.
        /// </summary>
        /// <param name="t"></param>
        public static void RegasmRegisterLocalServer(Type t)
        {
            if (t == null)
                throw new ArgumentException("The CLR type must be specified.", "t");

            // Open the CLSID key of the component.
            using (RegistryKey keyCLSID = Registry.ClassesRoot.OpenSubKey(
                @"CLSID\" + t.GUID.ToString("B"), true))
            {
                // Remove the auto-generated InprocServer32 key after registration
                // (REGASM puts it there but we are going out-of-proc).
                keyCLSID.DeleteSubKeyTree("InprocServer32");

                // Create "LocalServer32" under the CLSID key
                using (RegistryKey subkey = keyCLSID.CreateSubKey("LocalServer32"))
                {
                    subkey.SetValue(
                        string.Empty,
                        Assembly.GetExecutingAssembly().Location,
                        RegistryValueKind.String);
                }
            }
        }

        /// <summary>
        /// Unregister the component.
        /// </summary>
        /// <param name="t"></param>
        public static void RegasmUnregisterLocalServer(Type t)
        {
            if (t == null)
                throw new ArgumentException("The CLR type must be specified.", "t");

            // Delete the CLSID key of the component
            Registry.ClassesRoot.DeleteSubKeyTree(@"CLSID\" + t.GUID.ToString("B"));
        }
    }
}
