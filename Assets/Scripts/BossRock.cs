using System.Collections;
using UnityEngine;

public class BossRock : Bullet
{
    Rigidbody rigid;
    float angularPower = 2;  // 초기 회전력 값
    float scaleValue = 0.1f; // 초기 스케일 값
    bool isShoot;           // 발사 여부 플래그

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }

    // 돌이 발사될 시간
    IEnumerator GainPowerTimer()
    {
        yield return new WaitForSeconds(2.2f);
        isShoot = true;
    }


    // 돌이 발사되기 전까지 힘을 모아주는 코루틴, 회전력으로 이동
    IEnumerator GainPower()
    {
        while (!isShoot)
        {
            angularPower += 0.02f;
            scaleValue += 0.001f;
            transform.localScale = Vector3.one * scaleValue;
            rigid.AddTorque(transform.right * angularPower, ForceMode.Acceleration);  // 회전력 적용
            yield return null;
        }
    }
}