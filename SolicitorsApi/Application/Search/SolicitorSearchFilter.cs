using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public class SolicitorSearchFilter : ISolicitorSearchFilter
{
    public IReadOnlyList<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        decimal? minimumReviewScore)
    {
        IEnumerable<Solicitor> query = solicitors;

        if (minimumReviewScore.HasValue)
        {
            query = query.Where(solicitor =>
                solicitor.Review?.Score is not null &&
                solicitor.Review.Score.Value >= minimumReviewScore.Value);
        }

        return query.ToArray();
    }
}
