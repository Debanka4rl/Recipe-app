
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace supaathrowaway.Models

{
    [Table("recipes")] // Double-check if your Supabase table is plural ("recipes") or singular ("recipe")
        public class Recipe : BaseModel
        {
            [PrimaryKey("id", false)] // false indicates the DB automatically increments/generates the ID
            public int Id { get; set; }

            [Column("Name")]
            public string Name { get; set; } = string.Empty;

            [Column("Complexity")]
            public string Complexity { get; set; } = string.Empty;

            [Column("Description")]
            public string Description { get; set; } = string.Empty;

            [Column("Picture")]
            public string? Picture { get; set; } // Nullable string since it can be empty
        }
    }