using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Parky.Application.Common;

public sealed class StoredFileResult
{
    public string ImageUrl { get; init; } = string.Empty;

    public string PublicId { get; init; } = string.Empty;
}