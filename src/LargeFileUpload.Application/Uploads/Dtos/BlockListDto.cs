namespace LargeFileUpload.Application.Uploads.Dtos;

public record BlockInfoDto(
    string Name,
    long Size,
    bool IsCommitted);

public record BlockListDto(
    IReadOnlyList<BlockInfoDto> CommittedBlocks,
    IReadOnlyList<BlockInfoDto> UncommittedBlocks)
{
    public int TotalStagedBlocks => CommittedBlocks.Count + UncommittedBlocks.Count;
    public long TotalStagedBytes => CommittedBlocks.Sum(b => b.Size) + UncommittedBlocks.Sum(b => b.Size);
}
