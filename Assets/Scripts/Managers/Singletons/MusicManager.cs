using System.Collections.Generic;
using ExtensionFunctions;
using Interfaces;
using UnityEngine;

namespace Managers.Singletons
{
    public class MusicManager : SingletonMonoBehaviour<MusicManager>
    {
        #region Constants

        [SerializeField] private string sfxDir = "Audio/Sfx/";
        [SerializeField] private List<string> dieClips = new() { "die_01", "die_02", "die_03", "die_04", "die_05" };

        private readonly string[][] _attackClips =
        {
            new[] { "whack_01", "whack_01", "stab_01" },
            new[] { "stab_02", "whack_01", "stab_01" },
            new[] { "stab_01", "whack_01", "whack_01" },
            new[] { "stab_01", "miltary_range_attack", null },
            new[] { "sword_clash_02", "whack_01", null },
        };

        private readonly string[][] _rangeClips =
        {
            new[] { null, "whoosh_02", null },
            new[] { null, "medival_range_attack", null },
            new[] { null, "medival_range_attack", "medival_tank_attack" },
            new[] { null, "miltary_range_attack_single", "explosion_02" },
            new[] { null, "future_range_attack_single", "future_tank_attack" },
            // other ages
        };

        private readonly string[][] _turretClips =
        {
            new[] { "knight_range_attack", "cave_turret_2_attack", "catapult" },
            new[] { "catapult", "catapult", "fire_01" },
            new[] { "medival_range_attack", "medival_range_attack", "medival_range_attack" },
            new[] { "miltary_turret_attack", "medival_tank_attack", "miltary_turret_3_attack" },
            new[] { "future_tank_attack", "future_turret_attack", "future_turret_attack" },
            // other ages
        };

        private readonly string[] _specialStartClips =
        {
            "special_1", "special_2", "special_3", "special_4", "special_5"
        };

        private readonly string[][] _specialHitClips =
        {
            new[] { "explosion_01", "explosion_02" },
            new[] { "stab_01" },
            null,
            new[] { "explosion_01", "explosion_02" },
            new[] { "future_tank_attack" },
            // other ages
        };

        private readonly float[] _specialVolumes = { 0.35f, 0.5f, 0.35f, 0.5f, 0.7f };

        #endregion

        #region Data

        [SerializeField] public AudioSource sfxAudioSource, backgroundAudioSource;
        private readonly Dictionary<string, AudioClip> _sfxClips = new();
        private readonly Dictionary<string, float> _lastPlayed = new();

        #endregion

        #region API

        public void PlayDie(int age = 0, int unitType = 0)
        {
            // TODO: move caller to handle client&Host
            if (age == 1 && unitType == 3)
                PlaySfx("cave_tank_die");
            else if (age == 2 && unitType == 3)
                PlaySfx("knight_tank_die");
            else if (age == 4 && unitType == 3)
            {
                PlaySfx("explosion_01");
                PlaySfx("fire_01", force: true);
            }
            else if (age == 5 && unitType == 3)
            {
                PlaySfx("explosion_01");
                PlaySfx("fire_01", force: true);
            }
            else
                PlaySfx(dieClips.RandomItem());
        }

        public void PlayAttack(int age, int unitLevel, bool isRanged)
        {
            PlaySfx((isRanged ? _rangeClips : _attackClips)[age - 1][unitLevel - 1]);
        }

        public void PlayTurret(int age, int turretLevel)
        {
            // TODO: move caller to handle client&Host
            PlaySfx(_turretClips[age - 1][turretLevel - 1]);
        }

        public void PlayStartSpecial(int age) =>
            PlaySfx(_specialStartClips[age - 1], maxPitchShift: 0.05f);

        public void PlayHitSpecial(int age)
        {
            var sfxs = _specialHitClips[age - 1];
            PlaySfx(sfxs[Random.Range(0, sfxs.Length)], maxPitchShift: 0.25f, volume: _specialVolumes[age - 1]);
        }

        public void PlayUI(string type)
        {
            PlaySfx(type);
        }

        public void PlayPopPowerup(bool collect)
        {
            PlaySfx(collect ? "powerup_collect" : "powerup_pop");
        }

        public void StartLevel()
        {
            backgroundAudioSource.Stop();
            backgroundAudioSource.Play();
        }

        public void EndLevel()
        {
            backgroundAudioSource.Stop();
        }

        public void SetMusicVolume(int volume)
        {
            backgroundAudioSource.volume = volume / 100f;
            // var dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20f;
            // sfxAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("MasterVolume", dB);
        }

        public void SetEffectsVolume(int volume)
        {
            sfxAudioSource.volume = volume / 100f;
            // var dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20f;
            // sfxAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("MasterVolume", dB);
        }

        #endregion

        #region Private Methods

        private void PlaySfx(string clip, float maxPitchShift = 0.05f, float volume = 1f, bool force = false)
        {
            if (clip is null) return;
            if (!force && _lastPlayed.ContainsKey(clip) && Time.time - _lastPlayed[clip] < 0.035f) return;
            _lastPlayed[clip] = Time.time;
            sfxAudioSource.pitch = Random.Range(1 - maxPitchShift, 1 + maxPitchShift);
            sfxAudioSource.PlayOneShot(GetSfx(clip), volume);
        }

        private AudioClip GetSfx(string clip)
        {
            if (!_sfxClips.ContainsKey(clip))
                _sfxClips.Add(clip, Resources.Load<AudioClip>(sfxDir + clip));
            return _sfxClips[clip];
        }

        #endregion
    }
}