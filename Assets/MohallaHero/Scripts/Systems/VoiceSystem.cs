using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MohallaHero
{
    public class VoiceProfile
    {
        public string macVoice;   // macOS `say` voice name
        public int rate;          // words per minute
        public bool female;       // used to pick a Windows voice
        public VoiceProfile(string macVoice, int rate, bool female) { this.macVoice = macVoice; this.rate = rate; this.female = female; }
    }

    /// <summary>
    /// Speaks dialogue aloud with the operating system's text-to-speech engine:
    /// macOS `say` (Indian English voices Rishi / Aman / Tara, Hindi voice Lekha, which also reads Hinglish well) or Windows System.Speech.
    /// Lines are queued so the player's reply and the villager's answer play one after the other.
    /// On other platforms speech is silently skipped.
    /// </summary>
    public class VoiceSystem : MonoBehaviour
    {
        const string PrefKey = "mohallahero_voices";

        readonly Queue<(VoiceProfile profile, string text)> queue = new Queue<(VoiceProfile, string)>();
        Process current;
        VoiceProfile currentProfile;
        string currentText;
        bool currentUsedNamedVoice;

        /// <summary>Runtime mute that is not saved (used by automated tests).</summary>
        public static bool Muted;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static bool IsMac => Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
        static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
        public static bool Supported => IsMac || IsWindows;

        // ------------------------------------------------------------------ voices

        public static readonly VoiceProfile Narrator = new VoiceProfile("Rishi", 168, false);
        public static readonly VoiceProfile Loudspeaker = new VoiceProfile("Aman", 205, false);
        public static readonly VoiceProfile Stranger = new VoiceProfile("Rishi", 180, false);
        public static readonly VoiceProfile StrangerFemale = new VoiceProfile("Lekha", 175, true);

        public static VoiceProfile ForPlayer(Gender g) =>
            g == Gender.Female ? new VoiceProfile("Tara", 185, true) : new VoiceProfile("Aman", 180, false);

        public static VoiceProfile ForNpc(NpcDef d) => new VoiceProfile(d.macVoice, d.voiceRate, d.female);

        /// <summary>True while a line is playing or queued.</summary>
        public bool Busy => current != null || queue.Count > 0;

        // ------------------------------------------------------------------ API

        /// <summary>Speak a line. By default interrupts whatever is playing; pass interrupt=false to queue after it.</summary>
        public void Say(VoiceProfile profile, string text, bool interrupt = true)
        {
            if (interrupt) Stop();
            if (!Enabled || Muted || !Supported || profile == null) return;
            text = Clean(text);
            if (string.IsNullOrWhiteSpace(text)) return;
            queue.Enqueue((profile, text));
            if (current == null) StartNext();
        }

        public void Stop()
        {
            queue.Clear();
            KillCurrent();
        }

        void Update()
        {
            if (current == null) { if (queue.Count > 0) StartNext(); return; }
            bool exited;
            try { exited = current.HasExited; } catch { exited = true; }
            if (!exited) return;

            int code = 0;
            try { code = current.ExitCode; } catch { }
            current.Dispose();
            current = null;

            // The named voice isn't installed on this machine → retry the same line with the default voice.
            if (code != 0 && currentUsedNamedVoice && IsMac)
            {
                Launch(currentProfile, currentText, false);
                return;
            }
            if (queue.Count > 0) StartNext();
        }

        void StartNext()
        {
            if (queue.Count == 0) return;
            var (profile, text) = queue.Dequeue();
            Launch(profile, text, true);
        }

        void Launch(VoiceProfile profile, string text, bool useNamedVoice)
        {
            currentProfile = profile;
            currentText = text;
            currentUsedNamedVoice = useNamedVoice;
            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                };
                if (IsMac)
                {
                    psi.FileName = "/usr/bin/say";
                    psi.Arguments = useNamedVoice ? $"-v \"{profile.macVoice}\" -r {profile.rate}" : $"-r {profile.rate}";
                }
                else
                {
                    int winRate = Mathf.Clamp((profile.rate - 175) / 15, -10, 10);
                    string gender = profile.female ? "Female" : "Male";
                    psi.FileName = "powershell";
                    psi.Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"Add-Type -AssemblyName System.Speech; " +
                                    "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                                    $"try {{ $s.SelectVoiceByHints([System.Speech.Synthesis.VoiceGender]::{gender}) }} catch {{}}; " +
                                    $"$s.Rate = {winRate}; $s.Speak([Console]::In.ReadToEnd())\"";
                }
                current = Process.Start(psi);
                if (current == null) return;
                // Text goes through stdin, so no shell quoting problems with any dialogue line.
                current.StandardInput.Write(text);
                current.StandardInput.Close();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Mohalla Hero: text-to-speech unavailable: " + e.Message);
                current = null;
                queue.Clear();
            }
        }

        void KillCurrent()
        {
            if (current == null) return;
            try { if (!current.HasExited) current.Kill(); } catch { }
            try { current.Dispose(); } catch { }
            current = null;
        }

        void OnDestroy() => Stop();
        void OnApplicationQuit() => Stop();

        // ------------------------------------------------------------------ text cleanup

        static readonly Regex Tags = new Regex("<[^>]+>");
        static readonly Regex Rupees = new Regex("₹\\s?([0-9][0-9,]*)");

        /// <summary>Removes rich-text tags and symbols so the speech engine reads naturally.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = Tags.Replace(text, "");
            text = Rupees.Replace(text, m => m.Groups[1].Value.Replace(",", "") + " rupees");
            text = text.Replace("•", ",").Replace("—", ", ").Replace("·", ",").Replace("\n", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();
            return text.Length > 600 ? text.Substring(0, 600) : text;
        }
    }
}
