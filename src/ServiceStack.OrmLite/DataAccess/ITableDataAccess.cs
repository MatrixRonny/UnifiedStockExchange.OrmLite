namespace ServiceStack.OrmLite.DataAccess
{
    public interface ITableDataAccess<T>
    {
        bool TableExists();
        void CreateTable(bool overwrite = false);
        void DropTable();
        void Insert(T entity);
        ISelectFilter<T> CreateSelectFilter();
        IUpdateFilter<T> CreateUpdateFilter();
        IDeleteFilter<T> CreateDeleteFilter();
    }
}