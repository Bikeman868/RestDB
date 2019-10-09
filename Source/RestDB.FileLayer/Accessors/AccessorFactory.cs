using RestDB.Interfaces.FileLayer;

namespace RestDB.FileLayer.Accessors
{
    internal class AccessorFactory : IAccessorFactory
    {
        IVariableLengthRecordListAccessor IAccessorFactory.VariableLengthRecordList(IPageStore pageStore)
        {
            return new VariableLengthRecordListAccessor(pageStore);
        }
    }
}
