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

        [SerializeField] private string[][] attackClips =
        {
            new[] { "whack_01", "whack_01", "stab_01" },
            // other ages
        };

        [SerializeField] private string[][] rangeClips =
        {
            new[] { null, "whoosh_02", null },
            // other ages
        };

        #endregion

        [SerializeField] private AudioSource sfxAudioSource, backgroundAudioSource;
        private readonly Dictionary<string, AudioClip> sfxClips = new();

        private void PlaySfx(string clip)
        {
            sfxAudioSource.PlayOneShot(GetSfx(clip));
        }

        public void PlayDie(int age = 0, int unitType = 0)
        {
            if (age == 1 && unitType == 3)
                PlaySfx("cave_tank_die");
            else if (age == 2 && unitType == 3)
                PlaySfx("knight_tank_die");
            else
                PlaySfx(dieClips.RandomItem());
        }

        public void PlayAttack(int age, int unitType, bool isRanged)
        {
            PlaySfx((isRanged ? rangeClips : attackClips)[age - 1][unitType - 1]);
        }

        public void StartLevel()
        {
            backgroundAudioSource.Play();
        }

        private AudioClip GetSfx(string clip)
        {
            print(sfxDir + clip);
            if (!sfxClips.ContainsKey(clip))
                sfxClips.Add(clip, Resources.Load<AudioClip>(sfxDir + clip));
            return sfxClips[clip];
        }
    }
}