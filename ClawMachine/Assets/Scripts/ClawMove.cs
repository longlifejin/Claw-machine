using UnityEngine;

public class ClawMove : MonoBehaviour
{
    [SerializeField] Transform anchor; //케이블 상단 고정점
    [SerializeField] Transform claw; // 실제 갈고리 모형

    [Header("Claw Movement")]
    [SerializeField] float moveSpeed = 0.3f;
    [SerializeField] float rangeX = 0.31f;
    [SerializeField] float rangeZ = 0.31f; 
    [SerializeField] bool invertX = true;
    [SerializeField] bool invertZ = true;

    [SerializeField] float cableLength = 1.5f;
    [SerializeField] float gravity = 9.8f;
    [SerializeField] float damping = 0.98f;
    [SerializeField] float tiltAmount = 30f;
    private Vector3 prevClawPos;   // 한 프레임 전 위치 (속도 추출용)
    private Vector2 centerXZ;
    private Quaternion clawBaseRotation;

    [Header("Grab - Legs")]
    [SerializeField] Transform[] legs;
    [SerializeField] float openAngle = 30f;   // 펼칠 때 X 각도 (양수)
    [SerializeField] float closeAngle = -25f; // 오므릴 때 X 각도 (음수)

    [Header("Grab - Motion")]
    [SerializeField] float dropDepth = 3f;     // 얼마나 내려갈지 (cableLength 증가량)
    [SerializeField] float dropSpeed = 2f;     // 내려가고 올라오는 속도
    [SerializeField] float legSpeed = 90f;     // 다리 펼침/오므림 속도 (도/초)
    private Quaternion[] legBaseRot;
    private enum GrabState { Idle, Opening, Dropping, Closing, Lifting }
    private GrabState grabState = GrabState.Idle; 

    private float baseCableLength;  // grab 시작 시점의 cableLength 기억
    private float currentLegAngle;  // 현재 다리 X 각도

    private float h;
    private float v;

    private void Awake()
    {
        centerXZ = new Vector2(anchor.localPosition.x, anchor.localPosition.z);

        claw.position = anchor.position + Vector3.down * cableLength;
        //clawY = claw.position.y;
        clawBaseRotation = claw.rotation;
        prevClawPos = claw.position;

        legBaseRot = new Quaternion[legs.Length];
        for (int i = 0; i < legs.Length; i++)
            legBaseRot[i] = legs[i].localRotation;

        baseCableLength = cableLength;
        currentLegAngle = 0f;
        SetLegAngle(0f);
    }
    void Update()
    {
        float dt = Time.deltaTime;

        //--- 이동 입력 ---
        //h = Input.GetAxisRaw("Horizontal");  // A/D
        //v = Input.GetAxisRaw("Vertical");    // W/S

        //if (Input.GetKeyDown(KeyCode.Space) && grabState == GrabState.Idle)
        //    grabState = GrabState.Opening;
        //-----------------


        Vector3 pos = anchor.localPosition;
        pos.x = Mathf.Clamp(pos.x + v * moveSpeed * Time.deltaTime,
            centerXZ.x - rangeX, centerXZ.x + rangeX);
        pos.z = Mathf.Clamp(pos.z - h * moveSpeed * Time.deltaTime,
            centerXZ.y - rangeZ, centerXZ.y + rangeZ);
        anchor.localPosition = pos;

        // --- 진자: claw 위치 흔들림 ---
        Vector3 velocity = (claw.position - prevClawPos) * damping;
        prevClawPos = claw.position;

        Vector3 next = claw.position + velocity + Vector3.down * gravity * dt * dt;

        // 거리 제약: anchor로부터 cableLength 떨어진 호 위로 (Y 고정 안 함 → 출렁임 살아남)
        Vector3 dir = (next - anchor.position).normalized;
        claw.position = anchor.position + dir * cableLength;

        // --- 기울기: 줄 방향으로 claw를 기울임 ---
        Vector3 down = Vector3.down;
        Quaternion tilt = Quaternion.FromToRotation(down, dir);

        // 기울기를 0~1로 약하게 보간한 뒤, 기본 회전 '위에' 얹음
        Quaternion tiltScaled = Quaternion.Slerp(Quaternion.identity, tilt, tiltAmount / 90f);
        claw.rotation = tiltScaled * clawBaseRotation;   // 기본 회전 유지하면서 기울기만 추가

        

        UpdateGrab(dt);
    }

    private void SetLegAngle(float angleX)
    {
        for (int i = 0; i < legs.Length; i++)
            legs[i].localRotation = legBaseRot[i] * Quaternion.Euler(angleX, 0f, 0f);
    }

    private void UpdateGrab(float dt)
    {
        switch (grabState)
        {
            case GrabState.Idle:
                break;

            case GrabState.Opening:
                currentLegAngle = Mathf.MoveTowards(currentLegAngle, openAngle, legSpeed * dt);
                SetLegAngle(currentLegAngle);
                if (Mathf.Approximately(currentLegAngle, openAngle))
                    grabState = GrabState.Dropping;
                break;

            case GrabState.Dropping:
                cableLength = Mathf.MoveTowards(cableLength, baseCableLength + dropDepth, dropSpeed * dt);
                if (Mathf.Approximately(cableLength, baseCableLength + dropDepth))
                    grabState = GrabState.Closing;
                break;

            case GrabState.Closing:
                currentLegAngle = Mathf.MoveTowards(currentLegAngle, closeAngle, legSpeed * dt);
                SetLegAngle(currentLegAngle);
                if (Mathf.Approximately(currentLegAngle, closeAngle))
                    grabState = GrabState.Lifting;
                break;

            case GrabState.Lifting:
                cableLength = Mathf.MoveTowards(cableLength, baseCableLength, dropSpeed * dt);
                if (Mathf.Approximately(cableLength, baseCableLength))
                    grabState = GrabState.Idle;
                break;
        }
    }

    public void MoveInput(Vector2 vec)
    {
        h = vec.x;
        v = vec.y;
        Debug.Log(vec);
    }

    public void Confirm()
    {
        if (grabState == GrabState.Idle)
           grabState = GrabState.Opening;
    }
}