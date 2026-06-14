using System.ComponentModel.DataAnnotations;
using BookSwap.Db.Entities;
using BookSwap.Web.Helpers;

namespace BookSwap.Web.ViewModels;

public class CreateExchangeViewModel
{
    public int BookRequestedId { get; set; }
    public BookCardViewModel? BookRequested { get; set; }
    public List<BookCardViewModel> MyAvailableBooks { get; set; } = new();

    [Display(Name = "Книга для обмена")]
    public int? SelectedOfferedBookId { get; set; }
}

public class ExchangeDetailsViewModel
{
    public int Id { get; set; }
    public ExchangeStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OwnerSummaryViewModel Sender { get; set; } = new();
    public OwnerSummaryViewModel Receiver { get; set; } = new();
    public BookCardViewModel? BookOffered { get; set; }
    public BookCardViewModel? BookRequested { get; set; }
    public List<ChatMessageViewModel> Messages { get; set; } = new();
    public bool CanAccept { get; set; }
    public bool CanReject { get; set; }
    public bool CanCancel { get; set; }
    public bool CanComplete { get; set; }
    public bool CanLeaveReview { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
}

public class ChatMessageViewModel
{
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class ExchangeListItemViewModel
{
    public int Id { get; set; }
    public ExchangeStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserAvatar { get; set; } = string.Empty;
    public string BookRequestedTitle { get; set; } = string.Empty;
    public string? BookRequestedCover { get; set; }
    public string? BookOfferedTitle { get; set; }
    public string? BookOfferedCover { get; set; }

    public static ExchangeListItemViewModel From(ExchangeRequest e, string currentUserId)
    {
        var isSender = e.SenderId == currentUserId;
        return new ExchangeListItemViewModel
        {
            Id = e.Id,
            Status = e.Status,
            StatusLabel = MappingProfile.StatusToLabel(e.Status),
            CreatedAt = e.CreatedAt,
            OtherUserId = (isSender ? e.ReceiverId : e.SenderId) ?? "",
            OtherUserName = (isSender ? e.Receiver?.UserName : e.Sender?.UserName) ?? "",
            OtherUserAvatar = (isSender ? e.Receiver?.AvatarPath : e.Sender?.AvatarPath) ?? "",
            BookRequestedTitle = e.BookRequested?.Title ?? "",
            BookRequestedCover = e.BookRequested?.CoverImagePath,
            BookOfferedTitle = e.BookOffered?.Title,
            BookOfferedCover = e.BookOffered?.CoverImagePath
        };
    }
}

public class ReviewFormViewModel
{
    public int ExchangeRequestId { get; set; }
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [StringLength(1000)]
    [Display(Name = "Комментарий")]
    public string? Comment { get; set; }
}
