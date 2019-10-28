using NUnit.Framework;

namespace RestDB.UnitTests.FileLayer
{
    public class LargeSequentialAccessorFunctionalTests : SequentialAccessorFunctionalTests
    {
        [SetUp]
        public void Setup()
        {
            base.Setup();

            _accessor = _accessorFactory.LargeSequentialAccessor(_pageStore);
        }

        [Test] public void should_read_and_write_records() { base.should_read_and_write_records(); }
        [Test] public void should_enumerate_records() { base.should_enumerate_records(); }
        [Test] public void should_delete_records() { base.should_delete_records(); }
    }
}
