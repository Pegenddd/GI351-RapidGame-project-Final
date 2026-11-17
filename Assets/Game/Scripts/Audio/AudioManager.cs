using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>ช่องเสียง 4 ช่องตาม GDD 12.1 (ปรับได้ในหน้า Pause)</summary>
    public enum AudioChannel { Master, Music, SFX, UI }

    /// <summary>
    /// ระบบเสียงกลางของเกม (GDD หัวข้อ 12)
    /// - โหลดไฟล์จาก Resources/Audio/SFX และ Resources/Audio/Music ตามชื่อ ID เช่น "SFX_UI_Click"
    ///   ถ้ามีหลายเวอร์ชันให้ตั้งชื่อ ID_01, ID_02, ID_03 ระบบจะสุ่มเล่นเอง
    ///   => เปลี่ยนเสียงทีหลัง = เอาไฟล์ใหม่ชื่อเดิมไปทับได้เลย ไม่ต้องแก้โค้ด
    /// - สุ่ม Pitch ±8%, เสียงเดียวกันซ้อนได้ไม่เกิน 4, เสียงในโลกเบาลงตามระยะจากกล้อง
    /// - เพลงเปลี่ยนตามสถานะเกมอัตโนมัติ (พัก / เวฟ 1-4 / 5-8 / 9-12 / ชนะ / แพ้) Crossfade 1.5 วิ
    /// - ถ้าไม่มีในซีน จะสร้างตัวเองอัตโนมัติตอนเริ่มเล่น
    /// เรียกใช้จากโค้ดอื่นผ่าน Sfx.Play / Sfx.At / Sfx.UI
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Volume เริ่มต้น (ค่าที่ผู้เล่นปรับจะถูกจำไว้ใน PlayerPrefs)")]
        [Range(0f, 1f)] [SerializeField] private float defaultMaster = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float defaultMusic = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float defaultSfx = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float defaultUi = 0.7f;

        [Header("โฟลเดอร์ใน Resources")]
        [SerializeField] private string sfxFolder = "Audio/SFX";
        [SerializeField] private string musicFolder = "Audio/Music";

        [Header("กฎการเล่นเสียง (GDD 12.1)")]
        [Tooltip("จำนวนเสียงที่เล่นพร้อมกันได้ทั้งหมด")]
        [SerializeField] private int voiceCount = 40;
        [Tooltip("เสียงเดียวกันเล่นพร้อมกันได้ไม่เกิน")]
        [SerializeField] private int maxSameSound = 4;
        [Tooltip("เสียงเดียวกันต้องห่างกันอย่างน้อย (วินาที) กันเสียงซ้อนเฟรมเดียวกัน")]
        [SerializeField] private float sameSoundMinGap = 0.035f;
        [Tooltip("สุ่ม Pitch ± (0.08 = ±8%)")]
        [SerializeField] private float pitchJitter = 0.08f;
        [Tooltip("ระยะจากกล้องที่ยังดังเต็ม (หน่วยโลก)")]
        [SerializeField] private float fullVolumeDistance = 7f;
        [Tooltip("ระยะจากกล้องที่เงียบสนิท")]
        [SerializeField] private float silentDistance = 22f;
        [Tooltip("เสียงซ้าย-ขวาตามตำแหน่งบนจอ (0 = ปิด)")]
        [SerializeField] private float stereoPan = 0.45f;

        [Header("เพลง")]
        [Tooltip("เปลี่ยนเพลงตามสถานะเกมอัตโนมัติ")]
        [SerializeField] private bool autoMusic = true;
        [SerializeField] private float musicCrossfade = 1.5f;
        [Tooltip("ตอน Pause เพลงเบาลงเหลือเท่านี้")]
        [SerializeField] private float pauseMusicDuck = 0.5f;

        [Header("บรรยากาศ / เสียงเตือน")]
        [SerializeField] private bool playAmbience = true;
        [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.35f;
        [Tooltip("เสียงหัวใจเต้นเมื่อ HP ผู้เล่นต่ำกว่า")]
        [Range(0f, 1f)] [SerializeField] private float playerLowHpThreshold = 0.25f;
        [Tooltip("ระฆังเตือนเมื่อ HP ฐานต่ำกว่า")]
        [Range(0f, 1f)] [SerializeField] private float baseLowHpThreshold = 0.3f;
        [SerializeField] private float baseLowHpBellInterval = 4f;

        // ------------------------------------------------------------------ state
        private class Voice
        {
            public AudioSource src;
            public string id;
            public float startTime;
            public bool loop;
            public AudioChannel channel;
            public float baseVolume;
            public Transform follow;
            public Vector2 worldPos;
            public bool positional;
        }

        private readonly List<Voice> _voices = new List<Voice>();
        private readonly Dictionary<string, AudioClip[]> _cache = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, float> _lastPlay = new Dictionary<string, float>();
        private readonly Dictionary<int, Voice> _loops = new Dictionary<int, Voice>();
        private readonly HashSet<string> _missingLogged = new HashSet<string>();
        private int _nextLoopId = 1;

        private AudioSource _musicA, _musicB;
        private string _musicId;
        private float _musicDuck = 1f;
        private Coroutine _fade;
        private bool _sfxPaused;

        private int _ambField, _ambCrystal, _heartbeat;
        private float _baseBellTimer;

        private float _vMaster, _vMusic, _vSfx, _vUi;

        // ------------------------------------------------------------------ bootstrap

        /// <summary>คืนตัวจัดการเสียง (สร้างให้อัตโนมัติถ้ายังไม่มี)</summary>
        public static AudioManager Get()
        {
            if (Instance != null) return Instance;
            if (!Application.isPlaying || _quitting) return null;
            GameObject go = new GameObject("AudioManager (auto)");
            return go.AddComponent<AudioManager>();
        }

        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            _quitting = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
            Get();
        }

        private static void OnQuit() => _quitting = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _vMaster = PlayerPrefs.GetFloat("vol_Master", defaultMaster);
            _vMusic = PlayerPrefs.GetFloat("vol_Music", defaultMusic);
            _vSfx = PlayerPrefs.GetFloat("vol_SFX", defaultSfx);
            _vUi = PlayerPrefs.GetFloat("vol_UI", defaultUi);
            AudioListener.volume = _vMaster;

            for (int i = 0; i < Mathf.Max(8, voiceCount); i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _voices.Add(new Voice { src = s });
            }
            _musicA = MakeMusicSource();
            _musicB = MakeMusicSource();
        }

        private AudioSource MakeMusicSource()
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.volume = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ volume

        public float GetVolume(AudioChannel ch)
        {
            switch (ch)
            {
                case AudioChannel.Master: return _vMaster;
                case AudioChannel.Music: return _vMusic;
                case AudioChannel.SFX: return _vSfx;
                default: return _vUi;
            }
        }

        public void SetVolume(AudioChannel ch, float v)
        {
            v = Mathf.Clamp01(v);
            switch (ch)
            {
                case AudioChannel.Master: _vMaster = v; AudioListener.volume = v; break;
                case AudioChannel.Music: _vMusic = v; break;
                case AudioChannel.SFX: _vSfx = v; break;
                default: _vUi = v; break;
            }
            PlayerPrefs.SetFloat("vol_" + ch, v);
        }

        private float ChannelVolume(AudioChannel ch) => ch == AudioChannel.UI ? _vUi : ch == AudioChannel.Music ? _vMusic : _vSfx;

        // ------------------------------------------------------------------ clips

        private AudioClip[] Clips(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_cache.TryGetValue(id, out AudioClip[] arr)) return arr;
            List<AudioClip> list = new List<AudioClip>();
            AudioClip single = Resources.Load<AudioClip>(sfxFolder + "/" + id);
            if (single != null) list.Add(single);
            for (int i = 1; i <= 6; i++)
            {
                AudioClip c = Resources.Load<AudioClip>(sfxFolder + "/" + id + "_" + i.ToString("00"));
                if (c != null) list.Add(c);
            }
            arr = list.ToArray();
            _cache[id] = arr;
            if (arr.Length == 0 && _missingLogged.Add(id))
                Debug.Log("[Audio] ไม่พบไฟล์เสียง '" + id + "' ใน Resources/" + sfxFolder + " (ข้ามไป)");
            return arr;
        }

        // ------------------------------------------------------------------ one-shots

        /// <summary>เล่นเสียง 1 ครั้ง. position = null -> ไม่มีระยะ (UI / เสียงสำคัญ)</summary>
        public void PlayOneShot(string id, Vector2? position, AudioChannel channel, float volume = 1f, float pitch = 1f, bool jitter = true)
        {
            AudioClip[] clips = Clips(id);
            if (clips == null || clips.Length == 0 || volume <= 0f) return;

            float now = Time.unscaledTime;
            if (_lastPlay.TryGetValue(id, out float last) && now - last < sameSoundMinGap) return;

            int same = 0;
            for (int i = 0; i < _voices.Count; i++)
                if (_voices[i].id == id && _voices[i].src.isPlaying) same++;
            if (same >= maxSameSound) return;

            float att = 1f, pan = 0f;
            if (position.HasValue && !Attenuate(position.Value, out att, out pan)) return;

            Voice v = GetFreeVoice();
            if (v == null) return;
            _lastPlay[id] = now;

            AudioSource s = v.src;
            s.Stop();
            s.loop = false;
            s.clip = clips[Random.Range(0, clips.Length)];
            s.pitch = pitch * (jitter ? 1f + Random.Range(-pitchJitter, pitchJitter) : 1f);
            s.panStereo = pan;
            s.ignoreListenerPause = channel == AudioChannel.UI;
            v.id = id;
            v.channel = channel;
            v.baseVolume = volume;
            v.startTime = now;
            v.loop = false;
            v.follow = null;
            v.positional = false;
            s.volume = volume * att * ChannelVolume(channel);
            s.Play();
            if (_sfxPaused && channel != AudioChannel.UI) s.Pause();
        }

        private Voice GetFreeVoice()
        {
            Voice oldest = null;
            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (v.loop) continue;
                if (!v.src.isPlaying) return v;
                if (oldest == null || v.startTime < oldest.startTime) oldest = v;
            }
            return oldest; // ขโมยเสียงที่เก่าที่สุด
        }

        /// <summary>คำนวณความดังตามระยะจากกล้อง + pan ซ้ายขวา (false = ไกลเกินไม่ต้องเล่น)</summary>
        private bool Attenuate(Vector2 pos, out float att, out float pan)
        {
            att = 1f;
            pan = 0f;
            Camera cam = Camera.main;
            if (cam == null) return true;
            Vector2 c = cam.transform.position;
            float d = Vector2.Distance(c, pos);
            if (d > fullVolumeDistance)
            {
                float k = Mathf.InverseLerp(fullVolumeDistance, silentDistance, d);
                att = 1f - k * k * (3f - 2f * k);
            }
            float halfW = cam.orthographic ? cam.orthographicSize * cam.aspect : 10f;
            pan = Mathf.Clamp((pos.x - c.x) / Mathf.Max(1f, halfW), -1f, 1f) * stereoPan;
            return att > 0.02f;
        }

        // ------------------------------------------------------------------ loops

        /// <summary>เริ่มเสียงวนลูป คืนเลข handle (0 = เล่นไม่ได้) ใช้ StopLoop(handle) เพื่อหยุด</summary>
        public int StartLoop(string id, AudioChannel channel, float volume = 1f, Transform follow = null, Vector2? position = null)
        {
            AudioClip[] clips = Clips(id);
            if (clips == null || clips.Length == 0) return 0;
            Voice v = null;
            for (int i = 0; i < _voices.Count; i++)
                if (!_voices[i].loop && !_voices[i].src.isPlaying) { v = _voices[i]; break; }
            if (v == null) return 0;

            v.loop = true;
            v.id = id;
            v.channel = channel;
            v.baseVolume = volume;
            v.follow = follow;
            v.positional = follow != null || position.HasValue;
            v.worldPos = position ?? Vector2.zero;
            v.src.clip = clips[0];
            v.src.loop = true;
            v.src.pitch = 1f;
            v.src.panStereo = 0f;
            v.src.ignoreListenerPause = channel == AudioChannel.UI;
            v.src.time = Random.Range(0f, clips[0].length * 0.9f);
            UpdateLoopVolume(v);
            v.src.Play();
            int h = _nextLoopId++;
            _loops[h] = v;
            return h;
        }

        public void StopLoop(int handle)
        {
            if (handle == 0 || !_loops.TryGetValue(handle, out Voice v)) return;
            _loops.Remove(handle);
            v.src.Stop();
            v.src.loop = false;
            v.loop = false;
            v.follow = null;
            v.id = null;
        }

        public bool IsLoopPlaying(int handle) => handle != 0 && _loops.ContainsKey(handle);

        public void SetLoopVolume(int handle, float volume)
        {
            if (_loops.TryGetValue(handle, out Voice v)) v.baseVolume = volume;
        }

        private void UpdateLoopVolume(Voice v)
        {
            float att = 1f, pan = 0f;
            if (v.positional)
            {
                Vector2 p = v.follow != null ? (Vector2)v.follow.position : v.worldPos;
                if (!Attenuate(p, out att, out pan)) att = 0f;
            }
            v.src.volume = v.baseVolume * att * ChannelVolume(v.channel);
            v.src.panStereo = pan;
        }

        // ------------------------------------------------------------------ music

        public void PlayMusic(string id, bool loop = true)
        {
            if (_musicId == id) return;
            _musicId = id;
            AudioClip clip = string.IsNullOrEmpty(id) ? null : Resources.Load<AudioClip>(musicFolder + "/" + id);
            if (clip == null && !string.IsNullOrEmpty(id) && _missingLogged.Add(id))
                Debug.Log("[Audio] ไม่พบเพลง '" + id + "' ใน Resources/" + musicFolder);

            AudioSource from = _musicA.isPlaying && _musicA.volume >= _musicB.volume ? _musicA : _musicB;
            AudioSource to = from == _musicA ? _musicB : _musicA;
            to.Stop();
            to.clip = clip;
            to.loop = loop;
            to.volume = 0f;
            if (clip != null) to.Play();
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(from, to));
        }

        private float _fadeT = 1f;
        private AudioSource _musicCurrent, _musicPrev;

        private IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            _musicCurrent = to;
            _musicPrev = from;
            _fadeT = 0f;
            while (_fadeT < 1f)
            {
                _fadeT += Time.unscaledDeltaTime / Mathf.Max(0.05f, musicCrossfade);
                yield return null;
            }
            _fadeT = 1f;
            from.Stop();
            _musicPrev = null;
        }

        private void UpdateMusicVolumes()
        {
            float target = _vMusic * _musicDuck;
            if (_musicCurrent != null) _musicCurrent.volume = target * Mathf.Clamp01(_fadeT);
            if (_musicPrev != null) _musicPrev.volume = target * (1f - Mathf.Clamp01(_fadeT));
        }

        private void AutoMusic()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            GameState st = gm.State;
            float duckTarget = st == GameState.Paused ? pauseMusicDuck : st == GameState.LevelUp ? 0.75f : 1f;
            _musicDuck = Mathf.MoveTowards(_musicDuck, duckTarget, Time.unscaledDeltaTime * 2f);

            if (st == GameState.Victory) { PlayMusic("MUS_Victory", false); return; }
            if (st == GameState.Defeat) { PlayMusic("MUS_Defeat", false); return; }
            WaveManager w = gm.Waves;
            if (w == null) return;
            if (w.IsWaveActive)
            {
                int n = w.CurrentWaveNumber;
                PlayMusic(n <= 4 ? "MUS_Battle_Early" : n <= 8 ? "MUS_Battle_Mid" : "MUS_Battle_Final");
            }
            else PlayMusic("MUS_Build");
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            // หยุดเสียงในโลกชั่วคราวตอนเกมหยุด (Pause / เลือกการ์ด) เสียง UI ยังเล่นได้
            bool paused = Time.timeScale <= 0f;
            if (paused != _sfxPaused)
            {
                _sfxPaused = paused;
                for (int i = 0; i < _voices.Count; i++)
                {
                    Voice v = _voices[i];
                    if (v.channel == AudioChannel.UI) continue;
                    if (paused) v.src.Pause(); else v.src.UnPause();
                }
            }

            for (int i = 0; i < _voices.Count; i++)
            {
                Voice v = _voices[i];
                if (v.loop) UpdateLoopVolume(v);
            }

            if (autoMusic) AutoMusic();
            UpdateMusicVolumes();
            UpdateStateSounds();
        }

        /// <summary>เสียงที่ขึ้นกับสถานะ: บรรยากาศ, หัวใจเต้นตอน HP ต่ำ, ระฆังเตือนฐาน</summary>
        private void UpdateStateSounds()
        {
            GameManager gm = GameManager.Instance;
            bool inGame = gm != null && gm.State != GameState.Victory && gm.State != GameState.Defeat;

            if (playAmbience && inGame)
            {
                if (!IsLoopPlaying(_ambField)) _ambField = StartLoop("AMB_Field", AudioChannel.SFX, ambienceVolume);
                if (!IsLoopPlaying(_ambCrystal) && Registry.Crystal != null)
                    _ambCrystal = StartLoop("AMB_Crystal", AudioChannel.SFX, ambienceVolume * 0.8f, Registry.Crystal.transform);
            }
            else
            {
                StopLoop(_ambField); _ambField = 0;
                StopLoop(_ambCrystal); _ambCrystal = 0;
            }

            PlayerNecromancer p = Registry.Player;
            bool lowHp = inGame && p != null && p.IsAlive && p.CurrentHealth / Mathf.Max(1f, p.MaxHealth) < playerLowHpThreshold;
            if (lowHp && !IsLoopPlaying(_heartbeat)) _heartbeat = StartLoop("SFX_Player_LowHP", AudioChannel.SFX, 0.7f);
            else if (!lowHp && _heartbeat != 0) { StopLoop(_heartbeat); _heartbeat = 0; }

            Crystal c = Registry.Crystal;
            bool baseLow = inGame && gm.State == GameState.Playing && c != null && c.IsAlive && c.CurrentHealth / Mathf.Max(1f, c.MaxHealth) < baseLowHpThreshold;
            if (baseLow)
            {
                _baseBellTimer -= Time.deltaTime;
                if (_baseBellTimer <= 0f)
                {
                    _baseBellTimer = baseLowHpBellInterval;
                    PlayOneShot("SFX_Base_LowHP", null, AudioChannel.SFX, 0.8f, 1f, false);
                }
            }
            else _baseBellTimer = 0f;
        }
    }

    /// <summary>
    /// ทางลัดเรียกเสียง: Sfx.Play("SFX_xxx") / Sfx.At("SFX_xxx", ตำแหน่ง) / Sfx.UI("SFX_UI_xxx")
    /// ไม่มีไฟล์ = เงียบเฉยๆ ไม่ error
    /// </summary>
    public static class Sfx
    {
        /// <summary>เสียงเกมแบบไม่มีระยะ (ได้ยินเท่ากันทุกที่)</summary>
        public static void Play(string id, float volume = 1f, float pitch = 1f)
        {
            AudioManager m = AudioManager.Get();
            if (m != null) m.PlayOneShot(id, null, AudioChannel.SFX, volume, pitch);
        }

        /// <summary>เสียงในโลก ณ ตำแหน่ง (เบาลงตามระยะจากกล้อง)</summary>
        public static void At(string id, Vector2 position, float volume = 1f, float pitch = 1f)
        {
            AudioManager m = AudioManager.Get();
            if (m != null) m.PlayOneShot(id, position, AudioChannel.SFX, volume, pitch);
        }

        /// <summary>เสียง UI (ไม่เบาลงตามระยะ เล่นได้ตอน Pause)</summary>
        public static void UI(string id, float volume = 1f, float pitch = 1f)
        {
            AudioManager m = AudioManager.Get();
            if (m != null) m.PlayOneShot(id, null, AudioChannel.UI, volume, pitch, false);
        }

        public static int Loop(string id, float volume = 1f, Transform follow = null)
        {
            AudioManager m = AudioManager.Get();
            return m != null ? m.StartLoop(id, AudioChannel.SFX, volume, follow) : 0;
        }

        public static void StopLoop(ref int handle)
        {
            if (handle != 0 && AudioManager.Instance != null) AudioManager.Instance.StopLoop(handle);
            handle = 0;
        }
    }
}
