using ILearn.Infrastructure.Accounts;
using Npgsql;
var connection=Environment.GetEnvironmentVariable("ILEARN_TEST_CONNECTION")??throw new InvalidOperationException("Set ILEARN_TEST_CONNECTION to an isolated QA database.");
var builder=new NpgsqlConnectionStringBuilder(connection);
if(!builder.Database!.EndsWith("_qa"))throw new InvalidOperationException("Only a QA database is allowed.");
await using var source=NpgsqlDataSource.Create(connection);
await new SchemaInitializer(source).Initialize();
var first=Guid.NewGuid();var second=Guid.NewGuid();
async Task Execute(string sql,Guid id){await using var c=source.CreateCommand(sql);c.Parameters.AddWithValue(id);await c.ExecuteNonQueryAsync();}
try {
 foreach(var id in new[]{first,second})await Execute("INSERT INTO profiles(id,name,normalized_name,avatar,email,email_confirmed) VALUES($1,$1::text,$1::text,'avatar-01',$1::text||'@example.test',true)",id);
 var store=new PlayerRepository(source);
 var a=await store.Load(first);a.Entries["csharp/S05/a"]=new(){Draft="draft",Complete=true};await store.Save(first,a);
 if((await store.Load(second)).Entries.Count!=0)throw new Exception("Cross-account progress leak.");
 var stale=await store.Load(first);var fresh=await store.Load(first);await store.Save(first,fresh);
 try{await store.Save(first,stale);throw new Exception("Stale revision accepted.");}catch(InvalidOperationException ex) when(ex.Message.Contains("تب")){}
 if((await store.Load(first)).Entries["csharp/S05/a"].Draft!="draft")throw new Exception("Draft lost.");
 Console.WriteLine("PASS: account isolation, JSON roundtrip, stale revision rejected, draft retained.");
}finally{foreach(var id in new[]{first,second}){await Execute("DELETE FROM progress WHERE profile_id=$1",id);await Execute("DELETE FROM profiles WHERE id=$1",id);}}
