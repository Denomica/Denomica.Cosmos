using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Model
{
    /// <summary>
    /// Represents a document with timestamp metadata for creation and modification.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="DocumentBase"/> to include properties for tracking the creation
    /// and last modification timestamps of a document. The application is responsible for managing these timestamps,
    /// including setting appropriate values when creating or updating the document.
    /// </remarks>
    public class TimestampedDocumentBase : DocumentBase
    {
        /// <summary>
        /// The timestamp when the document was created.
        /// </summary>
        /// <remarks>
        /// Your application is fully responsible for managing this value.
        /// </remarks>
        public virtual DateTimeOffset Created
        {
            get { return this.GetProperty<DateTimeOffset>(nameof(Created), () => DateTimeOffset.Now); }
            set { this.SetProperty(nameof(Created), value); }
        }

        /// <summary>
        /// The timestamp when the document was last modified.
        /// </summary>
        /// <remarks>
        /// Your application is fully responsible for managing this value.
        /// </remarks>
        public virtual DateTimeOffset Modified
        {
            get { return this.GetProperty<DateTimeOffset>(nameof(Modified), () => DateTimeOffset.Now); }
            set { this.SetProperty(nameof(Modified), value); }
        }
    }
}
