using System;
using System.Runtime.InteropServices;

namespace WslSdk
{
    /// <summary>
    /// You must implement this interface for every class that you register in 
    /// the system registry and to which you assign a CLSID, so objects of that
    /// class can be created.
    /// http://msdn.microsoft.com/en-us/library/ms694364.aspx
    /// </summary>
    [ComImport]
    [ComVisible(false)]
    [Guid(NativeMethods.IID_IClassFactory)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory
    {
        /// <summary>
        /// Creates an uninitialized object.
        /// </summary>
        /// <param name="pUnkOuter"></param>
        /// <param name="riid">
        /// Reference to the identifier of the interface to be used to 
        /// communicate with the newly created object. If pUnkOuter is NULL, this
        /// parameter is frequently the IID of the initializing interface.
        /// </param>
        /// <param name="ppvObject">
        /// Address of pointer variable that receives the interface pointer 
        /// requested in riid. 
        /// </param>
        /// <returns>S_OK means success.</returns>
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

        /// <summary>
        /// Locks object application open in memory.
        /// </summary>
        /// <param name="fLock">
        /// If TRUE, increments the lock count; 
        /// if FALSE, decrements the lock count.
        /// </param>
        /// <returns>S_OK means success.</returns>
        [PreserveSig]
        int LockServer(bool fLock);
    }
}
