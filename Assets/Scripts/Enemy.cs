using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public int maxHealth;
    public int curHealth;
    public Transform target;
    public bool isChase;

    Rigidbody rigid;
    BoxCollider boxCollider;
    Material material;
    NavMeshAgent nav;
    Animator anim;

    void Awake() 
    {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        material = GetComponentInChildren<MeshRenderer>().material;
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        Invoke("ChaseStart",2);
    }

    void Update() 
    {
        if(isChase)
            nav.SetDestination(target.position);  //도착할 목표 위치 지정 함수 
    }

    void FixedUpdate() 
    {
        FixedVelocity();    
    }

    void ChaseStart()
    {
        isChase = true;
        anim.SetBool("isWalk",true);
    }
    void FixedVelocity()
    {
        if(isChase)
        {
            // 물리력이 NavAgent 이동을 방해하지 않도록 zero로 설정정
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        if(other.tag == "Melee")
        {
            Weapon weapon = other.GetComponent<Weapon>();
            curHealth -= weapon.damage;
            Vector3 reactVec = transform.position - other.transform.position;

            StartCoroutine(OnDamage(reactVec, false));

            Debug.Log("Melee : curHealth" + curHealth);
        }
        else if(other.tag == "Bullet")
        {
            Bullet bullet = other.GetComponent<Bullet>();
            curHealth -= bullet.damage;
            Vector3 reactVec = transform.position - other.transform.position;
            Destroy(other.gameObject);

            StartCoroutine(OnDamage(reactVec, false));
            Debug.Log("Bullet : curHealth" + curHealth);
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
        material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        
        if(curHealth > 0) 
            material.color = Color.white;
        else 
        {
            material.color = Color.gray;
            gameObject.layer = 14;
            isChase = false;
            nav.enabled = false;

            anim.SetTrigger("doDie");

            if (isGrenade)
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up * 3;

                // 수류탄에 의한 사망 리액션은 큰 힘과 회전을 추가하기 위해 freezRotation = false 설정
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15, ForceMode.Impulse);
            }
            else
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
            }
            Destroy(gameObject, 4);    
        }
    }
}
