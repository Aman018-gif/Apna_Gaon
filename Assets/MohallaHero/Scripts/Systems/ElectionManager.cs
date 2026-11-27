using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Chapter 6, the Election Mela finale: the loudspeaker rickshaw, the polling booth (EVM + ink on the finger),
    /// the count and the ending. The result depends on the Mohalla Rating and how many voters were informed;
    /// the player's own vote is secret and never judged.
    /// </summary>
    public class ElectionManager : MonoBehaviour
    {
        GameManager gm => GameManager.I;

        public Candidate MyVote { get; private set; } = (Candidate)(-1);
        public Candidate Winner { get; private set; }
        public Ending Ending { get; private set; }
        public int FinalRating { get; private set; }

        public void Init(Transform worldRoot) => LoudspeakerRickshaw.Spawn(worldRoot);

        public void OpenBooth()
        {
            gm.Player.Frozen = true;
            gm.UI.Open(gm.UI.Voting);
        }

        /// <summary>Called by the voting screen once the ink animation has finished.</summary>
        public void CastVote(Candidate c)
        {
            MyVote = c;
            gm.Story.Set("m6_done", 1);
            FinalRating = gm.Rating;
            int voters = gm.Story.Get("voters_informed");
            Winner = EndingEvaluator.Winner(FinalRating, voters);
            Ending = EndingEvaluator.Evaluate(FinalRating, voters);
            if (Ending == Ending.MohallaHero || Ending == Ending.JagrukNagrik) gm.Story.Set("park_fixed", 1);
            gm.Player.Frozen = false;
            gm.FinishGame();
            gm.UI.Fade("Votes ki ginti ho rahi hai...", null, () => gm.UI.Open(gm.UI.Ending));
        }

        public void Restore(SaveData d)
        {
            if (!d.gameOver) return;
            MyVote = (Candidate)d.myVote;
            Winner = (Candidate)d.winner;
            Ending = (Ending)d.ending;
            FinalRating = d.finalRating;
        }
    }
}
