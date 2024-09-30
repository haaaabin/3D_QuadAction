using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum Type { Melee, Range};
    public Type type; 
    public int damage;
    public float rate;
    public int maxAmmo; //전체 탄약
    public int curAmmo; //현재 탄약

    public BoxCollider meleeArea;
    public TrailRenderer trailEffect;

    public Transform bulletPos; 
    public GameObject bullet;
    public Transform bulletCasePos;
    public GameObject bulletCase;

    public void Use()
    {
        if(type == Type.Melee)
        {
            StopCoroutine("Swing");
            StartCoroutine("Swing");
        }
        else if(type == Type.Range && curAmmo > 0)
        {
            curAmmo--;  //탄약 소모
            StartCoroutine("Shot");
        }
    }

    // IEnumerator 열거형 함수 클래스
    IEnumerator Swing()
    {
        yield return new WaitForSeconds(0.1f);
        meleeArea.enabled = true;
        trailEffect.enabled = true;

        yield return new WaitForSeconds(0.3f);
        meleeArea.enabled = false;

        yield return new WaitForSeconds(0.3f);
        trailEffect.enabled = false;
    }

    IEnumerator Shot()
    {
        //총알 발사
        GameObject instantBullet = Instantiate(bullet, bulletPos.position, bulletPos.rotation);
        Rigidbody bulletRigid = instantBullet.GetComponent<Rigidbody>();
        bulletRigid.velocity = bulletPos.forward * 50;

        yield return null;

        //탄피 배출
        GameObject instantCase = Instantiate(bulletCase, bulletCasePos.position, bulletCasePos.rotation);
        Rigidbody caseRigid = instantCase.GetComponent<Rigidbody>();
        Vector3 caseVec = bulletCasePos.forward * Random.Range(-3,-2) + Vector3.up * Random.Range(2,3);
        caseRigid.AddForce(caseVec, ForceMode.Impulse);
        caseRigid.AddTorque(Vector3.up * 10, ForceMode.Impulse); //회전
    }

    // 일반 함수 : Use() 메인 루틴 -> Swing() 서브 루틴 -> Use() 메인 루틴 (교차 실행)
    // 코루틴 : Use() 메인 루틴 + Swing() 코루틴 (동시 실행)
}
