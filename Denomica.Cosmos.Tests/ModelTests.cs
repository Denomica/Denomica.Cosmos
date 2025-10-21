using Denomica.Cosmos.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Tests
{
    [TestClass]
    public class ModelTests
    {

        [TestMethod]
        public void TestModel01()
        {
            var ts = DateTimeOffset.Now;
            var model = new SyntheticPartitionKeyDocumentBase();
            Assert.IsTrue(model.Created > ts);
            Assert.IsTrue(model.Modified > ts);
        }

        [TestMethod]
        public void TestModel02()
        {
            var p1 = new Person { FirstName = "John", LastName = "Doe" };
            var p2 = new Person { FirstName = "Jane", LastName = "Doe" };

            Assert.AreEqual(p1.Partition, p2.Partition);
            Assert.AreEqual("Person|Doe", p1.Partition);
        }

        [TestMethod]
        public void TestModel03()
        {
            var m = new SyntheticPartitionKeyDocumentBase();
            Assert.AreEqual(nameof(SyntheticPartitionKeyDocumentBase), m.Partition);
        }

        [TestMethod]
        public void TestModel04()
        {
            var m = new TestDocument();
            Assert.AreEqual(nameof(TestDocument), m.Partition);
        }

        [TestMethod]
        public void TestModel05()
        {
            var m = new TestDocument2 { Foo = "bar" };
            Assert.AreEqual($"{nameof(TestDocument2)}/{m.Foo}", m.Partition);
        }

        [TestMethod]
        public void TestModel06()
        {
            var m = new TestDocument3 { Foo = "bar" };
            Assert.AreEqual(m.Foo, m.Partition);
        }

        [TestMethod]
        public void TestModel07()
        {
            var m = new TestDocument4 { Foo = "bar" };
            Assert.AreEqual(m.Foo, m.Partition);
        }

        [TestMethod]
        public void TestModel08()
        {
            var m = new TestDocument5 { Timestamp = new DateTime(2023, 3, 9) };
            Assert.AreEqual("20230309", m.Partition);
        }

        [TestMethod]
        public void TestModel09()
        {
            var m1 = new TestDocument6 { Index = 4 };
            var m2 = new TestDocument6 { Index = 45 };
            var m3 = new TestDocument6 { Index = 183 };


            Assert.AreEqual("04", m1.Partition);
            Assert.AreEqual("45", m2.Partition);
            Assert.AreEqual("183", m3.Partition);
        }

        [TestMethod]
        public void TestModel10()
        {
            var m = new TestDocument7 { D1 = 5.3, D2 = 12.5 };
            Assert.AreEqual("5.3/12,5", m.Partition);
        }

        [TestMethod]
        public void TestModel11()
        {
            var doc = new AdvancePartitionDocument { Timestamp = new DateTimeOffset(2023, 3, 1, 0, 0, 0, TimeSpan.FromHours(2)) };
            Assert.AreEqual("AdvancePartitionDocument/Foo/202303", doc.Partition);
        }



        [TestMethod]
        public void TestModelEvents01()
        {
            var model = new SyntheticPartitionKeyDocumentBase();
            DateTimeOffset created = default;
            model.PropertyValueChanged += (s, e) =>
            {
                created = (DateTimeOffset)e.NewValue!;
            };

            var created2 = model.Created;
            Assert.AreEqual(created, created2);
        }

    }
}
