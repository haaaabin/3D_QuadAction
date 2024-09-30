using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed;
    public GameObject[] weapon;
    public bool[] hasWeapon;
    public GameObject[] grenades;
    public int hasGrenades;

    public Camera followCamera;
    

    public int ammo;
    public int coin;
    public int health;

    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    float originalSpeed;
    float hAxis;
    float vAxis;

    bool wDown;
    bool jDown;
    bool fDown;
    bool rDown;


    bool iDown;
    bool sDown1;
    bool sDown2;
    bool sDown3;
    bool isSwap; //교체 시간차를 위한 플래그 
    bool isReload;

    bool isJump;
    bool isDodge;
    bool isFireReady = true;   //공격 준비

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rigid;
    Animator anim;

    GameObject nearObject;  //트리거 된 아이템을 저장하기 위한 변수
    Weapon equipWeapon; //기존에 장착된 무기를 저장하는 변수
    int equipWeaponIndex = -1;
    float fireDelay; // 공격 딜레이
    
    void Awake() 
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();  
        originalSpeed = speed;  
    }

    void Update()
    {
       Attack();
       Reload();
       Dodge();
       Swap();
       Interation();
    }

    void FixedUpdate() 
    {
       GetInput();    
       Move();
       Turn();
       Jump();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");   
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");
        fDown = Input.GetButton("Fire1");
        rDown = Input.GetButtonDown("Reload");
        iDown = Input.GetButtonDown("Interation");
        sDown1 = Input.GetButtonDown("Swap1");
        sDown2 = Input.GetButtonDown("Swap2");
        sDown3 = Input.GetButtonDown("Swap3");

    }

    void Move()
    {
        moveVec = new Vector3(hAxis,0,vAxis).normalized;
        
        // 회피 중에는 움직임 벡터 -> 회피 방향 벡터로 바뀌도록 구현
        if(isDodge)
            moveVec = dodgeVec;

        // 무기 교체, 공격 중에는 움직이지 않도록
        if(isSwap || isReload || !isFireReady)
            moveVec = Vector3.zero;

        transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;
        
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
    }

    void Turn()
    {
        // 키보드에 의한 회전
        transform.LookAt(transform.position + moveVec);

        // 마우스에 의한 회전
        if(fDown)
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if(Physics.Raycast(ray, out rayHit, 100)) //out : return 처럼 반환값을 주어진 변수에 저장하는 키워드
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0; //RayCastHit의 높이는 무시하도록 Y축값을 0으로 초기화
                transform.LookAt(transform.position + nextVec);
            }
        }
    }

    void Jump()
    {
        if(jDown && moveVec == Vector3.zero && !isJump && !isDodge && !isSwap)
        {
            rigid.AddForce(Vector3.up * 20, ForceMode.Impulse);
            anim.SetBool("isJump",true);
            anim.SetTrigger("doJump");
            isJump = true;
        }
    }

    void Attack()
    {
        if(equipWeapon == null)
            return;
        
        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.rate < fireDelay;

        if(fDown && isFireReady && !isDodge && !isSwap)
        {
            equipWeapon.Use();
            anim.SetTrigger(equipWeapon.type == Weapon.Type.Melee ? "doSwing" : "doShot");
            fireDelay = 0;
        }
    }
    
    void Reload()
    {
        if(equipWeapon == null || equipWeapon.type == Weapon.Type.Melee || ammo == 0)
            return;
        
        if(rDown && !isJump && !isDodge && !isSwap && isFireReady)
        {
            anim.SetTrigger("doReload");
            isReload = true;

            Invoke("ReloadOut", 3f);
        }
    }

    //탄약이 채워짐
    void ReloadOut()
    {
        int reAmmo = ammo < equipWeapon.maxAmmo ? ammo : equipWeapon.maxAmmo; //재장전할 탄약 수
        equipWeapon.curAmmo = reAmmo;
        ammo -= reAmmo;     // 남은 탄약에서 장전한 탄약 수만큼 차감    
        isReload = false;
    }

    void Dodge()
    {
        if(jDown && moveVec != Vector3.zero && !isJump && !isDodge && !isSwap)
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

    void Swap()
    {
        int weaponIndex = -1;

        if (sDown1) weaponIndex = 0;
        else if (sDown2) weaponIndex = 1;
        else if (sDown3) weaponIndex = 2;

        // 없는 무기나 이미 장착한 무기인 경우 스왑하지 않도록
        if(weaponIndex == -1 || !hasWeapon[weaponIndex] || equipWeaponIndex == weaponIndex)
            return;

        if(!isJump && !isDodge)
        {
            // 현재 장착한 무기가 있으면 비활성화
            if(equipWeapon != null)
                equipWeapon.gameObject.SetActive(false);

            equipWeaponIndex = weaponIndex;
            equipWeapon = weapon[weaponIndex].GetComponent<Weapon>();
            equipWeapon.gameObject.SetActive(true);

            anim.SetTrigger("doSwap");
            isSwap = true;

            Invoke("SwapOut", 0.4f);
        }
    }

    void SwapOut()
    {
        isSwap = false;
    }

    void Interation()
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

    void OnTriggerEnter(Collider other) 
    {
        if(other.tag == "Item")
        {
            Item item = other.GetComponent<Item>();
            switch(item.itemType)
            {
                case Item.Type.Ammo:
                    ammo += item.value;
                    if(ammo > maxAmmo)
                        ammo = maxAmmo;
                    break;
                case Item.Type.Coin:
                    coin += item.value;
                    if(coin > maxCoin)
                        coin = maxCoin;
                    break;
                case Item.Type.Heart:
                    health += item.value;
                    if(health > maxHealth)
                        health = maxHealth;
                    break;
                case Item.Type.Grenade:
                    grenades[hasGrenades].SetActive(true);
                    hasGrenades += item.value;
                    if(hasGrenades > maxHasGrenades)
                        hasGrenades = maxHasGrenades;
                    break;
            }
            Destroy(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other) 
    {
        if(other.tag == "Weapon")
            nearObject = other.gameObject;
        
        //Debug.Log(nearObject.name);
    }

    private void OnTriggerExit(Collider other) 
    {
        if(other.tag == "Weapon")
            nearObject = null;
    }
}