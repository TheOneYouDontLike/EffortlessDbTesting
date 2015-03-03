namespace EffortlessDbTesting
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using Autofac;
    using System.Linq;
    using NUnit.Framework;
    using Effort;

    [TestFixture]
    public class ExampleContextTests
    {
        [Test]
        public void Should_initialize_repository_with_fake_context()
        {
            // given
            var dbConnection = DbConnectionFactory.CreateTransient();
            
            var exampleContext = new ExampleContext(dbConnection);

            // when            
            exampleContext.DbUnicorns.Add(new DbUnicorn { Name = "edward", UniquePowers = new List<UniquePower>{new UniquePower
            {
                Description = "piercing the enemy"
            }}});
            exampleContext.SaveChanges();

            // then
            var dbUnicorn = exampleContext.DbUnicorns.First();
            Assert.That(dbUnicorn.Name, Is.EqualTo("edward"));
            Assert.That(dbUnicorn.UniquePowers.First().Description, Is.EqualTo("piercing the enemy"));
        }

        [Test]
        [Ignore]
        public void Should_inject_dbconnection_to_context()
        {
            // given
            var builder = new ContainerBuilder();

            builder.Register(c => new ExampleContext(new SqlConnection(@"Server=(localdb)\v11.0;Integrated Security=true;"))).AsSelf();

            var container = builder.Build();

            // when
            var exampleContext = container.Resolve(typeof (ExampleContext));

            // then
            Assert.That(exampleContext, Is.Not.Null);

            var context = exampleContext as ExampleContext;
            context.DbUnicorns.Add(new DbUnicorn {Name = "edward"});
            context.SaveChanges();
            Assert.That(context.DbUnicorns.First().Name, Is.EqualTo("edward"));

        }
    }

    public class ExampleContext : DbContext
    {
        public DbSet<DbUnicorn> DbUnicorns { get; set; }
        public ExampleContext(DbConnection dbConnection)
            : base(dbConnection, contextOwnsConnection: true)
        {
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbUnicorn>().HasKey(u => u.Id);
            modelBuilder.Entity<DbUnicorn>().Property(u => u.Name);
            modelBuilder.Entity<DbUnicorn>().HasMany(u => u.UniquePowers);
        }
    }

    public class DbUnicorn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<UniquePower> UniquePowers { get; set; }
    }

    public class UniquePower
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }
}
