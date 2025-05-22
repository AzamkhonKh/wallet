using System.ComponentModel.DataAnnotations;

namespace WalletNet.DTOs
{
    public class SpaceCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class SpaceUpdateDto
    {
        [MaxLength(100)]
        public string? Name { get; set; } // Name is optional for update, only provided fields are updated

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class SpaceResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
    }
}
