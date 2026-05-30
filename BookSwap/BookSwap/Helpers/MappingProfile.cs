using AutoMapper;
using BookExchange.Web.Entities;
using BookExchange.Web.ViewModels;

namespace BookExchange.Web.Helpers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Book, BookCardViewModel>()
            .ForMember(d => d.ConditionLabel, o => o.MapFrom(s => ConditionToLabel(s.Condition)))
            .ForMember(d => d.Owner, o => o.MapFrom(s => s.PrimaryOwner ?? new User()));

        CreateMap<User, OwnerSummaryViewModel>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.UserName));

        CreateMap<BookFormViewModel, Book>()
            .ForMember(d => d.Id, o => o.Condition(src => src.Id.HasValue))
            .ForMember(d => d.CoverImagePath, o => o.Ignore());
    }

    public static string ConditionToLabel(BookCondition c) => c switch
    {
        BookCondition.Excellent => "Отличное",
        BookCondition.Good => "Хорошее",
        BookCondition.Acceptable => "Удовлетворительное",
        _ => c.ToString()
    };

    public static string StatusToLabel(ExchangeStatus s) => s switch
    {
        ExchangeStatus.Pending => "Ожидает ответа",
        ExchangeStatus.Accepted => "Принят",
        ExchangeStatus.Rejected => "Отклонён",
        ExchangeStatus.Completed => "Завершён",
        ExchangeStatus.Cancelled => "Отменён",
        _ => s.ToString()
    };
}
