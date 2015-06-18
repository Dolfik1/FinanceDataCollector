using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceDataCollector.Model
{
    public class transaction
    {
        public ObjectId _id { get; set; }
        public string code { get; set; }
        public string contract { get; set; }
        public double price { get; set; }
        public int amount { get; set; }
        public DateTime dat_time { get; set; }
        public int trade_id { get; set; }

        public transaction()
        {
            dat_time = DateTime.MinValue;
        }
    }
}
