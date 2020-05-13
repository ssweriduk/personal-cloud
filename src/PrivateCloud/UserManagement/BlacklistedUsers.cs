using System;
using System.Collections.Generic;

namespace PrivateCloud.UserManagement
{
    public class BlacklistedUsers : HashSet<string>
    {
        public BlacklistedUsers() : base(new List<string>
        {
            "eli",
        })
        {
        }
    }
}
