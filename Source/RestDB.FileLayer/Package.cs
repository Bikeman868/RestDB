using System.Collections.Generic;
using Ioc.Modules;
using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer
{
    public class Package : IPackage
    {
        string IPackage.Name => "RestDB File Layer";

        IList<IocRegistration> IPackage.IocRegistrations {
            get
            {
                var r = new List<IocRegistration>();
                r.Add(new IocRegistration().Init<IDataFileFactory, DataFiles.DataFileFactory>());
                r.Add(new IocRegistration().Init<IFileSetFactory, FileSets.FileSetFactory>());
                return r;
            }
        }
    }
}
