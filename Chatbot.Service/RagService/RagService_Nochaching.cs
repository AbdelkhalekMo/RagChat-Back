using Chatbot.Repositories;
using Data;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Chatbot.Models;

namespace Chatbot.Services
{
    public class RagService : IRagService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ChatbotContext _context;
        private readonly List<(string[] Keywords, Func<string, string, Task<string>> Handler)> intents;

        public RagService(IInvoiceRepository invoiceRepository, ChatbotContext context)
        {
            _invoiceRepository = invoiceRepository;
            _context = context;

            // بناء قائمة النوايا بدوال المعالجة المناسبة
            // InvoiceSummaryIntent ستتم معالجتها الآن كأولوية قصوى خارج هذه القائمة
            intents = new List<(string[] Keywords, Func<string, string, Task<string>> Handler)>
            {
                (new[] {"اجمالي", "مجموع", "قيمة", "سعر", "total", "sum", "value", "amount"}, (q, lang) => TotalInvoicesIntent(q, lang)),
                (new[] {"عدد", "كم", "count", "how many" }, (q, lang) => CountInvoicesIntent(q, lang)),
                (new[] {"متأخرة", "متأخر", "overdue"}, (q, lang) => OverdueInvoicesIntent(q, lang)),
                (new[] {"غير مدفوعة","لم تدفع بعد","متدفعتش", "unpaid"}, (q, lang) => UnpaidInvoicesIntent(q, lang)),
                (new[] {"تتعدى", "اكبر", "أكبر","اكبر من","أكبر من", "greater", "above", "more than"}, (q, lang) => GreaterThanIntent(q, lang)),
                (new[] {"أقل", "اقل","أقل من","اقل من", "less", "below", "under"}, (q, lang) => LessThanIntent(q, lang)),
                (new[] {"عميل","زبون", "customer"}, (q, lang) => CustomerInvoicesIntent(q, lang)),
                (new[] {"شهر", "شهور", "شهرية", "month"}, (q, lang) => MonthIntent(q, lang)),
                (new[] {"أيام", "آخر","النهارده","اليوم","يوم", "latest", "last", "day", "days","today"}, (q, lang) => DaysIntent(q, lang))
            };
        }

        #region Public Methods
        public async Task<string> ProcessNaturalQueryAsync(string query, string language = null)
        {
            string detectedLanguage = language ?? DetectLanguage(query) ?? "en";
            string originalQuery = query; // الاحتفاظ بالاستعلام الأصلي لـ LLM
            string lowerCaseQuery = query?.Trim()?.ToLower() ?? ""; // استخدم متغير جديد للحالة الصغيرة

            // ** الخطوة 1: فحص الأولوية القصوى لرقم الفاتورة **
            string invoiceNumber = ExtractInvoiceNumberFromQuery(originalQuery, detectedLanguage);
            if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                // إذا تم الكشف عن رقم فاتورة، حاول التعامل معه كطلب ملخص مباشرة
                var invoice = await _invoiceRepository.GetInvoiceByNumberWithDetailsAsync(invoiceNumber);
                if (invoice == null)
                {
                    return detectedLanguage == "ar"
                        ? $"عذراً، لم يتم العثور على فاتورة برقم {invoiceNumber}."
                        : $"Sorry, invoice with number {invoiceNumber} was not found.";
                }
                return detectedLanguage == "ar"
                    ? $"ملخص الفاتورة رقم {invoice.InvoiceNumber}: صدرت بتاريخ {invoice.Date:yyyy-MM-dd} للعميل {invoice.CustomerName} بقيمة {invoice.Total:C} وتشمل {invoice.InvoiceDetails.Count} بند."
                    : $"Summary for invoice {invoice.InvoiceNumber}: issued on {invoice.Date:yyyy-MM-dd} for {invoice.CustomerName} totals {invoice.Total:C} with {invoice.InvoiceDetails.Count} items.";
            }

