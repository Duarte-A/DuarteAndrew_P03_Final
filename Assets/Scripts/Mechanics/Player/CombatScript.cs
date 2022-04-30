using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Cinemachine;

public class CombatScript : MonoBehaviour
{
    public EnemyManager enemyManager;
    public EnemyDetection enemyDetection;
    public MovementInput movementInput;
    public Animator animator;

    [Header("Target")]
    private EnemyScript lockedTarget;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown;

    [Header("States")]
    public bool isAttackingEnemy = false;
    public bool isCountering = false;

    [Header("Public References")]
    [SerializeField] private Transform punchPosition;
    [SerializeField] private ParticleSystemScript punchParticle;
    [SerializeField] private GameObject lastHitCamera;
    [SerializeField] private Transform lastHitFocusObject;

    //Coroutines
    private Coroutine counterCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine damageCoroutine;

    [Space]

    int animationCount = 0;
    string[] attacks;

    //Punch SFX
    RandomSoundArrayPlay _randomSoundArrayPlay;


    void Start()
    {
        _randomSoundArrayPlay = GetComponent<RandomSoundArrayPlay>();

        animator = GetComponent<Animator>();
        enemyManager = FindObjectOfType<EnemyManager>();
        
        enemyDetection = GetComponentInChildren<EnemyDetection>();
        movementInput = GetComponent<MovementInput>();
    }

    //This function gets called whenever the player inputs the punch action
    void AttackCheck()
    {
        if (isAttackingEnemy)
            return;

        
        //Check to see if the detection behavior has an enemy set
        if (enemyDetection.CurrentTarget() == null)
        {
            if (enemyManager.AliveEnemyCount() == 0)
            {
                Attack(null, 0);
                return;
            }
            else
            {
                lockedTarget = enemyManager.RandomEnemy();
            }
        }
        

        //If the player is moving the movement input, use the "directional" detection to determine the enemy
        if (enemyDetection.InputMagnitude() > .2f)
            lockedTarget = enemyDetection.CurrentTarget();

        //Extra check to see if the locked target was set
        if (lockedTarget == null)
            lockedTarget = enemyManager.RandomEnemy();

        //AttackTarget
        Attack(lockedTarget, TargetDistance(lockedTarget));
    }

    public void Attack(EnemyScript target, float distance)
    {
        //Types of attack animation
        attacks = new string[] { "OverhandKick", "RoundJumpKick", "FlyingKick", "FlyingKnee" };

        //Attack nothing in case target is null
        if (target == null)
        {
            AttackType("AirPunch", .2f, null, 0);
            return;
        }

        if (distance < 15)
        {
            animationCount = (int)Mathf.Repeat((float)animationCount + 1, (float)attacks.Length);
            string attackString = isLastHit() ? attacks[Random.Range(0, attacks.Length)] : attacks[animationCount];
            
            AttackType(attackString, attackCooldown, target, .65f);
        }
        else
        {
            lockedTarget = null;
            AttackType("AirPunch", .2f, null, 0);
        }

    }

    void AttackType(string attackTrigger, float cooldown, EnemyScript target, float movementDuration)
    {
        animator.SetTrigger(attackTrigger);

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine(isLastHit() ? 1.5f : cooldown));

        //Check if last enemy
        if (isLastHit())
            StartCoroutine(FinalBlowCoroutine());

        if (target == null)
            return;

        target.StopMoving();

        MoveTorwardsTarget(target, movementDuration);


        IEnumerator AttackCoroutine(float duration)
        {
            movementInput.acceleration = 0;
            isAttackingEnemy = true;
            movementInput.enabled = false;
            yield return new WaitForSeconds(duration);
            isAttackingEnemy = false;
            yield return new WaitForSeconds(.2f);
            movementInput.enabled = true;
            LerpCharacterAcceleration();
        }

