using BadLie.Course;
using BadLie.Sim;
using UnityEngine;

namespace BadLie.Game
{
    /// <summary>
    /// Sound effects, ambience and music with separate volume controls. Ball events from the
    /// shot playback map to impact sounds scaled by their strength.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        static AudioDirector inst;
        GameConfig cfg;
        AudioSource[] pool;
        int next;
        AudioSource music, ambience;
        float musicTarget = 1f, musicLevel;

        public static void Init(GameConfig cfg, Transform parent)
        {
            if (inst != null) return;
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            inst = go.AddComponent<AudioDirector>();
            inst.cfg = cfg;
            inst.pool = new AudioSource[12];
            for (int i = 0; i < inst.pool.Length; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                inst.pool[i] = s;
            }
            inst.music = go.AddComponent<AudioSource>();
            inst.music.loop = true;
            inst.music.playOnAwake = false;
            inst.ambience = go.AddComponent<AudioSource>();
            inst.ambience.loop = true;
            inst.ambience.playOnAwake = false;
            if (cfg.Music != null) { inst.music.clip = cfg.Music; inst.music.Play(); }
            if (cfg.Ambience != null) { inst.ambience.clip = cfg.Ambience; inst.ambience.Play(); }
            Settings.Changed += inst.ApplyVolumes;
            inst.ApplyVolumes();
        }

        void OnDestroy()
        {
            Settings.Changed -= ApplyVolumes;
            if (inst == this) inst = null;
        }

        void ApplyVolumes()
        {
            if (ambience != null) ambience.volume = 0.55f * Settings.SoundVolume;
        }

        void Update()
        {
            musicLevel = Mathf.MoveTowards(musicLevel, musicTarget, Time.unscaledDeltaTime * 0.5f);
            if (music != null) music.volume = 0.42f * Settings.MusicVolume * musicLevel;
        }

        /// <summary>Duck the music (0..1), e.g. under cards.</summary>
        public static void MusicLevel(float target)
        {
            if (inst != null) inst.musicTarget = target;
        }

        public static void Play(AudioClip c, float volume = 1f, float pitch = 1f)
        {
            if (inst == null || c == null) return;
            var s = inst.pool[inst.next];
            inst.next = (inst.next + 1) % inst.pool.Length;
            s.pitch = pitch;
            s.PlayOneShot(c, Mathf.Clamp01(volume) * Settings.SoundVolume);
        }

        static AudioClip Pick(AudioClip[] clips, float t)
        {
            if (clips == null || clips.Length == 0) return null;
            int i = Mathf.Clamp(Mathf.FloorToInt(t * clips.Length), 0, clips.Length - 1);
            return clips[i];
        }

        public static void UiTap() { if (inst != null) Play(inst.cfg.UiTap, 0.5f); }
        public static void UiSelect() { if (inst != null) Play(inst.cfg.UiSelect, 0.7f); }
        public static void Penalty() { if (inst != null) Play(inst.cfg.Penalty, 0.7f); }
        public static void HoleComplete() { if (inst != null) Play(inst.cfg.HoleComplete, 0.8f); }
        public static void RunWon() { if (inst != null) Play(inst.cfg.RunWon, 0.9f); }
        public static void RunLost() { if (inst != null) Play(inst.cfg.RunLost, 0.85f); }

        public static void Strike(float power)
        {
            if (inst == null) return;
            Play(Pick(inst.cfg.Strike, power), 0.45f + 0.55f * power, 1.08f - 0.12f * power);
        }

        public static void BallEvent(SimEvent e)
        {
            if (inst == null) return;
            var c = inst.cfg;
            float s = Mathf.Clamp01(e.Strength / 8f);
            switch (e.Type)
            {
                case SimEventType.Wall:
                case SimEventType.BankWall:
                    if (e.Material == WallMaterial.Hedge) Play(c.WallHedge, 0.3f + 0.6f * s, 1f);
                    else Play(Pick(c.WallStone, Random.value), 0.25f + 0.75f * s, e.Type == SimEventType.BankWall ? 1.18f : 0.95f + 0.1f * s);
                    if (e.Type == SimEventType.BankWall) Play(c.Magnet, 0.35f, 1.6f);
                    break;
                case SimEventType.Land:
                case SimEventType.Bounce:
                    if (e.Surface == SurfaceType.Sand) Play(c.LandSand, 0.3f + 0.6f * s);
                    else if (e.Surface == SurfaceType.Stone) Play(Pick(c.WallStone, 0.2f), 0.2f + 0.5f * s, 1.25f);
                    else Play(c.LandGrass, 0.25f + 0.6f * s);
                    break;
                case SimEventType.Skip: Play(c.Skip, 0.9f); break;
                case SimEventType.Splash: Play(c.Splash, 0.85f); break;
                case SimEventType.Fell: Play(c.Splash, 0.5f, 0.75f); break;
                case SimEventType.LipOut: Play(c.LipOut, 0.8f); break;
                case SimEventType.Holed: Play(c.CupDrop, 1f); break;
                case SimEventType.MagnetPull: Play(c.Magnet, 0.55f); break;
            }
        }
    }
}
