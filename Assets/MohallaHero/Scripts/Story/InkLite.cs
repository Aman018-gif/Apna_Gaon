using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MohallaHero
{
    /// <summary>
    /// What a story needs from the game: variables (shared with missions and saves) and external functions
    /// (declared with <c>EXTERNAL</c> in the .ink file, e.g. <c>has("photo")</c>, <c>trust("raju")</c>).
    /// </summary>
    public interface IStoryHost
    {
        int GetVar(string name);
        void SetVar(string name, int value);
        /// <summary>Calls an EXTERNAL function. Arguments arrive as strings (numbers are formatted invariantly).</summary>
        int Call(string function, string[] args);
        /// <summary>Text for <c>{expr}</c> inside a line, e.g. <c>{player_name}</c>. Return null to use the numeric value.</summary>
        string TextOf(string identifier);
    }

    // ---------------------------------------------------------------------------------------------- nodes

    public abstract class InkNode { public int Line; }

    public sealed class TextNode : InkNode
    {
        public string Speaker;          // null = narration
        public string Text;
        public List<string> Tags = new List<string>();
    }

    public sealed class DivertNode : InkNode { public string Target; }

    public sealed class CodeNode : InkNode { public string Code; }

    /// <summary><c>{condition: A | B}</c> on its own line, where A and B are a line of text or a divert.</summary>
    public sealed class BranchNode : InkNode
    {
        public string Condition;
        public List<InkNode> IfTrue = new List<InkNode>(), IfFalse = new List<InkNode>();
    }

    public sealed class InkChoice
    {
        public string Id;
        public string Text;
        public string Condition;
        public bool Sticky;
        public List<string> Tags = new List<string>();
        public List<InkNode> Body = new List<InkNode>();
    }

    public sealed class ChoiceGroupNode : InkNode { public List<InkChoice> Choices = new List<InkChoice>(); }

    public class InkParseException : Exception
    {
        public InkParseException(string file, int line, string message) : base($"{file}:{line}: {message}") { }
    }

    // ---------------------------------------------------------------------------------------------- story

    /// <summary>
    /// A parsed story. The supported subset of Ink (inkle's narrative scripting language) is:
    /// <list type="bullet">
    /// <item><c>=== knot ===</c>, <c>-> knot</c>, <c>-> END</c> / <c>-> DONE</c>, <c>INCLUDE file</c></item>
    /// <item><c>VAR name = 0</c>, <c>~ name = expr</c>, <c>~ name += expr</c>, <c>~ name -= expr</c>, <c>~ func("arg")</c></item>
    /// <item>one level of choices: <c>* [text]</c> (once-only), <c>+ [text]</c> (sticky), <c>* {cond} [text] -> knot #tag</c></item>
    /// <item>gathers <c>- text</c>, line conditionals <c>{cond: -> knot | text}</c>, <c>{expr}</c> and <c>{~a|b|c}</c> inside text</item>
    /// <item>tags <c>#key:value</c>, and the convention <c>Speaker: line</c></item>
    /// </list>
    /// Nested choices (<c>**</c>), stitches, tunnels, threads and lists are not supported; the parser reports them.
    /// Every file that parses here is also valid Ink, so the project can switch to the official ink-unity-integration.
    /// </summary>
    public sealed class InkStory
    {
        public readonly Dictionary<string, List<InkNode>> Knots = new Dictionary<string, List<InkNode>>();
        public readonly Dictionary<string, int> VarDefaults = new Dictionary<string, int>();
        public readonly List<string> Includes = new List<string>();
        public readonly HashSet<string> Externals = new HashSet<string>();

        public bool HasKnot(string knot) => knot != null && Knots.ContainsKey(knot);

        /// <summary>Parses one file. Content before the first knot goes into the knot "__root".</summary>
        public static InkStory Parse(string fileName, string source) => new InkStory().Add(fileName, source);

        /// <summary>Parses another file into this story (how INCLUDE is resolved). Knot names must be unique.</summary>
        public InkStory Add(string fileName, string source)
        {
            new Parser(this, fileName).Run(source);
            return this;
        }

        /// <summary>Checks that every divert points to a knot that exists. Returns the problems found.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            void Check(List<InkNode> nodes, string knot)
            {
                foreach (var n in nodes)
                {
                    switch (n)
                    {
                        case DivertNode d: CheckTarget(d.Target, knot, d.Line); break;
                        case BranchNode b:
                            Check(b.IfTrue, knot);
                            Check(b.IfFalse, knot);
                            break;
                        case ChoiceGroupNode g:
                            foreach (var c in g.Choices) Check(c.Body, knot);
                            break;
                    }
                }
            }
            void CheckTarget(string target, string knot, int line)
            {
                if (target == "END" || target == "DONE") return;
                if (!Knots.ContainsKey(target)) problems.Add($"knot '{knot}' line {line}: divert to unknown knot '{target}'");
            }
            foreach (var kv in Knots) Check(kv.Value, kv.Key);
            return problems;
        }

        // ------------------------------------------------------------------------------------------ parser

        sealed class Parser
        {
            readonly InkStory story;
            readonly string file;
            List<InkNode> knotNodes;
            string knotName;
            ChoiceGroupNode openGroup;
            InkChoice currentChoice;
            int lineNo;
            int choiceCounter;

            public Parser(InkStory story, string file) { this.story = story; this.file = file; }

            List<InkNode> Target => currentChoice != null ? currentChoice.Body : knotNodes;

            void Fail(string msg) => throw new InkParseException(file, lineNo, msg);

            void StartKnot(string name)
            {
                if (story.Knots.ContainsKey(name)) Fail($"knot '{name}' is defined twice");
                knotName = name;
                knotNodes = new List<InkNode>();
                story.Knots[name] = knotNodes;
                openGroup = null;
                currentChoice = null;
                choiceCounter = 0;
            }

            public void Run(string source)
            {
                var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                bool inBlockComment = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    lineNo = i + 1;
                    string line = lines[i].Trim();
                    if (inBlockComment)
                    {
                        int end = line.IndexOf("*/", StringComparison.Ordinal);
                        if (end < 0) continue;
                        inBlockComment = false;
                        line = line.Substring(end + 2).Trim();
                    }
                    if (line.StartsWith("/*", StringComparison.Ordinal))
                    {
                        if (line.IndexOf("*/", 2, StringComparison.Ordinal) < 0) inBlockComment = true;
                        continue;
                    }
                    if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
                    ParseLine(line);
                }
            }

            void ParseLine(string line)
            {
                if (line.StartsWith("INCLUDE ", StringComparison.Ordinal)) { story.Includes.Add(line.Substring(8).Trim()); return; }
                if (line.StartsWith("EXTERNAL ", StringComparison.Ordinal))
                {
                    string sig = line.Substring(9).Trim();
                    int p = sig.IndexOf('(');
                    story.Externals.Add(p > 0 ? sig.Substring(0, p).Trim() : sig);
                    return;
                }
                if (line.StartsWith("VAR ", StringComparison.Ordinal) || line.StartsWith("CONST ", StringComparison.Ordinal))
                {
                    var rest = line.Substring(line.IndexOf(' ') + 1);
                    int eq = rest.IndexOf('=');
                    if (eq < 0) Fail("VAR needs a value");
                    string name = rest.Substring(0, eq).Trim();
                    string val = rest.Substring(eq + 1).Trim();
                    int v = 0;
                    if (val == "true") v = 1;
                    else if (val == "false" || val.StartsWith("\"", StringComparison.Ordinal)) v = 0;
                    else if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) Fail($"VAR {name}: only whole numbers, true/false or strings are supported");
                    story.VarDefaults[name] = v;
                    return;
                }
                if (line.StartsWith("==", StringComparison.Ordinal))
                {
                    string name = line.Trim('=', ' ');
                    if (name.StartsWith("function ", StringComparison.Ordinal)) Fail("ink functions are not supported; use EXTERNAL functions");
                    if (name.Length == 0 || name.IndexOf(' ') >= 0) Fail($"bad knot header '{line}'");
                    StartKnot(name);
                    return;
                }
                if (line[0] == '=' ) Fail("stitches (= name) are not supported; use knots");

                if (knotNodes == null) StartKnot("__root");

                if (line[0] == '*' || line[0] == '+') { ParseChoice(line); return; }

                if (line[0] == '-' && !line.StartsWith("->", StringComparison.Ordinal))
                {
                    // Gather: closes the current choice group. Any text after it continues at knot level.
                    if (line.Length > 1 && line[1] == '-') Fail("nested gathers (- -) are not supported");
                    currentChoice = null;
                    openGroup = null;
                    string rest = line.Substring(1).Trim();
                    if (rest.Length > 0) ParseLine(rest);
                    return;
                }

                if (line[0] == '~') { Target.Add(new CodeNode { Code = line.Substring(1).Trim(), Line = lineNo }); MaybeCloseGroup(); return; }

                ParseContent(line, Target);
                MaybeCloseGroup();
            }

            // Content at knot level after a choice group (without a gather) would be part of the last choice's body
            // in Ink, which is how we treat it: nothing to do here. Kept as a hook for clarity.
            void MaybeCloseGroup() { }

            void ParseChoice(string line)
            {
                char marker = line[0];
                int depth = 0;
                while (depth < line.Length && (line[depth] == '*' || line[depth] == '+' || line[depth] == ' '))
                {
                    if (line[depth] != ' ') { if (line[depth] != marker) Fail("mixed choice markers"); }
                    depth++;
                }
                int markers = 0;
                foreach (char ch in line.Substring(0, depth)) if (ch == marker) markers++;
                if (markers > 1) Fail("nested choices (** or ++) are not supported; divert to a new knot instead");
                string rest = line.Substring(depth).Trim();

                var choice = new InkChoice { Sticky = marker == '+', Id = $"{knotName}#{choiceCounter++}" };

                // Optional condition(s) in braces before the text
                while (rest.StartsWith("{", StringComparison.Ordinal))
                {
                    int close = MatchingBrace(rest, 0);
                    if (close < 0) Fail("unclosed { in choice");
                    string cond = rest.Substring(1, close - 1).Trim();
                    choice.Condition = choice.Condition == null ? cond : $"({choice.Condition}) and ({cond})";
                    rest = rest.Substring(close + 1).Trim();
                }

                // Labels like (name) are not supported
                if (rest.StartsWith("(", StringComparison.Ordinal)) Fail("choice labels are not supported");

                SplitTags(ref rest, choice.Tags);
                string divert = SplitDivert(ref rest);

                string before = rest, inside = "", after = "";
                int open = rest.IndexOf('[');
                if (open >= 0)
                {
                    int close = rest.IndexOf(']', open);
                    if (close < 0) Fail("unclosed [ in choice");
                    before = rest.Substring(0, open);
                    inside = rest.Substring(open + 1, close - open - 1);
                    after = rest.Substring(close + 1);
                }
                choice.Text = (before + inside).Trim();
                if (choice.Text.Length == 0) Fail("fallback choices (no text) are not supported");

                string output = (before + after).Trim();
                if (output.Length > 0) choice.Body.Add(MakeText(output, new List<string>()));
                if (divert != null) choice.Body.Add(new DivertNode { Target = divert, Line = lineNo });

                if (openGroup == null)
                {
                    openGroup = new ChoiceGroupNode { Line = lineNo };
                    knotNodes.Add(openGroup);
                }
                openGroup.Choices.Add(choice);
                currentChoice = choice;
            }

            void ParseContent(string line, List<InkNode> into)
            {
                if (line.StartsWith("->", StringComparison.Ordinal))
                {
                    into.Add(new DivertNode { Target = line.Substring(2).Trim(), Line = lineNo });
                    return;
                }
                if (line[0] == '{' && MatchingBrace(line, 0) == line.Length - 1 && !line.StartsWith("{~", StringComparison.Ordinal))
                {
                    string inner = line.Substring(1, line.Length - 2);
                    int colon = TopLevelIndex(inner, ':');
                    if (colon > 0)
                    {
                        string cond = inner.Substring(0, colon).Trim();
                        string body = inner.Substring(colon + 1);
                        int bar = TopLevelIndex(body, '|');
                        string a = bar >= 0 ? body.Substring(0, bar) : body;
                        string b = bar >= 0 ? body.Substring(bar + 1) : null;
                        into.Add(new BranchNode { Condition = cond, IfTrue = BranchPart(a), IfFalse = b != null ? BranchPart(b) : new List<InkNode>(), Line = lineNo });
                        return;
                    }
                }
                var tags = new List<string>();
                SplitTags(ref line, tags);
                string divert = SplitDivert(ref line);
                if (line.Trim().Length > 0) into.Add(MakeText(line.Trim(), tags));
                if (divert != null) into.Add(new DivertNode { Target = divert, Line = lineNo });
            }

            List<InkNode> BranchPart(string part)
            {
                var nodes = new List<InkNode>();
                part = part.Trim();
                if (part.Length == 0) return nodes;
                var tags = new List<string>();
                SplitTags(ref part, tags);
                string divert = SplitDivert(ref part);
                if (part.Trim().Length > 0) nodes.Add(MakeText(part.Trim(), tags));
                if (divert != null) nodes.Add(new DivertNode { Target = divert, Line = lineNo });
                return nodes;
            }

            TextNode MakeText(string text, List<string> tags)
            {
                var node = new TextNode { Tags = tags, Line = lineNo };
                int colon = text.IndexOf(": ", StringComparison.Ordinal);
                if (colon > 0 && colon <= 24 && IsSpeakerName(text.Substring(0, colon)))
                {
                    node.Speaker = text.Substring(0, colon).Trim();
                    node.Text = text.Substring(colon + 2).Trim();
                }
                else node.Text = text;
                return node;
            }

            static bool IsSpeakerName(string s)
            {
                if (s.Length == 0 || !char.IsUpper(s[0])) return false;
                foreach (char ch in s)
                    if (!(char.IsLetter(ch) || ch == ' ' || ch == '.' || ch == '\'')) return false;
                return true;
            }

            /// <summary>Removes trailing <c>#tags</c> (outside braces and rich-text colour codes) and collects them.</summary>
            void SplitTags(ref string s, List<string> tags)
            {
                int depth = 0, angle = 0;
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                    else if (ch == '<' && depth == 0) angle++;
                    else if (ch == '>' && depth == 0 && angle > 0) angle--;
                    else if (ch == '#' && depth == 0 && angle == 0)
                    {
                        string tagPart = s.Substring(i + 1);
                        s = s.Substring(0, i);
                        foreach (var t in tagPart.Split('#'))
                            if (t.Trim().Length > 0) tags.Add(t.Trim());
                        return;
                    }
                }
            }

            /// <summary>Removes a trailing <c>-> target</c> and returns the target (or null).</summary>
            string SplitDivert(ref string s)
            {
                int depth = 0;
                for (int i = 0; i < s.Length - 1; i++)
                {
                    char ch = s[i];
                    if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                    else if (ch == '-' && s[i + 1] == '>' && depth == 0)
                    {
                        string target = s.Substring(i + 2).Trim();
                        s = s.Substring(0, i);
                        if (target.Length == 0 || target.IndexOf(' ') >= 0) Fail($"bad divert '-> {target}'");
                        return target;
                    }
                }
                return null;
            }
        }

        internal static int MatchingBrace(string s, int open)
        {
            int depth = 0;
            bool inString = false;
            for (int i = open; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '"') inString = !inString;
                if (inString) continue;
                if (ch == '{') depth++;
                else if (ch == '}' && --depth == 0) return i;
            }
            return -1;
        }

        internal static int TopLevelIndex(string s, char target)
        {
            int depth = 0, paren = 0;
            bool inString = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '"') inString = !inString;
                if (inString) continue;
                if (ch == '{') depth++;
                else if (ch == '}') depth--;
                else if (ch == '(') paren++;
                else if (ch == ')') paren--;
                else if (ch == target && depth == 0 && paren == 0) return i;
            }
            return -1;
        }
    }

    // ---------------------------------------------------------------------------------------------- runner

    public enum StepKind { Line, Choices, End }

    public sealed class ShownChoice
    {
        public int Index;
        public string Text;
        public List<string> Tags;
        internal InkChoice Source;
        public string Id => Source.Id;
    }

    public sealed class StoryStep
    {
        public StepKind Kind;
        public string Speaker;
        public string Text;
        public List<string> Tags = new List<string>();
        public List<ShownChoice> Choices = new List<ShownChoice>();

        public static readonly StoryStep EndStep = new StoryStep { Kind = StepKind.End };
    }

    /// <summary>
    /// Plays a story one step at a time: <see cref="Next"/> returns a line, a set of choices or the end.
    /// Once-only choices are remembered in host variables named <c>__chosen:knot#n</c>, so they survive saves.
    /// </summary>
    public sealed class InkRunner
    {
        readonly InkStory story;
        readonly IStoryHost host;
        readonly Stack<(List<InkNode> nodes, int index)> frames = new Stack<(List<InkNode>, int)>();
        readonly Random random;
        List<ShownChoice> pending;

        public string CurrentKnot { get; private set; }
        public bool Finished => frames.Count == 0 && pending == null;

        public InkRunner(InkStory story, IStoryHost host, int seed = 0)
        {
            this.story = story;
            this.host = host;
            random = seed == 0 ? new Random() : new Random(seed);
        }

        public static string ChosenVar(string choiceId) => "__chosen:" + choiceId;

        public void Start(string knot)
        {
            frames.Clear();
            pending = null;
            Divert(knot);
        }

        void Divert(string knot)
        {
            frames.Clear();
            if (knot == "END" || knot == "DONE") return;
            if (!story.Knots.TryGetValue(knot, out var nodes)) throw new InvalidOperationException($"Unknown knot '{knot}'");
            CurrentKnot = knot;
            frames.Push((nodes, 0));
        }

        public StoryStep Next()
        {
            if (pending != null) throw new InvalidOperationException("Choose() must be called before Next() when choices are shown");
            for (int guard = 0; guard < 10000; guard++)
            {
                if (frames.Count == 0) return StoryStep.EndStep;
                var (nodes, index) = frames.Pop();
                if (index >= nodes.Count) continue;
                frames.Push((nodes, index + 1));
                var step = Execute(nodes[index]);
                if (step != null) return step;
            }
            throw new InvalidOperationException($"Story seems stuck in a loop in knot '{CurrentKnot}'");
        }

        StoryStep Execute(InkNode node)
        {
            switch (node)
            {
                case TextNode t:
                    return new StoryStep { Kind = StepKind.Line, Speaker = t.Speaker, Text = Interpolate(t.Text), Tags = t.Tags };
                case DivertNode d:
                    Divert(d.Target);
                    return frames.Count == 0 ? StoryStep.EndStep : null;
                case CodeNode c:
                    RunCode(c.Code);
                    return null;
                case BranchNode b:
                    var part = Eval(b.Condition) != 0 ? b.IfTrue : b.IfFalse;
                    if (part.Count > 0) frames.Push((part, 0));
                    return null;
                case ChoiceGroupNode g:
                    var shown = new List<ShownChoice>();
                    foreach (var c in g.Choices)
                    {
                        if (!c.Sticky && host.GetVar(ChosenVar(c.Id)) != 0) continue;
                        if (c.Condition != null && Eval(c.Condition) == 0) continue;
                        shown.Add(new ShownChoice { Index = shown.Count, Text = Interpolate(c.Text), Tags = c.Tags, Source = c });
                    }
                    if (shown.Count == 0) return null;   // no choice left: fall through to the gather
                    pending = shown;
                    return new StoryStep { Kind = StepKind.Choices, Choices = shown };
            }
            return null;
        }

        public void Choose(int index)
        {
            if (pending == null || index < 0 || index >= pending.Count) throw new InvalidOperationException("No such choice");
            var c = pending[index].Source;
            pending = null;
            if (!c.Sticky) host.SetVar(ChosenVar(c.Id), 1);
            frames.Push((c.Body, 0));
        }

        // ------------------------------------------------------------------ code & text

        void RunCode(string code)
        {
            code = code.Trim();
            if (code.StartsWith("temp ", StringComparison.Ordinal)) code = code.Substring(5).Trim();
            int i = 0;
            while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
            string name = code.Substring(0, i);
            string rest = code.Substring(i).Trim();
            if (rest.StartsWith("(", StringComparison.Ordinal)) { Eval(code); return; }
            if (rest == "++") { host.SetVar(name, host.GetVar(name) + 1); return; }
            if (rest == "--") { host.SetVar(name, host.GetVar(name) - 1); return; }
            if (rest.StartsWith("+=", StringComparison.Ordinal)) { host.SetVar(name, host.GetVar(name) + Eval(rest.Substring(2))); return; }
            if (rest.StartsWith("-=", StringComparison.Ordinal)) { host.SetVar(name, host.GetVar(name) - Eval(rest.Substring(2))); return; }
            if (rest.StartsWith("=", StringComparison.Ordinal)) { host.SetVar(name, Eval(rest.Substring(1))); return; }
            throw new InvalidOperationException($"Cannot run '~ {code}'");
        }

        string Interpolate(string text)
        {
            if (text.IndexOf('{') < 0) return text;
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch != '{') { sb.Append(ch); i++; continue; }
                int close = InkStory.MatchingBrace(text, i);
                if (close < 0) { sb.Append(text.Substring(i)); break; }
                string inner = text.Substring(i + 1, close - i - 1);
                sb.Append(InlineValue(inner));
                i = close + 1;
            }
            return sb.ToString();
        }

        string InlineValue(string inner)
        {
            if (inner.StartsWith("~", StringComparison.Ordinal))
            {
                var options = SplitTopLevel(inner.Substring(1), '|');
                return Interpolate(options[random.Next(options.Count)]);
            }
            int colon = InkStory.TopLevelIndex(inner, ':');
            if (colon > 0)
            {
                string body = inner.Substring(colon + 1);
                var parts = SplitTopLevel(body, '|');
                bool ok = Eval(inner.Substring(0, colon)) != 0;
                return Interpolate((ok ? parts[0] : (parts.Count > 1 ? parts[1] : "")).Trim());
            }
            string trimmed = inner.Trim();
            if (IsIdentifier(trimmed))
            {
                var t = host.TextOf(trimmed);
                if (t != null) return t;
            }
            return Eval(trimmed).ToString(CultureInfo.InvariantCulture);
        }

        static List<string> SplitTopLevel(string s, char sep)
        {
            var list = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}') depth--;
                else if (s[i] == sep && depth == 0) { list.Add(s.Substring(start, i - start)); start = i + 1; }
            }
            list.Add(s.Substring(start));
            return list;
        }

        static bool IsIdentifier(string s)
        {
            if (s.Length == 0 || !(char.IsLetter(s[0]) || s[0] == '_')) return false;
            foreach (char ch in s) if (!(char.IsLetterOrDigit(ch) || ch == '_')) return false;
            return true;
        }

        public int Eval(string expr) => new ExprParser(expr, host).Parse();

        // ------------------------------------------------------------------ expressions

        sealed class ExprParser
        {
            readonly string s;
            readonly IStoryHost host;
            int p;

            public ExprParser(string s, IStoryHost host) { this.s = s; this.host = host; }

            public int Parse()
            {
                int v = Or();
                Skip();
                if (p < s.Length) throw new InvalidOperationException($"Unexpected '{s.Substring(p)}' in expression '{s}'");
                return v;
            }

            void Skip() { while (p < s.Length && char.IsWhiteSpace(s[p])) p++; }

            bool Eat(string token, bool word = false)
            {
                Skip();
                if (string.CompareOrdinal(s, p, token, 0, token.Length) != 0) return false;
                if (word && p + token.Length < s.Length && (char.IsLetterOrDigit(s[p + token.Length]) || s[p + token.Length] == '_')) return false;
                p += token.Length;
                return true;
            }

            int Or()
            {
                int v = And();
                while (Eat("||") || Eat("or", true)) { int r = And(); v = (v != 0 || r != 0) ? 1 : 0; }
                return v;
            }

            int And()
            {
                int v = Not();
                while (Eat("&&") || Eat("and", true)) { int r = Not(); v = (v != 0 && r != 0) ? 1 : 0; }
                return v;
            }

            int Not()
            {
                Skip();
                if (Eat("not", true)) return Not() == 0 ? 1 : 0;
                if (p < s.Length && s[p] == '!' && (p + 1 >= s.Length || s[p + 1] != '=')) { p++; return Not() == 0 ? 1 : 0; }
                return Compare();
            }

            int Compare()
            {
                int a = Sum();
                if (Eat(">=")) return a >= Sum() ? 1 : 0;
                if (Eat("<=")) return a <= Sum() ? 1 : 0;
                if (Eat("==")) return a == Sum() ? 1 : 0;
                if (Eat("!=")) return a != Sum() ? 1 : 0;
                if (Eat(">")) return a > Sum() ? 1 : 0;
                if (Eat("<")) return a < Sum() ? 1 : 0;
                return a;
            }

            int Sum()
            {
                int v = Product();
                while (true)
                {
                    if (Eat("+")) v += Product();
                    else if (Eat("-") ) v -= Product();
                    else return v;
                }
            }

            int Product()
            {
                int v = Atom();
                while (true)
                {
                    if (Eat("*")) v *= Atom();
                    else if (Eat("/")) { int d = Atom(); v = d == 0 ? 0 : v / d; }
                    else return v;
                }
            }

            int Atom()
            {
                Skip();
                if (p >= s.Length) throw new InvalidOperationException($"Incomplete expression '{s}'");
                char ch = s[p];
                if (ch == '(') { p++; int v = Or(); if (!Eat(")")) throw new InvalidOperationException($"Missing ) in '{s}'"); return v; }
                if (ch == '-') { p++; return -Atom(); }
                if (char.IsDigit(ch))
                {
                    int start = p;
                    while (p < s.Length && char.IsDigit(s[p])) p++;
                    return int.Parse(s.Substring(start, p - start), CultureInfo.InvariantCulture);
                }
                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = p;
                    while (p < s.Length && (char.IsLetterOrDigit(s[p]) || s[p] == '_')) p++;
                    string name = s.Substring(start, p - start);
                    if (name == "true") return 1;
                    if (name == "false") return 0;
                    Skip();
                    if (p < s.Length && s[p] == '(')
                    {
                        p++;
                        var args = new List<string>();
                        Skip();
                        if (p < s.Length && s[p] == ')') p++;
                        else
                        {
                            while (true)
                            {
                                Skip();
                                if (p < s.Length && s[p] == '"')
                                {
                                    int end = s.IndexOf('"', p + 1);
                                    if (end < 0) throw new InvalidOperationException($"Unclosed string in '{s}'");
                                    args.Add(s.Substring(p + 1, end - p - 1));
                                    p = end + 1;
                                }
                                else args.Add(Or().ToString(CultureInfo.InvariantCulture));
                                if (Eat(",")) continue;
                                if (Eat(")")) break;
                                throw new InvalidOperationException($"Bad arguments in '{s}'");
                            }
                        }
                        return host.Call(name, args.ToArray());
                    }
                    return host.GetVar(name);
                }
                throw new InvalidOperationException($"Unexpected '{ch}' in expression '{s}'");
            }
        }
    }
}
