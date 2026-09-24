using System;
using MediatR;

namespace ecommerce.Application.Inventory.Commands.Upload;

public record UploadCommand(Stream Stream, string OriginalFileName) : IRequest<Guid>;
