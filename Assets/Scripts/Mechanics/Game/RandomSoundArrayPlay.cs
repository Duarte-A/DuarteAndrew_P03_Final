using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundArrayPlay : MonoBehaviour
{
    //Punch SFX
    [SerializeField] AudioClip _koSound;
    [SerializeField] AudioClip _finishingSound;
    [SerializeField] AudioClip _counterSound;
    public float _clipVolume = 1f;
    
    AudioSource _audioSource;
    [SerializeField] AudioClip[] _randomSoundArray;

    EnemyScript _enemyScript;
    CombatScript _playerScript;


    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _enemyScript = FindObjectOfType<EnemyScript>();
    }


    public void HitSFX()
    {

        AudioClip clip = _randomSoundArray[UnityEngine.Random.Range(0, _randomSoundArray.Length)];

        _audioSource.PlayOneShot(clip);
    }

    public void KnockoutSFX()
    {
        _audioSource.PlayOneShot(_koSound, _clipVolume = .4f);
    }

    public void FinishingSFX()
    {
        _audioSource.PlayOneShot(_finishingSound, _clipVolume = 3f);
    }

    public void CounterSFX()
    {
        _audioSource.PlayOneShot(_counterSound, _clipVolume = 2f);
    }
}
