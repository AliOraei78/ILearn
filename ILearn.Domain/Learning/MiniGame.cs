namespace ILearn.Domain.Learning;
public sealed class MiniGame
{
 public string[] Features {get;set;}=[];
 public string[][] Traits {get;set;}=[];
 public string[] Results {get;set;}=[];
 public int Limit {get;set;}=4;
 public int[] Costs {get;set;}=[];
 public string[][] Covers {get;set;}=[];
 public string[] Goals {get;set;}=[];
 public MissionNode[] Nodes {get;set;}=[];
}
public sealed class MissionNode
{
 public string Text {get;set;}="";
 public string[] Choices {get;set;}=[];
 public int[] Next {get;set;}=[];
 public bool Success {get;set;}
}
public sealed class MiniGameState
{
 public List<int> History {get;set;}=[];
 public Dictionary<string,int> Marks {get;set;}=[];
 public List<int[]> Trials {get;set;}=[];
 public List<List<string>> Undo {get;set;}=[];
 public List<List<int>> PathUndo {get;set;}=[];
 public int Found {get;set;}=-1;
 public int Removed {get;set;}=-1;
}
public static class MiniGames
{
 public static bool ValidState(Activity a,MiniGameState s,IEnumerable<string> input)
 {
  if(a.Game is not {} g)return false;
  if(a.Kind=="mission")return s.History.Count<=g.Limit&&Node(a,s)>=0&&Node(a,s)<g.Nodes.Length;
  if(a.Kind is "concept" or "experiment")return s.History.Count<=g.Limit&&s.History.Distinct().Count()==s.History.Count&&s.History.All(i=>i>=0&&i<(a.Kind=="concept"?a.Options.Length:g.Results.Length));
  if(a.Kind=="budget")return Puzzles.Numbers(input,out var values)&&values.Distinct().Count()==values.Length&&values.All(i=>i>=0&&i<g.Costs.Length&&i<g.Covers.Length);
  if(a.Kind=="deduction")return s.Marks.All(pair=>pair.Value is >=0 and <=2&&pair.Key.Split(':') is [var r,var c]&&int.TryParse(r,out int row)&&int.TryParse(c,out int column)&&row>=0&&row<a.Rows.Length&&column>=0&&column<a.Options.Length);
  return a.Kind=="assessment";
 }
 public static int Node(Activity a, MiniGameState s)
 {
  int node=0;
  foreach(int choice in s.History) {
   if(node<0 || node>=a.Game!.Nodes.Length || choice<0 || choice>=a.Game.Nodes[node].Next.Length) return -1;
   node=a.Game.Nodes[node].Next[choice];
  }
  return node;
 }
 public static bool Record(Activity a, MiniGameState s,int value)
 {
  if(a.Game is null || s.History.Count>=a.Game.Limit) return false;
  if(a.Kind=="mission") {
   int node=Node(a,s);
   if(node<0 || node>=a.Game.Nodes.Length || value<0 || value>=a.Game.Nodes[node].Next.Length) return false;
  } else {
   int count=a.Kind=="concept"?a.Options.Length:a.Game.Results.Length;
   if(a.Kind is not ("concept" or "experiment") || value<0 || value>=count || s.History.Contains(value)) return false;
   if(a.Kind=="concept" && s.History.Any(i=>a.Answers.Contains(i.ToString()))) return false;
  }
  s.History.Add(value); return true;
 }
 public static bool Check(Activity a,MiniGameState s,IEnumerable<string> input)
 {
  var values=input.ToArray(); var g=a.Game;
  if(g is null || !ValidState(a,s,values)) return false;
  switch(a.Kind) {
   case "concept": return s.History.Count<=g.Limit && s.History.Distinct().Count()==s.History.Count && s.History.All(i=>i>=0&&i<a.Options.Length) && s.History.Any(i=>a.Answers.Contains(i.ToString()));
   case "experiment": return s.History.Count>0 && s.History.Count<=g.Limit && s.History.Distinct().Count()==s.History.Count && s.History.All(i=>i>=0&&i<g.Results.Length) && a.Check(values);
   case "mission": int node=Node(a,s); return s.History.Count<=g.Limit && node>=0 && node<g.Nodes.Length && g.Nodes[node].Success;
   case "budget":
    var selected=new List<int>();
    foreach(var value in values) { if(!int.TryParse(value,out int i)||i<0||i>=g.Costs.Length||i>=g.Covers.Length||selected.Contains(i)) return false; selected.Add(i); }
    return selected.Count>0 && selected.Sum(i=>g.Costs[i])<=g.Limit && g.Goals.All(goal=>selected.Any(i=>g.Covers[i].Contains(goal)));
   case "deduction": return a.Check(s.Marks.Where(p=>p.Value==1).Select(p=>p.Key));
   case "assessment": return a.Check(values);
   default: return false;
  }
 }
}
