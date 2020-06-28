using System;
using Amazon.CDK;
using PrivateCloud.CDK.Constructs.Roles;

namespace PrivateCloud.CDK.Stacks
{
    public class RolesStack : Stack
    {
        public RolesStack(Construct scope, string id) : base(scope, id)
        {
            new TeamCityRole(this, "TeamCityRole");
        }
    }
}
