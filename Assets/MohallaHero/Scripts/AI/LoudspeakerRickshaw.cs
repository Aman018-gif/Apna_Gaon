using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Raju's auto with a loudspeaker on the roof, looping along the Main Road. It blares party slogans before the
    /// election and voting reminders during the Mela; lines appear as subtitles (and speech when the player is near).
    /// </summary>
    public class LoudspeakerRickshaw : MonoBehaviour
    {
        SpriteRenderer sr, bubble;
        int target = 1;
        float announceTimer = 4f, wheelTimer;
        int frame;

        static readonly string[] BeforeElection =
        {
            "Netaji Jugaad Singh zindabad! Chaand pe free WiFi, har ghar mein helipad!",
            "Netaji ka vaada: har gaddha ab \"speed breaker\" kahlayega! Safety first!",
            "Dr. Vaada Verma: kal tak park, aaj se parking! Ya park? Aapki marzi!",
            "Shanti Nagar ke bhaiyon-behno! Kachra sadak pe nahi, dabbe mein daaliye. Gila-sukha alag! (Sponsored by Meera Didi's clinic)",
        };

        static readonly string[] DuringElection =
        {
            "Ravivaar ko vote dena na bhoolein! Booth: Mela Maidan. Apna photo ID saath laayein.",
            "Rishwat lena aur dena dono jurm hain! Koi gift de toh Election Officer ko bataiye.",
            "Aapka vote, aapki awaaz! Jaankari lijiye, phir button dabaiye.",
            "NOTA bhi aapka haq hai: None of the Above. Par vote zaroor dijiye!",
        };

        public static LoudspeakerRickshaw Spawn(Transform parent)
        {
            var r = WorldObjects.Create<LoudspeakerRickshaw>("LoudspeakerRickshaw", WorldLayout.CellFeet(WorldLayout.RickshawLoop[0]), parent);
            r.sr = WorldObjects.CreateSprite("Body", SpriteFactory.Rickshaw(0), r.transform.position, r.transform);
            r.bubble = WorldObjects.CreateSprite("Bubble", SpriteFactory.Bubble("talk"), r.transform.position, r.transform, 30);
            r.bubble.transform.localPosition = new Vector3(0, 2.1f, 0);
            r.bubble.enabled = false;
            return r;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.InWorld || gm.IsModalOpen) return;
            Vector2 goal = WorldLayout.CellCenter(WorldLayout.RickshawLoop[target]);
            Vector2 pos = transform.position;
            Vector2 delta = goal - pos;
            float step = 3.2f * Time.deltaTime;
            if (delta.magnitude <= step) { transform.position = goal; target = (target + 1) % WorldLayout.RickshawLoop.Length; }
            else transform.position = pos + delta.normalized * step;
            if (Mathf.Abs(delta.x) > 0.01f) sr.flipX = delta.x < 0;

            wheelTimer += Time.deltaTime;
            if (wheelTimer > 0.15f) { wheelTimer = 0f; frame ^= 1; sr.sprite = SpriteFactory.Rickshaw(frame); }

            // hide at night
            bool night = gm.Clock.HourFloat >= 21f;
            sr.enabled = !night;
            if (night) { bubble.enabled = false; return; }

            announceTimer -= Time.deltaTime;
            bubble.enabled = announceTimer > 13f;
            if (announceTimer > 0f) return;
            announceTimer = 18f;
            var lines = gm.Areas.IsUnlocked(AreaId.Maidan) ? DuringElection : BeforeElection;
            string line = lines[gm.Rng.Next(lines.Length)];
            float dist = Vector2.Distance(gm.Player.transform.position, transform.position);
            if (dist < 14f)
            {
                gm.UI.Subtitle("Loudspeaker", line);
                if (dist < 9f && !gm.Voice.Busy) gm.Voice.Say(VoiceSystem.Loudspeaker, line);
            }
        }
    }
}
