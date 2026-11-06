using System.Text.Json;
using ILearn.Application.Learning;
namespace ILearn.Tests;
public class MiniGameTests
{
 static List<Lesson> Lessons => JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../ILearn/Content/lessons.json"))))!;
 static Activity Game(string kind)=>Lessons.Single(l=>l.Id=="S08").Activities.Single(a=>a.Kind==kind);
 [Theory][InlineData("9:9",2)][InlineData("broken",0)][InlineData("0:9",-1)]
 public void CorruptDeductionMarksCannotAwardCompletion(string key,int value)
 {
  var activity=new Activity{Kind="deduction",Rows=["row"],Options=["choice"],Answers=["0:0"],Game=new()};
  var entry=new Entry();entry.GameState.Marks["0:0"]=1;entry.GameState.Marks[key]=value;
  Assert.False(MiniGames.ValidState(activity,entry.GameState,[]));
  LearningService.Check(activity,entry,[],0);
  Assert.False(entry.Complete);Assert.True(entry.NeedsReview);
 }
 [Fact] public void ChapterPreservesSixGamesAndAddsFocusedPractice(){var lesson=Lessons.Single(l=>l.Id=="S08");Assert.Equal(6,lesson.Activities.Count(a=>a.Game is not null));Assert.Equal(3,lesson.Activities.Count(a=>a.Kind=="workshop"));Assert.Equal(160,lesson.Activities.Sum(a=>a.Xp));}
 [Fact] public void GuessLimitAndDuplicates(){var a=Game("concept");var s=new MiniGameState();Assert.False(MiniGames.Record(a,s,-1));Assert.True(MiniGames.Record(a,s,0));Assert.False(MiniGames.Record(a,s,0));Assert.False(MiniGames.Check(a,s,[]));Assert.True(MiniGames.Record(a,s,2));Assert.True(MiniGames.Check(a,s,[]));Assert.False(MiniGames.Record(a,s,3));s=new();foreach(int i in new[]{0,1,3,4})Assert.True(MiniGames.Record(a,s,i));Assert.False(MiniGames.Record(a,s,2));}
 [Fact] public void ExperimentRequiresObservationAndCapsBudget(){var a=Game("experiment");var s=new MiniGameState();Assert.False(MiniGames.Check(a,s,a.Answers));foreach(int i in new[]{0,1,2})Assert.True(MiniGames.Record(a,s,i));Assert.False(MiniGames.Record(a,s,0));Assert.False(MiniGames.Record(a,s,3));Assert.True(MiniGames.Check(a,s,a.Answers));Assert.False(MiniGames.Check(a,s,["0"]));}
 [Fact] public void BudgetAcceptsMultipleSolutions(){var a=Game("budget");Assert.True(MiniGames.Check(a,new(),["0","1"]));Assert.True(MiniGames.Check(a,new(),["2"]));Assert.False(MiniGames.Check(a,new(),["0"]));Assert.False(MiniGames.Check(a,new(),["3"]));Assert.False(MiniGames.Check(a,new(),["2","2"]));Assert.False(MiniGames.Check(a,new(),["wrong"]));}
 [Fact] public void MissionPathsAndBounds(){var a=Game("mission");var s=new MiniGameState();Assert.False(MiniGames.Check(a,s,[]));foreach(int i in new[]{0,0,0})Assert.True(MiniGames.Record(a,s,i));Assert.True(MiniGames.Check(a,s,[]));Assert.False(MiniGames.Record(a,s,0));s=new();Assert.True(MiniGames.Record(a,s,1));Assert.False(MiniGames.Check(a,s,[]));Assert.False(MiniGames.Record(a,s,0));s.History.Add(99);Assert.False(MiniGames.Check(a,s,[]));}
 [Fact] public void AssessmentAndDeductionRequireExactAnswers(){var a=Game("assessment");Assert.True(MiniGames.Check(a,new(),a.Answers));Assert.False(MiniGames.Check(a,new(),a.Answers.Skip(1)));a=Game("deduction");var s=new MiniGameState();foreach(var key in a.Answers)s.Marks[key]=1;Assert.True(MiniGames.Check(a,s,[]));s.Marks["0:0"]=1;Assert.False(MiniGames.Check(a,s,[]));}
 [Fact] public void RoundTripAndOldSaves(){var old=JsonSerializer.Deserialize<Entry>("{\"Draft\":\"old\",\"Complete\":true}")!;Assert.Empty(old.GameState.History);old.GameState.History.Add(2);old.GameState.Marks["0:2"]=1;var restored=JsonSerializer.Deserialize<Entry>(JsonSerializer.Serialize(old))!;Assert.Equal("old",restored.Draft);Assert.True(restored.Complete);Assert.Equal(2,restored.GameState.History.Single());Assert.Equal(1,restored.GameState.Marks["0:2"]);}
 [Fact] public void CompletionNeverDuplicatesXp(){var a=Game("concept");var e=new Entry();MiniGames.Record(a,e.GameState,2);var lesson=new Lesson{Id="S08",Activities=[a]};var p=new Progress();p.Entries["csharp/S08/"+a.Id]=e;LearningService.Check(a,e,[],0);LearningService.Check(a,e,[],0);Assert.Equal(10,LearningService.Xp([lesson],p));}
 [Theory][InlineData("concept")][InlineData("experiment")][InlineData("budget")][InlineData("mission")][InlineData("assessment")][InlineData("deduction")]
 public void RedisDataUsesSameRules(string kind){var a=JsonSerializer.Deserialize<Activity>(JsonSerializer.Serialize(Game(kind)))!;a.Title="Redis scenario";a.Code="";a.Prompt="سناریوی آموزشی Redis";a.Options=a.Options.Select((_,i)=>"Redis option "+i).ToArray();var s=new MiniGameState();if(kind=="concept")MiniGames.Record(a,s,int.Parse(a.Answers[0]));if(kind=="experiment")MiniGames.Record(a,s,0);if(kind=="mission")foreach(int i in new[]{0,0,0})MiniGames.Record(a,s,i);if(kind=="deduction")foreach(var key in a.Answers)s.Marks[key]=1;Assert.True(MiniGames.Check(a,s,a.Answers));}
 interface IReader {string Read();}
 interface IWriter {void Write(string text);}
 sealed class Store:IReader,IWriter {string value="";string IReader.Read()=>value;void IWriter.Write(string text)=>value=text;}
 interface IDefault {string Read()=>"default";}
 sealed class DefaultStore:IDefault {}
 interface IFactory<T> {static abstract T Create();}
 sealed class Factory:IFactory<Factory>{public static Factory Create()=>new();}
 static T Create<T>() where T:IFactory<T> => T.Create();
 [Fact] public void ReferenceExamplesCompileAndRun(){var store=new Store();((IWriter)store).Write("report");Assert.Equal("report",((IReader)store).Read());Assert.Equal("default",((IDefault)new DefaultStore()).Read());Assert.IsType<Factory>(Create<Factory>());}
}
