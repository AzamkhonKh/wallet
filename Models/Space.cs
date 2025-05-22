using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WalletNet.Models; // Retaining this based on previous correction, User type will be WalletNet.Models.User

namespace BudgetMaster.Models // As per specific instruction for Space model
{
    public class Space
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int UserId { get; set; } // Changed back to int as per subtask clarification on User.Id

        [ForeignKey("UserId")]
        public virtual User User { get; set; } // User type is WalletNet.Models.User

        // Assuming Transaction model will be defined elsewhere.
        // The subtask implies a Transaction model that will have a SpaceId and a Space navigation property.
        public virtual ICollection<Transaction> Transactions { get; set; }

        public Space()
        {
            Transactions = new HashSet<Transaction>();
        }
    }
}
