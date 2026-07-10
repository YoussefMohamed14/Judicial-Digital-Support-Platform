using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Client + "," + Roles.Lawyer)]
    public class MessagesController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? contactId = null, bool system = false) {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var contacts = await BuildContactsAsync(currentUser);
            var systemMessages = await LoadSystemMessagesAsync(currentUser.Id);
            var unreadSystemMessages = systemMessages.Count(x => !x.IsRead);

            var selectedContact = system
                ? null
                : (!string.IsNullOrWhiteSpace(contactId)
                    ? contacts.FirstOrDefault(x => x.UserId == contactId)
                    : contacts.FirstOrDefault());

            IReadOnlyList<ChatMessageViewModel> chatMessages = Array.Empty<ChatMessageViewModel>();
            if (selectedContact != null) {
                chatMessages = await LoadChatMessagesAsync(currentUser.Id, selectedContact.UserId);
                await MarkConversationReadAsync(currentUser.Id, selectedContact.UserId);
            }

            var unreadRows = await _db.SystemNotifications
                .Where(x => x.RecipientId == currentUser.Id && !x.IsRead)
                .ToListAsync();

            foreach (var row in unreadRows) row.IsRead = true;
            if (unreadRows.Count > 0) await _db.SaveChangesAsync();

            return View(new MessagesIndexViewModel {
                Contacts = contacts,
                SystemMessages = systemMessages,
                UnreadSystemMessages = unreadSystemMessages,
                SelectedContactId = selectedContact?.UserId,
                SelectedContact = selectedContact,
                ChatMessages = chatMessages,
                SystemAvatarPath = "/images/jdsp-court-system.png"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string contactId, int afterId = 0) {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            if (!await CanChatAsync(currentUser.Id, contactId)) return Forbid();

            var messages = await LoadChatMessagesAsync(currentUser.Id, contactId, afterId);
            await MarkConversationReadAsync(currentUser.Id, contactId);

            return Json(messages.Select(x => new {
                x.Id,
                x.SenderId,
                x.SenderName,
                x.Body,
                x.MessageType,
                x.RelatedCaseId,
                x.PaymentId,
                Payment = x.Payment == null ? null : new {
                    x.Payment.PaymentId,
                    x.Payment.CaseId,
                    x.Payment.CaseName,
                    x.Payment.Amount,
                    x.Payment.BillingType,
                    x.Payment.Status,
                    x.Payment.TransactionRef,
                    x.Payment.Note,
                    x.Payment.DeclineReason,
                    x.Payment.RequestedByLawyerId
                },
                x.IsMine,
                x.CreatedAt,
                CreatedAtText = x.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string contactId, string body) {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            if (!await CanChatAsync(currentUser.Id, contactId)) return Forbid();

            body = (body ?? string.Empty).Trim();
            if (body.Length == 0)
                return BadRequest(Text("Message cannot be empty.", "لا يمكن إرسال رسالة فارغة."));
            if (body.Length > 2000)
                return BadRequest(Text("Message cannot exceed 2000 characters.", "لا يمكن أن تتجاوز الرسالة 2000 حرف."));

            var message = new ChatMessage {
                SenderId = currentUser.Id,
                ReceiverId = contactId,
                Body = body,
                MessageType = "User",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.ChatMessages.Add(message);
            await _db.SaveChangesAsync();

            return Json(new {
                message.Id,
                message.SenderId,
                SenderName = currentUser.FirstName + " " + currentUser.LastName,
                message.Body,
                message.MessageType,
                message.RelatedCaseId,
                message.PaymentId,
                IsMine = true,
                message.CreatedAt,
                CreatedAtText = message.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            });
        }

        private async Task<List<MessageContactViewModel>> BuildContactsAsync(ApplicationUser currentUser) {
            var contacts = new List<MessageContactViewModel>();
            var currentUserId = currentUser.Id;

            if (await _users.IsInRoleAsync(currentUser, Roles.Lawyer)) {
                contacts.AddRange(await _db.LegalServiceRequests.AsNoTracking()
                    .Where(x => x.LawyerId == currentUserId && x.Status == "Accepted")
                    .Select(x => new MessageContactViewModel {
                        UserId = x.ClientId,
                        FullName = x.Client.FirstName + " " + x.Client.LastName,
                        PhotoPath = x.Client.PhotoPath,
                        Relationship = "Accepted direct request"
                    })
                    .ToListAsync());

                contacts.AddRange(await _db.CaseLawyers.AsNoTracking()
                    .Where(x => x.LawyerId == currentUserId && x.Case != null &&
                        (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                    .Select(x => new MessageContactViewModel {
                        UserId = x.Case!.CreatedBy_Id,
                        FullName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                        PhotoPath = x.Case.Creator == null ? null : x.Case.Creator.PhotoPath,
                        Relationship = "Assigned case"
                    })
                    .ToListAsync());
            }
            else {
                contacts.AddRange(await _db.LegalServiceRequests.AsNoTracking()
                    .Where(x => x.ClientId == currentUserId && x.LawyerId != null && x.Status == "Accepted")
                    .Select(x => new MessageContactViewModel {
                        UserId = x.LawyerId!,
                        FullName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                        PhotoPath = x.Lawyer == null ? null : x.Lawyer.PhotoPath,
                        Relationship = "Accepted direct request"
                    })
                    .ToListAsync());

                contacts.AddRange(await _db.CaseLawyers.AsNoTracking()
                    .Where(x => x.Case != null && x.Case.CreatedBy_Id == currentUserId &&
                        (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                    .Select(x => new MessageContactViewModel {
                        UserId = x.LawyerId,
                        FullName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                        PhotoPath = x.Lawyer == null ? null : x.Lawyer.PhotoPath,
                        Relationship = "Assigned lawyer"
                    })
                    .ToListAsync());
            }

            var uniqueContacts = contacts
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(x => x.First())
                .OrderBy(x => x.FullName)
                .ToList();

            var contactIds = uniqueContacts.Select(x => x.UserId).ToList();
            var unreadRows = await _db.ChatMessages.AsNoTracking()
                .Where(x => x.ReceiverId == currentUserId && !x.IsRead && x.MessageType != "PaymentStatus" && contactIds.Contains(x.SenderId))
                .Select(x => new { x.SenderId, x.Id, x.PaymentId })
                .ToListAsync();

            var unread = unreadRows
                .GroupBy(x => x.SenderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.PaymentId.HasValue ? $"payment-{x.PaymentId.Value}" : $"message-{x.Id}").Count());

            foreach (var contact in uniqueContacts)
                contact.UnreadCount = unread.TryGetValue(contact.UserId, out var count) ? count : 0;

            return uniqueContacts;
        }

        private async Task<IReadOnlyList<SystemNotificationViewModel>> LoadSystemMessagesAsync(string userId) {
            return await _db.SystemNotifications.AsNoTracking()
                .Where(x => x.RecipientId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new SystemNotificationViewModel {
                    Id = x.Id,
                    Title = x.Title,
                    Body = x.Body,
                    Category = x.Category,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        private async Task<IReadOnlyList<ChatMessageViewModel>> LoadChatMessagesAsync(string currentUserId, string contactId, int afterId = 0) {
            var messages = await _db.ChatMessages.AsNoTracking()
                .Where(x => x.Id > afterId && x.MessageType != "PaymentStatus" &&
                    ((x.SenderId == currentUserId && x.ReceiverId == contactId) ||
                     (x.SenderId == contactId && x.ReceiverId == currentUserId)))
                .OrderBy(x => x.CreatedAt)
                .Take(100)
                .Select(x => new ChatMessageViewModel {
                    Id = x.Id,
                    SenderId = x.SenderId,
                    SenderName = x.Sender == null ? string.Empty : x.Sender.FirstName + " " + x.Sender.LastName,
                    Body = x.Body,
                    MessageType = x.MessageType,
                    RelatedCaseId = x.RelatedCaseId,
                    PaymentId = x.PaymentId,
                    Payment = x.Payment == null ? null : new PaymentMessageViewModel {
                        PaymentId = x.Payment.Id,
                        CaseId = x.Payment.CaseId,
                        CaseName = x.Payment.Case == null ? string.Empty : x.Payment.Case.CaseName,
                        Amount = x.Payment.Amount,
                        BillingType = x.Payment.BillingType,
                        Status = x.Payment.Status,
                        TransactionRef = x.Payment.TransactionRef,
                        Note = x.Payment.Note,
                        DeclineReason = x.Payment.DeclineReason,
                        RequestedByLawyerId = x.Payment.RequestedByLawyerId
                    },
                    IsMine = x.SenderId == currentUserId,
                    IsRead = x.IsRead,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            // Older test builds could create a second chat row for the same payment.
            // Keep only one card per payment and let status/amount edits update that card.
            return messages
                .GroupBy(x => x.PaymentId.HasValue ? $"payment-{x.PaymentId.Value}" : $"message-{x.Id}")
                .Select(g => g.OrderBy(x => x.CreatedAt).First())
                .OrderBy(x => x.CreatedAt)
                .ToList();
        }

        private async Task MarkConversationReadAsync(string currentUserId, string contactId) {
            var unread = await _db.ChatMessages
                .Where(x => x.SenderId == contactId && x.ReceiverId == currentUserId && !x.IsRead)
                .ToListAsync();

            foreach (var message in unread) message.IsRead = true;
            if (unread.Count > 0) await _db.SaveChangesAsync();
        }

        private async Task<bool> CanChatAsync(string currentUserId, string contactId) {
            if (string.IsNullOrWhiteSpace(contactId) || currentUserId == contactId) return false;

            var acceptedDirectRequest = await _db.LegalServiceRequests.AsNoTracking().AnyAsync(x =>
                x.Status == "Accepted" &&
                ((x.ClientId == currentUserId && x.LawyerId == contactId) ||
                 (x.ClientId == contactId && x.LawyerId == currentUserId)));

            if (acceptedDirectRequest) return true;

            return await _db.CaseLawyers.AsNoTracking().AnyAsync(x =>
                x.Case != null &&
                (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted") &&
                ((x.LawyerId == currentUserId && x.Case.CreatedBy_Id == contactId) ||
                 (x.LawyerId == contactId && x.Case.CreatedBy_Id == currentUserId)));
        }

        private string Text(string en, string ar) {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
        }
    }
}
