using Moq;
using Moq.Modules;
using RestDB.Interfaces;
using System;
using System.Diagnostics;

namespace RestDB.UnitTests.Mocks
{
    public class MockStartupLog : MockImplementationProvider<IStartUpLog>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IStartUpLog> mock)
        {
            mock.Setup(o => o.Write(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<string, bool>((message, isError) => 
                {
                    Console.WriteLine((isError ? "STARTUP ERROR: " : "STARTUP: ") + message);
                    return mock.Object;
                });
        }
    }
}
