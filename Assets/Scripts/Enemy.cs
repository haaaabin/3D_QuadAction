using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum Type { A, B, C, D };
    public Type enemyType;

    public int maxHealth;
    public int curHealth;
    public Transform target;
    public BoxCollider meleeArea; // 근접 공격 범위
    public GameObject bullet;
    public bool isChase;
    public bool isAttack;
    public bool isDead;

    [HideInInspector]
    public Rigidbody rigid;
    [HideInInspector]
    public BoxCollider boxCollider;
    [HideInInspector]
    public MeshRenderer[] meshes;
    [HideInInspector]
    public NavMeshAgent nav;
    [HideInInspector]
    public Animator anim;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        meshes = GetComponentsInChildren<MeshRenderer>();
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (enemyType != Type.D)
            Invoke("ChaseStart", 2);
    }

    void Update()
    {
        //NavMeshAgent가 활성화되어있을때만 추격
        if (nav.enabled && enemyType != Type.D)
        {
            nav.SetDestination(target.position);  //도착할 목표 위치 지정 함수 
            nav.isStopped = !isChase;   //추적 중이 아니라면 멈춤
        }
    }

    void FixedUpdate()
    {
        Targeting();
        FreezeVelocity();
    }

    void FreezeVelocity()
    {
        if (isChase)
        {
            // 물리력이 NavAgent 이동을 방해하지 않도록 zero로 설정정
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    void Targeting()
    {
        if (!isDead && enemyType != Type.D)
        {
            float targetRadius = 1.5f;
            float targetRange = 3f;

            switch (enemyType)
            {
                case Type.B:
                    targetRadius = 1f;
                    targetRange = 12f;
                    break;
                case Type.C:
                    targetRadius = 0.5f;
                    targetRange = 25f;
                    break;
            }

            // 적이 플레이어를 발견하면
            RaycastHit[] rayHits =
                Physics.SphereCastAll(transform.position,
                                    targetRadius,
                                    transform.forward,
                                    targetRange,
                                    LayerMask.GetMask("Player"));

            if (rayHits.Length > 0 && !isAttack)
            {
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack()
    {
        isChase = false;
        isAttack = true;
        anim.SetBool("isAttack", true);

        switch (enemyType)
        {
            case Type.A:    // 근접 공격
                yield return new WaitForSeconds(0.2f);
                meleeArea.enabled = true;

                yield return new WaitForSeconds(1f);
                meleeArea.enabled = false;

                yield return new WaitForSeconds(1f);
                break;

            case Type.B:    // 돌격 공격
                yield return new WaitForSeconds(0.1f);
                rigid.AddForce(transform.forward * 20, ForceMode.Impulse); //돌격
                meleeArea.enabled = true;

                yield return new WaitForSeconds(0.5f);
                rigid.velocity = Vector3.zero;
                meleeArea.enabled = false;

                yield return new WaitForSeconds(2f);
                break;

            case Type.C:    // 원거리 공격
                yield return new WaitForSeconds(0.5f);
                GameObject instantBullet = Instantiate(bullet, transform.position, transform.rotation);
                Rigidbody rigidBullet = instantBullet.GetComponent<Rigidbody>();
                rigidBullet.velocity = transform.forward * 20;

                yield return new WaitForSeconds(2f);
                break;
        }

        isChase = true;
        isAttack = false;
        anim.SetBool("isAttack", false);
    }

    void ChaseStart()
    {
        isChase = true;
        anim.SetBool("isWalk", true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Melee")
        {
            Weapon weapon = other.GetComponent<Weapon>();
            curHealth -= weapon.damage;
            Vector3 reactVec = transform.position - other.transform.position;

            StartCoroutine(OnDamage(reactVec, false));

        }
        else if (other.tag == "Bullet")
        {
            Bullet bullet = other.GetComponent<Bullet>();
            curHealth -= bullet.damage;
            Vector3 reactVec = transform.position - other.transform.position;
            Destroy(other.gameObject);

            StartCoroutine(OnDamage(reactVec, false));
        }
    }

    public void HitByGrenade(Vector3 explosionPos)
    {
        curHealth -= 100;
        Vector3 reactVec = transform.position - explosionPos;

        StartCoroutine(OnDamage(reactVec, true));
    }

    IEnumerator OnDamage(Vector3 reactVec, bool isGrenade)
    {
        SetMeshesColor(Color.red);
        yield return new WaitForSeconds(0.1f);

        if (curHealth > 0)
        {
            SetMeshesColor(Color.white);
        }
        else
        {
            SetMeshesColor(Color.gray);
            Die(reactVec, isGrenade);
        }
    }

    void SetMeshesColor(Color color)
    {
        foreach (var mesh in meshes)
            mesh.material.color = color;
    }

    void Die(Vector3 reactVec, bool isGrenade)
    {
        gameObject.layer = 14;
        isDead = true;
        isChase = false;
        nav.enabled = false;
        anim.SetTrigger("doDie");

        // 수류탄에 의한 사망 리액션 처리
        reactVec = reactVec.normalized;
        reactVec += isGrenade ? Vector3.up * 3 : Vector3.up;

        rigid.freezeRotation = !isGrenade;
        rigid.AddForce(reactVec * 5, ForceMode.Impulse);

        if (isGrenade)
            rigid.AddTorque(reactVec * 15, ForceMode.Impulse);

        if (enemyType != Type.D)
            Destroy(gameObject, 4);
    }
}
