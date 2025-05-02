using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace wfDocServices.Entity
{
    public class rabbitqueue
    {
        public int id { get; set; }
        public int erpid { get; set; }
        public string companyinternalid { get; set; }
        public string internalid {  get; set; }
        public string docnum {  get; set; }
        public string queuepayload { get; set; }
        public DateTime queuedatetime { get; set; }
        public string docsource { get; set; }
        public string doctype { get; set; }
    }
}
