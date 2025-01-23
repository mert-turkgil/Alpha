using System;

namespace Alpha.Models
{
    public class AlphaInfo
    {
        #nullable disable
        /// <summary>
        /// Name of the application or entity.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Version of the application or entity.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Date when the information was generated or updated.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Additional description or details.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A flag indicating if this entity is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Custom message or metadata related to the application.
        /// </summary>
        public string Message { get; set; }
    }
}
