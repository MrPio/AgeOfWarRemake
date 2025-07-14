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

        private readonly string[][] attackClips =
        {
            new[] { "whack_01", "whack_01", "stab_01" },
            // other ages
        };

        private readonly string[][] rangeClips =
        {
            new[] { null, "whoosh_02", null },
            // other ages
        };

        private readonly string[][] turretClips =
        {
            new[] { "knight_range_attack", "cave_turret_2_attack", "catapult" },
            // other ages
        };

        #endregion

        #region Data

        [SerializeField] private AudioSource sfxAudioSource, backgroundAudioSource;
        private readonly Dictionary<string, AudioClip> sfxClips = new();
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
            PlaySfx((isRanged ? rangeClips : attackClips)[age - 1][unitLevel - 1]);
        }

        public void PlayTurret(int age, int turretLevel)
        {
            // TODO: move caller to handle client&Host
            PlaySfx((turretClips)[age - 1][turretLevel - 1]);
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

        private void PlaySfx(string clip)
        {
            if (_lastPlayed.ContainsKey(clip) && Time.time - _lastPlayed[clip] < 0.035f) return;
            _lastPlayed[clip] = Time.time;
            sfxAudioSource.pitch = Random.Range(0.965f, 1.04f);
            sfxAudioSource.PlayOneShot(GetSfx(clip));
        }

        private AudioClip GetSfx(string clip)
        {
            print(sfxDir + clip);
            if (!sfxClips.ContainsKey(clip))
                sfxClips.Add(clip, Resources.Load<AudioClip>(sfxDir + clip));
            return sfxClips[clip];
        }

        #endregion
    }
}