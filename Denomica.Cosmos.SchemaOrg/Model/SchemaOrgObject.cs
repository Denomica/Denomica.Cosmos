using Denomica.Cosmos.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.SchemaOrg.Model
{
    /// <summary>
    /// An envelope for a Schema.org object stored in a Cosmos DB container.
    /// </summary>
    public class SchemaOrgObject : SyntheticPartitionKeyDocumentBase
    {
        /// <summary>
        /// The Schema.org type of the object.
        /// </summary>
        /// <remarks>
        /// The object type is found in the original data stored in <see cref="Data"/> under the key <c>"@type"</c>.
        /// </remarks>
        public override string Type
        {
            get { return this.GetProperty<string>(nameof(Type), string.Empty); }
            set { this.SetProperty(nameof(Type), value); }
        }

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string? Name
        {
            get { return this.GetProperty<string>(nameof(Name), string.Empty); }
            set { this.SetProperty(nameof(Name), value); }
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
        /// If the original object specified in <see cref="Data"/> has multiple images, only the first one is stored here. Also,
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
        /// The original data of the Schema.org object that the current envelope represents.
        /// </summary>
        public Dictionary<string, object?> Data
        {
            get { return this.GetProperty<Dictionary<string, object?>>(nameof(Data), () => new Dictionary<string, object?>()); }
            set { this.SetProperty(nameof(Data), value); }
        }
    }
}
