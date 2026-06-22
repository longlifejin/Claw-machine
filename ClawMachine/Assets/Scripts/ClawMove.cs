using UnityEngine;

public class ClawMove : MonoBehaviour
{
    [SerializeField] Transform anchor; //케이블 상단 고정점
    [SerializeField] Transform claw; // 실제 갈고리 모형

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
    private float clawY;   // 고정할 Y 높이


    private void Awake()
    {
        centerXZ = new Vector2(anchor.localPosition.x, anchor.localPosition.z);

        claw.position = anchor.position + Vector3.down * cableLength;
        //clawY = claw.position.y;
        clawBaseRotation = claw.rotation;
        prevClawPos = claw.position;
    }
    void Update()
    {
        float dt = Time.deltaTime;

        float h = Input.GetAxisRaw("Horizontal");  // A/D
        float v = Input.GetAxisRaw("Vertical");    // W/S

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
    }
}