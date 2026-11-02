using ILearn.Domain.Accounts;
using ILearn.Infrastructure.Accounts;
using ILearn;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text.Json;

var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var secretFile=Path.Combine(root,"ILearn","App_Data","test-account.json");
await using var source=NpgsqlDataSource.Create(Environment.GetEnvironmentVariable("ConnectionStrings__ILearn")??throw new InvalidOperationException("Set the local test connection."));
await new SchemaInitializer(source).Initialize();
if(File.Exists(secretFile)){
    var account=System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(secretFile))!;
    await using var lookup=source.CreateCommand("SELECT email FROM profiles WHERE normalized_name=$1");lookup.Parameters.AddWithValue(account["username"]!.GetValue<string>().ToUpperInvariant());
    if(await lookup.ExecuteScalarAsync() is string email)account["email"]=email;
    await File.WriteAllTextAsync(secretFile,account.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("Existing private test credentials retained; email metadata updated.");return;
}
var username="ilearn_tester";var password="T!"+Convert.ToHexString(RandomNumberGenerator.GetBytes(12))+"a";
var player=new Player(Guid.NewGuid(),username,"avatar-03",Guid.NewGuid().ToString("N"));
await using var command=source.CreateCommand("INSERT INTO profiles(id,name,normalized_name,password_hash,avatar,email,security_stamp,email_confirmed) VALUES($1,$2,$3,$4,$5,$6,$7,true)");
command.Parameters.AddWithValue(player.Id);command.Parameters.AddWithValue(username);command.Parameters.AddWithValue(username.ToUpperInvariant());command.Parameters.AddWithValue(new PasswordHasher<Player>().HashPassword(player,password));command.Parameters.AddWithValue(player.Avatar);command.Parameters.AddWithValue("qa.ilearn@example.test");command.Parameters.AddWithValue(player.Stamp);
await command.ExecuteNonQueryAsync();Directory.CreateDirectory(Path.GetDirectoryName(secretFile)!);await File.WriteAllTextAsync(secretFile,JsonSerializer.Serialize(new{username,email="qa.ilearn@example.test",password},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine("Created ilearn_tester; credentials saved only in ignored App_Data/test-account.json.");
