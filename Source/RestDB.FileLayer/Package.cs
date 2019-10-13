using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ioc.Modules;
using RestDB.Interfaces;
using RestDB.Interfaces.FileLayer;

[assembly: InternalsVisibleTo("RestDB.UnitTests")]

namespace RestDB.FileLayer
{
    public class Package : IPackage
    {
        string IPackage.Name => "RestDB File Layer";

        IList<IocRegistration> IPackage.IocRegistrations {
            get
            {
                var r = new List<IocRegistration>();

                // Interfaces implemented in this library

                r.Add(new IocRegistration().Init<IDataFileFactory, DataFiles.DataFileFactory>());
                r.Add(new IocRegistration().Init<ILogFileFactory, LogFiles.LogFileFactory>());
                r.Add(new IocRegistration().Init<IFileSetFactory, FileSets.FileSetFactory>());
                r.Add(new IocRegistration().Init<IPagePoolFactory, Pages.PagePoolFactory>());
                r.Add(new IocRegistration().Init<IPageStoreFactory, Pages.PageStoreFactory>());
                r.Add(new IocRegistration().Init<IAccessorFactory, Accessors.AccessorFactory>());

                // Interface dependencies

                r.Add(new IocRegistration().Init<IStartupLog>());
                r.Add(new IocRegistration().Init<IErrorLog>());

                return r;
            }
        }
    }
}
