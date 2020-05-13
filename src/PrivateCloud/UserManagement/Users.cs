using System;
using System.Collections.Generic;

namespace PrivateCloud.UserManagement
{
    public class Users : HashSet<string>
    {
        public Users() : base(new List<string>
        {
            "steve",
            "eli"
        })
        { }
    }
}
