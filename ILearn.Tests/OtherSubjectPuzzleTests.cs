namespace ILearn.Tests;

public class OtherSubjectPuzzleTests
{
    public static IEnumerable<object[]> Examples()
    {
        yield return Example("counterexample",["2","3","5"],new()
        {
            Cases=[
                new(){Input=[0],Actual="زوج",Expected="زوج",Reason="دو با ادعا سازگار است."},
                new(){Input=[1],Actual="فرد",Expected="زوج",Success=true,Reason="ادعای همهٔ عددهای مثبت زوج‌اند را نقض می‌کند."},
                new(){Input=[2],Actual="فرد",Expected="زوج",Success=true,Reason="ضدنمونهٔ معتبر دیگری است."}]
        });
        yield return Example("repair",["+","-","*"],new()
        {
            Initial=[1],Limit=1,Tests=["2 OP 3 = 5"],Cases=[new(){Input=[0],Passed=[true]},new(){Input=[1],Passed=[false]},new(){Input=[2],Passed=[false]}]
        },["عملگر معادله"]);
        yield return Example("simulation",["دریافت یک بسته","تحویل یک بسته"],new()
        {
            States=["موجودی صفر","موجودی یک","موجودی دو"],Transitions=[[1,-1],[2,0],[-1,1]],Targets=[2],Limit=4
        });
        yield return Example("pipeline",["افزودن یک","دو برابر کردن"],new()
        {
            States=["1","2","3","4"],Transitions=[[1,1],[2,3],[3,-1],[-1,-1]],Targets=[3],Limit=3
        });
        yield return Example("groups",["2","3","4","5"],new()
        {
            Groups=[[0,2],[1,3]],Labels=["زوج","فرد"],Limit=3
        });
        yield return Example("questions",["مربع","مستطیل غیرمربع","دایره"],new()
        {
            Questions=["چهار ضلع دارد؟","تمام ضلع‌ها هم‌اندازه‌اند؟"],Traits=[[true,true],[true,false],[false,false]],Target=0,Limit=2
        });
        yield return Example("contradiction",["x > 3","x <= 3","x عدد صحیح است"],new()
        {
            Cases=[new(){Input=[0,1],Removals=[0,1],Reason="یک مقدار همزمان در دو نیم‌خط ناسازگار نیست."}]
        });
        yield return Example("argument",["جمع دو عدد زوج، زوج است","2+4=6","2k+2m=2(k+m)","عدد 3 فرد است"],new()
        {
            Cases=[new(){Input=[0,1,2],Success=true}]
        },["ادعا","مثال شاهد","دلیل عمومی"]);
        yield return Example("analogy",["ضرب در دو","ضرب در سه","3","6","9"],new()
        {
            Cases=[new(){Input=[0,2,3],Success=true}]
        },["رابطهٔ 2 به 4","عضو اول جفت دوم","عضو دوم جفت دوم"]);
        yield return Example("constraints",["جوشاندن آب","آماده‌کردن فنجان","دم‌کردن چای","سرو چای"],new()
        {
            Edges=[[0,2],[1,2],[2,3]]
        });
        yield return Example("domino",["متر","سانتی‌متر","میلی‌متر","دقیقه"],new()
        {
            Edges=[[0,1],[1,2]],Required=[1],Target=2,Limit=3
        });
        yield return Example("spectrum",["جایگاه عدد"],new()
        {
            Min=0,Max=10,Step=1,Low=4,High=6,Limit=4,Criterion="برآورد جایگاه عدد پنج با خطای حداکثر یک واحد",Anchors=["ابتدا: صفر","انتها: ده"]
        });
    }

    static object[] Example(string kind,string[] options,Puzzle puzzle,string[]? rows=null)=>
        [new Activity {Id=kind,Kind=kind,Title="نمونهٔ ریاضی یا زندگی روزمره",Prompt="با دادهٔ مستقل از C# مسئله را حل کن.",Options=options,Rows=rows??[],Puzzle=puzzle}];

    [Theory]
    [MemberData(nameof(Examples))]
    public void EveryNewTemplateWorksWithoutProgrammingSpecificRules(Activity activity)
    {
        Puzzles.Validate(activity);
        var entry=CSharpV2Tests.Solve(activity);
        Assert.True(Puzzles.Check(activity,entry.GameState,entry.Answers),activity.Kind);
        Assert.False(Puzzles.Check(activity,new(),["invalid"]));
    }
}
