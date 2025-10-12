using System.Collections.Generic;
using ExtensionFunctions;
using UnityEngine;

namespace Managers
{
    public class MusicManager : MonoBehaviour
    {
        #region Constants

        [SerializeField] private string sfxDir = "Audio/Sfx/";
        [SerializeField] private List<string> dieClips = new() { "die_01", "die_02", "die_03", "die_04", "die_05" };

        private readonly string[][] _attackClips =
        {
            new[] { "whack_01", "whack_01", "stab_01" },
            new[] { "stab_02", "whack_01", "stab_01" },
            // other ages
        };

        private readonly string[][] _rangeClips =
        {
            new[] { null, "whoosh_02", null },
            new[] { null, "medival_range_attack", null },
            // other ages
        };

        private readonly string[][] _turretClips =
        {
            new[] { "knight_range_attack", "cave_turret_2_attack", "catapult" },
            // other ages
        };

        private readonly string[][] _specialClips =
        {
            new[] { "explosion_01", "explosion_02" },
            new[] { "stab_01" },
            // other ages
        };

        private readonly float[] _specialVolumes = { 0.35f, 0.5f, 0.2f, 0.2f, 0.2f };

        #endregion

        #region Data

        [SerializeField] private AudioSource sfxAudioSource, backgroundAudioSource;
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
            else
                PlaySfx(dieClips.RandomItem());
        }

        public void PlayAttack(int age, int unitLevel, bool isRanged)
        {
            // TODO: move caller to handle client&Host
            PlaySfx((isRanged ? _rangeClips : _attackClips)[age - 1][unitLevel - 1]);
        }

        public void PlayTurret(int age, int turretLevel)
        {
            // TODO: move caller to handle client&Host
            PlaySfx(_turretClips[age - 1][turretLevel - 1]);
        }

        public void PlaySpecial(int age)
        {
            var sfxs = _specialClips[age - 1];
            PlaySfx(sfxs[Random.Range(0, sfxs.Length)], maxPitchShift: 0.25f, volume: _specialVolumes[age - 1]);
        }

        public void PlayUI(string type)
        {
            PlaySfx(type);
        }

        public void StartLevel()
        {
            backgroundAudioSource.Play();
        }

        #endregion

        #region Private Methods

        private void PlaySfx(string clip, float maxPitchShift = 0.05f, float volume = 1f)
        {
            if (_lastPlayed.ContainsKey(clip) && Time.time - _lastPlayed[clip] < 0.035f) return;
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