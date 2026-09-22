using System;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Domain
{
    public abstract class Entity
    {
        public int Id { get; set; }

        public string CreatedBy { get; set; } = "System";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // dual-db-port (R9 option b): PostgreSQL maps this uint property to the
        // xmin system column as the optimistic-concurrency token; the SQL Server
        // branch ignores it (rowversion on RowVersion is unchanged there).
        public uint xmin { get; set; }

        public bool IsNewEntity()
        {
            return Id == 0;
        }
    }
}