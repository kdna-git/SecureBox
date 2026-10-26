using System;
using System.Collections.Generic;
using System.Text;

namespace SecureBox.Domain.Models;

public record class FileItem(
    string Name,
    string FilePath,
    string Type,
    string Extension,
    long? Size,
    bool IsFavorite,
    int Depth
);
