namespace JDSP.ViewModels.Lawyers {
    public class LawyerSearchViewModel {
        public string? SearchTerm { get; set; }

        public string? Specialization { get; set; }

        public List<LawyerListItemViewModel> Lawyers { get; set; } = new();
    }
}