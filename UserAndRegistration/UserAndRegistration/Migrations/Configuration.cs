namespace UserAndRegistration.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<UserAndRegistration.Models.LoginDatabaseEntities2>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        
    }
}
