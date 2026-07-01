using System.ComponentModel.DataAnnotations;

namespace JDSP.Models {
    public class LawyerFollow {
        public int LawyerFollowId { get; set; }

        [Required]
        public string FollowerId { get; set; } = string.Empty;

        public ApplicationUser Follower { get; set; } = null!;

        [Required]
        public string LawyerId { get; set; } = string.Empty;

        public ApplicationUser Lawyer { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}