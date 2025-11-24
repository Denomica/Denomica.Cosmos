using Denomica.Cosmos.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Tests
{

    public enum DocumentStatus
    {
        Draft = 0,
        Approved = 1,
        Deleted = -1
    };

    public class ContainerItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public object Partition { get; set; } = Guid.NewGuid().ToString();
    }

    public class Item1
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Partition { get; set; } = Guid.NewGuid().ToString();

        public int Index { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is Item1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{this.Id}|{this.Partition}|{this.Index}";
        }
    }

    public class ChildItem1 : Item1
    {
        public string? DisplayName { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ChildItem1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{this.DisplayName}";
        }
    }

    public class ItemCountEntity
    {
        public int ItemCount { get; set; }
    }

    public class TestDocument : SyntheticPartitionKeyDocumentBase
    {

        [PartitionKeyProperty(0)]
        public override string Type { get => base.Type; set => base.Type = value; }
    }

    public class TestDocument2 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }
    }

    public class TestDocument3 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument4 : TestDocument
    {
        [PartitionKeyProperty(1)]
        public string Foo
        {
            get { return this.GetProperty<string>(nameof(Foo)); }
            set { this.SetProperty(nameof(Foo), value); }
        }

        public override string Type { get => base.Type; set => base.Type = value; }
    }

    public class TestDocument5 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "yyyyMMdd")]
        public DateTime Timestamp
        {
            get { return this.GetProperty<DateTime>(nameof(Timestamp)); }
            set { this.SetProperty(nameof(Timestamp), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument6 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "D2")]
        public int Index
        {
            get { return this.GetProperty<int>(nameof(Index)); }
            set { this.SetProperty(nameof(Index), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }

    public class TestDocument7 : TestDocument
    {
        [PartitionKeyProperty(0, formatString: "F1", culture: "en-US")]
        public double D1
        {
            get { return this.GetProperty<double>(nameof(D1)); }
            set { this.SetProperty(nameof(D1), value); }
        }

        [PartitionKeyProperty(1, formatString: "F1", culture: "fi-FI")]
        public double D2
        {
            get { return this.GetProperty<double>(nameof(D2)); }
            set { this.SetProperty(nameof(D2), value); }
        }

        protected override bool InheritPartitionKeyProperties => false;
    }


    public class Person : SyntheticPartitionKeyDocumentBase
    {

        public string FirstName
        {
            get { return this.GetProperty<string>(nameof(FirstName)); }
            set { this.SetProperty(nameof(FirstName), value); }
        }

        [PartitionKeyProperty(1)]
        public string LastName
        {
            get { return this.GetProperty<string>(nameof(LastName)); }
            set { this.SetProperty(nameof(LastName), value); }
        }

        [PartitionKeyProperty(0)]
        public override string Type { get => base.Type; set => base.Type = value; }

        protected override string PartitionKeyPropertySeparator => "|";
    }

    public class Employee : Person
    {
        public string EmployeeId
        {
            get { return this.GetProperty<string>(nameof(EmployeeId)); }
            set { this.SetProperty(nameof(EmployeeId), value); }
        }

        public DateOnly HireDate
        {
            get { return this.GetProperty<DateOnly>(nameof(HireDate)); }
            set { this.SetProperty(nameof(HireDate), value); }
        }
    }

    public class TimePeriod : SyntheticPartitionKeyDocumentBase
    {
        public DateOnly Start
        {
            get { return this.GetProperty<DateOnly>(nameof(Start)); }
            set { this.SetProperty(nameof(Start), value); }
        }

        public DateOnly? End
        {
            get { return this.GetProperty<DateOnly?>(nameof(End)); }
            set { this.SetProperty(nameof(End), value); }
        }
    }

    public class ContentItem : SyntheticPartitionKeyDocumentBase
    {
        public string Title
        {
            get { return this.GetProperty<string>(nameof(Title)); }
            set { this.SetProperty(nameof(Title), value); }
        }

        public string? Body
        {
            get { return this.GetProperty<string?>(nameof(Body)); }
            set { this.SetProperty(nameof(Body), value); }
        }

        public DocumentStatus Status
        {
            get { return this.GetProperty<DocumentStatus>(nameof(Status)); }
            set { this.SetProperty(nameof(Status), value); }
        }
    }

    public class AdvancePartitionDocument : TestDocument
    {
        [PartitionKeyProperty(1, formatString: "Foo/{0:yyyyMM}")]
        public DateTimeOffset Timestamp { get; set; }
    }
}
