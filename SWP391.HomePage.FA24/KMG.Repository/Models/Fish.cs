    using System;
    using System.Collections.Generic;

    namespace KMG.Repository.Models;

    public partial class Fish
    {
        public int FishesId { get; set; }

        public int? Quantity { get; set; }

        public int? KoiTypeId { get; set; }

        public decimal? Price { get; set; }

        public string? ImageFishes { get; set; }

        public virtual KoiType? KoiType { get; set; }

        public virtual ICollection<OrderFish> OrderFishes { get; set; } = new List<OrderFish>();
    }
