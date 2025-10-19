using Denomica.Cosmos.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.SchemaOrg.Model
{
    /// <summary>
    /// An envelope for a Schema.org object stored in a Cosmos DB container.
    /// </summary>
    public class SchemaOrgEnvelope : SyntheticPartitionKeyDocumentBase
    {

        /// <summary>
        /// Logical owner or source context (e.g., site, tenant, or organization).
        /// Included in the synthetic partition key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value of this property is typically a domain name, site name, tenant name, 
        /// organization name, or anything else that describes the source or owner of the object.
        /// </para>
        /// <para>
        /// The value is included in the synthetic partition key stored in the <see cref="SyntheticPartitionKeyDocumentBase.Partition"/> property.
        /// </para>
        /// </remarks>
        [PartitionKeyProperty(0)]
        public string Owner
        {
            get { return this.GetProperty(nameof(Owner), defaultValue: string.Empty); }
            set { this.SetProperty(nameof(Owner), value); }
        }

        /// <summary>
        /// The Schema.org <c>@type</c> value of the object contained in this envelope,
        /// for example: "Product", "Article", or "Organization".
        /// Included in the synthetic partition key.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value is included in the synthetic partition key stored in the <see cref="SyntheticPartitionKeyDocumentBase.Partition"/> property.
        /// </para>
        /// </remarks>
        [PartitionKeyProperty(1)]
        public string ObjectType
        {
            get { return this.GetProperty(nameof(ObjectType), defaultValue: string.Empty); }
            set { this.SetProperty(nameof(ObjectType), value); }
        }

        /// <summary>
        /// The group property is used to provide a logical grouping of objects, especially those
        /// with identical values for <see cref="Owner"/> and <see cref="ObjectType"/>. It is a
        /// generic version of a Category/Department/Section/Industry property that you can use
        /// with your objects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value is included in the synthetic partition key stored in the <see cref="SyntheticPartitionKeyDocumentBase.Partition"/> property.
        /// </para>
        /// </remarks>
        [PartitionKeyProperty(2)]
        public string? Group
        {
            get { return this.GetProperty<string?>(nameof(Group)); }
            set { this.SetProperty(nameof(Group), value); }
        }

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string? Name
        {
            get { return this.GetProperty<string?>(nameof(Name)); }
            set { this.SetProperty(nameof(Name), value); }
        }

        /// <summary>
        /// schema.org/Thing.identifier — normalized to a single string when possible,
        /// for example a plain string or stringified PropertyValue.
        /// </summary>
        public string? Identifier
        {
            get { return this.GetProperty<string?>(nameof(Identifier)); }
            set { this.SetProperty(nameof(Identifier), value); }
        }

        /// <summary>
        /// Gets or sets the description associated with the object.
        /// </summary>
        public string? Description
        {
            get { return this.GetProperty<string>(nameof(Description), string.Empty); }
            set { this.SetProperty(nameof(Description), value); }
        }

        /// <summary>
        /// Gets or sets the URI of the image associated with this instance.
        /// </summary>
        /// <remarks>
        /// If the original object specified in <see cref="Content"/> has multiple images, only the first one is stored here. Also,
        /// if the image or images are represented as <c>ImageObject</c> objects, only the URL of the first image is stored here.
        /// </remarks>
        public Uri? Image
        {
            get { return this.GetProperty<Uri?>(nameof(Image)); }
            set { this.SetProperty(nameof(Image), value); }
        }

        /// <summary>
        /// Gets or sets the URL associated with this instance.
        /// </summary>
        public Uri? Url
        {
            get { return this.GetProperty<Uri?>(nameof(Url)); }
            set { this.SetProperty(nameof(Url), value); }
        }

        /// <summary>
        /// The original content of the Schema.org object that the current envelope represents.
        /// </summary>
        public Dictionary<string, object?> Content
        {
            get { return this.GetProperty(nameof(Content), () => new Dictionary<string, object?>()); }
            set { this.SetProperty(nameof(Content), value); }
        }
    }
}
