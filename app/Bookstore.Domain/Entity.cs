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

        // PostgreSQL-only optimistic-concurrency token backed by the xmin system
        // column (R9 option b); ignored by the SQL Server model (see ApplicationDbContext).
        public uint xmin { get; set; }

        public bool IsNewEntity()
        {
            return Id == 0;
        }
    }
}