using FluentValidation;
using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Uploads.Validators;

public class CommitChunkedUploadValidator : AbstractValidator<CommitChunkedUploadRequest>
{
    public CommitChunkedUploadValidator()
    {
        RuleFor(x => x.BlockIds)
            .NotNull().WithMessage("BlockIds list cannot be null.")
            .NotEmpty().WithMessage("At least one BlockId is required for commitment.")
            .Must(blocks => blocks.Count <= 50000).WithMessage("Block count exceeds Azure limit of 50,000 blocks.");
    }
}
