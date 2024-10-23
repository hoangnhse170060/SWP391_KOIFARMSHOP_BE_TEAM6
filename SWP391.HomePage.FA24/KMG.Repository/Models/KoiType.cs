using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class KoiType
{
    public int KoiTypeId { get; set; }

    public string Name { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Fish> Fish { get; set; } = new List<Fish>();
    [JsonIgnore]
    public virtual ICollection<Koi> Kois { get; set; } = new List<Koi>();
}
