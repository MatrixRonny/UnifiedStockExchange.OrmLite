using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql.Tests
{
    [TestFixture]
    public class OrmLiteCreateTableWithIndexesTests
        : OrmLiteTestBase
    {

        [Test]
        public void Can_create_ModelWithIndexFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIndexFields>(true);

                var sql = OrmLiteConfig.DialectProvider.ToCreateIndexStatements(ModelDefinition.CreateInstance<ModelWithIndexFields>()).Join();

                Assert.IsTrue(sql.Contains("idx_modelwithindexfields_name"));
                Assert.IsTrue(sql.Contains("uidx_modelwithindexfields_uniquename"));
            }
        }

        [Test]
        public void Can_create_ModelWithCompositeIndexFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithCompositeIndexFields>(true);

                var sql = OrmLiteConfig.DialectProvider.ToCreateIndexStatements(ModelDefinition.CreateInstance<ModelWithCompositeIndexFields>()).Join();

                Assert.IsTrue(sql.Contains("idx_modelwithcompositeindexfields_name"));
                Assert.IsTrue(sql.Contains("idx_modelwithcompositeindexfields_composite1_composite2"));
            }
        }

        [Test]
        public void Can_create_ModelWithNamedCompositeIndex_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithNamedCompositeIndex>(true);

                var sql = OrmLiteConfig.DialectProvider.ToCreateIndexStatements(ModelDefinition.CreateInstance<ModelWithNamedCompositeIndex>()).Join();

                Assert.IsTrue(sql.Contains("idx_modelwithnamedcompositeindex_name"));
                Assert.IsTrue(sql.Contains("custom_index_name"));
                Assert.IsFalse(sql.Contains("uidx_modelwithnamedcompositeindexfields_composite1_composite2"));
            }
        }

    }
}