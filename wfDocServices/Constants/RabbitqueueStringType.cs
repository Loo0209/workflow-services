using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wfDocServices.Constants
{
    public static class RabbitqueueStringType
    {
        public static readonly string ActionNotify = "action_notify";
        public static readonly string VerificationCode = "verification_code";
        public static readonly string ForgetPass = "ForgetPass";
        public static readonly string ActivateAcc = "ActivateAcc";
    }
}
