using System;

namespace RestDB.Interfaces.FileLayer
{
    public interface IVirtualFile
    {
        IBlock Allocate();
    }
}
