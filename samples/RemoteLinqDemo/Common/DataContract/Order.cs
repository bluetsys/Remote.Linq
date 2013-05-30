﻿// Copyright (c) Christof Senn. All rights reserved. 

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Common.DataContract
{
    [DataContract]
    public class Order
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public IList<OrderItem> Items
        {
            get { return _items ?? (_items = new List<OrderItem>()); }
        }
        private IList<OrderItem> _items;

        public decimal TotalAmount
        {
            get { return Items.Sum(i => i.TotalAmount); }
        }

        public override string ToString()
        {
            return string.Format("Order: {0} Item{2}  Total {1:C}", Items.Count, TotalAmount, Items.Count > 1 ? "s" : null);
        }
    }
}
