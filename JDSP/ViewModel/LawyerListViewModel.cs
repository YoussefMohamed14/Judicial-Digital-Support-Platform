//it has view model only because they are also ApplicationUsers

namespace JDSP.ViewModels.Lawyers {
    public class LawyerListItemViewModel {
        public int LawyerProfileId { get; set; }

        public string LawyerUserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public decimal ConsultationPrice { get; set; }

        public bool IsAvailable { get; set; }

        public bool IsFollowed { get; set; }
    }
}