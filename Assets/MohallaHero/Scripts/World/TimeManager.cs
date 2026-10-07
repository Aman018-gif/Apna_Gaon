using UnityEngine;

namespace MohallaHero
{
    /// <summary>
    /// Game clock: a day runs 07:00 → 23:00 in about ten real minutes. Drives NPC schedules, street lights and the
    /// RTI reply (which arrives the next day). Time skips forward only by resting at home or story events.
    /// The clock stops while any modal screen is open.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public int Day { get; private set; } = 1;
        public float Minute { get; private set; } = Balance.DayStartMinute;
        public float HourFloat => Minute / 60f;
        public bool Running = true;
        int lastHour = -1;

        public DayPhase Phase
        {
            get
            {
                float h = HourFloat;
                if (h < 12f) return DayPhase.Morning;
                if (h < 17f) return DayPhase.Afternoon;
                if (h < 20f) return DayPhase.Evening;
                return DayPhase.Night;
            }
        }

        public bool IsDark => HourFloat >= 19f;

        public void Set(int day, float minute)
        {
            Day = Mathf.Max(1, day);
            Minute = Mathf.Clamp(minute, Balance.DayStartMinute, Balance.DayEndMinute - 1);
            lastHour = Mathf.FloorToInt(HourFloat);
        }

        /// <summary>Jumps forward to an hour later today (never backwards).</summary>
        public void SkipTo(float hour)
        {
            Minute = Mathf.Clamp(Mathf.Max(Minute, hour * 60f), Balance.DayStartMinute, Balance.DayEndMinute - 1);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!Running || gm == null || !gm.InWorld || gm.IsModalOpen) return;

            Minute += Time.deltaTime / Balance.RealSecondsPerGameMinute;
            int h = Mathf.FloorToInt(HourFloat);
            if (h != lastHour)
            {
                lastHour = h;
                GameEvents.RaiseHourChanged(h);
            }
            if (Minute >= Balance.DayEndMinute)
            {
                Minute = Balance.DayEndMinute - 1;
                gm.EndOfDay();
            }
        }

        public void BeginNextDay()
        {
            Day++;
            Minute = Balance.DayStartMinute;
            lastHour = Mathf.FloorToInt(HourFloat);
        }
    }
}