        IEnumerator FinalBlowCoroutine()
        {
            PlayFinishingSFX();
            Time.timeScale = .5f;
            lastHitCamera.SetActive(true);
            lastHitFocusObject.position = lockedTarget.transform.position;
            yield return new WaitForSecondsRealtime(2);
            lastHitCamera.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    void MoveTorwardsTarget(EnemyScript target, float duration)
    {
        
        lockedTarget.GetComponent<EnemyScript>().OnPlayerTrajectory(target);
        transform.DOLookAt(target.transform.position, .2f);

        transform.DOMove(TargetOffset(target.transform), duration);

    }

    
    void CounterCheck()
    {
        //Initial check
        if (isCountering || isAttackingEnemy || !enemyManager.AnEnemyIsPreparingAttack())
            return;

        lockedTarget = ClosestCounterEnemy();
        
        lockedTarget.GetComponent<EnemyScript>().OnPlayerCounter(lockedTarget);

        if (TargetDistance(lockedTarget) > 2)
        {
            Attack(lockedTarget, TargetDistance(lockedTarget));
            
            return;
        }


        float duration = .2f;
        animator.SetTrigger("Dodging");
        PlayCounterSFX();
        transform.DOLookAt(lockedTarget.transform.position, duration);
        transform.DOMove(transform.position + lockedTarget.transform.forward, duration);

        if (counterCoroutine != null)
            StopCoroutine(counterCoroutine);
        counterCoroutine = StartCoroutine(CounterCoroutine(duration));

    }
        IEnumerator CounterCoroutine(float duration)
        {
            isCountering = true;
            movementInput.enabled = false;
            yield return new WaitForSeconds(duration);
            Attack(lockedTarget, TargetDistance(lockedTarget));
            isCountering = false;

        }
    

    float TargetDistance(EnemyScript target)
    {
        return Vector3.Distance(transform.position, target.transform.position);
    }

    public Vector3 TargetOffset(Transform target)
    {
        Vector3 position;
        position = target.position;
        return Vector3.MoveTowards(position, transform.position, .95f);
    }

    public void HitEvent(EnemyScript target)
    {
        if (lockedTarget == null || enemyManager.AliveEnemyCount() == 0)
            return;

        
        lockedTarget.GetComponent<EnemyScript>().OnPlayerHit(lockedTarget);
        _randomSoundArrayPlay.GetComponent<RandomSoundArrayPlay>().HitSFX();

    }

    public void PlayHitSFX()
    {
        _randomSoundArrayPlay.GetComponent<RandomSoundArrayPlay>().HitSFX();
    }

    public void PlayFinishingSFX()
    {
        _randomSoundArrayPlay.GetComponent<RandomSoundArrayPlay>().FinishingSFX();
    }

    public void PlayCounterSFX()
    {
        _randomSoundArrayPlay.GetComponent<RandomSoundArrayPlay>().CounterSFX();
    }


    public void DamageEvent()
    {
        animator.SetTrigger("Hit");
        
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamageCoroutine());

        IEnumerator DamageCoroutine()
        {
            movementInput.enabled = false;
            yield return new WaitForSeconds(.5f);
            movementInput.enabled = true;
            LerpCharacterAcceleration();
        }
    }

    public void StunLookAt(EnemyScript target)
    {
        float duration = .2f;
        transform.DOLookAt(lockedTarget.transform.position, duration);

    }

    EnemyScript ClosestCounterEnemy()
    {
        float minDistance = 100;
        int finalIndex = 0;

        for (int i = 0; i < enemyManager.allEnemies.Length; i++)
        {
            EnemyScript enemy = enemyManager.allEnemies[i].enemyScript;

            if (enemy.IsPreparingAttack())
            {
                if (Vector3.Distance(transform.position, enemy.transform.position) < minDistance)
                {
                    minDistance = Vector3.Distance(transform.position, enemy.transform.position);
                    finalIndex = i;
                }
            }
        }

        return enemyManager.allEnemies[finalIndex].enemyScript;

    }

    void LerpCharacterAcceleration()
    {
        movementInput.acceleration = 0;
        DOVirtual.Float(0, 1, .6f, ((acceleration) => movementInput.acceleration = acceleration));
    }

    bool isLastHit()
    {
        if (lockedTarget == null)
            return false;

        return enemyManager.AliveEnemyCount() == 1 && lockedTarget.health <= 1;
    }

    #region Input

    
    private void OnCounter()
    {
        CounterCheck();
    }
    

    private void OnAttack()
    {
        AttackCheck();
    }

    #endregion

}