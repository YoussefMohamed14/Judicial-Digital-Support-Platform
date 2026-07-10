using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Settings {
    public class SettingsViewModel {
        public string CurrentEmail { get; set; } = string.Empty;

        [EmailAddress]
        public string? SwitchEmail { get; set; }

        [DataType(DataType.Password)]
        public string? SwitchPassword { get; set; }

        public string PreferredLanguage { get; set; } = "en";
    }
}
