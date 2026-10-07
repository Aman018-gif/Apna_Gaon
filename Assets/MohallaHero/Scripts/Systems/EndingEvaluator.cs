using UnityEngine;

namespace MohallaHero
{
    public struct RatingInput
    {
        public int civic, green, courage, honesty;   // karma XP
        public int supporters;                       // residents who joined the march (0..6)
        public int votersInformed;                   // doors knocked with honest information (0..10)
        public int violence;                         // acts of violence/vandalism chosen

        public static RatingInput From(KarmaSystem k, int supporters, int voters, int violence) => new RatingInput
        {
            civic = k.Xp(KarmaStat.Civic), green = k.Xp(KarmaStat.Green), courage = k.Xp(KarmaStat.Courage), honesty = k.Xp(KarmaStat.Honesty),
            supporters = supporters, votersInformed = voters, violence = violence
        };
    }

    /// <summary>
    /// The Mohalla Rating (0–100, shown as 1–5 stars) and the endings. Pure functions, unit-tested.
    /// Rating = 70 × average karma share (each stat's XP / its cap) + 10 × supporter share + 20 × informed-voter share
    /// − 25 per act of violence.
    /// </summary>
    public static class EndingEvaluator
    {
        public static int Rating(RatingInput r)
        {
            float karma = (Share(r.civic, KarmaStat.Civic) + Share(r.green, KarmaStat.Green) + Share(r.courage, KarmaStat.Courage) + Share(r.honesty, KarmaStat.Honesty)) / 4f;
            float supporters = Mathf.Clamp01(r.supporters / (float)TrustSystem.Convincible.Length);
            float voters = Mathf.Clamp01(r.votersInformed / (float)Balance.VoterDoors);
            float rating = karma * Balance.RatingKarmaWeight + supporters * Balance.RatingSupportersWeight + voters * Balance.RatingVotersWeight
                           - r.violence * Balance.ViolencePenalty;
            return Mathf.Clamp(Mathf.RoundToInt(rating), 0, 100);
        }

        static float Share(int xp, KarmaStat s) => Mathf.Clamp01(xp / (float)Balance.RatingCap(s));

        /// <summary>1 star below 20, 5 stars from 80.</summary>
        public static int Stars(int rating) => Mathf.Clamp(1 + rating / 20, 1, 5);

        /// <summary>
        /// Who wins the ward election. The player's own (secret) vote is one vote among thousands: what changes the result
        /// is how many neighbours they informed and how the mohalla changed overall.
        /// </summary>
        public static Candidate Winner(int rating, int votersInformed)
        {
            if (votersInformed >= Balance.VotersToInform && rating >= 55) return Candidate.Meera;
            if (votersInformed >= 3 || rating >= 45) return Candidate.Vaada;
            return Candidate.Jugaad;
        }

        public static Ending Evaluate(int rating, int votersInformed)
        {
            var w = Winner(rating, votersInformed);
            if (w == Candidate.Meera) return rating >= 75 ? Ending.MohallaHero : Ending.JagrukNagrik;
            if (w == Candidate.Vaada) return Ending.AadhaSach;
            return Ending.JugaadRaj;
        }

        public static string Title(Ending e)
        {
            switch (e)
            {
                case Ending.MohallaHero: return "Mohalla Hero!";
                case Ending.JagrukNagrik: return "Jagruk Nagrik (Aware Citizen)";
                case Ending.AadhaSach: return "Aadha Sach (Half the Way)";
                case Ending.JugaadRaj: return "Jugaad Raj Returns";
                default: return "";
            }
        }

        public static string Description(Ending e)
        {
            switch (e)
            {
                case Ending.MohallaHero:
                    return "Meera Didi wins by a landslide! The park gets real benches (not golden dustbins), street lights work, and the RTI " +
                           "reply is framed in the Ward Office. Pintu makes you the hero of his most viral meme. Shanti Nagar is bright, green and proud.";
                case Ending.JagrukNagrik:
                    return "Meera Didi wins, just! The mohalla is waking up: the park is being fixed and people check facts before forwarding. " +
                           "There is still work to do, but now everyone knows they can do it together.";
                case Ending.AadhaSach:
                    return "Dr. Vaada Verma wins. He promises to fix the park... and also promises the opposite on Tuesdays. " +
                           "Netaji is gone, but the mohalla's problems aren't. Next time, more informed voters could change the result.";
                case Ending.JugaadRaj:
                    return "Netaji Jugaad Singh wins again and announces free WiFi on Mars. The park stays a garbage dump. " +
                           "But it's not over: every election is a new chance, and the choices you make every day still count.";
                default: return "";
            }
        }

        public static string CandidateName(Candidate c)
        {
            switch (c)
            {
                case Candidate.Jugaad: return "Netaji Jugaad Singh";
                case Candidate.Vaada: return "Dr. Vaada Verma";
                case Candidate.Meera: return "Meera Didi (Independent)";
                default: return "NOTA (None of the Above)";
            }
        }

        public static string Symbol(Candidate c)
        {
            switch (c)
            {
                case Candidate.Jugaad: return "Rocket";
                case Candidate.Vaada: return "See-saw";
                case Candidate.Meera: return "Lantern";
                default: return "Cross";
            }
        }
    }
}
