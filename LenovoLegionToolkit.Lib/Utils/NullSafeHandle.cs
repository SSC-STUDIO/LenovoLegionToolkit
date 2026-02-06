using System;
using System.Runtime.InteropServices;

namespace LenovoLegionToolkit.Lib.Utils;

public sealed class NullSafeHandle : SafeHandle
{
    public static readonly NullSafeHandle Null = new();

    public NullSafeHandle()
        : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => false;

    protected override bool ReleaseHandle() => true;
}
