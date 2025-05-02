using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wfDocServices.Entity
{
    public class Group_User_Info
    {
        public int tenant_id { get; set; }
        public string company_code { get; set; }
        public string company_name { get; set; }
        public string group_name { get; set; }
        public string description { get; set; }
        public string username { get; set; }
    }
}
