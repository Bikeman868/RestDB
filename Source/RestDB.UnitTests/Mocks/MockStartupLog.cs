using Moq;
using Moq.Modules;
using RestDB.Interfaces;
using System;
using System.Diagnostics;

namespace RestDB.UnitTests.Mocks
{
    public class MockStartupLog : MockImplementationProvider<IStartupLog>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IStartupLog> mock)
        {
            mock.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<string, bool>((message, isError) => 
                {
                    Console.WriteLine((isError ? "STARTUP ERROR: " : "STARTUP: ") + message);
                    return mock.Object;
                });
        }
    }
}
