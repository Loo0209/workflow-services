using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wfDocServices.Entity
{
    public class Approval_Workflow_Notification_Info
    {
        public int  tenant_id { get; set; }
        public string tenant_code { get; set; }
        public int document_rules_id { get; set; }
        public int approvalrule_id { get; set; }
        public int approval_workflow_id { get; set; }
        public int request_id { get; set; }
        public string docnum { get; set; }
        public int company_id { get; set; }
        public string company_code { get; set; }
        public int doc_id { get; set; }
        public string doc_code { get; set; }
        public int level { get; set; }
        public int user_group_id { get; set; }
        public string user_group_name { get; set; }
        public string approval_type { get; set; }
        public string action_type { get; set; }
        public string comment { get; set; }
        public int act_by { get; set; }
        public string act_by_name { get; set; }
        public DateTime act_at { get; set; }
    }
}
