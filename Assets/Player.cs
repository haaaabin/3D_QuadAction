using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed;
    public GameObject[] weapon;
    public bool[] hasWeapon;

    float originalSpeed;
    float hAxis;
    float vAxis;
    bool wDown;
    bool jDown;
    bool iDown;
    bool isJump;
    bool isDodge;

    Vector3 moveVec;
    Vector3 dodgeVec;
    Rigidbody rigid;
    Animator anim;

    GameObject nearObject;  //트리거 된 아이템을 저장하기 위한 변수

    void Awake() 
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();  
        originalSpeed = speed;  
    }

    void Update()
    {
       GetInput();
       Interaction();
    }

    void FixedUpdate()
    {
       Move();
       Turn();
       Jump();
       Dodge();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");   
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");
        iDown = Input.GetButtonDown("Interaction");
    }

    void Move()
    {
        moveVec = new Vector3(hAxis,0,vAxis).normalized;
        
        // 회피 중에는 움직임 벡터 -> 회피 방향 벡터로 바뀌도록 구현
        if(isDodge)
            moveVec = dodgeVec;

        transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;
        
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
    }

    void Turn()
    {
        if (moveVec != Vector3.zero)
        {
            transform.LookAt(transform.position + moveVec);
        }
    }

    void Jump()
    {
        if(jDown && moveVec == Vector3.zero && !isJump && !isDodge)
        {
            rigid.AddForce(Vector3.up * 20, ForceMode.Impulse);
            anim.SetBool("isJump",true);
            anim.SetTrigger("doJump");
            isJump = true;
        }
    }

    void Dodge()
    {
        if(jDown && moveVec != Vector3.zero && !isJump && !isDodge)
        {
            dodgeVec = moveVec;
            speed = originalSpeed * 2;
            anim.SetTrigger("doDodge");
            isDodge = true;

            Invoke("DodgeOut", 0.5f);
        }
    }

    void DodgeOut()
    {
        speed = originalSpeed;
        isDodge = false;
    }

    void Interaction()
    {
        if(iDown && nearObject != null && !isJump && !isDodge)
        {
            if(nearObject.tag == "Weapon")
            {
                Item item  = nearObject.GetComponent<Item>();
                int weaponIndex = item.value;
                hasWeapon[weaponIndex] = true;

                Destroy(nearObject);
            }
        }
    }
    
    void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump",false);
            isJump  = false;
        }
    }

    private void OnTriggerStay(Collider other) 
    {
        if(other.tag == "Weapon")
            nearObject = other.gameObject;
        
        Debug.Log(nearObject.name);
    }

    private void OnTriggerExit(Collider other) 
    {
        if(other.tag == "Weapon")
            nearObject = null;
    }
}