            // ** الخطوة 2: العودة إلى مطابقة النوايا العامة إذا لم يتم العثور على رقم فاتورة صريح **
            var matchedIntent = intents.FirstOrDefault(entry =>
                entry.Keywords.Any(keyword => lowerCaseQuery.Contains(keyword)));

            if (matchedIntent.Handler != null)
            {
                return await matchedIntent.Handler(lowerCaseQuery, detectedLanguage);
            }

            // --- الحل البديل الذكي مع LLM ---
            string detectedIntentCategoryForLLM = null;
            Dictionary<string, object> relevantDataForLLM = new Dictionary<string, object>();

            // محاولة تخمين الفئة حتى لو لم يتم مطابقة معالج مباشر
            if (lowerCaseQuery.Contains("اجمالي") || lowerCaseQuery.Contains("مجموع") || lowerCaseQuery.Contains("total") || lowerCaseQuery.Contains("sum"))
            {
                detectedIntentCategoryForLLM = "Total";
            }
            else if (lowerCaseQuery.Contains("عدد") || lowerCaseQuery.Contains("كم") || lowerCaseQuery.Contains("count") || lowerCaseQuery.Contains("how many"))
            {
                detectedIntentCategoryForLLM = "Count";
            }
            else if (lowerCaseQuery.Contains("عميل") || lowerCaseQuery.Contains("customer"))
            {
                detectedIntentCategoryForLLM = "Customer";
                string customerName = ExtractCustomerName(originalQuery);
                if (!string.IsNullOrEmpty(customerName)) relevantDataForLLM["customerName"] = customerName;
            }
            // تم إضافة هذا الشرط لتوجه LLM بشكل صارم إذا كانت النية ملخص فاتورة ولم يتم العثور على رقم فاتورة.
            else if (lowerCaseQuery.Contains("ملخص") || lowerCaseQuery.Contains("summary") || lowerCaseQuery.Contains("فاتورة") || lowerCaseQuery.Contains("invoice"))
            {
                detectedIntentCategoryForLLM = "Summary";
                // إذا وصل التنفيذ إلى هنا، فهذا يعني أن ExtractInvoiceNumberFromQuery() في الخطوة 1 لم يعثر على رقم فاتورة
                // لذا، يجب أن نطلب من LLM أن يطلب رقم الفاتورة من المستخدم.
                relevantDataForLLM["askForInvoiceNumber"] = true; // علامة جديدة للـ LLM
            }

