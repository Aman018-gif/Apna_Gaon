using System;
using System.Collections.Generic;
using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Loads the story: Resources/Stories/mohalla.txt plus every file it INCLUDEs (Unity only imports .txt as text,
    /// so the Ink files use that extension). Parsed once and cached.
    /// </summary>
    public static class StoryLibrary
    {
        public const string MainFile = "mohalla.txt";
        static InkStory cached;

        public static InkStory Story => cached ?? (cached = Build(ReadResource));

        public static void ClearCache() => cached = null;

        static string ReadResource(string file)
        {
            string name = file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? file.Substring(0, file.Length - 4) : file;
            var asset = Resources.Load<TextAsset>("Stories/" + name);
            return asset != null ? asset.text : null;
        }

        /// <summary>Builds the story from the main file and its includes using any file reader (tests read from disk).</summary>
        public static InkStory Build(Func<string, string> read)
        {
            var story = new InkStory();
            var queue = new Queue<string>();
            var seen = new HashSet<string>();
            queue.Enqueue(MainFile);
            seen.Add(MainFile);
            while (queue.Count > 0)
            {
                string file = queue.Dequeue();
                string text = read(file);
                if (text == null) throw new InvalidOperationException("Story file not found: " + file);
                int before = story.Includes.Count;
                story.Add(file, text);
                for (int i = before; i < story.Includes.Count; i++)
                    if (seen.Add(story.Includes[i])) queue.Enqueue(story.Includes[i]);
            }
            return story;
        }
    }
}
