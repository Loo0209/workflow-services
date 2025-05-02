using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wfDocServices.Entity
{
    public class Users
    {
        public int id { get; set; }
        public int tenant_id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public DateTime? last_login { get; set; }
        public bool is_activate { get; set; }
        public bool is_locked { get; set; }
        public bool is_autolocked { get; set; }
        public int role_id { get; set; }
        public byte[] avatar { get; set; }
        public string status { get; set; }
        public string pwd_hash { get; set; }
        public int pwd_validity { get; set; }
        public DateTime? pwd_expired { get; set; }
        public int failed_attempts { get; set; }
        public DateTime? last_failed_attempt { get; set; }
        public int login_spam_attempt { get; set; }
        public int create_by { get; set; }
        public DateTime? create_at { get; set; }
        public int update_by { get; set; }
        public DateTime? update_at { get; set; }
        public string account_type { get; set; }
        public string account_role { get; set; }
    }
}
