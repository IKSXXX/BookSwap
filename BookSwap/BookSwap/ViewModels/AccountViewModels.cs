using System.ComponentModel.DataAnnotations;

namespace BookSwap.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(60, MinimumLength = 3)]
    [Display(Name = "Имя пользователя")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare("Password")]
    [Display(Name = "Подтвердите пароль")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class EditProfileViewModel
{
    [Required, StringLength(60, MinimumLength = 3)]
    [Display(Name = "Имя пользователя")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Город")]
    public string? Location { get; set; }

    [Display(Name = "Аватар")]
    public IFormFile? Avatar { get; set; }

    public string? ExistingAvatarPath { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string? CurrentPassword { get; set; }
}

public class UserProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarPath { get; set; }
    public string? Location { get; set; }
    public double Rating { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int BooksCount { get; set; }
    public int CompletedExchanges { get; set; }
    public bool IsCurrent { get; set; }
    public List<BookCardViewModel> MyBooks { get; set; } = new();
    public List<ExchangeListItemViewModel> Exchanges { get; set; } = new();
    public List<BookCardViewModel> Favorites { get; set; } = new();
    public List<BookCardViewModel> Wishlist { get; set; } = new();
    public List<ReviewDisplayViewModel> ReviewsReceived { get; set; } = new();
    public List<ReviewDisplayViewModel> ReviewsGiven { get; set; } = new();
}

public class ReviewDisplayViewModel
{
    public string FromUserId { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string? FromUserAvatar { get; set; }
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public string? ToUserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
