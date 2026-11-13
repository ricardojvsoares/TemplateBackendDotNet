using System.Reflection;
using DbUp;
using Npgsql;

DotNetEnv.Env.TraversePath().Load();

var cs = new NpgsqlConnectionStringBuilder
{
    Host = Environment.GetEnvironmentVariable("POSTGRES_HOST"),
    Port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"),
    Database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE"),
    Username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME"),
    Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD"),
}.ConnectionString;

EnsureDatabase.For.PostgresqlDatabase(cs);

var result = DeployChanges.To.PostgresqlDatabase(cs)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .LogToConsole()
    .Build()
    .PerformUpgrade();

return result.Successful ? 0 : 1;