namespace EffortlessDbTesting
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Data.Entity.Validation;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using Autofac;
    using System.Linq;
    using NUnit.Framework;
    using Effort;

    [TestFixture]
    public class ExampleContextTests
    {
        readonly DbConnection _dbConnection = DbConnectionFactory.CreateTransient();

        [Test]
        public void Should_initialize_repository_with_fake_context()
        {
            // given
            var exampleContext = new ExampleContext(_dbConnection, null);
            exampleContext.Database.CreateIfNotExists();

            // when            
            exampleContext.DbUnicorns.Add(new DbUnicorn
            {
                Name = "edward",
                UniquePowers = new List<UniquePower>{
                    new UniquePower
                    {
                        Description = "piercing the enemy"
                    }}
            });
            exampleContext.SaveChanges();

            // then
            var firstUnicorn = exampleContext.DbUnicorns.First();
            Assert.That(firstUnicorn.Name, Is.EqualTo("edward"));
            Assert.That(firstUnicorn.UniquePowers.First().Description, Is.EqualTo("piercing the enemy"));

            exampleContext.Database.Delete();
            exampleContext.Dispose();
        }


        [Test]
        public void Should_initialize_context_properly()
        {
            // given
            var exampleContext = new ExampleContext(_dbConnection, new ExampleContextInitializer());
            exampleContext.Database.CreateIfNotExists();

            // when
            var dbUnicorn = exampleContext.DbUnicorns.Include(u=>u.UniquePowers).First();

            // then
            Assert.That(dbUnicorn.Name, Is.EqualTo("gary"));
            Assert.That(dbUnicorn.UniquePowers.First().Description, Is.EqualTo("karate"));

            
            exampleContext.Database.Delete();
            exampleContext.Dispose();
        }

        [Test]
        [Ignore]
        public void Should_inject_dbconnection_to_context()
        {
            // given
            var builder = new ContainerBuilder();

            //string connectionString = @"Server=(localdb)\v11.0;Database=Unicorns;Integrated Security=true;";
            const string connectionString = @"Server=.\SQLEXPRESS;Database=Unicorns;Integrated Security=True;MultipleActiveResultSets=true";
            builder.Register(c => new ExampleContext(new SqlConnection(connectionString), new ExampleContextInitializer())).AsSelf();

            var container = builder.Build();

            // when
            var exampleContext = container.Resolve(typeof(ExampleContext));
            var context = exampleContext as ExampleContext;

            // then
            Assert.That(context, Is.Not.Null);

            context.DbUnicorns.Add(new DbUnicorn { Name = "edward" });
            context.SaveChanges();
            Assert.That(context.DbUnicorns.First().Name, Is.EqualTo("gary"));
            Assert.That(context.DbUnicorns.ToList()[1].Name, Is.EqualTo("edward"));
            
        }
    }

    public class ExampleContext : DbContext
    {
        public DbSet<DbUnicorn> DbUnicorns { get; set; }
        public DbSet<UniquePower> UniquePowers { get; set; }
        public ExampleContext(DbConnection dbConnection, IDatabaseInitializer<ExampleContext> initializer)
            : base(dbConnection, contextOwnsConnection: true)
        {
            if (initializer != null)
            {
                Database.SetInitializer(initializer);
            }
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbUnicorn>().HasKey(u => u.Id);
            modelBuilder.Entity<DbUnicorn>().Property(u => u.Name);
        }
    }

    public class ExampleContextInitializer : DropCreateDatabaseAlways<ExampleContext>
    {
        public override void InitializeDatabase(ExampleContext context)
        {
            var dbUnicorn = new DbUnicorn
            {
                Id = 1,
                Name = "gary"
            };
            context.DbUnicorns.Add(dbUnicorn);
            SaveContext(context);

            dbUnicorn.UniquePowers = new List<UniquePower>
            {
                new UniquePower
                {
                    Id = 1,
                    Description = "karate"
                }
            };
            SaveContext(context);
        }

        private static void SaveContext(ExampleContext context)
        {
            try
            {
                context.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Debug.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Debug.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
        }
    }

    public class DbUnicorn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public virtual List<UniquePower> UniquePowers { get; set; }
    }

    public class UniquePower
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public virtual DbUnicorn DbUnicorn { get; set; }
    }
}
