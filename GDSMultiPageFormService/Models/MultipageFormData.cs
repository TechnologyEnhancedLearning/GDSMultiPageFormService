namespace GDS.MultiPageFormData.Models
{
    using System;
    internal class MultiPageFormData
    {
        public int Id { get; set; }

        public Guid TempDataGuid { get; set; }

        public string Json { get; set; }

        public string Feature { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
