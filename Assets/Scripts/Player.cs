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
    public GameObject grenadeObj;
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
    bool gDown;


    bool iDown;
    bool sDown1;
    bool sDown2;
    bool sDown3;
    bool isSwap; //교체 시간차를 위한 플래그 
    bool isReload;

    bool isJump;
    bool isDodge;
    bool isFireReady = true;   //공격 준비
    bool isBorder; //벽 충돌 플래그
    bool isDamage; //무적타임

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rigid;
    Animator anim;
    MeshRenderer[] meshRenderers;
    GameObject nearObject;  //트리거 된 아이템을 저장하기 위한 변수
    Weapon equipWeapon; //기존에 장착된 무기를 저장하는 변수
    int equipWeaponIndex = -1;
    float fireDelay; // 공격 딜레이

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalSpeed = speed;
    }

    void Update()
    {
        GetInput();
        Attack();
        Grenade();
        Reload();
        Dodge();
        Swap();
        Interation();
    }

    void FixedUpdate()
    {
        FixedRotation();
        StopToWall();
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
        gDown = Input.GetButton("Fire2");
        rDown = Input.GetButtonDown("Reload");
        iDown = Input.GetButtonDown("Interation");
        sDown1 = Input.GetButtonDown("Swap1");
        sDown2 = Input.GetButtonDown("Swap2");
        sDown3 = Input.GetButtonDown("Swap3");

    }

    void Move()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        // 회피 중에는 움직임 벡터 -> 회피 방향 벡터로 바뀌도록 구현
        if (isDodge)
            moveVec = dodgeVec;

        // 무기 교체, 공격 중에는 움직이지 않도록
        if (isSwap || isReload || !isFireReady)
            moveVec = Vector3.zero;

        // 벽에 충돌하면 이동 제한
        if (!isBorder)
            transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;

        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);

    }

    void Turn()
    {
        // 키보드에 의한 회전
        transform.LookAt(transform.position + moveVec);

        // 마우스에 의한 회전
        if (fDown)
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, 100)) //out : return 처럼 반환값을 주어진 변수에 저장하는 키워드
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0; //RayCastHit의 높이는 무시하도록 Y축값을 0으로 초기화
                transform.LookAt(transform.position + nextVec);
            }
        }
    }

    void Jump()
    {
        if (jDown && moveVec == Vector3.zero && !isJump && !isDodge && !isSwap)
        {
            rigid.AddForce(Vector3.up * 20, ForceMode.Impulse);
            anim.SetBool("isJump", true);
            anim.SetTrigger("doJump");
            isJump = true;
        }
    }

    void Attack()
    {
        if (equipWeapon == null)
            return;

        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.rate < fireDelay;

        if (fDown && isFireReady && !isDodge && !isSwap)
        {
            equipWeapon.Use();
            anim.SetTrigger(equipWeapon.type == Weapon.Type.Melee ? "doSwing" : "doShot");
            fireDelay = 0;
        }
    }

    void Grenade()
    {
        if (hasGrenades == 0) return;
        if (gDown && !isReload && !isSwap)
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, 100)) //out : return 처럼 반환값을 주어진 변수에 저장하는 키워드
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 10; //RayCastHit의 높이는 무시하도록 Y축값을 0으로 초기화

                GameObject instantGrenade = Instantiate(grenadeObj, transform.position, transform.rotation);
                Rigidbody rigidGrenade = instantGrenade.GetComponent<Rigidbody>();
                rigidGrenade.AddForce(nextVec, ForceMode.Impulse);
                rigidGrenade.AddTorque(Vector3.back * 10, ForceMode.Impulse);

                hasGrenades--;
                grenades[hasGrenades].SetActive(false);
            }
        }
    }

    void Reload()
    {
        if (equipWeapon == null || equipWeapon.type == Weapon.Type.Melee || ammo == 0)
            return;

        if (rDown && !isJump && !isDodge && !isSwap && isFireReady)
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
        if (jDown && moveVec != Vector3.zero && !isJump && !isDodge && !isSwap)
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
        if (weaponIndex == -1 || !hasWeapon[weaponIndex] || equipWeaponIndex == weaponIndex)
            return;

        if (!isJump && !isDodge)
        {
            // 현재 장착한 무기가 있으면 비활성화
            if (equipWeapon != null)
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
        if (iDown && nearObject != null && !isJump && !isDodge)
        {
            if (nearObject.tag == "Weapon")
            {
                Item item = nearObject.GetComponent<Item>();
                int weaponIndex = item.value;
                hasWeapon[weaponIndex] = true;

                Destroy(nearObject);
            }
        }
    }

    void FixedRotation()
    {
        // 물리 회전 속도를 0으로 지정하면 스스로 돌지 않게 됨.
        rigid.angularVelocity = Vector3.zero;
    }

    void StopToWall()
    {
        Debug.DrawRay(transform.position, transform.forward * 2.5f, Color.green);
        isBorder = Physics.Raycast(transform.position, transform.forward, 2.5f, LayerMask.GetMask("Wall"));
    }

    

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Item")
        {
            Item item = other.GetComponent<Item>();
            switch (item.itemType)
            {
                case Item.Type.Ammo:
                    ammo += item.value;
                    if (ammo > maxAmmo)
                        ammo = maxAmmo;
                    break;
                case Item.Type.Coin:
                    coin += item.value;
                    if (coin > maxCoin)
                        coin = maxCoin;
                    break;
                case Item.Type.Heart:
                    health += item.value;
                    if (health > maxHealth)
                        health = maxHealth;
                    break;
                case Item.Type.Grenade:
                    grenades[hasGrenades].SetActive(true);
                    hasGrenades += item.value;
                    if (hasGrenades > maxHasGrenades)
                        hasGrenades = maxHasGrenades;
                    break;
            }
            Destroy(other.gameObject);
        }
        else if (other.tag == "EnemyBullet")
        {
            if (!isDamage) //무적 타임이 아닐 때만 공격 받게끔
            {
                Bullet enemyBullet = other.GetComponent<Bullet>();
                health -= enemyBullet.damage;

                bool isBossAttack = other.name == "Boss Melee Area";
                StartCoroutine(OnDamage(isBossAttack));
            }
            if (other.GetComponent<Rigidbody>() != null)
                Destroy(other.gameObject);
        }
    }

    // Damage 리액션
    IEnumerator OnDamage(bool isBossAttack)
    {
        isDamage = true;    // 무적 타임
        SetMeshesColor(Color.yellow);

        // 넉백
        if (isBossAttack)
            rigid.AddForce(transform.forward * -25, ForceMode.Impulse);

        yield return new WaitForSeconds(1f);

        isDamage = false;
        SetMeshesColor(Color.white);

        if (isBossAttack)
            rigid.velocity = Vector3.zero;
    }

    // 모든 재질의 색상 변경
    void SetMeshesColor(Color color)
    {
        foreach (var mesh in meshRenderers)
            mesh.material.color = color;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon")
            nearObject = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon")
            nearObject = null;
    }
}