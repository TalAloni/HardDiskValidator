
namespace HardDiskValidator
{
    public enum TestName
    {
        Read,
        ReadWipeDamagedRead,
        ReadWriteVerifyRestore,
        WriteVerify,
        Write,  // The first part of WriteVerify
        Verify, // The second part of WriteVerify
    }
}