            // استدعاء LLM بموجه محسن
            var metadataPrompt = await BuildDbSchemaSummaryPrompt(originalQuery, detectedLanguage, detectedIntentCategoryForLLM, relevantDataForLLM);
            return await CallLlmAndReturnAnswer(metadataPrompt, detectedLanguage);
        }
        #endregion

        #region Intent Handlers

        private async Task<string> TotalInvoicesIntent(string query, string language)
        {
            if (query.Contains("هذا الشهر") || query.Contains("this month"))
            {
                var total = await _invoiceRepository.GetTotalInvoicesThisMonthAsync();
                return language == "ar"
                    ? $"إجمالي قيمة الفواتير لهذا الشهر ({DateTime.Now.Month}/{DateTime.Now.Year}) هو {total:C}."
                    : $"The total value of invoices for this month ({DateTime.Now.Month}/{DateTime.Now.Year}) is {total:C}.";
            }
            else
            {
                var daysRangeTuple = ExtractDaysRange(query);
                if (daysRangeTuple.Days > 0 || daysRangeTuple.Type == "today" || daysRangeTuple.Type == "week" || daysRangeTuple.Type == "month_past" || daysRangeTuple.Type == "year_past") // Check if a specific time range is requested
                {
                    var endDate = DateTime.Today;
                    var startDate = endDate.AddDays(-daysRangeTuple.Days);

                    if (daysRangeTuple.Type == "today")
                    {
                        startDate = DateTime.Today;
                    }
                    else if (daysRangeTuple.Type == "week")
                    {
                        startDate = DateTime.Today.AddDays(-7);
                    }
                    else if (daysRangeTuple.Type == "month_past")
                    {
                        endDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
                        startDate = new DateTime(endDate.Year, endDate.Month, 1);
                    }
                    else if (daysRangeTuple.Type == "year_past")
                    {
                        endDate = new DateTime(DateTime.Today.Year, 1, 1).AddDays(-1);
                        startDate = new DateTime(endDate.Year, 1, 1);
                    }

                    var invoices = await _invoiceRepository.GetInvoicesByDateRangeAsync(startDate, endDate);
                    if (!invoices.Any()) return RespondNoData(language);
                    var totalInRange = invoices.Sum(i => i.Total);
                    return language == "ar"
                        ? $"إجمالي قيمة الفواتير خلال {GetTimeRangeDescription(daysRangeTuple.Days, language, daysRangeTuple.Type)} هو {totalInRange:C}."
                        : $"The total value of invoices in {GetTimeRangeDescription(daysRangeTuple.Days, language, daysRangeTuple.Type)} is {totalInRange:C}.";
                }
            }

            // Fallback to all invoices if no specific time range or "this month" is mentioned
            var allInvoices = await _invoiceRepository.GetAllWithDetailsAsync(1, 100);
            if (!allInvoices.Any()) return RespondNoData(language);
            var totalAll = allInvoices.Sum(i => i.Total);
            return language == "ar"
                ? $"إجمالي قيمة الفواتير هو {totalAll:C}."
                : $"The total value of invoices is {totalAll:C}.";
        }

        private async Task<string> CountInvoicesIntent(string query, string language)
        {
            if (query.Contains("الأسبوع الماضي") || query.Contains("last week"))
            {
                var count = await _invoiceRepository.GetInvoiceCountLastWeekAsync();
                return language == "ar"
                    ? $"عدد الفواتير في الأسبوع الماضي هو {count}."
                    : $"The total number of invoices last week is {count}.";
            }
            var invoices = await _invoiceRepository.GetAllWithDetailsAsync(1, 100);
            if (!invoices.Any()) return RespondNoData(language);
            var countAll = invoices.Count();
            return language == "ar"
                ? $"عدد الفواتير هو {countAll}."
                : $"The total number of invoices is {countAll}.";
        }

        private async Task<string> OverdueInvoicesIntent(string query, string language)
        {
            int overdue = await _invoiceRepository.GetOverdueInvoicesCountAsync();
            return language == "ar"
                ? $"عدد الفواتير المتأخرة هو {overdue}."
                : $"The number of overdue invoices is {overdue}.";
        }

        private async Task<string> UnpaidInvoicesIntent(string query, string language)
        {
            decimal unpaid = await _invoiceRepository.GetTotalUnpaidInvoicesAsync();
            return language == "ar"
                ? $"إجمالي قيمة الفواتير غير المدفوعة هو {unpaid:C}."
                : $"The total value of unpaid invoices is {unpaid:C}.";
        }

        private async Task<string> GreaterThanIntent(string query, string language)
        {
            int threshold = ExtractThreshold(query);
            var invoices = await _invoiceRepository.GetAllWithDetailsAsync(1, 100);
            if (!invoices.Any()) return RespondNoData(language);
            var filtered = invoices.Where(i => i.Total > threshold).ToList();
            if (!filtered.Any())
                return language == "ar"
                    ? $"لا توجد فواتير تتعدى قيمتها {threshold}."
                    : $"There are no invoices greater than {threshold}.";
            var numbers = filtered.Select(i => i.InvoiceNumber);
            return language == "ar"
                ? $"فواتير أكبر من {threshold}: {string.Join(", ", numbers)}"
                : $"Invoices greater than {threshold}: {string.Join(", ", numbers)}";
        }

        private async Task<string> LessThanIntent(string query, string language)
        {
            int threshold = ExtractThreshold(query);
            var invoices = await _invoiceRepository.GetAllWithDetailsAsync(1, 100);
            if (!invoices.Any()) return RespondNoData(language);
            var filtered = invoices.Where(i => i.Total < threshold).ToList();
            if (!filtered.Any())
                return language == "ar"
                    ? $"لا توجد فواتير أقل من {threshold}."
                    : $"There are no invoices less than {threshold}.";
            var numbers = filtered.Select(i => i.InvoiceNumber);
            return language == "ar"
                ? $"فواتير أقل من {threshold}: {string.Join(", ", numbers)}"
                : $"Invoices less than {threshold}: {string.Join(", ", numbers)}";
        }

        private async Task<string> CustomerInvoicesIntent(string query, string language)
        {
            string customer = ExtractCustomerName(query);
            if (string.IsNullOrEmpty(customer)) return RespondDefault(language);
            var invoices = await _invoiceRepository.GetInvoicesByCustomerWithDetailsAsync(customer);
            if (!invoices.Any())
                return language == "ar"
                    ? $"لا توجد فواتير للعميل {customer}."
                    : $"No invoices for customer {customer}.";
            var numbers = invoices.Select(i => i.InvoiceNumber);
            return language == "ar"
                ? $"فواتير العميل {customer}: {string.Join(", ", numbers)}"
                : $"Invoices for customer {customer}: {string.Join(", ", numbers)}";
        }

        private async Task<string> MonthIntent(string query, string language)
        {
            int month = ExtractMonthNumber(query);
            int year = ExtractYear(query, DateTime.Now.Year);
            var endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)); // نهاية الشهر
            var startDate = new DateTime(year, month, 1); // بداية الشهر
            var invoices = await _invoiceRepository.GetInvoicesByDateRangeAsync(startDate, endDate); // استخدام الدالة المحدثة
            if (!invoices.Any())
                return language == "ar"
                    ? $"لا توجد فواتير لشهر {month}/{year}."
                    : $"There are no invoices for {month}/{year}.";
            var numbers = invoices.Select(i => i.InvoiceNumber);
            return language == "ar"
                ? $"فواتير شهر {month}/{year}: {string.Join(", ", numbers)}"
                : $"Invoices for {month}/{year}: {string.Join(", ", numbers)}";
        }

        private async Task<string> DaysIntent(string query, string language)
        {
            var daysRange = ExtractDaysRange(query);
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-daysRange.Days);

            if (daysRange.Type == "today")
            {
                startDate = DateTime.Today;
            }
            else if (daysRange.Type == "week")
            {
                startDate = DateTime.Today.AddDays(-7);
            }
            else if (daysRange.Type == "month_past")
            {
                endDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
                startDate = new DateTime(endDate.Year, endDate.Month, 1);
            }
            else if (daysRange.Type == "year_past")
            {
                endDate = new DateTime(DateTime.Today.Year, 1, 1).AddDays(-1);
                startDate = new DateTime(endDate.Year, 1, 1);
            }

            var invoices = await _invoiceRepository.GetInvoicesByDateRangeAsync(startDate, endDate);
            if (!invoices.Any())
                return RespondNoData(language);
            var count = invoices.Count();
            var total = invoices.Sum(i => i.Total);
            return language == "ar"
                ? $"خلال {GetTimeRangeDescription(daysRange.Days, language, daysRange.Type)}: عدد الفواتير {count}، الإجمالي {total:C}."
                : $"In {GetTimeRangeDescription(daysRange.Days, language, daysRange.Type)}: invoice count {count}, total value {total:C}.";
        }

        #endregion

        #region Helper Methods
        private int ExtractThreshold(string query)
        {
            var match = Regex.Match(query, @"\d+");
            return (match.Success && int.TryParse(match.Value, out int value)) ? value : 0;
        }

        private (int Days, string Type) ExtractDaysRange(string query)
        {
            if (query.Contains("يوم") || query.Contains("days"))
            {
                var match = Regex.Match(query, @"(\d+)\s?(يوم|days?)", RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var days))
                    return (days, "days");
            }
            if (query.Contains("اليوم") || query.Contains("today"))
                return (0, "today");
            if (query.Contains("الأسبوع الماضي") || query.Contains("last week"))
                return (7, "week");
            if (query.Contains("اسبوع") || query.Contains("week"))
                return (7, "days");
            if (query.Contains("الشهر الماضي") || query.Contains("last month"))
                return (0, "month_past");
            if (query.Contains("السنة الماضية") || query.Contains("last year"))
                return (0, "year_past");
            return (30, "days");
        }

        private int ExtractMonthNumber(string query)
        {
            var match = Regex.Match(query, @"\b(1[0-2]|0?[1-9])\b");
            return match.Success && int.TryParse(match.Value, out int value) ? value : DateTime.Now.Month;
        }

        private int ExtractYear(string query, int defaultYear)
        {
            var match = Regex.Match(query, @"(20\d{2})");
            return match.Success && int.TryParse(match.Value, out int value) ? value : defaultYear;
        }

        private string ExtractInvoiceNumberFromQuery(string query, string language)
        {
            var match = Regex.Match(query, @"\b(?:invoice|فاتورة|inv)[\s-]*(\d+)\b", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return "INV-" + match.Groups[1].Value;
            }
            return null;
        }


        private string ExtractCustomerName(string query)
        {
            var patternArabic = @"العميل\s+([^\s,\.]+)";
            var patternEnglish = @"customer\s+([a-zA-Z]+\s?[a-zA-Z]*)";
            var match = Regex.Match(query, patternArabic);
            if (!match.Success)
                match = Regex.Match(query, patternEnglish, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private string ExtractItemName(string query)
        {
            var patternArabic = @"(لابتوب|كاميرا|هاتف|ساعة)";
            var patternEnglish = @"(laptop|camera|phone|watch)";
            var match = Regex.Match(query, patternArabic, RegexOptions.IgnoreCase);
            if (!match.Success)
                match = Regex.Match(query, patternEnglish, RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Trim() : null;
        }

        private string RespondNoData(string language)
            => language == "ar"
                ? "لا توجد بيانات فواتير متاحة حالياً."
                : "There are currently no invoice data available.";

        private string RespondDefault(string language)
            => language == "ar"
                ? "عذراً، لم نتمكن من معالجة طلبك."
                : "Sorry, we couldn't process your request right now.";

        private string GetTimeRangeDescription(int days, string language, string type = "days")
        {
            if (type == "today")
                return language == "ar" ? "اليوم" : "today";
            if (type == "week")
                return language == "ar" ? "الأسبوع الماضي" : "last week";
            if (type == "month_past")
                return language == "ar" ? "الشهر الماضي" : "last month";
            if (type == "year_past")
                return language == "ar" ? "السنة الماضية" : "last year";

            return language == "ar" ? $"آخر {days} أيام" : $"last {days} days";
        }

        private string DetectLanguage(string query)
        {
            if (string.IsNullOrEmpty(query)) return null;

            var arabicKeywords = new[] { "ال", "فاتورة", "الإجمالي", "عدد", "عميل" };
            var englishKeywords = new[] { "the", "invoice", "total", "count", "customer" };

            if (arabicKeywords.Any(k => query.Contains(k)))
                return "ar";
            if (englishKeywords.Any(k => query.Contains(k)))
                return "en";

            return "en";
        }
        #endregion

        #region LLM Integration
        private async Task<string> CallLlmAndReturnAnswer(string prompt, string language)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var body = new { model = "mistral", prompt, stream = false };
                var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate", body);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("response", out var responseElement))
                    return responseElement.GetString() ?? RespondDefault(language);
                return RespondDefault(language);
            }
            catch (Exception ex)
            {
                // يمكنك تسجيل الخطأ هنا للمراجعة
                return language == "ar"
                    ? $"عذراً، حدث خطأ أثناء محاولة توليد الإجابة. يرجى المحاولة مرة أخرى لاحقاً. ({ex.Message})"
                    : $"Sorry, an error occurred while trying to generate the answer. Please try again later. ({ex.Message})";
            }
        }

        private async Task<string> BuildDbSchemaSummaryPrompt(string question, string language, string detectedIntentCategory = null, Dictionary<string, object> relevantData = null)
        {
            string schemaDesc = language == "ar"
                ? "جدول الفواتير يحتوي على الحقول: رقم، تاريخ، اسم العميل، الإجمالي، الحالة، تاريخ الدفع، تفاصيل الفاتورة.\n"
                : "Invoices table fields: Number, Date, Customer, Total, Status, PaymentDate, Invoice Details.\n";

            IEnumerable<Invoice> sampleInvoices = new List<Invoice>();
            if (detectedIntentCategory == "Customer" && relevantData != null && relevantData.ContainsKey("customerName"))
            {
                string customer = relevantData["customerName"]?.ToString();
                if (!string.IsNullOrEmpty(customer))
                {
                    sampleInvoices = await _invoiceRepository.GetInvoicesByCustomerWithDetailsAsync(customer);
                }
                if (!sampleInvoices.Any())
                {
                    sampleInvoices = (await _invoiceRepository.GetAllWithDetailsAsync(1, 2)).Take(2).ToList();
                }
            }
            else if (detectedIntentCategory == "Summary" && relevantData != null && relevantData.ContainsKey("invoiceNumber"))
            {
                string invoiceNum = relevantData["invoiceNumber"]?.ToString();
                if (!string.IsNullOrEmpty(invoiceNum))
                {
                    var inv = await _invoiceRepository.GetInvoiceByNumberWithDetailsAsync(invoiceNum);
                    if (inv != null) sampleInvoices = new List<Invoice> { inv };
                }
                if (!sampleInvoices.Any())
                {
                    sampleInvoices = (await _invoiceRepository.GetAllWithDetailsAsync(1, 2)).Take(2).ToList(); // Fallback to general samples
                }
            }
            else
            {
                sampleInvoices = (await _invoiceRepository.GetAllWithDetailsAsync(1, 2)).Take(2).ToList();
            }

            string samples = sampleInvoices.Any()
                ? string.Join("\n", sampleInvoices.Select(i =>
                    $"{i.InvoiceNumber}, {i.Date:yyyy-MM-dd}, {i.CustomerName}, {i.Total}, {i.Status}"))
                : (language == "ar"
                    ? "لا توجد بيانات فواتير حالياً."
                    : "No current invoice data.");

            StringBuilder promptBuilder = new StringBuilder();
            promptBuilder.AppendLine(schemaDesc);
            promptBuilder.AppendLine(language == "ar" ? "أمثلة على البيانات:\n" : "Sample data:\n");
            promptBuilder.AppendLine(samples);

            if (detectedIntentCategory != null)
            {
                promptBuilder.AppendLine(language == "ar" ? $"السياق المحتمل للسؤال: {detectedIntentCategory}." : $"Probable query context: {detectedIntentCategory}.");
            }

            if (relevantData != null && relevantData.Any())
            {
                promptBuilder.AppendLine(language == "ar" ? "بيانات إضافية ذات صلة:" : "Additional relevant data:");
                foreach (var item in relevantData)
                {
                    promptBuilder.AppendLine($"{item.Key}: {item.Value}");
                }
            }

            promptBuilder.AppendLine(language == "ar"
                ? $"السؤال: {question}\nأجب بناءً على المعلومات المقدمة فقط. إذا لم تتمكن، اعتذر للمستخدم بأدب واطلب إعادة صياغة السؤال أو تقديم معلومات أكثر تحديداً. **الرد يجب أن يكون باللغة العربية.**"
                : $"Question: {question}\nOnly answer using the provided information. If you cannot, kindly apologize to the user and ask them to rephrase the question or provide more specific information. **The response should be in English.**");

            return promptBuilder.ToString();
        }
        #endregion
    }
}