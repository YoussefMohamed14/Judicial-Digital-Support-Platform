using JDSP.Models;

namespace JDSP.Helpers {
    public static class LawyerProfileRules {
        public const string DefaultIncompleteBio = "This lawyer has not completed the professional bio yet.";
        public const string DefaultSpecialization = "General Practice";

        public static string NormalizeSpecialization(string? selectedSpecialization, string? customSpecialization) {
            var selected = selectedSpecialization?.Trim() ?? string.Empty;
            if (selected == UiText.OtherSpecialization) return customSpecialization?.Trim() ?? string.Empty;
            return selected;
        }

        public static bool IsValidPriceUnit(string? unit)
            => unit == UiText.PriceUnitHour || unit == UiText.PriceUnitMonth;

        public static bool IsProfessionalProfileComplete(LawyerProfile? profile) {
            if (profile == null) return false;
            if (string.IsNullOrWhiteSpace(profile.Bio)) return false;
            if (profile.Bio.Trim() == DefaultIncompleteBio) return false;
            if (profile.Bio.Trim().Length < 10) return false;
            if (string.IsNullOrWhiteSpace(profile.Specialization)) return false;
            if (profile.ConsultationPrice <= 0) return false;
            if (!IsValidPriceUnit(profile.ConsultationPriceUnit)) return false;
            return true;
        }

        public static bool IsKnownSpecialization(string? specialization)
            => !string.IsNullOrWhiteSpace(specialization) && UiText.SpecializationOptions.Contains(specialization.Trim());

        public static string ToSelectedSpecialization(string? specialization)
            => IsKnownSpecialization(specialization) ? specialization!.Trim() : UiText.OtherSpecialization;

        public static string ToCustomSpecialization(string? specialization)
            => IsKnownSpecialization(specialization) ? string.Empty : (specialization?.Trim() ?? string.Empty);
    }
}
