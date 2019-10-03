using Moq;
using Moq.Modules;
using RestDB.Interfaces;

namespace RestDB.UnitTests.Mocks
{
    public class MockAuditLog : MockImplementationProvider<IAuditLog>
    {
        protected override void SetupMock(IMockProducer mockProducer, Mock<IAuditLog> mock)
        {
        }
    }
}
