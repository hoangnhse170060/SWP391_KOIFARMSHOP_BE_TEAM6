using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class KoiType
{
    public int KoiTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Fish> Fish { get; set; } = new List<Fish>();

    public virtual ICollection<Koi> Kois { get; set; } = new List<Koi>();
}
