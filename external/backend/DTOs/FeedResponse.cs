namespace backend_deob.DTOs;

public class FeedResponse
{
    public required List<FeedEntryDto> Entries { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
