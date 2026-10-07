using System.Collections.Generic;
using NUnit.Framework;

namespace MohallaHero.Tests
{
    public class InkLiteTests
    {
        class Host : IStoryHost
        {
            public readonly Dictionary<string, int> Vars = new Dictionary<string, int>();
            public readonly List<string> Calls = new List<string>();
            public int GetVar(string name) => Vars.TryGetValue(name, out var v) ? v : 0;
            public void SetVar(string name, int value) => Vars[name] = value;
            public int Call(string f, string[] args)
            {
                Calls.Add(f + "(" + string.Join(",", args) + ")");
                if (f == "has") return args[0] == "photo" ? 1 : 0;
                if (f == "double") return int.Parse(args[0]) * 2;
                return 0;
            }
            public string TextOf(string id) => id == "player_name" ? "Priya" : null;
        }

        static List<StoryStep> RunAll(InkStory story, Host host, string knot, params int[] choices)
        {
            var steps = new List<StoryStep>();
            var r = new InkRunner(story, host, 1);
            r.Start(knot);
            int c = 0;
            for (int guard = 0; guard < 100; guard++)
            {
                var s = r.Next();
                steps.Add(s);
                if (s.Kind == StepKind.End) break;
                if (s.Kind == StepKind.Choices) r.Choose(c < choices.Length ? choices[c++] : 0);
            }
            return steps;
        }

        static List<string> Lines(List<StoryStep> steps)
        {
            var list = new List<string>();
            foreach (var s in steps) if (s.Kind == StepKind.Line) list.Add((s.Speaker != null ? s.Speaker + ": " : "") + s.Text);
            return list;
        }

        const string Sample = @"
VAR count = 0
EXTERNAL has(item)
=== start ===
// a comment
Asha Ma'am: Hello {player_name}!
The street is quiet.
* [Switch it off] #green:+4 #why:Saves power
    You: Click!
    ~ count += 1
* {has(""photo"")} [Show the photo] #trust:+15
    You: Look at this.
* {has(""receipt"")} [Show the bill]
    You: This bill.
+ [Ask again]
    -> again
- Pintu: Count is {count}.
{count >= 1: Pintu: Nice! -> END | Pintu: Hmm.}
Pintu: Never shown if count >= 1.
-> END

=== again ===
Lala ji: {~Hello|Namaste|Hi} again.
-> END
";

        [Test]
        public void ParsesSpeakersTagsAndInterpolation()
        {
            var story = InkStory.Parse("sample", Sample);
            var host = new Host();
            var steps = RunAll(story, host, "start", 0);
            var lines = Lines(steps);
            Assert.AreEqual("Asha Ma'am: Hello Priya!", lines[0]);
            Assert.AreEqual("The street is quiet.", lines[1]);
            Assert.AreEqual("You: Click!", lines[2]);
            Assert.AreEqual("Pintu: Count is 1.", lines[3]);
            Assert.AreEqual("Pintu: Nice!", lines[4]);
            Assert.AreEqual(5, lines.Count, "the divert inside the conditional must end the story");
            var choiceStep = steps.Find(s => s.Kind == StepKind.Choices);
            Assert.AreEqual(3, choiceStep.Choices.Count, "the receipt choice is hidden because has(receipt) is 0");
            CollectionAssert.Contains(choiceStep.Choices[0].Tags, "green:+4");
            CollectionAssert.Contains(choiceStep.Choices[0].Tags, "why:Saves power");
        }

        [Test]
        public void OnceOnlyChoicesDisappear_StickyChoicesStay()
        {
            var story = InkStory.Parse("sample", Sample);
            var host = new Host();
            RunAll(story, host, "start", 0);
            var second = RunAll(story, host, "start", 1);   // "Switch it off" is gone now, so index 1 = "Ask again"
            var choices = second.Find(s => s.Kind == StepKind.Choices).Choices;
            Assert.AreEqual(2, choices.Count);
            Assert.AreEqual("Show the photo", choices[0].Text);
            Assert.AreEqual("Ask again", choices[1].Text);
            var lines = Lines(second);
            StringAssert.EndsWith("again.", lines[lines.Count - 1]);
        }

        [Test]
        public void ConditionalLineElseBranchAndExpressions()
        {
            var story = InkStory.Parse("t", @"
=== k ===
~ x = 2 + 3 * 2
~ y = double(x)
{x == 8 and not (y < 16): -> good | Narrator: bad}
-> END
=== good ===
Value {y}, {x > 5: big|small}.
-> END");
            var host = new Host();
            var lines = Lines(RunAll(story, host, "k"));
            Assert.AreEqual(8, host.Vars["x"]);
            Assert.AreEqual(16, host.Vars["y"]);
            Assert.AreEqual("Value 16, big.", lines[0]);
        }

        [Test]
        public void GatherContinuesAfterAnyChoice()
        {
            var story = InkStory.Parse("t", @"
=== k ===
* [A]
    one
* [B]
    two
- after
- ~ done = 1
end line");
            var host = new Host();
            var lines = Lines(RunAll(story, host, "k", 1));
            CollectionAssert.AreEqual(new[] { "two", "after", "end line" }, lines);
            Assert.AreEqual(1, host.Vars["done"]);
        }

        [Test]
        public void NoVisibleChoice_FallsThroughToGather()
        {
            var story = InkStory.Parse("t", @"
=== k ===
* {false} [hidden]
    nope
- shown");
            var lines = Lines(RunAll(story, new Host(), "k"));
            CollectionAssert.AreEqual(new[] { "shown" }, lines);
        }

        [Test]
        public void UnsupportedSyntaxIsReported()
        {
            Assert.Throws<InkParseException>(() => InkStory.Parse("t", "=== k ===\n* [a]\n** [nested]\n"));
            Assert.Throws<InkParseException>(() => InkStory.Parse("t", "=== k ===\n= stitch\n"));
            Assert.Throws<InkParseException>(() => InkStory.Parse("t", "=== k ===\ntext\n=== k ===\n"));
        }

        [Test]
        public void ValidateFindsBrokenDiverts()
        {
            var story = InkStory.Parse("t", "=== a ===\n-> b\n=== c ===\n* [x] -> nowhere\n");
            var problems = story.Validate();
            Assert.AreEqual(2, problems.Count);
        }

        [Test]
        public void IncludesAreCollected()
        {
            var story = InkStory.Parse("main", "INCLUDE other.txt\nVAR a = 5\nVAR b = true\nVAR s = \"hi\"\n=== k ===\nx\n");
            CollectionAssert.AreEqual(new[] { "other.txt" }, story.Includes);
            Assert.AreEqual(5, story.VarDefaults["a"]);
            Assert.AreEqual(1, story.VarDefaults["b"]);
            Assert.AreEqual(0, story.VarDefaults["s"]);
        }
    }
}
