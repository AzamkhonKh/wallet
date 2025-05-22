using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BudgetMaster.Models; // For Space

namespace WalletNet.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        // public string Name { get; set;} // Removed as per new requirements, Amount is used instead

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? PhotoPath { get; set; }

        [Required]
        public int UserId { get; set; } // Foreign key to User.Id (int)

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int SpaceId { get; set; } // Foreign key to Space.Id (int)

        [ForeignKey("SpaceId")]
        public virtual Space Space { get; set; } // Navigation to BudgetMaster.Models.Space
    }
}