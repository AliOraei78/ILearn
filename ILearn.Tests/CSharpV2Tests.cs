using System.Text.Json;
using System.Text.RegularExpressions;
using ILearn.Application.Learning;

namespace ILearn.Tests;

public class CSharpV2Tests
{
    static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    static List<Lesson> Lessons => JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.Combine(Root,"ILearn/Content/csharp-v2.json")))!;
    static Activity Puzzle(string id) => Lessons.Single(l=>l.Id==id).Activities.Single(a=>a.Puzzle is not null);
    static string[] Strings(IEnumerable<int> values)=>values.Select(i=>i.ToString()).ToArray();

    [Fact]
    public void EveryUnitMatchesTheRoadmapAndHasItsTeachingAndIndependentPractice()
    {
        var lessons=Lessons;
        Assert.Equal(Enumerable.Range(1,123).Select(i=>$"R{i:000}"),lessons.Select(l=>l.Id));
        Assert.Equal(Enumerable.Range(1,11),lessons.Select(l=>l.Phase).Distinct());
        var rows=Regex.Matches(File.ReadAllText(Path.Combine(Root,"csharp-mastery-roadmap.md")),@"(?m)^\| (R\d{3}) \| (.*?) \| (.*?) \| (.*?) \| (.*?) \|\r?$").Take(123).ToArray();
        Assert.Equal(123,rows.Length);
        foreach(var (lesson,row) in lessons.Zip(rows))
        {
            Assert.Equal(row.Groups[1].Value,lesson.Id);Assert.Equal(row.Groups[2].Value,lesson.Title);
            Assert.Equal(row.Groups[4].Value.Replace("`",""),lesson.Topics);Assert.Equal(row.Groups[5].Value,lesson.Objective);
            Assert.Equal(Regex.Matches(row.Groups[3].Value,@"R\d{3}").Select(m=>m.Value),lesson.Prerequisites);
            Assert.All(lesson.Prerequisites,id=>Assert.True(string.CompareOrdinal(id,lesson.Id)<0));
            Assert.True(lesson.Summary.Length>200,lesson.Id);Assert.NotEmpty(lesson.Example);Assert.NotEmpty(lesson.ExampleOutput);
            Assert.True(lesson.Activities.Count(a=>!a.IsAi)>=3,lesson.Id);
            Assert.Equal(lesson.Activities.Count,lesson.Activities.Select(a=>a.Id).Distinct().Count());
            Assert.Contains(lesson.Activities,a=>a.Kind=="workshop");
            foreach(var a in lesson.Activities)
            {
                Assert.NotEmpty(a.Prompt);Assert.NotEmpty(a.Solution);Assert.NotEmpty(a.Explanation);Assert.NotEmpty(a.Hint);
                foreach(var file in a.ReferenceFiles){Assert.False(Path.IsPathRooted(file.Key));Assert.DoesNotContain("..",file.Key.Split('/'));Assert.NotEmpty(file.Value);}
                if(a.Puzzle is not null)Puzzles.Validate(a);
                else if(!a.IsAi && a.Kind!="mission")Assert.NotEmpty(a.Answers);
                if(a.Kind=="workshop"){Assert.NotEmpty(a.SolutionCode);Assert.Equal("code",a.AnswerFormat);}
            }
        }
        Assert.Equal(Puzzles.Kinds.Order(),lessons.SelectMany(l=>l.Activities).Where(a=>a.Puzzle is not null).Select(a=>a.Kind).Distinct().Order());
        Assert.Equal(25,lessons.SelectMany(l=>l.Activities).Select(a=>a.Kind).Distinct().Count());
    }

    [Fact]
    public void EveryActivityCanBeCompletedReplayedAndRoundTrippedWithoutAwardingOldWorkToNewUnits()
    {
        var lessons=Lessons;var progress=new Progress();
        progress.Entries["csharp/S08/assessment"]=new Entry {Complete=true};
        Assert.False(progress.HasStarted("R028"));
        foreach(var lesson in lessons)
        {
            LearningService.Start(progress,lesson.Id);
            foreach(var activity in lesson.Activities)
            {
                var entry=Solve(activity);progress.Entries[$"csharp/{lesson.Id}/{activity.Id}"]=entry;
                if(activity.IsAi)LearningService.Complete(entry);
                else
                {
                    LearningService.Check(activity,entry,entry.Answers,activity.Clues.Length);
                    LearningService.Check(activity,entry,entry.Answers,activity.Clues.Length);
                }
                Assert.True(entry.Complete,lesson.Id+"/"+activity.Id+": "+entry.Feedback);Assert.False(entry.NeedsReview);
                var restored=JsonSerializer.Deserialize<Entry>(JsonSerializer.Serialize(entry))!;
                if(activity.Puzzle is not null)Assert.True(Puzzles.Check(activity,restored.GameState,restored.Answers),lesson.Id);
            }
        }
        var expected=lessons.Sum(l=>l.Activities.Sum(a=>a.Xp));
        Assert.Equal(expected,LearningService.Xp(lessons,progress));
        var legacy=new Lesson {Id="S08",Activities=[new Activity{Id="assessment"}]};
        Assert.Equal(expected+10,LearningService.Xp(lessons.Append(legacy),progress));
        Assert.Equal(expected,LearningService.Xp(lessons,JsonSerializer.Deserialize<Progress>(JsonSerializer.Serialize(progress))!));
    }

    [Fact]
    public void UnsolvedAndCorruptPuzzleStatesNeverComplete()
    {
        foreach(var a in Lessons.SelectMany(l=>l.Activities).Where(a=>a.Puzzle is not null))
        {
            Assert.False(Puzzles.Check(a,new(),[]),a.Title);
            Assert.False(Puzzles.Check(a,new(),["invalid"]),a.Title);
            Assert.False(Puzzles.Check(a,new(){History=[-1,9999]},["9999"]),a.Title);
        }
    }

    [Fact]
    public void CounterexamplesRequireAnObservedCurrentInputAndAcceptDifferentWitnesses()
    {
        var a=Puzzle("R004");var s=new MiniGameState();
        Assert.False(Puzzles.Check(a,s,["1"]));Assert.False(Puzzles.Try(a,s,["99"]));
        Assert.True(Puzzles.Try(a,s,["0"]));Assert.False(Puzzles.Check(a,s,["0"]));
        Assert.True(Puzzles.Try(a,s,["1"]));Assert.True(Puzzles.Check(a,s,["1"]));
        Assert.True(Puzzles.Try(a,s,["2"]));Assert.True(Puzzles.Check(a,s,["2"]));
        Assert.False(Puzzles.Try(a,s,["2"]));Assert.Equal(3,s.Trials.Count);
        s.Trials.Add([2]);Assert.False(Puzzles.Check(a,s,["2"]));
    }

    [Fact]
    public void RepairBudgetCountsChangedPositionsAndAcceptsEveryPassingConfiguration()
    {
        var a=new Activity {Kind="repair",Options=["old","fix","other fix"],Rows=["first","second"],Puzzle=new(){Initial=[0,0],Limit=1,Tests=["contract"],Cases=[new(){Input=[1,0],Passed=[true]},new(){Input=[2,0],Passed=[true]},new(){Input=[1,1],Passed=[true]}]}};
        Puzzles.Validate(a);Assert.Equal(1,Puzzles.RepairCost(a,["1","0"]));Assert.Equal(0,Puzzles.RepairCost(a,["0","0"]));
        Assert.True(Puzzles.Check(a,new(),["1","0"]));Assert.True(Puzzles.Check(a,new(),["2","0"]));
        Assert.False(Puzzles.Check(a,new(),["1","1"]));Assert.False(Puzzles.Check(a,new(),["1"]));Assert.False(Puzzles.Check(a,new(),["0","0"]));
    }

    [Fact]
    public void SimulationRejectsInvalidActionsAndReplaysHistory()
    {
        var a=Puzzle("R003");var s=new MiniGameState();
        Assert.False(Puzzles.Record(a,s,-1));Assert.True(Puzzles.Record(a,s,1));Assert.Equal(1,Puzzles.State(a,s.History));
        Assert.True(Puzzles.Record(a,s,1));Assert.True(Puzzles.Check(a,s,[]));
        s.History.RemoveAt(1);Assert.Equal(1,Puzzles.State(a,s.History));Assert.False(Puzzles.Check(a,s,[]));
        s.History.Add(999);Assert.False(Puzzles.Check(a,s,[]));
    }

    [Fact]
    public void PipelineAcceptsDifferentOrdersAndRejectsInvalidRepeatedOrOverlongPaths()
    {
        var a=Puzzle("R063");
        Assert.True(Puzzles.Check(a,new(){History=[0,1]},[]));Assert.True(Puzzles.Check(a,new(){History=[1,0]},[]));
        Assert.False(Puzzles.Check(a,new(){History=[0,0,1]},[]));Assert.False(Puzzles.Check(a,new(){History=[2,0,1]},[]));
        Assert.False(Puzzles.Check(a,new(){History=[0,1,0,1]},[]));
    }

    [Fact]
    public void GroupsLockSolvedCardsAndIncompleteOrRepeatedTrialsSpendNoQuota()
    {
        var a=Puzzle("R005");var s=new MiniGameState();var first=a.Puzzle!.Groups[0];
        Assert.False(Puzzles.Try(a,s,Strings(first.Take(2))));Assert.Empty(s.Trials);
        Assert.True(Puzzles.Try(a,s,Strings(first)));Assert.False(Puzzles.Try(a,s,Strings(first.Reverse())));
        Assert.Equal(0,Puzzles.GroupErrors(a,s));Assert.False(Puzzles.Try(a,s,["0","0","0"]));
        Assert.True(Puzzles.Try(a,s,Strings(a.Puzzle.Groups[1])));Assert.True(Puzzles.Check(a,s,[]));
    }

    [Fact]
    public void QuestionsKeepFinalGuessAvailableAtQuotaAndDoNotChargeRepeatedQuestions()
    {
        var a=Puzzle("R015");var s=new MiniGameState();
        Assert.True(Puzzles.Record(a,s,0));Assert.False(Puzzles.Record(a,s,0));Assert.True(Puzzles.Record(a,s,1));
        Assert.False(Puzzles.Record(a,s,0));Assert.Equal(2,s.History.Count);
        Assert.True(Puzzles.Check(a,s,["2"]));Assert.False(Puzzles.Check(a,s,["0"]));
    }

    [Fact]
    public void ContradictionRequiresMinimalDiscoveryAndValidRemoval()
    {
        var a=Puzzle("R029");var s=new MiniGameState();
        Assert.False(Puzzles.FindConflict(a,s,["0","1","2"]));Assert.False(Puzzles.FindConflict(a,s,["0","2"]));
        Assert.True(Puzzles.FindConflict(a,s,["1","0"]));Assert.False(Puzzles.Check(a,s,[]));
        s.Removed=0;Assert.True(Puzzles.Check(a,s,[]));s.Removed=1;Assert.True(Puzzles.Check(a,s,[]));s.Removed=2;Assert.False(Puzzles.Check(a,s,[]));
    }

    [Fact]
    public void ArgumentsRequireRelevantEvidenceAndAnalogiesRespectDirection()
    {
        var a=Puzzle("R028");Assert.True(Puzzles.Check(a,new(),["0","2","4"]));Assert.False(Puzzles.Check(a,new(),["0","2","5"]));
        a=Puzzle("R026");Assert.True(Puzzles.Check(a,new(),["0","2","3"]));Assert.False(Puzzles.Check(a,new(),["0","3","2"]));
    }

    [Fact]
    public void RepairTrialsKeepObservedVersionsWithoutCompletingTheGameOrAcceptingCorruptHistory()
    {
        var a=Puzzle("R002");var state=new MiniGameState();
        Assert.False(Puzzles.Try(a,state,[]));Assert.False(Puzzles.Try(a,state,["999"]));Assert.Empty(state.Trials);
        Assert.True(Puzzles.Try(a,state,["0"]));Assert.False(Puzzles.Try(a,state,["0"]));
        Assert.False(Puzzles.Check(a,state,["0"]));Assert.Single(state.Trials);
        Assert.True(Puzzles.Try(a,state,["1"]));Assert.Equal(2,state.Trials.Count);
        Assert.True(Puzzles.Check(a,state,["1"]));Assert.True(Puzzles.Check(a,new(),["1"]));
        var saved=JsonSerializer.Deserialize<MiniGameState>(JsonSerializer.Serialize(state))!;
        Assert.True(Puzzles.ValidState(a,saved));Assert.Equal([0],saved.Trials[0]);Assert.Equal([1],saved.Trials[1]);
        saved.Trials.Add([999]);Assert.False(Puzzles.ValidState(a,saved));Assert.False(Puzzles.Check(a,saved,["1"]));
        saved.Trials=[new[]{0},new[]{0}];Assert.False(Puzzles.ValidState(a,saved));
    }

    [Fact]
    public void ConstraintOrderAcceptsAllTopologicalOrdersAndRefusesCyclesAndDuplicates()
    {
        var a=Puzzle("R016");Assert.True(Puzzles.Check(a,new(),["0","1","2","3","4"]));Assert.True(Puzzles.Check(a,new(),["1","2","0","3","4"]));
        Assert.False(Puzzles.Check(a,new(),["0","1","2","4","3"]));Assert.False(Puzzles.Check(a,new(),["0","1","2","3","3"]));
        a.Puzzle!.Edges=[[0,1],[1,0]];Assert.Throws<InvalidDataException>(()=>Puzzles.Validate(a));
    }

    [Fact]
    public void DominoNeedsRequiredCardsAndDoesNotAllowRepeatedCards()
    {
        var a=Puzzle("R047");a.Puzzle!.Edges=[..a.Puzzle.Edges,[0,3]];
        Assert.False(Puzzles.Check(a,new(){History=[3]},[]));Assert.True(Puzzles.Check(a,new(){History=[1,2,3]},[]));
        Assert.False(Puzzles.Check(a,new(){History=[1,1,2,3]},[]));Assert.False(Puzzles.Record(a,new(),3+99));
    }

    [Fact]
    public void SpectrumAcceptsRangeEndpointsAfterTrialAndEnforcesStepAndQuota()
    {
        var a=Puzzle("R010");
        a.Puzzle!.Low = 11;
        a.Puzzle.High = 13;
        foreach(int value in new[]{11,12,13}) {var s=new MiniGameState();Assert.True(Puzzles.Try(a,s,[value.ToString()]));Assert.True(Puzzles.Check(a,s,[value.ToString()]));}
        Assert.False(Puzzles.Check(a,new(),["12"]));var state=new MiniGameState();
        foreach(int value in new[]{0,1,2,3})Assert.True(Puzzles.Try(a,state,[value.ToString()]));
        Assert.False(Puzzles.Try(a,state,["12"]));Assert.False(Puzzles.Check(a,state,["12"]));
        a.Puzzle!.Step=2;Assert.False(Puzzles.Try(a,new(),["11"]));Assert.False(Puzzles.Try(a,new(),["99"]));
    }

    [Theory]
    [InlineData("11", false)]
    [InlineData("12", true)]
    [InlineData("13", false)]
    public void ReadingEveryTableCellRequiresTheExactCount(string value, bool correct)
    {
        var activity = Puzzle("R010");
        var state = new MiniGameState();

        Assert.True(Puzzles.Try(activity, state, [value]));
        Assert.Equal(correct, Puzzles.Check(activity, state, [value]));
    }

    [Fact]
    public void OldAndNewRoundStateSurvivesJsonWithoutLosingDraftOrCompletion()
    {
        var old=JsonSerializer.Deserialize<Entry>("""{"Draft":"old","Complete":true,"GameState":{"History":[0]}}""")!;
        Assert.Empty(old.GameState.Trials);Assert.Empty(old.GameState.PathUndo);Assert.Equal(-1,old.GameState.Found);
        old.GameState.Trials.Add([1]);old.GameState.Undo.Add(["2"]);old.GameState.PathUndo.Add([0,1]);old.GameState.Found=0;old.GameState.Removed=1;
        var restored=JsonSerializer.Deserialize<Entry>(JsonSerializer.Serialize(old))!;Assert.True(restored.Complete);Assert.Equal("old",restored.Draft);
        Assert.Equal(1,restored.GameState.Trials[0][0]);Assert.Equal("2",restored.GameState.Undo[0][0]);Assert.Equal(new[]{0,1},restored.GameState.PathUndo[0]);Assert.Equal(1,restored.GameState.Removed);
    }

    [Fact]
    public void SchemaContradictionNeedsAllThreeClaimsWithoutAnUnstatedAssumption()
    {
        var activity=Puzzle("R072");
        foreach(var pair in new[]{new[]{0,1},new[]{0,2},new[]{1,2}})
            Assert.False(Puzzles.FindConflict(activity,new(),Strings(pair)));
        foreach(var removal in Enumerable.Range(0,3))
        {
            var state=new MiniGameState();
            Assert.True(Puzzles.FindConflict(activity,state,["0","1","2"]));
            state.Removed=removal;
            Assert.True(Puzzles.Check(activity,state,[]));
        }
    }

    [Fact]
    public void EveryQuestionTargetCanBeDistinguishedWithinTheQuestionBudget()
    {
        foreach(var activity in Lessons.SelectMany(l=>l.Activities).Where(a=>a.Kind=="questions"))
        {
            var puzzle=activity.Puzzle!;
            Assert.InRange(puzzle.Questions.Length,1,16);
            Assert.True(Enumerable.Range(0,1<<puzzle.Questions.Length).Any(mask=>
                System.Numerics.BitOperations.PopCount((uint)mask)<=puzzle.Limit&&
                puzzle.Traits.Select((traits,index)=>(traits,index)).All(candidate=>candidate.index==puzzle.Target||
                    candidate.traits.Where((value,column)=>(mask&(1<<column))!=0&&value!=puzzle.Traits[puzzle.Target][column]).Any())),activity.Title);
        }
    }

    [Fact]
    public void EveryRepairSelectionHasOneDefinedTestResultAndChoiceLabelsAreDistinct()
    {
        foreach(var activity in Lessons.SelectMany(l=>l.Activities))
        {
            Assert.Equal(activity.Options.Length,activity.Options.Distinct().Count());
            if(activity.Kind!="repair")continue;
            Assert.Equal((int)Math.Pow(activity.Options.Length,activity.Rows.Length),activity.Puzzle!.Cases.Length);
            Assert.Equal(activity.Puzzle.Cases.Length,activity.Puzzle.Cases.Select(c=>string.Join(",",c.Input)).Distinct().Count());
        }
    }

    [Fact]
    public void AnalogyFeedbackExplainsTheRelationshipBeingTested()
    {
        var activity=Puzzle("R011");var entry=Solve(activity);
        LearningService.Check(activity,entry,entry.Answers,0);
        Assert.Contains("پارامتر",entry.Feedback);Assert.Contains("آرگومان",entry.Feedback);
        Assert.DoesNotContain("overload",entry.Feedback);
    }

    internal static Entry Solve(Activity a)
    {
        var entry=new Entry {Draft=a.SolutionCode.Length>0?a.SolutionCode:a.Solution,Answers=a.Answers.ToList()};var s=entry.GameState;
        if(a.Puzzle is { } p)
        {
            switch(a.Kind)
            {
                case "counterexample":entry.Answers=Strings(p.Cases.First(c=>c.Success).Input).ToList();Puzzles.Try(a,s,entry.Answers);break;
                case "repair":entry.Answers=Strings(p.Cases.First(c=>c.Passed.All(x=>x)&&Puzzles.RepairCost(a,Strings(c.Input))<=p.Limit).Input).ToList();break;
                case "simulation":case "pipeline":case "domino":
                    var queue=new Queue<List<int>>();queue.Enqueue([]);bool found=false;
                    while(queue.TryDequeue(out var path))
                    {
                        if(Puzzles.Check(a,new(){History=path},[])){s.History=path;found=true;break;}
                        if(path.Count>=p.Limit)continue;
                        for(int i=0;i<a.Options.Length;i++) {var candidate=new MiniGameState{History=path.ToList()};if(Puzzles.Record(a,candidate,i))queue.Enqueue(candidate.History);}
                    }
                    Assert.True(found,a.Title);break;
                case "groups":foreach(var group in p.Groups)Assert.True(Puzzles.Try(a,s,Strings(group)));entry.Answers=[];break;
                case "questions":foreach(var i in Enumerable.Range(0,Math.Min(p.Limit,p.Questions.Length)))Puzzles.Record(a,s,i);entry.Answers=[p.Target.ToString()];break;
                case "contradiction":Puzzles.FindConflict(a,s,Strings(p.Cases[0].Input));s.Removed=p.Cases[0].Removals[0];entry.Answers=[];break;
                case "argument":case "analogy":entry.Answers=Strings(p.Cases.First(c=>c.Success).Input).ToList();break;
                case "constraints":
                    var order=new List<int>();while(order.Count<a.Options.Length)order.Add(Enumerable.Range(0,a.Options.Length).First(i=>!order.Contains(i)&&p.Edges.All(e=>e[1]!=i||order.Contains(e[0]))));entry.Answers=Strings(order).ToList();break;
                case "spectrum":int value=p.Min+(int)Math.Ceiling((p.Low-p.Min)/(double)p.Step)*p.Step;entry.Answers=[value.ToString()];Puzzles.Try(a,s,entry.Answers);break;
            }
        }
        if(a.Kind=="concept")MiniGames.Record(a,s,int.Parse(a.Answers[0]));
        if(a.Kind=="experiment")MiniGames.Record(a,s,0);
        if(a.Kind=="deduction")foreach(var answer in a.Answers)s.Marks[answer]=1;
        if(a.Kind=="mission")
        {
            var queue=new Queue<List<int>>();queue.Enqueue([]);bool found=false;
            while(queue.TryDequeue(out var path))
            {
                var candidate=new MiniGameState{History=path};int node=MiniGames.Node(a,candidate);
                if(a.Game!.Nodes[node].Success){s.History=path;found=true;break;}
                if(path.Count<a.Game.Limit)for(int i=0;i<a.Game.Nodes[node].Next.Length;i++)queue.Enqueue([..path,i]);
            }
            Assert.True(found,a.Title);
        }
        return entry;
    }
}
