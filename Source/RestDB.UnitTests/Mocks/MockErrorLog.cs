﻿using Moq;
using Moq.Modules;
using RestDB.Interfaces;
using System;
using System.Diagnostics;

namespace RestDB.UnitTests.Mocks
{
    public class MockErrorLog : MockImplementationProvider<IErrorLog>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IErrorLog> mock)
        {
            mock.Setup(o => o.Write(It.IsAny<string>()))
                .Returns<string>(message =>
                {
                    Console.WriteLine("ERROR: " + message);
                    return mock.Object;
                });
        }
    }
}