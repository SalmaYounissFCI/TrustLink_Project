using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SW_Project.Interfaces;
using SW_Project.Models;
using SW_Project.ViewModels.Message;

namespace SW_Project.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IActionResult> Inbox()
        {
            var userId = _userManager.GetUserId(User);

            var conversations = await _unitOfWork.Conversations.FindAllAsync(
                c => c.ParticipantAId == userId || c.ParticipantBId == userId,
                c => c.ParticipantA,
                c => c.ParticipantB,
                c => c.Listing,
                c => c.Listing.ListingImages);

            var orderedConversations = conversations.OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt).ToList();

            var inboxItems = new List<InboxItemVM>();

            foreach (var conv in orderedConversations)
            {
                var otherUser = conv.ParticipantAId == userId ? conv.ParticipantB : conv.ParticipantA;

                var allMessages = await _unitOfWork.Messages.FindAllAsync(m => m.ConversationId == conv.Id);
                var lastMessage = allMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();

                var unreadCount = allMessages.Count(m => m.ReceiverId == userId && !m.IsRead);

                inboxItems.Add(new InboxItemVM
                {
                    ConversationId = conv.Id,
                    OtherUserId = otherUser?.Id ?? "",
                    OtherUserName = otherUser?.Name ?? "Unknown User",
                    OtherUserAvatar = !string.IsNullOrEmpty(otherUser?.Name) ? otherUser.Name.Substring(0, 1).ToUpper() : "?",
                    LastMessage = lastMessage?.Text ?? "No messages yet",
                    LastMessageAt = lastMessage?.SentAt ?? conv.CreatedAt,
                    UnreadCount = unreadCount,
                    ListingId = conv.ListingId,
                    ListingTitle = conv.Listing?.Title,
                    ListingImageUrl = conv.Listing?.ListingImages?.FirstOrDefault()?.ImagePath
                });
            }

            var viewModel = new InboxVM
            {
                Conversations = inboxItems.OrderByDescending(i => i.LastMessageAt).ToList(),
                UnreadCount = inboxItems.Sum(i => i.UnreadCount)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(string userId, int? listingId = null)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(userId))
                    return RedirectToAction("Inbox");

                if (userId == currentUserId) return RedirectToAction("Inbox");

                var otherUser = await _unitOfWork.Users.GetByIdAsync(userId);
                if (otherUser == null) return RedirectToAction("Inbox");

                var conversation = await _unitOfWork.Conversations.FindAsync(
                    c => (c.ParticipantAId == currentUserId && c.ParticipantBId == userId) ||
                         (c.ParticipantAId == userId && c.ParticipantBId == currentUserId),
                    c => c.Messages,
                    c => c.Listing,
                    c => c.Listing.ListingImages);

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        ParticipantAId = currentUserId,
                        ParticipantBId = userId,
                        ListingId = listingId,
                        CreatedAt = DateTime.Now,
                        LastMessageAt = DateTime.Now
                    };
                    await _unitOfWork.Conversations.AddAsync(conversation);
                    await _unitOfWork.CompleteAsync();

                    // Refresh to get the conversation with includes
                    conversation = await _unitOfWork.Conversations.FindAsync(
                        c => c.Id == conversation.Id,
                        c => c.Messages,
                        c => c.Listing,
                        c => c.Listing.ListingImages);
                }

                // Mark unread messages as read
                var unreadMessages = conversation.Messages?.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList() ?? new List<Message>();
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadAt = DateTime.Now;
                    _unitOfWork.Messages.Update(msg);
                }
                await _unitOfWork.CompleteAsync();

                var messages = (conversation.Messages ?? new List<Message>())
                    .OrderBy(m => m.SentAt)
                    .Select(m => new MessageItemVM
                    {
                        Id = m.Id,
                        Text = m.Text,
                        SenderId = m.SenderId,
                        SenderName = m.Sender?.Name ?? "User",
                        IsFromCurrentUser = m.SenderId == currentUserId,
                        SentAt = m.SentAt,
                        IsRead = m.IsRead
                    }).ToList();

                var viewModel = new ConversationVM
                {
                    ConversationId = conversation.Id,
                    OtherUserId = otherUser.Id,
                    OtherUserName = otherUser.Name,
                    OtherUserAvatar = !string.IsNullOrEmpty(otherUser.Name) ? otherUser.Name.Substring(0, 1).ToUpper() : "?",
                    ListingId = conversation.ListingId,
                    ListingTitle = conversation.Listing?.Title,
                    ListingImageUrl = conversation.Listing?.ListingImages?.FirstOrDefault()?.ImagePath,
                    Messages = messages,
                    SendMessage = new SendMessageVM
                    {
                        ReceiverId = userId,
                        ListingId = conversation.ListingId
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("Inbox");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(SendMessageVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Text))
                return RedirectToAction("Conversation", new { userId = model.ReceiverId, listingId = model.ListingId });

            try
            {
                var currentUserId = _userManager.GetUserId(User);

                var conversation = await _unitOfWork.Conversations.FindAsync(
                    c => (c.ParticipantAId == currentUserId && c.ParticipantBId == model.ReceiverId) ||
                         (c.ParticipantAId == model.ReceiverId && c.ParticipantBId == currentUserId));

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        ParticipantAId = currentUserId,
                        ParticipantBId = model.ReceiverId,
                        ListingId = model.ListingId,
                        CreatedAt = DateTime.Now
                    };
                    await _unitOfWork.Conversations.AddAsync(conversation);
                    await _unitOfWork.CompleteAsync();

                    conversation = await _unitOfWork.Conversations.GetByIdAsync(conversation.Id);
                }

                var message = new Message
                {
                    ConversationId = conversation.Id,
                    SenderId = currentUserId,
                    ReceiverId = model.ReceiverId,
                    Text = model.Text,
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                await _unitOfWork.Messages.AddAsync(message);
                conversation.LastMessageAt = message.SentAt;
                _unitOfWork.Conversations.Update(conversation);
                await _unitOfWork.CompleteAsync();

                return RedirectToAction("Conversation", new { userId = model.ReceiverId, listingId = model.ListingId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: ERROR: {ex.Message}");
                TempData["Error"] = "Failed to send message.";
                return RedirectToAction("Inbox");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });

            var unreadCount = await _unitOfWork.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);
            return Json(new { count = unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> StartConversation(int listingId)
        {
            var listing = await _unitOfWork.Listings.GetByIdAsync(listingId);
            if (listing == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (listing.OwnerId == currentUserId)
                return RedirectToAction("Details", "Listings", new { id = listingId });

            return RedirectToAction("Conversation", new { userId = listing.OwnerId, listingId = listingId });
        }
    }
}