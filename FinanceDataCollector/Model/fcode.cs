using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace FinanceDataCollector.Model
{
    public class fcode
    {
        public ObjectId _id { get; set; }
        public string code { get; set; }
    }
}
