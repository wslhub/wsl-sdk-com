using System;
using System.Runtime.InteropServices;

namespace WslSdk.Contracts
{
    [ComVisible(true)]
    [Guid("BA5F6950-5000-483F-82FE-8CD86DC25B77")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IWslServiceEvents
    {
        [DispId(1)]
        void StdoutDataReceived(string data);

        [DispId(2)]
        void StderrDataReceived(string data);
    }
}
