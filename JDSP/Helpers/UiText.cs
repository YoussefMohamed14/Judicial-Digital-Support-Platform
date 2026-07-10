namespace JDSP.Helpers {
    public static class UiText {
        public const string PriceUnitHour = "Hour";
        public const string PriceUnitMonth = "Month";
        public const string OtherSpecialization = "Other";

        public static readonly IReadOnlyList<string> SpecializationOptions = new[] {
            "General Practice",
            "Family Law",
            "Criminal Law",
            "Civil Law",
            "Commercial Law",
            "Corporate Law",
            "Labor Law",
            "Real Estate Law",
            "Administrative Law",
            "Tax Law",
            "Intellectual Property",
            "Immigration Law",
            "Contract Law",
            "Cybercrime Law"
        };

        public static string Status(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Open" => "مفتوحة",
                "Pending" => "قيد الانتظار",
                "In Progress" => "قيد التنفيذ",
                "Waiting for next hearing date" => "بانتظار موعد الجلسة القادمة",
                "Closed" => "مغلقة",
                "Accepted" => "مقبول",
                "Approved" => "معتمد",
                "NotRequired" => "غير مطلوب",
                "Rejected" => "مرفوض",
                "Price Proposed" => "تم اقتراح السعر",
                "OfferAccepted" => "تم قبول العرض",
                "Requested" => "مطلوب",
                "Paid" => "مدفوع",
                "Declined" => "مرفوض",
                "Available" => "متاح",
                "Withdrawn" => "تم السحب",
                "PartiallyWithdrawn" => "سحب جزئي",
                "Active" => "نشط",
                "Public" => "عام",
                "Direct" => "مباشر",
                _ => value
            };
        }

        public static string RequestType(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Public" => "عام",
                "Direct" => "مباشر",
                _ => value
            };
        }

        public static string CaseType(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Civil" => "مدنية",
                "Criminal" => "جنائية",
                "Family" => "أحوال شخصية",
                "Commercial" => "تجارية",
                "Administrative" => "إدارية",
                "Direct Request" => "طلب مباشر",
                "Public Request" => "طلب عام",
                _ => value
            };
        }

        public static string Availability(bool isAvailable, bool ar)
            => ar ? (isAvailable ? "متاح" : "غير متاح") : (isAvailable ? "Available" : "Unavailable");

        public static string Years(int years, bool ar)
            => ar ? $"{years} سنة خبرة" : $"{years} years of experience";

        public static string YearsShort(int years, bool ar)
            => ar ? $"{years} سنة" : $"{years} years";

        public static string Specialization(string? value, bool ar) {
            if (string.IsNullOrWhiteSpace(value)) return ar ? "غير محدد" : "Not specified";
            if (!ar) return value;
            return value switch {
                "General Practice" => "ممارسة عامة",
                "Family Law" => "قانون الأسرة",
                "Criminal Law" => "القانون الجنائي",
                "Civil Law" => "القانون المدني",
                "Commercial Law" => "القانون التجاري",
                "Corporate Law" => "قانون الشركات",
                "Labor Law" => "قانون العمل",
                "Real Estate Law" => "القانون العقاري",
                "Administrative Law" => "القانون الإداري",
                "Tax Law" => "قانون الضرائب",
                "Intellectual Property" => "الملكية الفكرية",
                "Immigration Law" => "قانون الهجرة",
                "Contract Law" => "قانون العقود",
                "Cybercrime Law" => "قانون الجرائم الإلكترونية",
                "Not specified" => "غير محدد",
                "Other" => "أخرى",
                _ => value
            };
        }

        public static string Role(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Client" => "عميل",
                "Lawyer" => "محامٍ",
                "Admin" => "مدير",
                "CourtEmployee" => "موظف محكمة",
                _ => value
            };
        }

        public static string Relationship(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Accepted direct request" => "طلب مباشر مقبول",
                "Assigned case" => "قضية مسندة",
                "Assigned lawyer" => "محامٍ مسند",
                _ => value
            };
        }

        public static string BillingCycle(string? value, bool ar) {
            if (!ar || string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            return value switch {
                "Monthly" => "شهري",
                "Yearly" => "سنوي",
                "Weekly" => "أسبوعي",
                "Daily" => "يومي",
                _ => value
            };
        }

        public static string PriceUnit(string? value, bool ar) {
            var unit = string.IsNullOrWhiteSpace(value) ? PriceUnitHour : value;
            return unit switch {
                PriceUnitMonth => ar ? "شهري" : "per month",
                _ => ar ? "بالساعة" : "per hour"
            };
        }

        public static string PriceSuffix(string? value, bool ar) {
            var unit = string.IsNullOrWhiteSpace(value) ? PriceUnitHour : value;
            return unit switch {
                PriceUnitMonth => ar ? "/شهر" : "/month",
                _ => ar ? "/ساعة" : "/h"
            };
        }

        public static string Price(decimal amount, string? unit, bool ar)
            => $"{amount:N2} {PriceSuffix(unit, ar)}";
    }
}
