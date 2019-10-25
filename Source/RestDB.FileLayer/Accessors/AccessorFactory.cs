using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.Accessors
{
    internal class AccessorFactory : IAccessorFactory
    {
        IRandomRecordAccessor IAccessorFactory.LargeFixedRandomAccessor(IPageStore pageStore, uint recordSize)
        {
            throw new System.NotImplementedException();
        }

        ISequentialRecordAccessor IAccessorFactory.LargeSequentialAccessor(IPageStore pageStore)
        {
            return new LargeSequentialAccessor(pageStore);
        }

        IRandomRecordAccessor IAccessorFactory.LargeVariableRandomAccessor(IPageStore pageStore)
        {
            throw new System.NotImplementedException();
        }

        IRandomRecordAccessor IAccessorFactory.SmallFixedRandomAccessor(IPageStore pageStore, uint recordSize)
        {
            throw new System.NotImplementedException();
        }

        ISequentialRecordAccessor IAccessorFactory.SmallSequentialAccessor(IPageStore pageStore)
        {
            return new SmallSequentialAccessor(pageStore);
        }

        IRandomRecordAccessor IAccessorFactory.SmallVariableRandomAccessor(IPageStore pageStore)
        {
            throw new System.NotImplementedException();
        }
    }
}
